"""Downloader for Comparative Political Data Set (CPDS) and Government Composition.

Source: https://cpds-data.org/
Authors: Klaus Armingeon, Sarah Engler, Lucas Leemann, David Weisstanner.
Covers 36 democratic countries longitudinally (1960-2023).
"""
from __future__ import annotations

import argparse
from pathlib import Path
from typing import List, Optional

from src.download.base import BaseDownloader

CPDS_FILES = {
    # Core longitudinal dataset (Excel)
    "cpds_1960_2023.xlsx": "https://cpds-data.org/wp-content/uploads/2026/05/cpds-1960-2023-update-2025.xlsx",
    # Government composition dataset (Excel)
    "government_composition_1960_2023.xlsx": "https://cpds-data.org/wp-content/uploads/2025/06/government_composition_1960-2023_update_2025.xlsx",
    # Documentation & Codebooks
    "codebook_cpds.pdf": "https://cpds-data.org/wp-content/uploads/2025/09/codebook_cpds.pdf",
    "codebook_government_composition.pdf": "https://cpds-data.org/wp-content/uploads/2025/06/codebook_government_composition.pdf",
}


class CPDSDownloader(BaseDownloader):
    def __init__(self, raw_dir: Optional[Path] = None):
        super().__init__(dataset_name="cpds", raw_dir=raw_dir)

    def run(self, force: bool = False) -> List[Path]:
        downloaded = []
        for filename, url in CPDS_FILES.items():
            path = self.download_file(url, filename, force=force)
            downloaded.append(path)
        return downloaded


def main() -> None:
    parser = argparse.ArgumentParser(description="Download Comparative Political Data Set files.")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files")
    args = parser.parse_args()

    downloader = CPDSDownloader()
    downloader.run(force=args.force)


if __name__ == "__main__":
    main()

