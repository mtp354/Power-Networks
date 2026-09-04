"""Unified CLI and orchestrator for downloading open datasets in Power Networks.

Usage:
    python -m src.download.cli --list
    python -m src.download.cli --status
    python -m src.download.cli --dataset poldata
    python -m src.download.cli --dataset cpds
    python -m src.download.cli --dataset sec_edgar
    python -m src.download.cli --dataset opendatainception
    python -m src.download.cli --dataset fec --cycle 2026 --skip-indiv
    python -m src.download.cli --dataset wikidata --max-batches 2
    python -m src.download.cli --all
"""
from __future__ import annotations

import argparse
import sys
from typing import Dict, Type

from src.download.base import BaseDownloader, format_size, logger
from src.download.cpds import CPDSDownloader
from src.download.fec import FECDownloader
from src.download.irs import IRSDownloader
from src.download.opendatainception import OpenDataInceptionDownloader
from src.download.poldata import PolDataDownloader
from src.download.sec_edgar import SECDownloader
from src.download.wikidata import WikidataDownloader

DATASETS: Dict[str, Dict[str, any]] = {
    "cpds": {
        "cls": CPDSDownloader,
        "description": "Comparative Political Data Set (longitudinal 1960-2023 & government composition)",
        "source": "https://cpds-data.org/",
    },
    "poldata": {
        "cls": PolDataDownloader,
        "description": "PolData political science dataset collection (cabinets, institutions, elites)",
        "source": "https://github.com/erikgahner/PolData",
    },
    "sec_edgar": {
        "cls": SECDownloader,
        "description": "SEC EDGAR company tickers and corporate directory index",
        "source": "https://www.sec.gov/search-filings",
    },
    "opendatainception": {
        "cls": OpenDataInceptionDownloader,
        "description": "Open Data Inception global open data portal catalog (2600+ portals)",
        "source": "https://opendatainception.io/",
    },
    "fec": {
        "cls": FECDownloader,
        "description": "Federal Election Commission bulk campaign finance files (Phase 2a)",
        "source": "https://www.fec.gov/data/receipts/individual-contributions/",
    },
    "wikidata": {
        "cls": WikidataDownloader,
        "description": "Targeted SPARQL extractor for Phase 1 political elites (Phase 3)",
        "source": "https://query.wikidata.org/",
    },
    "irs": {
        "cls": IRSDownloader,
        "description": "IRS Tax-Exempt Organization Business Master File & Non-profit registry",
        "source": "https://www.irs.gov/charities-non-profits/tax-exempt-organization-search",
    },
}


def print_list() -> None:
    print("\n=======================================================")
    print("Power Networks — Available Direct Dataset Downloaders")
    print("=======================================================\n")
    for name, meta in DATASETS.items():
        print(f"  • {name:<18} : {meta['description']}")
        print(f"    {'':<18}   Source: {meta['source']}\n")
    print("Note: Gated datasets requiring accounts/subscriptions are tracked in:")
    print("      docs/DATASETS_SAVED_FOR_LATER.md\n")


def print_status() -> None:
    print("\n=======================================================")
    print("Power Networks — Local Raw Data Storage Status")
    print("=======================================================\n")
    grand_total_bytes = 0
    grand_total_files = 0

    for name, meta in DATASETS.items():
        downloader: BaseDownloader = meta["cls"]()
        status = downloader.get_status()
        grand_total_bytes += status["total_size_bytes"]
        grand_total_files += status["file_count"]

        status_marker = "✓ Present" if status["file_count"] > 0 else "○ Empty"
        print(f"[{status_marker}] {name:<18} : {status['file_count']} files ({status['total_size_str']})")
        for f in status["files"][:5]:
            print(f"       - {f['name']} ({f['size_str']})")
        if len(status["files"]) > 5:
            print(f"       ... and {len(status['files']) - 5} more files")
        print()

    print(f"Total Local Storage: {grand_total_files} files, {format_size(grand_total_bytes)}\n")


def run_downloader(name: str, args: argparse.Namespace) -> None:
    if name not in DATASETS:
        logger.error(f"Unknown dataset '{name}'. Available: {list(DATASETS.keys())}")
        sys.exit(1)

    meta = DATASETS[name]
    downloader = meta["cls"]()
    logger.info(f"Executing download pipeline for: {name}")

    if name == "fec":
        downloader.run(cycles=args.cycle, include_indiv=not args.skip_indiv, force=args.force)
    elif name == "wikidata":
        downloader.run(max_batches=args.max_batches, force=args.force)
    elif name == "opendatainception":
        downloader.run(force=args.force, format_type=args.format)
    else:
        downloader.run(force=args.force)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Power Networks Dataset Download Orchestrator",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument("--list", action="store_true", help="List all available downloaders and descriptions")
    parser.add_argument("--status", action="store_true", help="Report local raw files and disk usage")
    parser.add_argument("--dataset", type=str, choices=list(DATASETS.keys()), help="Download a specific dataset")
    parser.add_argument("--all", action="store_true", help="Download all available open datasets")
    parser.add_argument("--force", action="store_true", help="Re-download and overwrite existing files")

    # Dataset-specific options
    parser.add_argument("--cycle", type=int, nargs="+", default=[2026], help="FEC election cycle year(s)")
    parser.add_argument("--skip-indiv", action="store_true", help="Skip massive FEC individual contributions file")
    parser.add_argument("--format", choices=["json", "csv", "both"], default="json", help="Format for Open Data Inception")
    parser.add_argument("--max-batches", type=int, default=None, help="Max SPARQL query batches for Wikidata")

    args = parser.parse_args()

    if args.list:
        print_list()
        return

    if args.status:
        print_status()
        return

    if args.dataset:
        run_downloader(args.dataset, args)
    elif args.all:
        for name in DATASETS:
            try:
                run_downloader(name, args)
            except Exception as exc:
                logger.error(f"Pipeline error on {name}: {exc}")
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
