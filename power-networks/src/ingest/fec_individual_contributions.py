"""Ingest FEC individual contributions (Phase 2a, part 2 — donation edges).

The bulk file is tens of millions of rows and the vast majority of contributors
are private citizens who are out of scope: this script only creates donation
edges for contributors who fuzzy-match an already-known official (someone
already in the graph from congress-legislators or as an FEC candidate). It
never creates new person entities for contributors, and it never silently
merges a low-confidence match — those go to `match_candidates` instead.

Populates:
- edges: donated_to (person -> committee) for high-confidence matches
- match_candidates: moderate-confidence matches for manual review

Source: https://www.fec.gov/data/browse-data/?tab=bulk-data (indiv{cycle}.zip)
"""
from __future__ import annotations

import heapq
import json
import zipfile
from pathlib import Path

import pandas as pd

from src.resolve.matching import blocking_key, name_similarity, normalize_name
from src.schema import RAW_DIR, load_table, new_id, save_table, table_path

SOURCE_NAME = "fec_indiv"
RAW_DIR_HERE = RAW_DIR / "fec"
CYCLE_FOLDER = "2026"
CYCLE_SUFFIX = "26"

INDIV_COLUMNS = ["CMTE_ID", "AMNDT_IND", "RPT_TP", "TRANSACTION_PGI", "IMAGE_NUM",
                 "TRANSACTION_TP", "ENTITY_TP", "NAME", "CITY", "STATE", "ZIP_CODE",
                 "EMPLOYER", "OCCUPATION", "TRANSACTION_DT", "TRANSACTION_AMT",
                 "OTHER_ID", "TRAN_ID", "FILE_NUM", "MEMO_CD", "MEMO_TEXT", "SUB_ID"]

CHUNK_SIZE = 500_000
AUTO_ACCEPT_THRESHOLD = 92.0
REVIEW_THRESHOLD = 85.0
TOP_K_REVIEW_PER_OFFICIAL = 5  # common surnames can otherwise flood the review queue
OFFICIAL_SOURCES = {"congress-legislators", "fec"}


def build_official_blocking_index() -> dict[str, list[tuple[str, str]]]:
    """Map last-name blocking key -> [(entity_id, normalized_full_name), ...]
    for every known official (congress-legislators + FEC candidates)."""
    entities = load_table("entities")
    ext_ids = load_table("entity_external_ids")

    official_entity_ids = set(ext_ids.loc[ext_ids["source"].isin(OFFICIAL_SOURCES), "entity_id"])
    officials = entities[(entities["entity_type"] == "person") & (entities["entity_id"].isin(official_entity_ids))]

    index: dict[str, list[tuple[str, str]]] = {}
    for row in officials.itertuples(index=False):
        normalized = normalize_name(row.canonical_name)
        if not normalized:
            continue
        key = normalized.split()[-1]
        index.setdefault(key, []).append((row.entity_id, row.canonical_name))
    return index


def build_committee_index() -> dict[str, str]:
    ext_ids = load_table("entity_external_ids")
    rows = ext_ids[ext_ids["id_type"] == "fec_committee_id"]
    return dict(zip(rows["id_value"], rows["entity_id"]))


def _purge_previous_run() -> None:
    edges = load_table("edges")
    edges = edges[edges["source_dataset"] != SOURCE_NAME]
    save_table("edges", edges)

    match_candidates = load_table("match_candidates")
    match_candidates = match_candidates[match_candidates["source_dataset"] != SOURCE_NAME]
    save_table("match_candidates", match_candidates)


