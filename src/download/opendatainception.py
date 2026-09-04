"""Downloader for Open Data Inception catalog (2,600+ open data portals worldwide).

Source: https://opendatainception.io/
Powered by OpenDataSoft: https://public.opendatasoft.com/explore/dataset/open-data-sources/
"""
from __future__ import annotations

import argparse
from pathlib import Path
from typing import List, Optional

from src.download.base import BaseDownloader

ODS_EXPORTS = {
    "open_data_sources.json": "https://public.opendatasoft.com/api/explore/v2.1/catalog/datasets/open-data-sources/exports/json",
    "open_data_sources.csv": "https://public.opendatasoft.com/api/explore/v2.1/catalog/datasets/open-data-sources/exports/csv",
}


ODS_USER_AGENT = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36"


class OpenDataInceptionDownloader(BaseDownloader):
    def __init__(self, raw_dir: Optional[Path] = None):
        super().__init__(dataset_name="open_data_inception", raw_dir=raw_dir, user_agent=ODS_USER_AGENT)

    def run(self, force: bool = False, format_type: str = "json") -> List[Path]:
        downloaded = []
        if format_type in ["json", "both"]:
            path = self.download_file(ODS_EXPORTS["open_data_sources.json"], "open_data_sources.json", force=force)
            downloaded.append(path)
        if format_type in ["csv", "both"]:
            path = self.download_file(ODS_EXPORTS["open_data_sources.csv"], "open_data_sources.csv", force=force)
            downloaded.append(path)
        return downloaded


def main() -> None:
    parser = argparse.ArgumentParser(description="Download Open Data Inception dataset catalog.")
    parser.add_argument("--format", choices=["json", "csv", "both"], default="json", help="Export format")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files")
    args = parser.parse_args()

    downloader = OpenDataInceptionDownloader()
    downloader.run(force=args.force, format_type=args.format)


if __name__ == "__main__":
    main()
