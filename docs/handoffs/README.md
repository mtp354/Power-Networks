# Fantasy Earth: small implementation handoffs

Prepared 2026-10-25 from commit `d1fec0c`. These briefs are implementation instructions; none of the tasks below has been implemented by preparing them. Recheck the actual files before starting, because another worker may have completed a task since this snapshot.

## Choose a task

| Brief | Difficulty | Prerequisites | Scope |
|---|---|---|---|
| [Task 5: biome rules](task-5-biome-rules.md) | Easiest | Existing World assembly | Two small production types and exhaustive mapping tests |
| [Task 6a: ClimateModel](task-6a-climate-model.md) | Easiest | Existing World assembly | One wrapper and synthetic-data tests; only part of original Task 6 |
| [Task 7: alignment gate](task-7-alignment-gate.md) | Moderate; review carefully | None of Tasks 5–6 | A small state machine with timing, hysteresis, and invalid-input tests |
| [Task 8: log formatting](task-8-session-log-format.md) | Easy once dependencies exist | Tasks 5 and 7 | A value type, deterministic JSON formatting, and locale/escaping tests |

Suggested order with one worker: **5 → 6a → 7 → 8**. Task 7 does not depend on the climate dataset, so original Task 6 can remain partially unfinished while Tasks 7–8 proceed. Task 6a does not need Task 5 either.

Use a cheaper coding model for Tasks 5 and 6a first. Task 7 has the most consequential edge cases and deserves an independent review before device integration. Reserve climate acquisition/GeoTIFF processing, rendering (Task 9), scene composition, cloud authentication, and device testing (Task 10) for a separate session.

## Prompts to give the worker

Start the worker in this repository with file-editing and shell access. Give it one prompt at a time:

```text
Read docs/handoffs/README.md and docs/handoffs/task-5-biome-rules.md.
Implement only the Task 5 brief. Use one worker, preserve existing changes,
run the available validation, and report the result using the shared format.
```

```text
Read docs/handoffs/README.md and docs/handoffs/task-6a-climate-model.md.
Implement only Task 6a, including its synthetic tests. Do not download data
or mark the full Task 6 complete. Report using the shared format.
```

```text
Read docs/handoffs/README.md and docs/handoffs/task-7-alignment-gate.md.
Implement only Task 7 with its specified state transitions and timing tests.
Use one worker and report using the shared format.
```

```text
Read docs/handoffs/README.md and docs/handoffs/task-8-session-log-format.md.
Check that Tasks 5 and 7 exist first, then implement only Task 8.
Use one worker and report using the shared format.
```

## Shared context and boundaries

- Repository: `/home/matt/Projects/Power Networks/Power-Networks`.
- Unity project: `ar-game/`; Editor `6000.3.27f1` (Unity 6.3 LTS).
- Existing production code: `ar-game/Assets/FantasyEarth/World/`.
- Existing tests: `ar-game/Assets/FantasyEarth/Tests/EditMode/World/`.
- Baseline: 14 World EditMode cases passed on 2026-10-25. This is historical evidence, not a test result for your changes. Task 4's previous independent review was interrupted by Copilot quota; these briefs do not close that review.
- Original task contracts: `docs/superpowers/plans/2026-10-24-fantasy-earth-plan-1-foundation.md`. The selected brief repeats the relevant contract. Read only its corresponding original task if more context is needed; do not start executing the whole plan.
- Use one worker for these bounded assignments. Do not launch nested agents or add tooling. Follow any higher-priority instructions in the worker's own environment.
- Use existing C# formatting and NUnit conventions. Production World, Fantasy, Positioning, and Diagnostics assemblies must have `noEngineReferences: true` and use no UnityEngine/UnityEditor APIs.
- Keep World independent: it references no other FantasyEarth assembly. Task 6a must not introduce a Fantasy dependency into World.
- Preserve every existing reference and setting in `FantasyEarth.Tests.EditMode.asmdef`. Add only the new assembly required by your task. Do not reference an assembly before creating it.
- Include valid `.meta` files for new files/folders under `Assets/`. Prefer Unity-generated metadata; if the Editor is unavailable, follow the neighboring file type's metadata with fresh, unique GUIDs. Never regenerate existing GUIDs.
- Edit only the selected brief's file list and its `.meta` files, plus a concise task report. Leave the original plan and historical progress ledger unchanged; report completion for the maintainer to reconcile.
- Preserve unrelated changes. Do not alter `power-networks/`, packages, Player/XR settings, scenes, render assets, Gradle templates, or existing World APIs.
- Existing setup decisions: minimum Android API **25**; EDM4U is **bundled with ARCore Extensions**, so do not add another EDM4U package/registry; iOS Build Support is installed because Extensions' editor code requires it. The committed Gradle template intentionally excludes generated resolver blocks.
- No cloud access, data download, paid service, secrets, device deployment, or new package is needed for these assignments.
- Follow the owner's current Git instructions. If committing, stage only task files, use the brief's suggested title, and include the original plan's trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`. Do not push as part of these briefs.

