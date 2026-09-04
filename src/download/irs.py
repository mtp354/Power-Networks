"""Downloader for IRS Tax-Exempt Organization datasets (Form 990 & EO BMF).

Source: https://www.irs.gov/charities-non-profits/tax-exempt-organization-search
Provides the Exempt Organizations Business Master File (EO BMF) extracts and Publication 78 data
covering non-profit foundations, think tanks, and public charities.
"""
from __future__ import annotations

import argparse
from pathlib import Path
from typing import List, Optional

from src.download.base import BaseDownloader

IRS_FILES = {
    # Exempt Organizations Business Master File (EO BMF) CSVs by region
    "eo_region1_northeast.csv": "https://www.irs.gov/pub/irs-soi/eo1.csv",
    "eo_region2_midatlantic_greatlakes.csv": "https://www.irs.gov/pub/irs-soi/eo2.csv",
    "eo_region3_gulfcoast_pacific.csv": "https://www.irs.gov/pub/irs-soi/eo3.csv",
    "eo_region4_international.csv": "https://www.irs.gov/pub/irs-soi/eo4.csv",
    # Publication 78 cumulative data
    "pub78_data.zip": "https://apps.irs.gov/pub/epostcard/data-download-pub78.zip",
}


class IRSDownloader(BaseDownloader):
    def __init__(self, raw_dir: Optional[Path] = None):
        super().__init__(dataset_name="irs", raw_dir=raw_dir)

    def run(self, force: bool = False, regions_only: bool = True) -> List[Path]:
        downloaded = []
        files = {k: v for k, v in IRS_FILES.items() if not (k == "pub78_data.zip" and regions_only)}
        for filename, url in files.items():
            path = self.download_file(url, filename, force=force)
            downloaded.append(path)
        return downloaded


def main() -> None:
    parser = argparse.ArgumentParser(description="Download IRS tax-exempt non-profit master files.")
    parser.add_argument("--include-pub78", action="store_true", help="Include Publication 78 data zip")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files")
    args = parser.parse_args()

    downloader = IRSDownloader()
    downloader.run(force=args.force, regions_only=not args.include_pub78)


if __name__ == "__main__":
    main()

