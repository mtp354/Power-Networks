"""Downloader for FEC bulk campaign finance datasets (Phase 2a).

Provides candidate master, committee master, linkages, and individual contributions
bulk files from the Federal Election Commission.
"""
from __future__ import annotations

import argparse
from pathlib import Path
from typing import List, Optional

from src.download.base import BaseDownloader, logger

FEC_BULK_BASE = "https://www.fec.gov/files/bulk-downloads"
DEFAULT_CYCLES = [2026, 2024]

HEADER_FILES = {
    "cm_header.csv": f"{FEC_BULK_BASE}/data_dictionaries/cm_header_file.csv",
    "cn_header.csv": f"{FEC_BULK_BASE}/data_dictionaries/cn_header_file.csv",
    "ccl_header.csv": f"{FEC_BULK_BASE}/data_dictionaries/ccl_header_file.csv",
    "indiv_header.csv": f"{FEC_BULK_BASE}/data_dictionaries/indiv_header_file.csv",
}


class FECDownloader(BaseDownloader):
    def __init__(self, raw_dir: Optional[Path] = None):
        super().__init__(dataset_name="fec", raw_dir=raw_dir)

    def download_headers(self, force: bool = False) -> List[Path]:
        """Download column header reference files."""
        downloaded = []
        for filename, url in HEADER_FILES.items():
            path = self.download_file(url, filename, force=force, subfolder="headers")
            downloaded.append(path)
        return downloaded

    def download_cycle(self, cycle: int, include_indiv: bool = True, force: bool = False) -> List[Path]:
        """Download candidate, committee, and contribution bulk zip files for a given 2-year cycle."""
        yy = str(cycle)[-2:]
        cycle_folder = f"{cycle}"

        files_to_download = [
            (f"cm{yy}.zip", f"{FEC_BULK_BASE}/{cycle}/cm{yy}.zip"),  # Committee master
            (f"cn{yy}.zip", f"{FEC_BULK_BASE}/{cycle}/cn{yy}.zip"),  # Candidate master
            (f"ccl{yy}.zip", f"{FEC_BULK_BASE}/{cycle}/ccl{yy}.zip"),  # Candidate-committee linkage
        ]

        if include_indiv:
            files_to_download.append(
                (f"indiv{yy}.zip", f"{FEC_BULK_BASE}/{cycle}/indiv{yy}.zip")  # Individual contributions
            )

        downloaded = []
        for filename, url in files_to_download:
            try:
                path = self.download_file(url, filename, force=force, subfolder=cycle_folder)
                downloaded.append(path)
            except Exception as exc:
                logger.error(f"[fec] Failed downloading {filename} for cycle {cycle}: {exc}")
                raise

        return downloaded

    def run(self, cycles: Optional[List[int]] = None, include_indiv: bool = True, force: bool = False) -> List[Path]:
        target_cycles = cycles or DEFAULT_CYCLES
        results = []
        results.extend(self.download_headers(force=force))
        for cycle in target_cycles:
            results.extend(self.download_cycle(cycle, include_indiv=include_indiv, force=force))
        return results


def main() -> None:
    parser = argparse.ArgumentParser(description="Download FEC bulk campaign finance files.")
    parser.add_argument("--cycle", type=int, nargs="+", default=[2026], help="Election cycle year(s), e.g. 2026 2024")
    parser.add_argument("--skip-indiv", action="store_true", help="Skip large individual contributions file")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files")
    args = parser.parse_args()

    downloader = FECDownloader()
    downloader.run(cycles=args.cycle, include_indiv=not args.skip_indiv, force=args.force)


if __name__ == "__main__":
    main()

