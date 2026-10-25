import io
import zipfile

import numpy as np
import pytest
import tifffile

from build_koppen_bytes import main, to_grid_bytes


def test_to_grid_bytes_row_major():
    array = np.array([[0, 1, 2], [28, 29, 30]], dtype=np.uint8)
    assert to_grid_bytes(array) == bytes([0, 1, 2, 28, 29, 30])


def test_rejects_wrong_shape():
    with pytest.raises(ValueError, match="shape"):
        to_grid_bytes(np.zeros((1800, 3601), dtype=np.uint8), (1800, 3600))


def test_rejects_values_above_30():
    with pytest.raises(ValueError, match="30"):
        to_grid_bytes(np.array([[31]], dtype=np.uint8))


@pytest.mark.parametrize("array", [np.zeros((2, 3)), np.array([1, 2], dtype=np.uint8)])
def test_rejects_wrong_dtype_or_dimensions(array):
    with pytest.raises(ValueError):
        to_grid_bytes(array)


def cached_archive(cache_dir, *, south_up=False, longitude=-180.0, scale=0.1):
    cache_dir.mkdir()
    array = np.zeros((1800, 3600), dtype=np.uint8)
    array[0, :] = 1
    array[-1, :] = 30
    tiff = io.BytesIO()
    tifffile.imwrite(
        tiff,
        array,
        extratags=[
            (33550, "d", 3, (scale, -scale if south_up else scale, 0), False),
            (33922, "d", 6, (0, 0, 0, longitude, -90 if south_up else 90, 0), False),
        ],
    )
    with zipfile.ZipFile(cache_dir / "koppen_geiger_tif.zip", "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("1991_2020/koppen_geiger_0p1.tif", tiff.getvalue())


@pytest.mark.parametrize("south_up", [False, True])
def test_main_reuses_cache_and_normalizes_rows(tmp_path, monkeypatch, south_up):
    cache = tmp_path / "cache"
    cached_archive(cache, south_up=south_up)

    def unexpected_download(*args, **kwargs):
        pytest.fail("A cached archive must not be downloaded again")

    monkeypatch.setattr("urllib.request.urlopen", unexpected_download)
    out = tmp_path / "output" / "grid.bytes"
    main(cache, out)
    cells = out.read_bytes()
    assert len(cells) == 6_480_000
    assert cells[0] == (30 if south_up else 1)
    assert cells[-1] == (1 if south_up else 30)


@pytest.mark.parametrize("metadata", [{"longitude": 0}, {"scale": 0.2}])
def test_main_rejects_wrong_georeferencing(tmp_path, metadata):
    cache = tmp_path / "cache"
    cached_archive(cache, **metadata)
    out = tmp_path / "grid.bytes"
    with pytest.raises(ValueError, match="georeferenc"):
        main(cache, out)
    assert not out.exists()
