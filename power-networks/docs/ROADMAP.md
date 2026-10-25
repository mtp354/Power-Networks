# Power Networks — Project Roadmap

Living roadmap for the elite power-network graph project. Update this file as phases complete or scope changes.

## Guiding principles
- Build slowly: one or two domains at a time, each fully wired into the shared entity-resolution schema before starting the next.
- No silent merges: anything below a confidence threshold goes to `match_candidates.parquet` for review, never straight into `edges.parquet`.
- Ingestion scripts are reusable functions/CLIs from day one so scheduling can be bolted on later without a rewrite.

## Phase status

| Phase | Domain | Status |
|---|---|---|
| 0 | Repo scaffolding, entity-resolution schema (`entities`, `entity_external_ids`, `edges`, `match_candidates`) | Done |
| 1a | US Congress + president/VP (unitedstates/congress-legislators) | Done |
| 1b | EU MEPs, current term (European Parliament Open Data Portal) | Done |
| 2a | US political donations — candidate/committee/linkage graph + individual donation edges (FEC bulk files) | Done: committees, candidates, linkage, and 42,454 high-confidence donor→committee edges ingested (31.6M contribution rows scanned, 2026 cycle only) |
| 2b | EU political party/foundation financing (APPF annual reports) | Not started |
| 3 | Wikidata structured layer (positions, education, party, employer, spouse) | Done for US legislators (127/128 SPARQL batches; EU MEPs not yet linked to Wikidata QIDs) |
| 4+ | Domain backlog (see below) | Raw data acquired for several backlog items (see below); no ingest scripts yet |

## Raw data acquired, not yet ingested (backlog candidates)
A separate `src/download/` layer (raw-fetch only, no entity-graph writes) now covers:
- **IRS Tax-Exempt Organization Business Master File** (`data/raw/irs/`, ~340 MB) — non-profits/foundations/think tanks registry, maps to the "think tanks" backlog domain.
- **SEC EDGAR company tickers/CIK directory** (`data/raw/sec_edgar/`) — maps to the "corporate interlocking directorates" backlog domain (CIK is the join key for later EDGAR filings work).
- **ACLED aggregated conflict data** (`data/raw/ACLED/`, manually downloaded — the public aggregate/regional files turned out not to require an account, see `docs/DATASETS_SAVED_FOR_LATER.md`) — maps to the paramilitary/conflict domain from the original source list.
- **CPDS** and **PolData** (`data/raw/cpds/`, `data/raw/poldata/`) — comparative political science reference datasets (government composition, cabinets); useful as contextual/validation data rather than a direct entity source.
- **Open Data Inception catalog** (`data/raw/open_data_inception/`) — a meta-catalog of 2,600+ open data portals, useful for discovering further sources, not an entity source itself.

None of these have ingest scripts yet; they are raw downloads only. Prioritize ingest scripts for IRS (think tanks/NGOs) and SEC EDGAR (interlocking directorates) next, as both directly extend existing domains.

## Domain backlog (future phases, roughly ordered by expected value/effort)
1. Corporate interlocking directorates (shared board memberships) — raw SEC EDGAR ticker/CIK directory already downloaded
2. Offshore finance / shell companies (ICIJ Offshore Leaks, Panama/Pandora Papers)
3. Lobbying registries (US Senate LDA, EU Transparency Register)
4. Elite education & credentialing (Ivy-plus/Oxbridge alumni, secret societies)
5. Think tanks & closed-door gatherings (CFR, Chatham House, Bilderberg, WEF) — raw IRS non-profit registry already downloaded
6. Diplomatic corps (ambassadors/postings)
7. Defense/arms industry (contractors, SIPRI arms-trade data)
8. Paramilitary/conflict actors — raw ACLED aggregates already downloaded (manually)
9. Energy sector elites, sports ownership & federations, central banking, sovereign wealth/family offices, religious institutions, private clubs — each evaluated individually once the above are integrated

