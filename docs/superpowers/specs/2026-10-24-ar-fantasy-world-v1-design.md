# Fantasy Earth AR: first version design

Date: 2026-10-24
Status: approved 2026-10-24

## Intent

An open-world fantasy RPG layered over the real Earth, played on an Android phone from any street corner. The long-term game includes scanning real objects into fantasy artifacts, monsters, combat, a player economy, factions, and live real-world feeds. Everything starts from one experience: **seeing your real surroundings as a fantasy world**. If that doesn't feel magical and believable, nothing built on top of it will matter.

This first version proves that experience and nothing else. The goal is to release to the public eventually, with strangers downloading it from the Play Store. The first version is for the owner and a handful of testers.

## What the first version does

The app has two views and a swipe between them. The interface is otherwise almost invisible: no HUD, menus, or buttons beyond what's strictly required (permissions, attribution, a swipe handle).

### 1. Camera view (opens first)

- The app opens straight into the live camera.
- **Rebuilt mode ("C")**: where Google's visual positioning has a confident lock, real buildings are covered by fantasy structures (stone towers, keeps, timber halls, crenellations, roofs, banners) that sit exactly on the real buildings' shapes. The ground is restyled as cobblestone, grass, or dirt to match.
- **Painted mode ("B")**: where there's no lock, while still locating, or where Google has no 3D data, the real camera image is shown with a storybook-painting grade (warm color grading, mist, drifting light motes, glowing lamps).
- Decorative props (trees, rocks, rune stones, and the like) are generated from a fixed seed tied to their real-world location. Two people on the same corner see the same props in the same places. Props appear in the camera view only in rebuilt mode; see "Calls I made".
- The fantasy look follows the real climate. There are a few biomes (for example temperate, desert, snowy, tropical), chosen from real climate data for the player's location.

### 2. Map view (swipe up from the camera; swipe down to return)

- A tilted, low-angle 3D fantasy rendering of the player's real neighbourhood, Pokémon GO-style. It is not a flat map and not Google Maps.
- It's built from open map data. Real roads become paths, parks and green areas become woods, water becomes rivers and lakes, and buildings become fantasy structures on their real footprints.
- The player's avatar (one default fantasy character) stands at the player's real position and turns with the phone's heading.
- It uses the same seeded props and climate biome as the camera view, so a tree on the map is the same tree you see through the camera.

## Quality bar (in priority order)

1. **No wobble or sliding.** Fantasy buildings drifting off, jittering on, or sliding across real buildings is the worst possible failure, worse than showing less. Rebuilt mode appears only when positioning accuracy is good enough that the alignment holds. Otherwise the app stays in painted mode. Switching between modes is a smooth blend with hysteresis, never flicker.
2. It must look like a world, not a camera filter.
3. Reasonable start-up time, heat, and battery use on the test phone.

## Success looks like

On a Samsung Galaxy S25, at a street corner in an area Google's positioning covers:

- Within a short wait, the camera view switches to rebuilt mode. Walking around the block and panning the phone, the fantasy buildings stay put on the real ones.
- In an area with no coverage, or indoors, the app shows painted mode cleanly and never shows misaligned buildings.
- Swiping up shows the 3D fantasy neighbourhood with the avatar in the right place, facing the right way. Swiping down returns to the camera.
- Two phones on the same corner show the same props and the same fantasy buildings in the same places.
- The biome visibly differs between climates, which can be tested by spoofing location.

## Non-goals for the first version

Scanning, items, inventory, monsters, combat, skill trees, the economy and auction house, factions, other players (visible or not), accounts, a game server, live real-world feeds, avatar customization, iOS.

## Anti-goals (failure even if it "works")

- Misaligned or drifting fantasy buildings (see the quality bar).
- An app that reads as a camera filter app.
- Anything that makes the later game harder: the first version must not be built in a way the vision below can't grow from.

## Long-term vision the first version must not block

These come from the owner's design notes and this session. They are not built now, but the architecture must leave room for them.

- **Two layers.** A canonical real-world model (geography, buildings, climate, and later live feeds, each observation carrying source, timestamp, and confidence), plus a separate fantasy interpretation layer that turns that model into what's shown. The first version builds this split from day one, even with only a few data sources.
- Deterministic, seeded procedural generation for anything not observed. Static data is generated once and cached; dynamic data updates selectively.
- The fantasy world mirrors real geography: climate drives biome, and real density drives the density of the fantasy world.
- Later features:
  - Scanning real objects (costs resources) into fantasy items with generated stats and abilities. Generic items can be identical; spending extra makes a rare scan unique, and the more novel an item, the more special it can be, within balance limits. Unique items enter the shared world.
  - Monsters visible in both views, spawning by geography.
  - Combat, loot, skill trees, trading and bartering, and a global auction house.
  - Player-created factions.
  - One shared Earth with geographically partitioned servers, starting from one server.
- **Social rules:**
  - Every action needs physical proximity, with no fast travel and no remote interaction. The global auction house is the one exception.
  - No voice and no free-form text; only preset messages and in-game interactions.
  - No territory claims and no player-built permanent structures.
  - Each player chooses whether other players can see them.

