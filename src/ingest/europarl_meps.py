"""Ingest current-term (10th parliamentary term) Members of the European
Parliament from the EP Open Data Portal.

Populates:
- entities: one row per MEP (person) and per political group/national party/committee (organization)
- entity_external_ids: europarl person id, citizenship (ISO3 country code)
- edges: member_of (political group, national party, parliament) and committee_member edges

Source: https://data.europarl.europa.eu/en/developer-corner/opendata-api
"""
from __future__ import annotations

import json
import time
from pathlib import Path

import pandas as pd
import requests

from src.schema import RAW_DIR, load_table, new_id, save_table

API_BASE = "https://data.europarl.europa.eu/api/v2"
SOURCE_NAME = "europarl"
RAW_DIR_HERE = RAW_DIR / SOURCE_NAME
MEPS_DIR = RAW_DIR_HERE / "meps"
ORGS_DIR = RAW_DIR_HERE / "orgs"
PARLIAMENTARY_TERM = 10  # 2024-2029

# membership classifications we turn into edges (delegations are out of scope for now)
RELATION_BY_CLASSIFICATION = {
    "def/ep-entities/EU_POLITICAL_GROUP": "member_of",
    "def/ep-entities/NATIONAL_POLITICAL_GROUP": "member_of",
    "def/ep-entities/COMMITTEE_PARLIAMENTARY_STANDING": "committee_member",
}


def _get_json(url: str, params: dict | None = None) -> dict:
    headers = {
        "Accept": "application/ld+json",
        "User-Agent": "Mozilla/5.0 (research data pipeline; academic use)",
    }
    for attempt in range(8):
        resp = requests.get(url, params=params, headers=headers, timeout=60)
        if resp.status_code == 429 or not resp.content:
            time.sleep(min(60, 5 * (attempt + 1)))
            continue
        resp.raise_for_status()
        try:
            data = resp.json()
        except ValueError:
            time.sleep(min(60, 5 * (attempt + 1)))
            continue
        time.sleep(1.0)  # be polite to the public API
        return data
    resp.raise_for_status()
    return resp.json()


def fetch_current_term_person_ids() -> list[str]:
    MEPS_DIR.mkdir(parents=True, exist_ok=True)
    list_path = RAW_DIR_HERE / "meps-term-10.json"
    if not list_path.exists():
        data = _get_json(f"{API_BASE}/meps", {"parliamentary-term": PARLIAMENTARY_TERM, "format": "application/ld+json"})
        list_path.write_text(json.dumps(data))
    else:
        data = json.loads(list_path.read_text())
    return [record["identifier"] for record in data["data"]]


def fetch_mep_detail(person_id: str) -> dict:
    cache_path = MEPS_DIR / f"{person_id}.json"
    if cache_path.exists():
        return json.loads(cache_path.read_text())["data"][0]
    data = _get_json(f"{API_BASE}/meps/{person_id}", {"format": "application/ld+json"})
    cache_path.write_text(json.dumps(data))
    return data["data"][0]


def fetch_org_label(org_id: str) -> str:
    org_key = org_id.split("/")[-1]
    cache_path = ORGS_DIR / f"{org_key}.json"
    ORGS_DIR.mkdir(parents=True, exist_ok=True)
    if cache_path.exists():
        data = json.loads(cache_path.read_text())
    else:
        data = _get_json(f"{API_BASE}/corporate-bodies/{org_key}", {"format": "application/ld+json"})
        cache_path.write_text(json.dumps(data))
    record = data["data"][0]
    return record.get("label") or record.get("isVersionOf", org_key)


def run() -> None:
    person_ids = fetch_current_term_person_ids()

    entities_rows: list[dict] = []
    ext_id_rows: list[dict] = []
    edge_rows: list[dict] = []
    org_entity_by_id: dict[str, str] = {}  # europarl org id -> our entity_id

    def get_or_create_org(org_id: str) -> str:
        if org_id in org_entity_by_id:
            return org_entity_by_id[org_id]
        entity_id = new_id()
        org_entity_by_id[org_id] = entity_id
        entities_rows.append({
            "entity_id": entity_id,
            "entity_type": "organization",
            "canonical_name": fetch_org_label(org_id),
            "notes": "",
        })
        ext_id_rows.append({
            "entity_id": entity_id,
            "source": SOURCE_NAME,
            "id_type": "europarl_org_id",
            "id_value": org_id.split("/")[-1],
        })
        return entity_id

    for person_id in person_ids:
        record = fetch_mep_detail(person_id)
        entity_id = new_id()
        entities_rows.append({
            "entity_id": entity_id,
            "entity_type": "person",
            "canonical_name": record.get("label", ""),
            "notes": "",
        })
        ext_id_rows.append({
            "entity_id": entity_id,
            "source": SOURCE_NAME,
            "id_type": "europarl_person_id",
            "id_value": person_id,
        })
        citizenship = record.get("citizenship")
        if citizenship:
            ext_id_rows.append({
                "entity_id": entity_id,
                "source": SOURCE_NAME,
                "id_type": "citizenship_country",
                "id_value": citizenship.rsplit("/", 1)[-1],
            })

        for membership in record.get("hasMembership", []):
            classification = membership.get("membershipClassification")
            relation_type = RELATION_BY_CLASSIFICATION.get(classification)
            if not relation_type:
                continue
            org_id = membership.get("organization")
            if not org_id:
                continue
            target_id = get_or_create_org(org_id)
            period = membership.get("memberDuring", {})
            edge_rows.append({
                "edge_id": new_id(),
                "source_entity_id": entity_id,
                "target_entity_id": target_id,
                "relation_type": relation_type,
                "source_dataset": SOURCE_NAME,
                "start_date": period.get("startDate", ""),
                "end_date": period.get("endDate", ""),
                "attributes_json": json.dumps({"role": membership.get("role", "")}),
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

    print(f"Added {len(entities_rows)} entities ({len(person_ids)} MEPs + "
          f"{len(org_entity_by_id)} orgs), {len(ext_id_rows)} external ids, "
          f"{len(edge_rows)} edges.")


if __name__ == "__main__":
    run()
