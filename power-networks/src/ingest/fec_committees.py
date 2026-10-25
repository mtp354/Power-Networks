"""Ingest FEC candidate master, committee master, and candidate-committee
linkage bulk files (Phase 2a, part 1 — committee/candidate network).

The much larger individual-contributions file is deliberately not parsed yet
(see docs/ROADMAP.md): this script wires up the committee/candidate graph and
cross-references candidates against congress-legislators via the FEC id
already captured in Phase 1, which is the load-bearing structure that future
donation edges will attach to.

Populates:
- entities: one row per FEC committee (organization); one row per FEC
  candidate not already known from congress-legislators (person)
- entity_external_ids: fec_committee_id, fec_cand_id
- edges: candidate_committee_linkage between candidates and their committees

Source: https://www.fec.gov/data/browse-data/?tab=bulk-data
"""
from __future__ import annotations

import zipfile
from pathlib import Path

import pandas as pd

from src.schema import RAW_DIR, load_table, new_id, purge_source, save_table

SOURCE_NAME = "fec"
RAW_DIR_HERE = RAW_DIR / SOURCE_NAME
CYCLE_FOLDER = "2026"
CYCLE_SUFFIX = "26"  # matches the downloaded 2026 cycle filenames (cm26.zip, cn26.zip, ccl26.zip)

CM_COLUMNS = ["CMTE_ID", "CMTE_NM", "TRES_NM", "CMTE_ST1", "CMTE_ST2", "CMTE_CITY",
              "CMTE_ST", "CMTE_ZIP", "CMTE_DSGN", "CMTE_TP", "CMTE_PTY_AFFILIATION",
              "CMTE_FILING_FREQ", "ORG_TP", "CONNECTED_ORG_NM", "CAND_ID"]
CN_COLUMNS = ["CAND_ID", "CAND_NAME", "CAND_PTY_AFFILIATION", "CAND_ELECTION_YR",
              "CAND_OFFICE_ST", "CAND_OFFICE", "CAND_OFFICE_DISTRICT", "CAND_ICI",
              "CAND_STATUS", "CAND_PCC", "CAND_ST1", "CAND_ST2", "CAND_CITY",
              "CAND_ST", "CAND_ZIP"]
CCL_COLUMNS = ["CAND_ID", "CAND_ELECTION_YR", "FEC_ELECTION_YR", "CMTE_ID",
               "CMTE_TP", "CMTE_DSGN", "LINKAGE_ID"]


def _read_pipe_delimited(zip_path: Path, member: str, columns: list[str]) -> pd.DataFrame:
    with zipfile.ZipFile(zip_path) as zf:
        with zf.open(member) as f:
            return pd.read_csv(f, sep="|", names=columns, header=None, encoding="latin-1", dtype=str)


def load_candidate_fec_id_index() -> dict:
    """Map FEC candidate id -> existing entity_id, for candidates already known
    from congress-legislators (captured there as id_type 'fec')."""
    ext_ids = load_table("entity_external_ids")
    fec_rows = ext_ids[ext_ids["id_type"] == "fec"]
    return dict(zip(fec_rows["id_value"], fec_rows["entity_id"]))


def run() -> None:
    purge_source(SOURCE_NAME)  # full refresh: safe to re-run without duplicating rows

    cand_entity_by_fec_id = load_candidate_fec_id_index()
    entities_rows: list[dict] = []
    ext_id_rows: list[dict] = []
    edge_rows: list[dict] = []
    committee_entity_by_id: dict[str, str] = {}

    candidates = _read_pipe_delimited(RAW_DIR_HERE / CYCLE_FOLDER / f"cn{CYCLE_SUFFIX}.zip", "cn.txt", CN_COLUMNS)
    committees = _read_pipe_delimited(RAW_DIR_HERE / CYCLE_FOLDER / f"cm{CYCLE_SUFFIX}.zip", "cm.txt", CM_COLUMNS)
    linkages = _read_pipe_delimited(RAW_DIR_HERE / CYCLE_FOLDER / f"ccl{CYCLE_SUFFIX}.zip", "ccl.txt", CCL_COLUMNS)

    matched_existing = 0
    for row in candidates.itertuples(index=False):
        entity_id = cand_entity_by_fec_id.get(row.CAND_ID)
        if entity_id:
            matched_existing += 1
        else:
            entity_id = new_id()
            cand_entity_by_fec_id[row.CAND_ID] = entity_id
            entities_rows.append({
                "entity_id": entity_id,
                "entity_type": "person",
                "canonical_name": row.CAND_NAME or row.CAND_ID,
                "notes": f"FEC candidate: office={row.CAND_OFFICE}, state={row.CAND_OFFICE_ST}, party={row.CAND_PTY_AFFILIATION}",
            })
        ext_id_rows.append({
            "entity_id": entity_id,
            "source": SOURCE_NAME,
            "id_type": "fec_cand_id",
            "id_value": row.CAND_ID,
        })

    for row in committees.itertuples(index=False):
        entity_id = new_id()
        committee_entity_by_id[row.CMTE_ID] = entity_id
        entities_rows.append({
            "entity_id": entity_id,
            "entity_type": "organization",
            "canonical_name": row.CMTE_NM or row.CMTE_ID,
            "notes": f"FEC committee: type={row.CMTE_TP}, designation={row.CMTE_DSGN}, party={row.CMTE_PTY_AFFILIATION}",
        })
        ext_id_rows.append({
            "entity_id": entity_id,
            "source": SOURCE_NAME,
            "id_type": "fec_committee_id",
            "id_value": row.CMTE_ID,
        })

    skipped_linkages = 0
    for row in linkages.itertuples(index=False):
        cand_entity_id = cand_entity_by_fec_id.get(row.CAND_ID)
        cmte_entity_id = committee_entity_by_id.get(row.CMTE_ID)
        if not cand_entity_id or not cmte_entity_id:
            skipped_linkages += 1
            continue
        edge_rows.append({
            "edge_id": new_id(),
            "source_entity_id": cand_entity_id,
            "target_entity_id": cmte_entity_id,
            "relation_type": "candidate_committee_linkage",
            "source_dataset": SOURCE_NAME,
            "start_date": "",
            "end_date": "",
            "attributes_json": f'{{"election_year": "{row.FEC_ELECTION_YR}"}}',
            "confidence": 1.0,
        })

    entities = load_table("entities")
    ext_ids = load_table("entity_external_ids")
    edges = load_table("edges")

    entities = pd.concat([entities, pd.DataFrame(entities_rows)], ignore_index=True)
    ext_ids = pd.concat([ext_ids, pd.DataFrame(ext_id_rows)], ignore_index=True)
    edges = pd.concat([edges, pd.DataFrame(edge_rows)], ignore_index=True)

    save_table("entities", entities)
    save_table("entity_external_ids", ext_ids)
    save_table("edges", edges)

    print(f"Candidates: {len(candidates)} ({matched_existing} matched to existing entities via FEC id). "
          f"Committees: {len(committees)}. Linkage edges: {len(edge_rows)} ({skipped_linkages} skipped, missing side).")


if __name__ == "__main__":
    run()