## Near-term milestones
1. ~~**Phase 2a (part 1) — FEC committees/candidates/linkage**: committee and candidate entities, cross-referenced against existing officials via FEC id, linkage edges.~~ Done.
2. ~~**Phase 3 — Wikidata layer**: SPARQL pulls keyed off `wikidata` external IDs collected in Phase 1a; typed edges (position held, educated at, employed by, party, spouse) without duplicating existing entities.~~ Done for US legislators.
3. ~~**Phase 2a (part 2) — FEC individual contributions**: downloaded the 2.1 GB/31.6M-row 2026-cycle bulk file, fuzzy-matched contributors (blocked on last name) against existing officials only.~~ Done: 42,454 auto-accepted `donated_to` edges (score ≥ 92, median confidence 1.0), 19,726 bounded review candidates (top 5 per official, score 85–92) in `match_candidates`. Individual contributions for other cycles (e.g. 2024) not yet pulled.
4. **Phase 2b — EU political financing**: parse APPF annual reports into party/foundation-level donation edges (coarser than FEC; no individual-donor matching available).
5. **Wikidata coverage for EU MEPs**: MEPs are not yet linked to Wikidata QIDs (unlike US legislators, whose `wikidata` field was added in Phase 1a) — add QID lookup/matching so Phase 3 can extend to MEPs.
6. **IRS non-profit registry ingest**: parse the EO Business Master File into organization entities (think tanks/foundations/NGOs backlog domain) — raw data already downloaded (~325 MB).
7. **SEC EDGAR ingest**: parse company tickers/CIK directory into organization entities as the seed for corporate interlocking directorates — raw data already downloaded.
8. **Verification pass**: row-count sanity checks, referential-integrity check (anti-join), networkx load smoke test — repeat after every phase.
9. Re-evaluate domain backlog priority once Phases 2–3 are integrated and the fusion approach (bipartite person↔org graph, projected to person-person) has been validated on two real domains.

## Estimated data storage requirements

Baseline measured from the repo today (Phase 1, 2a, and 3 complete):

| Location | Size | Notes |
|---|---|---|
| `data/processed/entities.parquet` | ~2.6 MB | 49,279 rows (22,442 people + 26,837 orgs) |
| `data/processed/entity_external_ids.parquet` | ~2.8 MB | 121,525 rows |
| `data/processed/edges.parquet` | ~6.8 MB | 120,363 unique (source,target) pairs / 121,426 rows — committee/political-group memberships, FEC linkages, 42,454 FEC donation edges, Wikidata edges |
| `data/processed/match_candidates.parquet` | ~1.2 MB | 19,726 rows, bounded to top 5 per official (see Phase 2a note below) |
| `data/raw/` (all sources incl. IRS, ACLED, FEC individual contributions, wikidata cache) | ~2.5 GB | See breakdown below |
| **Total (current)** | **~2.5 GB** | |

Raw data breakdown by source (`data/raw/`):

| Source | Size | Ingested into graph? |
|---|---|---|
| `fec` | ~2.1 GB | Yes (Phase 2a: committees/candidates/linkage + individual contributions, 2026 cycle only) |
| `irs` | ~325 MB | Not yet (backlog: think tanks/NGOs) |
| `ACLED` | ~43 MB | Not yet (backlog: paramilitary/conflict) |
| `wikidata` | ~43 MB | Yes (Phase 3) |
| `europarl` | ~16 MB | Yes (Phase 1b) |
| `congress-legislators` | ~10 MB | Yes (Phase 1a) |
| `cpds` | ~6.3 MB | Not yet (reference data) |
| `open_data_inception` | ~2.5 MB | Not yet (source catalog, not an entity source) |
| `sec_edgar` | ~2.5 MB | Not yet (backlog: interlocking directorates) |
| `poldata` | ~0.4 MB | Not yet (reference data) |

Projected growth by phase (order-of-magnitude; raw = downloaded source files, processed = parquet tables):