def run() -> None:
    _purge_previous_run()  # full refresh: safe to re-run without duplicating rows

    official_index = build_official_blocking_index()
    committee_index = build_committee_index()

    edge_rows: list[dict] = []
    seen_edges: set[tuple[str, str]] = set()
    # bounded min-heap per official: keeps only the top-K review candidates so a
    # common surname can't flood the review queue with thousands of unrelated hits
    review_heaps: dict[str, list[tuple[float, str, dict]]] = {}

    zip_path = RAW_DIR_HERE / CYCLE_FOLDER / f"indiv{CYCLE_SUFFIX}.zip"
    total_rows = 0
    matched_high = 0
    with zipfile.ZipFile(zip_path) as zf:
        with zf.open("itcont.txt") as f:
            reader = pd.read_csv(
                f, sep="|", names=INDIV_COLUMNS, header=None,
                encoding="latin-1", dtype=str, chunksize=CHUNK_SIZE,
                quoting=3, on_bad_lines="skip",
            )
            for chunk in reader:
                total_rows += len(chunk)
                individuals = chunk[chunk["ENTITY_TP"] == "IND"]
                for row in individuals.itertuples(index=False):
                    if pd.isna(row.NAME) or not row.NAME:
                        continue
                    committee_entity_id = committee_index.get(row.CMTE_ID)
                    if not committee_entity_id:
                        continue

                    key = blocking_key(row.NAME)
                    candidates = official_index.get(key)
                    if not candidates:
                        continue

                    best_score = 0.0
                    best_entity_id = None
                    for entity_id, official_name in candidates:
                        score = name_similarity(row.NAME, official_name)
                        if score > best_score:
                            best_score = score
                            best_entity_id = entity_id

                    if best_score >= AUTO_ACCEPT_THRESHOLD:
                        edge_key = (best_entity_id, committee_entity_id)
                        if edge_key in seen_edges:
                            continue
                        seen_edges.add(edge_key)
                        edge_rows.append({
                            "edge_id": new_id(),
                            "source_entity_id": best_entity_id,
                            "target_entity_id": committee_entity_id,
                            "relation_type": "donated_to",
                            "source_dataset": SOURCE_NAME,
                            "start_date": row.TRANSACTION_DT or "",
                            "end_date": "",
                            "attributes_json": json.dumps({
                                "amount": row.TRANSACTION_AMT,
                                "employer": row.EMPLOYER,
                                "occupation": row.OCCUPATION,
                                "match_score": best_score,
                            }),
                            "confidence": round(best_score / 100, 4),
                        })
                        matched_high += 1
                    elif best_score >= REVIEW_THRESHOLD:
                        heap = review_heaps.setdefault(best_entity_id, [])
                        record_ref = f"fec_indiv:{CYCLE_FOLDER}:{row.SUB_ID}"
                        if len(heap) < TOP_K_REVIEW_PER_OFFICIAL:
                            heapq.heappush(heap, (best_score, record_ref, {}))
                        elif best_score > heap[0][0]:
                            heapq.heapreplace(heap, (best_score, record_ref, {}))

    match_candidate_rows = [
        {
            "candidate_id": new_id(),
            "entity_id": entity_id,
            "raw_record_ref": record_ref,
            "source_dataset": SOURCE_NAME,
            "confidence_score": round(score / 100, 4),
            "review_status": "pending",
        }
        for entity_id, heap in review_heaps.items()
        for score, record_ref, _ in heap
    ]

    edges = load_table("edges")
    match_candidates = load_table("match_candidates")
    edges = pd.concat([edges, pd.DataFrame(edge_rows)], ignore_index=True)
    match_candidates = pd.concat([match_candidates, pd.DataFrame(match_candidate_rows)], ignore_index=True)
    save_table("edges", edges)
    save_table("match_candidates", match_candidates)

    print(f"Processed {total_rows} contribution rows. Auto-accepted {matched_high} donation edges "
          f"(score >= {AUTO_ACCEPT_THRESHOLD}). Sent {len(match_candidate_rows)} to match_candidates for "
          f"review (top {TOP_K_REVIEW_PER_OFFICIAL} per official, score {REVIEW_THRESHOLD}-{AUTO_ACCEPT_THRESHOLD}).")


if __name__ == "__main__":
    run()
