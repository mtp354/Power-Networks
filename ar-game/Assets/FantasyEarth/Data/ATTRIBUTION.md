# Köppen-Geiger climate data

Beck, H. E., McVicar, T. R., Vergopolan, N., et al. (2023). *High-resolution (1 km) Köppen-Geiger maps for 1901–2099 based on constrained CMIP6 projections.* Scientific Data 10, 724. Paper: https://doi.org/10.1038/s41597-023-02549-6.

Dataset: https://doi.org/10.6084/m9.figshare.21789074.v2. Licensed under **Creative Commons Attribution 4.0 International (CC BY 4.0)**: https://creativecommons.org/licenses/by/4.0/.

Fantasy Earth uses the dataset's **1991–2020, 0.1-degree** classification raster, rather than its 1-km raster. Source archive: https://ndownloader.figshare.com/files/61012822 (`koppen_geiger_tif.zip`, 130,618,411 bytes; published MD5 `7fc2f5a15d4f5fe0ce59c9a9b502aa09`). Selected member: `1991_2020/koppen_geiger_0p1.tif`.

Transformation by the Fantasy Earth project: extract the class values and serialize them as one unsigned byte per cell in row-major order, west to east and north to south. The source grid is 3,600 columns by 1,800 rows, beginning at 180°W, 90°N. Values 1–30 retain the source Köppen classes; 0 denotes ocean/no data. The `.bytes` file has no header or georeferencing metadata; its dimensions and orientation are defined by `KoppenGrid`.

Generated `koppen_1991_2020_0p1.bytes`: 6,480,000 bytes; SHA-256 `533e3f2f20ac758ce87014aeb3151e7552432f1146e4bb3022cff9012fb6b047`.

Rebuild with `Tools/climate/build_koppen_bytes.py` using `Tools/climate/requirements.txt`. Downloads and the local virtual environment stay in the ignored `Tools/climate/.cache/` directory. No class values are synthesized or modified.

The runtime model records the fixed period-end timestamp `2020-12-31T00:00:00Z`. Its confidence flag is 1 for a populated cell and 0 for no data; it does not represent the source dataset's separate classification-confidence estimates.

The visible in-app credits screen is scheduled for Plan 3. Preserve this attribution and license link when redistributing the bundled data.
