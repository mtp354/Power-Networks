"""Base downloader with polite headers, chunked streaming, retry logic, and caching."""
from __future__ import annotations

import logging
import os
import time
from pathlib import Path
from typing import Dict, Optional
import requests

from src.schema import RAW_DIR

logger = logging.getLogger("download")
logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")

DEFAULT_USER_AGENT = "Mozilla/5.0 (compatible; PowerNetworksResearch/1.0; academic use; mailto:research@institution.edu)"


def format_size(num_bytes: int) -> str:
    """Format bytes into human readable string."""
    for unit in ["B", "KB", "MB", "GB", "TB"]:
        if abs(num_bytes) < 1024.0:
            return f"{num_bytes:3.1f} {unit}"
        num_bytes /= 1024.0
    return f"{num_bytes:.1f} PB"


class BaseDownloader:
    """Base class for all dataset downloaders."""

    def __init__(self, dataset_name: str, raw_dir: Optional[Path] = None, user_agent: Optional[str] = None):
        self.dataset_name = dataset_name
        self.raw_dir = (raw_dir or RAW_DIR) / dataset_name
        self.user_agent = user_agent or DEFAULT_USER_AGENT
        self.raw_dir.mkdir(parents=True, exist_ok=True)

    def get_session(self, custom_headers: Optional[Dict[str, str]] = None) -> requests.Session:
        session = requests.Session()
        headers = {"User-Agent": self.user_agent}
        if custom_headers:
            headers.update(custom_headers)
        session.headers.update(headers)
        return session

    def download_file(
        self,
        url: str,
        dest_filename: str,
        force: bool = False,
        headers: Optional[Dict[str, str]] = None,
        timeout: int = 120,
        max_retries: int = 5,
        subfolder: Optional[str] = None,
    ) -> Path:
        """Download a file with streaming and retry logic, returning the local Path."""
        target_dir = self.raw_dir if not subfolder else self.raw_dir / subfolder
        target_dir.mkdir(parents=True, exist_ok=True)
        dest_path = target_dir / dest_filename

        if dest_path.exists() and dest_path.stat().st_size > 0 and not force:
            logger.info(f"[{self.dataset_name}] Skipping {dest_filename} (already exists: {format_size(dest_path.stat().st_size)})")
            return dest_path

        session = self.get_session(custom_headers=headers)
        temp_path = dest_path.with_suffix(dest_path.suffix + ".tmp")

        logger.info(f"[{self.dataset_name}] Starting download: {url} -> {dest_path.name}")
        for attempt in range(max_retries):
            try:
                with session.get(url, stream=True, timeout=timeout) as response:
                    if response.status_code == 429 or response.status_code in [500, 502, 503, 504]:
                        sleep_time = min(60, 5 * (attempt + 1))
                        logger.warning(f"[{self.dataset_name}] Received {response.status_code}. Retrying in {sleep_time}s...")
                        time.sleep(sleep_time)
                        continue
                    response.raise_for_status()

                    total_size = int(response.headers.get("content-length", 0))
                    downloaded = 0
                    with open(temp_path, "wb") as f:
                        for chunk in response.iter_content(chunk_size=1024 * 1024):  # 1MB chunks
                            if chunk:
                                f.write(chunk)
                                downloaded += len(chunk)

                # Rename temp file to final destination once complete
                if temp_path.exists():
                    temp_path.replace(dest_path)
                logger.info(f"[{self.dataset_name}] Successfully downloaded {dest_filename} ({format_size(dest_path.stat().st_size)})")
                return dest_path

            except (requests.RequestException, IOError) as exc:
                if temp_path.exists():
                    temp_path.unlink()
                if attempt == max_retries - 1:
                    logger.error(f"[{self.dataset_name}] Failed to download {url} after {max_retries} attempts: {exc}")
                    raise
                sleep_time = min(60, 5 * (attempt + 1))
                logger.warning(f"[{self.dataset_name}] Download error: {exc}. Retrying in {sleep_time}s...")
                time.sleep(sleep_time)

        raise RuntimeError(f"Failed to download {url}")

    def get_status(self) -> Dict[str, any]:
        """Return inventory and size of locally downloaded files for this dataset."""
        files = []
        total_size = 0
        if self.raw_dir.exists():
            for p in self.raw_dir.rglob("*"):
                if p.is_file() and not p.name.endswith(".tmp"):
                    size = p.stat().st_size
                    total_size += size
                    files.append({
                        "name": str(p.relative_to(self.raw_dir)),
                        "size_bytes": size,
                        "size_str": format_size(size),
                    })
        return {
            "dataset": self.dataset_name,
            "raw_dir": str(self.raw_dir),
            "file_count": len(files),
            "total_size_bytes": total_size,
            "total_size_str": format_size(total_size),
            "files": files,
        }

