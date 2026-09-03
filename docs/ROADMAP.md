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
| 2a | US political donations (FEC bulk individual contributions) | Not started |
| 2b | EU political party/foundation financing (APPF annual reports) | Not started |
| 3 | Wikidata structured layer (positions, education, party, employer, spouse) for entities already discovered | Not started |
| 4+ | Domain backlog (see below) | Not started |

## Domain backlog (future phases, roughly ordered by expected value/effort)
1. Corporate interlocking directorates (shared board memberships)
2. Offshore finance / shell companies (ICIJ Offshore Leaks, Panama/Pandora Papers)
3. Lobbying registries (US Senate LDA, EU Transparency Register)
4. Elite education & credentialing (Ivy-plus/Oxbridge alumni, secret societies)
5. Think tanks & closed-door gatherings (CFR, Chatham House, Bilderberg, WEF)
6. Diplomatic corps (ambassadors/postings)
7. Defense/arms industry (contractors, SIPRI arms-trade data)
8. Energy sector elites, sports ownership & federations, central banking, sovereign wealth/family offices, religious institutions, private clubs — each evaluated individually once the above are integrated

## Near-term milestones
1. **Phase 2a — FEC individual contributions**: bulk download, committee-entity creation, fuzzy-match contributors against existing officials only (no full deanonymization), unresolved rows to `match_candidates`.
2. **Phase 2b — EU political financing**: parse APPF annual reports into party/foundation-level donation edges (coarser than FEC; no individual-donor matching available).
3. **Phase 3 — Wikidata layer**: SPARQL pulls keyed off `wikidata_qid` external IDs already collected in Phase 1; add typed edges without duplicating existing entities.
4. **Verification pass**: row-count sanity checks, referential-integrity check (anti-join), networkx load smoke test — repeat after every phase.
5. Re-evaluate domain backlog priority once Phases 2–3 are integrated and the fusion approach (bipartite person↔org graph, projected to person-person) has been validated on two real domains.

## Estimated data storage requirements

Baseline measured from the repo today (Phase 1a + 1b complete):

| Location | Size | Notes |
|---|---|---|
| `data/processed/entities.parquet` | ~0.75 MB | 14,631 rows (13,526 people + 1,105 orgs: US legislators/committees + EU MEPs/political groups/committees) |
| `data/processed/entity_external_ids.parquet` | ~1.3 MB | 73,254 rows |
| `data/processed/edges.parquet` | ~0.6 MB | 13,073 rows (US committee memberships + EU political-group/committee memberships, full history per MEP) |
| `data/raw/congress-legislators` | ~10 MB | Source YAML |
| `data/raw/europarl` | ~16 MB | Per-MEP + per-org JSON cache (743 MEPs, 875 orgs) |
| **Total (Phase 1 complete)** | **~29 MB** | |

Projected growth by phase (order-of-magnitude; raw = downloaded source files, processed = parquet tables):

| Phase | Raw size | Processed size | Row-count driver |
|---|---|---|---|
| 1a + 1b (done) | ~26 MB | ~2.6 MB | Legislators, MEPs, committees, political groups |
| 2a — FEC individual contributions | **3–8 GB per 2-year election cycle** | 300 MB–1 GB (matched subset only; raw contributor rows are not fully loaded into `entities`) | FEC bulk files run 20–40M rows per cycle; only high-confidence matches become edges, but `match_candidates` and cached raw files still need this disk budget |
| 2b — EU political financing (APPF) | ~50–200 MB | <10 MB | Annual PDFs/reports per party/foundation, small row counts |
| 3 — Wikidata (targeted SPARQL, not full dump) | 100–500 MB | 50–200 MB | Bounded by number of entities already discovered (thousands, not millions) — avoids the ~100+ GB full Wikidata JSON dump entirely |
| 4+ — Domain backlog (aggregate, rough estimate) | 5–20 GB | 1–3 GB | Dominated by leak datasets (ICIJ Offshore Leaks is multi-GB) and any full-corpus lobbying/board-membership pulls |

**Planning guidance:**
- Multiple years of FEC cycles (if pulled) multiply the 2a estimate directly — budget ~5 GB raw per cycle retained.
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
