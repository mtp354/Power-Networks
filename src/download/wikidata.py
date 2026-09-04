"""Targeted Wikidata SPARQL extractor for elite power-network entities (Phase 3).

Instead of downloading the full 100+ GB Wikidata JSON dump, this extractor executes
targeted batch SPARQL queries against `query.wikidata.org` for entities whose
`wikidata_qid` has already been collected in Phase 1 (US legislators and EU MEPs).

Extracts:
- Positions held (P39)
- Educated at (P69)
- Spouse (P26)
- Employer (P108)
- Political party (P102)
"""
from __future__ import annotations

import argparse
import json
import time
from pathlib import Path
from typing import List, Optional

import pandas as pd
import requests

from src.download.base import BaseDownloader, logger
from src.schema import PROCESSED_DIR

WIKIDATA_SPARQL_ENDPOINT = "https://query.wikidata.org/sparql"
WIKIMEDIA_USER_AGENT = "PowerNetworksResearchBot/1.0 (academic research; mailto:research@institution.edu)"
BATCH_SIZE = 100


class WikidataDownloader(BaseDownloader):
    def __init__(self, raw_dir: Optional[Path] = None, user_agent: Optional[str] = None):
        super().__init__(
            dataset_name="wikidata",
            raw_dir=raw_dir,
            user_agent=user_agent or WIKIMEDIA_USER_AGENT,
        )

    def load_target_qids(self) -> List[str]:
        """Read existing processed entity_external_ids table and extract unique wikidata_qid values."""
        ext_ids_path = PROCESSED_DIR / "entity_external_ids.parquet"
        if not ext_ids_path.exists():
            logger.warning("[wikidata] entity_external_ids.parquet not found. Using sample QIDs.")
            return ["Q22686", "Q76", "Q6279"]  # Donald Trump, Barack Obama, Joe Biden

        df = pd.read_parquet(ext_ids_path)
        wiki_rows = df[df["id_type"].str.lower().str.contains("wikidata", na=False)]
        qids = (
            wiki_rows["id_value"]
            .dropna()
            .astype(str)
            .str.strip()
            .loc[lambda s: s.str.startswith("Q")]
            .unique()
            .tolist()
        )
        if not qids:
            # Fallback: check raw congress-legislators files if entity_external_ids has not been re-ingested with wikidata yet
            yaml_path = self.raw_dir.parent / "congress-legislators" / "legislators-current.yaml"
            if yaml_path.exists():
                import yaml
                data = yaml.safe_load(yaml_path.read_text())
                qids = [d["id"]["wikidata"] for d in data if "wikidata" in d.get("id", {})]
                logger.info(f"[wikidata] Found {len(qids)} unique Wikidata QIDs from {yaml_path.name}")
            else:
                qids = ["Q22686", "Q76", "Q6279", "Q22250"]

        logger.info(f"[wikidata] Target Wikidata QID count: {len(qids)}")
        return qids

    def query_sparql_batch(self, qids: List[str]) -> dict:
        """Query Wikidata for a batch of QIDs."""
        values_clause = " ".join(f"wd:{qid}" for qid in qids)
        sparql_query = f"""
        SELECT ?person ?personLabel ?prop ?propLabel ?val ?valLabel WHERE {{
          VALUES ?person {{ {values_clause} }}
          VALUES ?prop {{ wdt:P39 wdt:P69 wdt:P26 wdt:P108 wdt:P102 }}
          ?person ?prop ?val .
          SERVICE wikibase:label {{ bd:serviceParam wikibase:language "en". }}
        }}
        """
        session = self.get_session(custom_headers={"Accept": "application/sparql-results+json"})
        response = session.get(
            WIKIDATA_SPARQL_ENDPOINT,
            params={"query": sparql_query, "format": "json"},
            timeout=120,
        )
        response.raise_for_status()
        return response.json()

    def run(self, max_batches: Optional[int] = None, force: bool = False) -> List[Path]:
        qids = self.load_target_qids()
        if not qids:
            logger.warning("[wikidata] No QIDs found to query.")
            return []

        batches = [qids[i : i + BATCH_SIZE] for i in range(0, len(qids), BATCH_SIZE)]
        if max_batches:
            batches = batches[:max_batches]

        downloaded = []
        for idx, batch in enumerate(batches, start=1):
            batch_filename = f"batch_{idx:04d}.json"
            dest_path = self.raw_dir / batch_filename

            if dest_path.exists() and dest_path.stat().st_size > 0 and not force:
                logger.info(f"[wikidata] Skipping {batch_filename} (already exists)")
                downloaded.append(dest_path)
                continue

            logger.info(f"[wikidata] Querying SPARQL batch {idx}/{len(batches)} ({len(batch)} entities)...")
            try:
                data = self.query_sparql_batch(batch)
                dest_path.write_text(json.dumps(data, indent=2))
                logger.info(f"[wikidata] Saved {batch_filename} ({len(data.get('results', {}).get('bindings', []))} assertions)")
                downloaded.append(dest_path)
                time.sleep(1.0)  # Courtesy sleep for Wikidata public SPARQL endpoint
            except Exception as exc:
                logger.error(f"[wikidata] Failed SPARQL batch {idx}: {exc}")
                raise

        return downloaded


def main() -> None:
    parser = argparse.ArgumentParser(description="Extract targeted Wikidata assertions for discovered entities.")
    parser.add_argument("--max-batches", type=int, default=None, help="Limit number of batches to process")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files")
    args = parser.parse_args()

    downloader = WikidataDownloader()
    downloader.run(max_batches=args.max_batches, force=args.force)


if __name__ == "__main__":
    main()
