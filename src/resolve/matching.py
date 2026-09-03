"""Shared name-normalization and fuzzy-matching helpers for entity resolution.

Used by ingest scripts that must match raw records (no stable external ID) against
existing entities, e.g. FEC individual contributors matched to known officials.
"""
from __future__ import annotations

import re
import unicodedata

from rapidfuzz import fuzz

_SUFFIXES = {"jr", "sr", "ii", "iii", "iv", "v"}


def normalize_name(name: str) -> str:
    """Lowercase, strip accents/punctuation/suffixes for comparison purposes only."""
    if not name:
        return ""
    ascii_name = "".join(
        c for c in unicodedata.normalize("NFKD", name) if not unicodedata.combining(c)
    )
    ascii_name = ascii_name.lower()
    # FEC names are often "LAST, FIRST MIDDLE" — normalize to "first middle last"
    if "," in ascii_name:
        last, _, rest = ascii_name.partition(",")
        ascii_name = f"{rest.strip()} {last.strip()}"
    tokens = re.sub(r"[^a-z\s]", " ", ascii_name).split()
    tokens = [t for t in tokens if t not in _SUFFIXES]
    return " ".join(tokens)


def name_similarity(name_a: str, name_b: str) -> float:
    """Return a 0-100 similarity score between two raw (un-normalized) names."""
    return fuzz.token_sort_ratio(normalize_name(name_a), normalize_name(name_b))
