"""Ingest US Congress members, the president/VP, committees, and committee
membership from the unitedstates/congress-legislators project (CC0).

Populates:
- entities: one row per legislator/president/VP (person) and per committee (organization)
- entity_external_ids: bioguide (primary key), wikidata page name, opensecrets, fec, govtrack
- edges: committee_member edges between legislators and committees

Source: https://github.com/unitedstates/congress-legislators
"""
from __future__ import annotations

import json
from pathlib import Path

import pandas as pd
import requests
import yaml

from src.schema import RAW_DIR, load_table, new_id, save_table

RAW_BASE = "https://raw.githubusercontent.com/unitedstates/congress-legislators/main"
SOURCE_NAME = "congress-legislators"
RAW_DIR_HERE = RAW_DIR / SOURCE_NAME

FILES = [
    "legislators-current.yaml",
    "legislators-historical.yaml",
    "executive.yaml",
    "committees-current.yaml",
    "committee-membership-current.yaml",
]

# id fields in the legislator/executive YAML that we carry into entity_external_ids
ID_FIELDS = ["bioguide", "thomas", "govtrack", "opensecrets", "votesmart", "cspan",
             "wikipedia", "ballotpedia", "icpsr", "house_history", "lis", "fec"]


def download_raw_files() -> None:
    RAW_DIR_HERE.mkdir(parents=True, exist_ok=True)
    for filename in FILES:
        dest = RAW_DIR_HERE / filename
        if dest.exists():
            continue
        resp = requests.get(f"{RAW_BASE}/{filename}", timeout=60)
        resp.raise_for_status()
        dest.write_bytes(resp.content)


def _person_name(name: dict) -> str:
    parts = [name.get("first"), name.get("middle"), name.get("last"), name.get("suffix")]
    return " ".join(p for p in parts if p)


def _add_person(entities_rows, ext_id_rows, name: str, ids: dict) -> str:
    entity_id = new_id()
    entities_rows.append({
        "entity_id": entity_id,
        "entity_type": "person",
        "canonical_name": name,
        "notes": "",
    })
    for id_type in ID_FIELDS:
        value = ids.get(id_type)
        if value is None:
            continue
        # fec is a list of committee ids; everything else is a scalar
        values = value if isinstance(value, list) else [value]
        for v in values:
            ext_id_rows.append({
                "entity_id": entity_id,
                "source": SOURCE_NAME,
                "id_type": id_type,
                "id_value": str(v),
            })
    return entity_id


def ingest_legislators(entities_rows, ext_id_rows, bioguide_to_entity: dict) -> None:
    for filename in ["legislators-current.yaml", "legislators-historical.yaml"]:
        records = yaml.safe_load((RAW_DIR_HERE / filename).read_text())
        for record in records:
            ids = record.get("id", {})
            bioguide = ids.get("bioguide")
            if not bioguide or bioguide in bioguide_to_entity:
                continue  # skip records with no stable key or already seen
            name = _person_name(record.get("name", {}))
            entity_id = _add_person(entities_rows, ext_id_rows, name, ids)
            bioguide_to_entity[bioguide] = entity_id


def ingest_executive(entities_rows, ext_id_rows) -> None:
    records = yaml.safe_load((RAW_DIR_HERE / "executive.yaml").read_text())
    for record in records:
        ids = record.get("id", {})
        name = _person_name(record.get("name", {}))
        # presidents/VPs who also served in Congress are already added via bioguide;
        # only add here if we don't already have a bioguide-keyed entity for them
        if ids.get("bioguide"):
            continue
        _add_person(entities_rows, ext_id_rows, name, ids)


def ingest_committees(entities_rows, ext_id_rows) -> dict:
    committees = yaml.safe_load((RAW_DIR_HERE / "committees-current.yaml").read_text())
    thomas_to_entity = {}
    for committee in committees:
        entity_id = new_id()
        entities_rows.append({
            "entity_id": entity_id,
            "entity_type": "organization",
            "canonical_name": committee["name"],
            "notes": committee.get("type", ""),
        })
        thomas_id = committee.get("thomas_id")
        if thomas_id:
            thomas_to_entity[thomas_id] = entity_id
            ext_id_rows.append({
                "entity_id": entity_id,
                "source": SOURCE_NAME,
                "id_type": "thomas_committee_id",
                "id_value": thomas_id,
            })
        for sub in committee.get("subcommittees", []):
            sub_entity_id = new_id()
            entities_rows.append({
                "entity_id": sub_entity_id,
                "entity_type": "organization",
                "canonical_name": f"{committee['name']} - {sub['name']}",
                "notes": "subcommittee",
            })
            sub_thomas_id = f"{thomas_id}{sub['thomas_id']}"
            thomas_to_entity[sub_thomas_id] = sub_entity_id
            ext_id_rows.append({
                "entity_id": sub_entity_id,
                "source": SOURCE_NAME,
                "id_type": "thomas_committee_id",
                "id_value": sub_thomas_id,
            })
    return thomas_to_entity


def ingest_committee_membership(edge_rows, bioguide_to_entity: dict, thomas_to_entity: dict) -> None:
    membership = yaml.safe_load((RAW_DIR_HERE / "committee-membership-current.yaml").read_text())
    for committee_id, members in membership.items():
        target_id = thomas_to_entity.get(committee_id)
        if not target_id:
            continue
        for member in members:
            source_id = bioguide_to_entity.get(member.get("bioguide"))
            if not source_id:
                continue
            edge_rows.append({
                "edge_id": new_id(),
                "source_entity_id": source_id,
                "target_entity_id": target_id,
                "relation_type": "committee_member",
                "source_dataset": SOURCE_NAME,
                "start_date": "",
                "end_date": "",
                "attributes_json": json.dumps({"title": member.get("title", ""), "party": member.get("party", "")}),
                "confidence": 1.0,
            })


def run() -> None:
    download_raw_files()

    entities_rows: list[dict] = []
    ext_id_rows: list[dict] = []
    edge_rows: list[dict] = []
    bioguide_to_entity: dict = {}

    ingest_legislators(entities_rows, ext_id_rows, bioguide_to_entity)
    ingest_executive(entities_rows, ext_id_rows)
    thomas_to_entity = ingest_committees(entities_rows, ext_id_rows)
    ingest_committee_membership(edge_rows, bioguide_to_entity, thomas_to_entity)

    entities = load_table("entities")
    ext_ids = load_table("entity_external_ids")
    edges = load_table("edges")

    entities = pd.concat([entities, pd.DataFrame(entities_rows)], ignore_index=True)
    ext_ids = pd.concat([ext_ids, pd.DataFrame(ext_id_rows)], ignore_index=True)
    edges = pd.concat([edges, pd.DataFrame(edge_rows)], ignore_index=True)

    save_table("entities", entities)
    save_table("entity_external_ids", ext_ids)
    save_table("edges", edges)

    print(f"Added {len(entities_rows)} entities, {len(ext_id_rows)} external ids, "
          f"{len(edge_rows)} committee-membership edges.")


if __name__ == "__main__":
    run()
