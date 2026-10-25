# Fantasy Earth: small implementation handoffs

Prepared 2026-10-25 from commit `d1fec0c`. These briefs are implementation instructions; none of the tasks below has been implemented by preparing them. Recheck the actual files before starting, because another worker may have completed a task since this snapshot.

**Completion update, 2026-10-25:** All four briefs below are complete. Task 5 is in `a566ec9`; full Task 6 (including 6a) is in `e7fb32b`; Task 7 is in `da14188`; Task 8 is in `649bea8`. The complete suite has **108 passing Unity EditMode cases**. See the [completion report](reports/completion-through-task-8.md), [Task 5 review](reports/task-5-review.md), and [verified Unity workflow](../UNITY_WORKFLOW.md). The next work is original Plan 1 Task 9, followed by Task 10; do not redispatch these completed briefs.

## Completed task briefs

**Latest development checkpoint:** Tasks 9 and 10's code, the camera dependency
repair, reusable building generation, and the offline building-data/matching
slice are implemented. The latest suite has 175 passing Unity cases and eight
passing importer cases. Plan 1 device acceptance is now complete: the outdoor
follow-up recorded 82 Locked samples, first lock at 74.906 seconds and locked
medians 0.88 m / 1.36°. A first live Streetscape placement build with temporary
session identities is now implemented and installed; outdoor physical placement
validation remains. Shared seeded ground-prop generation, biome meshes and
camera/map visibility are also implemented and previewed offline; terrain
anchoring and World wiring remain. Stable map integration and the map view follow.
See the [live-placement report](reports/live-building-placement.md) and
[data/matching report](reports/building-data-matching.md), plus the
[ground-prop report](reports/ground-props.md); do not redispatch these historical briefs.

| Brief | Difficulty | Prerequisites | Scope |
|---|---|---|---|
| [Task 5: biome rules](task-5-biome-rules.md) | Easiest | Existing World assembly | Two small production types and exhaustive mapping tests |
| [Task 6a: ClimateModel](task-6a-climate-model.md) | Easiest | Existing World assembly | One wrapper and synthetic-data tests; only part of original Task 6 |
| [Task 7: alignment gate](task-7-alignment-gate.md) | Moderate; review carefully | None of Tasks 5–6 | A small state machine with timing, hysteresis, and invalid-input tests |
| [Task 8: log formatting](task-8-session-log-format.md) | Easy once dependencies exist | Tasks 5 and 7 | A value type, deterministic JSON formatting, and locale/escaping tests |

The completed implementation order was **6 → 7 → 8**, following the inherited Task 5. Task 7 does not depend on the climate dataset, so original Task 6 can remain partially unfinished while Tasks 7–8 proceed. Task 6a does not need Task 5 either.

Tasks 5 and 6a were the simplest bounded assignments. Task 7 has the most consequential edge cases and was included in final review before device integration. Reserve climate acquisition/GeoTIFF processing, rendering (Task 9), scene composition, cloud authentication, and device testing (Task 10) for a separate session.

## Prompts to give the worker

These prompts are preserved as historical examples of the completed handoffs. Do not dispatch them for new implementation; the corresponding types and tests already exist:

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
- Baseline: 14 World EditMode cases passed on 2026-10-25. This is historical evidence, not a test result for your changes. Task 4's previous independent review was interrupted by Copilot quota; its 14 cases are included in the latest 108-case passing suite.
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

The preparation-time `unity status` probe returned `STATUS_NO_INSTANCES`. During takeover, opening the Editor and using approved host access established the existing Pipeline connection and enabled the passing takeover and subsequent 108-case runs. Sandboxed discovery alone can still return no instances. See [the verified workflow](../UNITY_WORKFLOW.md); do not install another bridge or reinstall Pipeline to address this discovery issue. MCP tools depend on the client's configured servers; see the [official OpenAI MCP documentation](https://learn.chatgpt.com/docs/extend/mcp?surface=cli).

1. Inspect `git status --short`, the selected files, and the existing test asmdef. Preserve any pre-existing changes.
2. Write the brief's meaningful behavior tests before implementation. If Unity is available, observe the expected initial failure, then implement and test. A missing-type compile failure is valid initial evidence; zero discovered tests is never a passing result.
3. Use the running Editor for this exact checkout. Quote the project path, which contains a space:

   ```bash
   PROJ="/home/matt/Projects/Power Networks/Power-Networks/ar-game"
   unity status --project-path "$PROJ" --format json
   ```

   If unavailable, report that explicitly. If the session permits opening this project, `unity open "$PROJ"` can start it; otherwise finish the source work and report that Unity validation is pending. Do not borrow an Editor attached to another checkout. If the owner confirms an Editor is open but discovery fails, report a possible sandbox/connection limitation instead of asserting that it is closed.
4. Ensure the Editor keeps ticking while unfocused. The verified command is `unity command editor set_auto_tick --enable true --interval_ms 16 --project-path "$PROJ" --format json`; discover it with `unity command --query tick` if needed. Searching for `autotick` does not match this version's command name. Do not enter Play Mode for these tests.
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
