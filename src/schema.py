"""Column definitions and I/O helpers for the four core Parquet tables.

Every ingest script reads/writes through these tables so that new domains can
attach to existing entities without needing their own schema.
"""
from __future__ import annotations

import uuid
from pathlib import Path

import pandas as pd

DATA_DIR = Path(__file__).resolve().parent.parent / "data"
PROCESSED_DIR = DATA_DIR / "processed"
RAW_DIR = DATA_DIR / "raw"

ENTITIES_SCHEMA = {
    "entity_id": "string",
    "entity_type": "string",  # "person" | "organization"
    "canonical_name": "string",
    "notes": "string",
}

ENTITY_EXTERNAL_IDS_SCHEMA = {
    "entity_id": "string",
    "source": "string",  # e.g. "congress-legislators", "europarl", "wikidata", "fec"
    "id_type": "string",  # e.g. "bioguide", "wikidata_qid", "fec_id"
    "id_value": "string",
}

EDGES_SCHEMA = {
    "edge_id": "string",
    "source_entity_id": "string",
    "target_entity_id": "string",
    "relation_type": "string",
    "source_dataset": "string",
    "start_date": "string",  # ISO date, nullable
    "end_date": "string",  # ISO date, nullable
    "attributes_json": "string",  # JSON-encoded extra attributes, nullable
    "confidence": "float64",
}

MATCH_CANDIDATES_SCHEMA = {
    "candidate_id": "string",
    "entity_id": "string",  # candidate match target, nullable if no plausible match
    "raw_record_ref": "string",  # pointer to source file/row for traceability
    "source_dataset": "string",
    "confidence_score": "float64",
    "review_status": "string",  # "pending" | "confirmed" | "rejected"
}

TABLES = {
    "entities": ENTITIES_SCHEMA,
    "entity_external_ids": ENTITY_EXTERNAL_IDS_SCHEMA,
    "edges": EDGES_SCHEMA,
    "match_candidates": MATCH_CANDIDATES_SCHEMA,
}


def new_id() -> str:
    return str(uuid.uuid4())


def empty_table(name: str) -> pd.DataFrame:
    """Return an empty, correctly-typed DataFrame for the given table name."""
    schema = TABLES[name]
    return pd.DataFrame({col: pd.Series(dtype=dtype) for col, dtype in schema.items()})


def table_path(name: str) -> Path:
    return PROCESSED_DIR / f"{name}.parquet"


def load_table(name: str) -> pd.DataFrame:
    """Load a table, creating it (empty) on disk first if it doesn't exist yet."""
    path = table_path(name)
    if not path.exists():
        save_table(name, empty_table(name))
    return pd.read_parquet(path)


def save_table(name: str, df: pd.DataFrame) -> None:
    schema = TABLES[name]
    missing = set(schema) - set(df.columns)
    if missing:
        raise ValueError(f"{name}: DataFrame missing required columns {missing}")
    PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
    df = df[list(schema)].astype(schema)
    df.to_parquet(table_path(name), index=False)


def init_tables() -> None:
    """Create all four tables (empty) on disk if they don't already exist."""
    for name in TABLES:
        if not table_path(name).exists():
            save_table(name, empty_table(name))


if __name__ == "__main__":
    init_tables()
    print(f"Initialized tables in {PROCESSED_DIR}")