## Decisions made (by the owner, in conversation)

- Platform: Android first. Test device: Samsung Galaxy S25.
- Budget: small. Roughly under $100/month for services (map tiles, Google Cloud, and similar), plus a few one-off asset purchases such as Unity Asset Store art packs, where they clearly save time or improve the look.
- Field testing: the owner tests on their S25, on streets near home that have Google Street View coverage, so Geospatial positioning can lock on there. On-device logging that helps diagnose alignment problems after a walk is worth building.
- Engine: Unity. The project must move from Unity 6000.6.0f1 to a **Unity 6 LTS** release, because ARCore Extensions' bundled `Google.Protobuf.dll` breaks script compilation on 6000.6.0f1.
- Positioning: **Google ARCore Geospatial API** (via ARCore Extensions for AR Foundation). Its building/terrain geometry (Streetscape Geometry) is the source of building shapes for rebuilt mode.
- Camera look: rebuilt mode where 3D data and a confident lock exist; painted mode everywhere else.
- Map look: tilted 3D fantasy world with an avatar, from **open map data** (OpenStreetMap / Overture Maps), not the Google Maps SDK. Google's role is positioning and camera-view building shapes.
- First version scope: the camera view and map view only (see Non-goals).
- The anatomy repo (`wheresryan22/anatomy`) is not used.
- Repository: the game lives in the `mtp354/Power-Networks` repo. The existing Power Networks code (`src/`, `data/`, its `docs/` files, `ReadMe.txt`, `requirements.txt`) moves into a `power-networks/` subfolder.

## Calls I made (the owner can override)

- The Unity project goes in the repo at `ar-game/`, moved from its current location at `~/Projects/Power Networks/AR-Game`. It's tracked with git and Git LFS for large binary assets, with a Unity `.gitignore` (excluding `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and `.superpowers/`).
- Design docs live at the repo root in `docs/superpowers/`. Only the Power Networks files in `docs/` move to `power-networks/docs/`.
- No backend in the first version. Everything runs on the phone, using Google's ARCore service and open map data.
- Props must match between the map and the camera. Both views draw from the same seeded generator keyed on real-world location.
- Biome data comes from a bundled global climate classification map (for example Köppen-Geiger), so it works offline.
- First-version distribution is sideloading plus the Play Store internal testing track. A public listing comes later.
- In rebuilt mode, anything that's not a building or the ground (people, cars, sky) stays as the camera image with the painted grade applied, so it blends in.
- **Each real building looks the same everywhere.** A building's fantasy style is keyed on a stable building ID from the open map data (for example Overture's stable GERS IDs), so it looks identical in both views, on every phone, in every session. In the camera view, each Streetscape Geometry mesh is matched to its map footprint by overlap. Meshes with no match get a style seeded from their location. This keeps the world consistent and leaves room for the shared world later.
- **Ground-anchored props appear in the camera view only with a confident lock.** In painted mode, props would drift at GPS/compass accuracy, which breaks the no-wobble bar. Painted mode carries its "world, not filter" feel with effects that don't need anchoring: the grade, mist, drifting motes, glowing lamps, and sky effects. The map view always shows props.

## Left to the builder

- Which Unity 6 LTS patch release, as long as ARCore Extensions compiles cleanly and AR Foundation 6 works.
- Exact accuracy thresholds and the blend and hysteresis behavior for switching between rebuilt and painted mode, tuned on the S25 against the "no wobble" bar.
- How fantasy buildings are generated from the rough building meshes (re-skinning, procedural roofs and decorations, and so on).
- The open map data source and delivery (self-hosted vector tiles, a hosted tile provider, or direct Overture/OSM extracts), and the Unity rendering approach for the map view, within the budget above and licensing that allows commercial use (attribution and ODbL obligations met).
- Seed scheme (for example a global world seed plus a geographic cell ID), the prop set, and density rules.
- Shader and art approach for the painted grade and fantasy materials, within the "world, not filter" bar.
- Google Cloud project setup and authentication for the ARCore API, plus checking current quota and pricing terms before testers use it.
- Test strategy: unit tests for pure logic (seeding, biome lookup, mode-switch state machine, data normalization), and on-device checks for AR alignment.

## Current state of the environment

- The Unity project currently sits at `~/Projects/Power Networks/AR-Game`. It was created from the AR Mobile template on Unity 6000.6.0f1 and includes AR Foundation, ARCore XR, ARKit XR, URP, XR Interaction Toolkit, Input System, and the Unity Pipeline package. ARCore Extensions was added, then removed because of the Protobuf compile conflict. The project compiles cleanly without it.
- Android build support (SDK, NDK, and JDK) is installed for 6000.6.0f1 only. Moving to a 6 LTS editor requires installing that editor and its Android module.
- The `unity` CLI and Pipeline package can drive the editor (recompile, console, packages). `adb` is on PATH. git-lfs is installed.