## Tools and validation

The implementation requires file editing, Git, and Unity EditMode testing. A direct Unity MCP, Desktop Commander, GitHub access, REA, and Superpowers are unnecessary for these tasks.

At preparation time, this Codex session had shell/file editing, web research, GitHub connector tools, and REA MCP tools exposed. `unity` CLI `1.0.0-beta.8`, `git`, `git-lfs`, and `adb` were found on PATH. `com.unity.pipeline` `0.9.1-exp.1` is installed in the project. GitHub and REA backends were not exercised during preparation. No direct Unity MCP or Desktop Commander tools were exposed. A different worker must check its own available tools.

The latest `unity status` probe returned `STATUS_NO_INSTANCES`. A worker must establish its own connection before claiming live validation. Do not install another bridge or reinstall Pipeline to address that result. MCP tools depend on the client's configured servers; see the [official OpenAI MCP documentation](https://learn.chatgpt.com/docs/extend/mcp?surface=cli).

1. Inspect `git status --short`, the selected files, and the existing test asmdef. Preserve any pre-existing changes.
2. Write the brief's meaningful behavior tests before implementation. If Unity is available, observe the expected initial failure, then implement and test. A missing-type compile failure is valid initial evidence; zero discovered tests is never a passing result.
3. Use the running Editor for this exact checkout. Quote the project path, which contains a space:

   ```bash
   PROJ="/home/matt/Projects/Power Networks/Power-Networks/ar-game"
   unity status --project-path "$PROJ" --format json
   ```

   If unavailable, report that explicitly. If the session permits opening this project, `unity open "$PROJ"` can start it; otherwise finish the source work and report that Unity validation is pending. Do not borrow an Editor attached to another checkout. If the owner confirms an Editor is open but discovery fails, report a possible sandbox/connection limitation instead of asserting that it is closed.
4. Ensure the Editor keeps ticking while unfocused. Discover the installed command with `unity command --query autotick --project-path "$PROJ" --format json` and enable its autotick option. Do not enter Play Mode for these tests.
5. Recompile and poll the status command until the recompile finishes; a successful trigger response alone is insufficient:

   ```bash
   unity command script recompile --project-path "$PROJ" --format json
   unity command script recompile_status --project-path "$PROJ" --format json
   ```

   Require no compile errors before treating a test run as valid. Discover command spelling through `unity command --query <term>` if these installed-package commands differ; do not upgrade tools speculatively.
6. Run the full FantasyEarth EditMode suite once on the final code, and obtain its terminal results:

   ```bash
   unity command test run --mode EditMode --filter FantasyEarth --project-path "$PROJ" --format json
   unity command test status --project-path "$PROJ" --format json
   ```

   Poll status if asynchronous. Record the final discovered/passed/failed/skipped counts. Require all existing and new cases to pass with zero failed. During implementation, narrow the filter to your fixture when useful; do not repeatedly rerun a passing suite on unchanged code.
7. Inspect `git diff --check`, `git diff --stat`, and `git status --short`. For new, untracked files, read their content too; they are absent from a normal `git diff`. Verify scope, asmdef references, and unique metadata GUIDs.

If Unity cannot be reached, deliver the implementation and report the exact failed command and error. Use **implemented, Unity validation pending**, never **complete/tested**. A standalone C# check can be supplemental evidence, but it does not prove Unity imports, asmdef wiring, or test discovery.

## Required completion report

Write `docs/handoffs/reports/task-<number>-report.md` (use `task-6a-report.md` for the partial task), then give the owner a concise summary:

- Status: `implemented and validated`, `implemented, Unity validation pending`, or `blocked by missing prerequisite`.
- Files changed and any implementation decision beyond the brief.
- Exact validation commands, their final outcomes, and test counts; identify historical evidence separately.
- Any remaining issue, unrelated diff, or skipped requirement.
- Commit hash if committed, otherwise state that the changes are uncommitted.

For Task 6a, always say that **original Task 6 remains partial** until the dataset tool, asset, attribution, and real-location tests are finished.
