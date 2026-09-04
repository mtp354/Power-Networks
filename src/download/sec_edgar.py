"""Downloader for SEC EDGAR corporate directory and filings index.

Complies strictly with the SEC Fair Access Policy:
- Custom User-Agent: 'PowerNetworksAcademicProject research@institution.edu'
- Polite pacing under 10 requests per second.

Source: https://www.sec.gov/search-filings
"""
from __future__ import annotations

import argparse
from pathlib import Path
from typing import List, Optional

from src.download.base import BaseDownloader

SEC_USER_AGENT = "PowerNetworksAcademicProject research@institution.edu"

SEC_FILES = {
    "company_tickers.json": "https://www.sec.gov/files/company_tickers.json",
    "company_tickers_exchange.json": "https://www.sec.gov/files/company_tickers_exchange.json",
    "company_tickers_mf.json": "https://www.sec.gov/files/company_tickers_mf.json",
}


class SECDownloader(BaseDownloader):
    def __init__(self, raw_dir: Optional[Path] = None, user_agent: Optional[str] = None):
        super().__init__(
            dataset_name="sec_edgar",
            raw_dir=raw_dir,
            user_agent=user_agent or SEC_USER_AGENT,
        )

    def run(self, force: bool = False) -> List[Path]:
        downloaded = []
        for filename, url in SEC_FILES.items():
            path = self.download_file(
                url,
                filename,
                force=force,
                headers={"User-Agent": self.user_agent},
            )
            downloaded.append(path)
        return downloaded


def main() -> None:
    parser = argparse.ArgumentParser(description="Download SEC EDGAR directory and reference datasets.")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files")
    parser.add_argument("--user-agent", type=str, default=None, help="Custom User-Agent formatted as 'SampleName contact@domain.com'")
    args = parser.parse_args()

    downloader = SECDownloader(user_agent=args.user_agent)
    downloader.run(force=args.force)


if __name__ == "__main__":
    main()

