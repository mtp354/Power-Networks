"""Ingest targeted Wikidata SPARQL assertions (Phase 3) for entities already
discovered in Phase 1 (currently: US congress-legislators, which carry a
`wikidata` external id; EU MEPs are not yet linked to Wikidata QIDs).

Reads the batch files downloaded by `src.download.wikidata.WikidataDownloader`
and adds typed edges without duplicating existing entities: the source person
is resolved via the existing `wikidata` external id, and each distinct target
QID (position, school, employer, party, spouse) is created once and reused
across assertions/batches.

Source: https://query.wikidata.org/ (targeted SPARQL, not the full dump)
"""
from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from src.schema import RAW_DIR, load_table, new_id, purge_source, save_table

SOURCE_NAME = "wikidata"
BATCH_DIR = RAW_DIR / "wikidata"
ENTITY_URI_PREFIX = "http://www.wikidata.org/entity/"

# Wikidata property -> (relation_type, target entity_type)
PROP_MAP = {
    "P39": ("position_held", "organization"),
    "P69": ("educated_at", "organization"),
    "P108": ("employed_by", "organization"),
    "P102": ("member_of", "organization"),
    "P26": ("spouse", "person"),
}


def _qid(uri: str) -> str:
    return uri.rsplit("/", 1)[-1]


def load_person_qid_index() -> dict:
    """Map wikidata QID -> existing entity_id for people already discovered in Phase 1."""
    ext_ids = load_table("entity_external_ids")
    wiki_rows = ext_ids[ext_ids["id_type"] == "wikidata"]
    return dict(zip(wiki_rows["id_value"], wiki_rows["entity_id"]))


def run() -> None:
    purge_source(SOURCE_NAME)  # full refresh: safe to re-run without duplicating rows

    qid_to_entity = load_person_qid_index()
    entities_rows: list[dict] = []
    ext_id_rows: list[dict] = []
    edge_rows: list[dict] = []
    seen_edges: set[tuple[str, str, str]] = set()

    def get_or_create_target(qid: str, label: str, entity_type: str) -> str:
        if qid in qid_to_entity:
            return qid_to_entity[qid]
        entity_id = new_id()
        qid_to_entity[qid] = entity_id
        entities_rows.append({
            "entity_id": entity_id,
            "entity_type": entity_type,
            "canonical_name": label or qid,
            "notes": "",
        })
        ext_id_rows.append({
            "entity_id": entity_id,
            "source": SOURCE_NAME,
            "id_type": "wikidata_qid",
            "id_value": qid,
        })
        return entity_id

    batch_files = sorted(BATCH_DIR.glob("batch_*.json"))
    assertions = 0
    skipped_unknown_person = 0
    for batch_file in batch_files:
        data = json.loads(batch_file.read_text())
        for binding in data.get("results", {}).get("bindings", []):
            prop_id = _qid(binding["prop"]["value"])
            if prop_id not in PROP_MAP:
                continue
            if binding["val"]["type"] != "uri":
                continue

            person_qid = _qid(binding["person"]["value"])
            source_entity_id = qid_to_entity.get(person_qid)
            if not source_entity_id:
                skipped_unknown_person += 1
                continue

            relation_type, target_entity_type = PROP_MAP[prop_id]
            val_qid = _qid(binding["val"]["value"])
            val_label = binding.get("valLabel", {}).get("value", "")
            target_entity_id = get_or_create_target(val_qid, val_label, target_entity_type)

            edge_key = (source_entity_id, target_entity_id, relation_type)
            if edge_key in seen_edges:
                continue
            seen_edges.add(edge_key)

            edge_rows.append({
                "edge_id": new_id(),
                "source_entity_id": source_entity_id,
                "target_entity_id": target_entity_id,
                "relation_type": relation_type,
                "source_dataset": SOURCE_NAME,
                "start_date": "",
                "end_date": "",
                "attributes_json": json.dumps({"wikidata_property": prop_id}),
                "confidence": 1.0,
            })
            assertions += 1

    entities = load_table("entities")
    ext_ids = load_table("entity_external_ids")
    edges = load_table("edges")

    entities = pd.concat([entities, pd.DataFrame(entities_rows)], ignore_index=True)
    ext_ids = pd.concat([ext_ids, pd.DataFrame(ext_id_rows)], ignore_index=True)
    edges = pd.concat([edges, pd.DataFrame(edge_rows)], ignore_index=True)

    save_table("entities", entities)
    save_table("entity_external_ids", ext_ids)
    save_table("edges", edges)

    print(f"Processed {len(batch_files)} batches. Added {len(entities_rows)} new target "
          f"entities, {len(edge_rows)} edges. Skipped {skipped_unknown_person} assertions "
          f"for persons not found via existing wikidata external id.")


if __name__ == "__main__":
    run()