| Phase | Raw size | Processed size | Row-count driver |
|---|---|---|---|
| 1a + 1b (done) | ~26 MB | ~2.6 MB | Legislators, MEPs, committees, political groups |
| 2a — FEC (done, 2026 cycle only) | ~2.1 GB actual | ~11 MB actual (edges + bounded match_candidates) | 31.6M contribution rows scanned; only 42,454 high-confidence edges + 19,726 bounded review rows kept — the original 300MB–1GB processed-size estimate was too high because capping review candidates to top-5-per-official (not every fuzzy hit) keeps `match_candidates` small |
| 2a — additional FEC cycles (e.g. 2024, 2022) | +3–8 GB raw per additional cycle | small incremental (same matching approach) | Same per-cycle download/parse cost repeats for each historical cycle if pulled |
| 2b — EU political financing (APPF) | ~50–200 MB | <10 MB | Annual PDFs/reports per party/foundation, small row counts |
| 3 — Wikidata (done for US legislators) | ~43 MB actual | included in processed totals above | 127/128 targeted SPARQL batches; EU MEPs not yet covered |
| 4+ — Domain backlog (aggregate, rough estimate) | 5–20 GB | 1–3 GB | Dominated by leak datasets (ICIJ Offshore Leaks is multi-GB) and any full-corpus lobbying/board-membership pulls |

**Planning guidance:**
- Multiple years of FEC cycles (if pulled) multiply the 2a raw-size line directly — budget ~2 GB raw per additional cycle at current scope (individual contributions dominate).
- The match_candidates bounding strategy (top-K per entity, not every threshold-passing row) is now the standard pattern for any future fuzzy-matching ingest — without it, common-name collisions can produce millions of low-value rows (seen firsthand: an unbounded first pass produced 1.93M rows before capping to 19,726).
- Keep `data/raw/` out of git (already in `.gitignore`); it should live on local disk or cheap object storage, not in version control.
- A reasonable target for the next 12 months of work (Phases 1–3 complete) is **10–15 GB total on-disk**, comfortably fitting on a laptop; full backlog completion could reach **30–50 GB**, at which point moving raw archives to external/object storage is worth planning for.

## Future visualization process plan

Visualization is intentionally deferred until at least two domains are fused (Phase 3 complete), so early tooling choices are validated against real bipartite person↔org data rather than a single flat list.

1. **Stage 1 — Local exploratory analysis (during Phases 1–3)**
   - Load `entities.parquet` / `edges.parquet` directly into `networkx` (already the Phase-1 verification smoke test).
   - Ad-hoc static plots (matplotlib / `networkx.draw`) and basic metrics (degree distribution, connected components) — sanity-checking only, not a deliverable.

2. **Stage 2 — Analyst-facing graph exploration (once 2–3 domains are fused)**
   - Export subgraphs to **Gephi** (GEXF format) for layout experimentation (ForceAtlas2, community detection via modularity) on person-to-person projections.
   - Establish a repeatable "bipartite → person-person projection" export script (shared affiliation = edge), since this projection is the actual object of interest for elite-network analysis, not the raw bipartite graph.

3. **Stage 3 — Interactive web dashboard (once schema and projections are stable)**
   - Candidate stack: a `networkx`/`igraph` backend feeding a web front-end (e.g. Cytoscape.js or Sigma.js) for filtering by domain, date range, confidence, and entity type.
   - Support drill-down from a person node to the underlying `edges` rows and their `source_dataset`/`confidence` provenance — critical for an academic audience that will want to verify claims.

4. **Stage 4 — Public-facing / publishable outputs (post-validation)**
   - Curated, static visualizations (e.g. Observable, D3) for specific published findings, generated from versioned snapshots of the graph rather than the live pipeline.
   - Any public release must first pass through the confidence/provenance review implied by `match_candidates` — no low-confidence edges in public-facing visuals.

Visualization tooling decisions are deferred to avoid over-engineering ahead of real fused data; revisit this section once Phase 3 is complete.
