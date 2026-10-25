"""Bundle the pinned Beck et al. 1991–2020 0.1-degree Köppen raster."""

import argparse
import hashlib
import io
import math
from pathlib import Path
import urllib.request
import zipfile

import numpy as np
import tifffile


SOURCE_URL = "https://ndownloader.figshare.com/files/61012822"
SOURCE_MD5 = "7fc2f5a15d4f5fe0ce59c9a9b502aa09"
ARCHIVE_NAME = "koppen_geiger_tif.zip"
MEMBER_NAME = "1991_2020/koppen_geiger_0p1.tif"
EXPECTED_SHAPE = (1800, 3600)
SCRIPT_DIR = Path(__file__).resolve().parent


def to_grid_bytes(array: np.ndarray, expected_shape: tuple[int, int] | None = None) -> bytes:
    """Serialize validated climate cells in north-to-south, row-major order."""
    if array.ndim != 2 or not array.size:
        raise ValueError("Climate grid must be a nonempty two-dimensional array")
    if expected_shape is not None and array.shape != expected_shape:
        raise ValueError(f"Expected shape {expected_shape}, got {array.shape}")
    if array.dtype != np.uint8:
        raise ValueError(f"Expected uint8 climate cells, got {array.dtype}")
    if array.max() > 30:
        raise ValueError("Climate classes must be in the range 0–30")
    return array.tobytes(order="C")


def _download_archive(destination: Path) -> None:
    partial = destination.with_suffix(".zip.part")
    digest = hashlib.md5()
    request = urllib.request.Request(SOURCE_URL, headers={"User-Agent": "FantasyEarth-climate-builder/1"})
    with urllib.request.urlopen(request, timeout=120) as response, partial.open("wb") as output:
        while chunk := response.read(1024 * 1024):
            output.write(chunk)
            digest.update(chunk)
    if digest.hexdigest() != SOURCE_MD5:
        partial.unlink()
        raise ValueError("Downloaded archive checksum does not match the pinned Figshare file")
    partial.replace(destination)


def main(cache_dir: Path, out_path: Path) -> None:
    cache_dir = Path(cache_dir)
    out_path = Path(out_path)
    cache_dir.mkdir(parents=True, exist_ok=True)
    archive_path = cache_dir / ARCHIVE_NAME
    if not archive_path.exists():
        print(f"Downloading {SOURCE_URL}", flush=True)
        _download_archive(archive_path)

    with zipfile.ZipFile(archive_path) as archive:
        with tifffile.TiffFile(io.BytesIO(archive.read(MEMBER_NAME))) as raster:
            page = raster.pages[0]
            array = page.asarray()
            scale_tag = page.tags.get("ModelPixelScaleTag")
            tie_tag = page.tags.get("ModelTiepointTag")
            if scale_tag is None or tie_tag is None or page.tags.get("ModelTransformationTag") is not None:
                raise ValueError("Unsupported or missing GeoTIFF georeferencing")
            sx, sy, _ = scale_tag.value
            i, j, _, x, y, _ = tie_tag.value
            west = x - i * sx
            first_edge = y + j * sy
            if not (
                math.isclose(sx, 0.1, abs_tol=1e-8)
                and math.isclose(abs(sy), 0.1, abs_tol=1e-8)
                and math.isclose(west, -180, abs_tol=1e-8)
                and math.isclose(first_edge, 90 if sy > 0 else -90, abs_tol=1e-8)
            ):
                raise ValueError("GeoTIFF georeferencing must cover the global 0.1-degree grid")
            if sy < 0:
                array = np.flipud(array)
            cells = to_grid_bytes(array, EXPECTED_SHAPE)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_bytes(cells)
    print(f"Wrote {len(cells)} bytes to {out_path}", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache-dir", type=Path, default=SCRIPT_DIR / ".cache")
    parser.add_argument(
        "--out-path", type=Path,
        default=SCRIPT_DIR / "../../Assets/FantasyEarth/Data/koppen_1991_2020_0p1.bytes",
    )
    args = parser.parse_args()
    main(args.cache_dir, args.out_path)
