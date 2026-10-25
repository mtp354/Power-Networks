"""Downloader for PolData (Political science dataset collection).

Source: https://github.com/erikgahner/PolData
Author: Erik Gahner Larsen
Indexes political datasets: cabinets, political elites, ministers, and institutions.
"""
from __future__ import annotations

import argparse
from pathlib import Path
from typing import List, Optional

from src.download.base import BaseDownloader

POLDATA_BASE = "https://raw.githubusercontent.com/erikgahner/PolData/master"

POLDATA_FILES = {
    "PolData.csv": f"{POLDATA_BASE}/PolData.csv",
    "PolData.xlsx": f"{POLDATA_BASE}/PolData.xlsx",
    "README.md": f"{POLDATA_BASE}/README.md",
}


class PolDataDownloader(BaseDownloader):
    def __init__(self, raw_dir: Optional[Path] = None):
        super().__init__(dataset_name="poldata", raw_dir=raw_dir)

    def run(self, force: bool = False) -> List[Path]:
        downloaded = []
        for filename, url in POLDATA_FILES.items():
            path = self.download_file(url, filename, force=force)
            downloaded.append(path)
        return downloaded


def main() -> None:
    parser = argparse.ArgumentParser(description="Download PolData database files.")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files")
    args = parser.parse_args()

    downloader = PolDataDownloader()
    downloader.run(force=args.force)


if __name__ == "__main__":
    main()
