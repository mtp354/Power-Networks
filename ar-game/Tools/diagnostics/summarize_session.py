"""Summarize private device logs after removing coordinates ON the phone."""

import argparse
import json
import math
from pathlib import Path
import re
import shlex
import statistics
import subprocess


PACKAGE = "com.mtp354.fantasyearth"
SESSION_DIRECTORY = f"/sdcard/Android/data/{PACKAGE}/files/sessions"


def _reject_constant(value):
    raise ValueError("Non-finite JSON number")


def summarize(lines):
    records = []
    for number, line in enumerate(lines, 1):
        if not line.strip():
            continue
        try:
            entry = json.loads(line, parse_constant=_reject_constant)
            if "lat" in entry or "lon" in entry:
                raise ValueError("Coordinates must be removed on the device before analysis")
            if not isinstance(entry["t"], int) or not isinstance(entry["alignment"], str):
                raise ValueError("Missing session time or alignment")
        except (KeyError, TypeError, ValueError) as error:
            raise ValueError(f"Invalid sanitized session record on line {number}: {error}") from None
        records.append(entry)
    locked = [entry for entry in records if entry["alignment"] == "Locked"]

    def median(field):
        values = [entry.get(field) for entry in locked]
        values = [value for value in values if isinstance(value, (int, float)) and math.isfinite(value) and value >= 0]
        return statistics.median(values) if values else None

    return {
        "records": len(records),
        "lockedRecords": len(locked),
        "firstLockSeconds": (locked[0]["t"] - records[0]["t"]) / 1000 if locked else None,
        "medianLockedHorizontalM": median("hAcc"),
        "medianLockedYawDeg": median("yawAcc"),
        "earthStates": sorted({entry.get("earth", "Unknown") for entry in records}),
    }


def read_device_session(serial=None):
    adb = ["adb"] + (["-s", serial] if serial else [])

    def remote(arguments):
        command = " ".join(shlex.quote(arg) for arg in arguments)
        result = subprocess.run([*adb, "exec-out", command], capture_output=True, text=True, check=True)
        return result.stdout

    # Unity's Android persistentDataPath uses the package's external files directory.
    names = remote(["ls", "-1t", SESSION_DIRECTORY]).splitlines()
    names = [name for name in names if re.fullmatch(r"\d{8}-\d{6}\.jsonl", name)]
    if not names:
        raise ValueError("No private session logs yet; launch and pause the development app first")
    # This expression runs on Android. Precise coordinates never enter the host output.
    sanitized = remote(["sed", "-E", 's/,"lat":[^,]*,"lon":[^,]*//', SESSION_DIRECTORY + "/" + names[0]])
    return names[0], sanitized.splitlines()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--serial")
    parser.add_argument("--file", type=Path, help="Read an already sanitized local log instead of the device")
    args = parser.parse_args()
    if args.file:
        name, lines = args.file.name, args.file.read_text().splitlines()
    else:
        name, lines = read_device_session(args.serial)
    result = summarize(lines)
    result["session"] = name
    print(json.dumps(result, indent=2, allow_nan=False))
