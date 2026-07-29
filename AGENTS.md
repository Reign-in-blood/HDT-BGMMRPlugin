# AGENTS.md — BGMMRPlugin

## 1. Purpose and scope

This file contains the permanent development instructions for the repository:

```text
BGMMRPlugin
```

It applies to the entire repository unless a more specific `AGENTS.md` file is later added inside a subdirectory.

Before changing any file:

1. read this file completely;
2. inspect the relevant source files;
3. understand the current behavior and known limitations;
4. make the smallest coherent change;
5. review the complete diff;
6. compile and test when the environment allows it;
7. clearly report what was and was not verified.

Direct instructions from the user for a specific task take priority over this file.

---

## 2. Project identity — do not confuse this repository

This repository is **BGMMRPlugin**.

It is a Windows plugin for **Hearthstone Deck Tracker (HDT)**, designed specifically for **Hearthstone Battlegrounds**.

The plugin displayed inside HDT is currently named:

```text
BGMMRPlugin
```

Its purpose is to augment the in-game Battlegrounds leaderboard with compact real-time information.

Current features include:

- displaying the names of the eight lobby players;
- displaying an exact public Battlegrounds MMR when available;
- displaying `< 8000` when an exact public MMR is unavailable;
- following `PLAYER_LEADERBOARD_PLACE` when players move in the leaderboard;
- displaying the local player name in green;
- displaying the next or current opponent name in red;
- shifting the next or current opponent block 30 reference pixels to the right;
- displaying each player's live Tavern Tier;
- displaying a marker for the last completed opponent;
- dimming dead players while keeping them attached to their final leaderboard position;
- supporting both normal Battlegrounds and Duos leaderboard data.

### This repository is not HDT-FinalStatsPlugin

Never confuse this project with `HDT-FinalStatsPlugin`.

This project does **not** primarily:

- collect cumulative match statistics;
- count gold spent, Tavern refreshes, purchases, or played cards;
- calculate combat wins, losses, or damage totals;
- display a final match summary;
- save final-board screenshots;
- maintain local match history.

Those features belong to another plugin.

### This repository is not BoardStatsPlugin

Never assume this project is `BoardStatsPlugin`.

This plugin is attached to the Battlegrounds player leaderboard. It is not primarily a board-layout or minion-statistics plugin.

When writing documentation, commit messages, comments, release notes, or code, always refer to the correct project:

```text
Repository: BGMMRPlugin
Plugin concept: Battlegrounds leaderboard MMR and status overlay
HDT display name: BGMMRPlugin
Canonical DLL: HDT-BGMMRPlugin.dll
```

---

## 3. Canonical DLL, project, and release names

The canonical compiled plugin filename is:

```text
HDT-BGMMRPlugin.dll
```

This name must be used consistently in:

- the `.csproj` assembly output;
- `Build.bat`;
- the `bin` build result;
- the `dist` copy;
- installation instructions;
- README documentation;
- release archives;
- release notes;
- GitHub releases;
- WPF pack-resource assembly URIs;
- HTTP user-agent metadata;
- diagnostic messages referring to the distributed DLL.

Do not distribute the former names:

```text
BGMmrOverlayPlugin.dll
BGMMRPlugin.dll
```

### Required project configuration

The project file must contain:

```xml
<RootNamespace>BGMMRPlugin</RootNamespace>
<AssemblyName>HDT-BGMMRPlugin</AssemblyName>
```

Current source namespace:

```csharp
namespace BGMMRPlugin
```

Expected Release build output:

```text
bin\Release\HDT-BGMMRPlugin.dll
```

Expected distribution output:

```text
dist\HDT-BGMMRPlugin.dll
```

`Build.bat` must:

1. compile the project;
2. verify that `bin\Release\HDT-BGMMRPlugin.dll` exists;
3. copy it to `dist\HDT-BGMMRPlugin.dll`;
4. report that exact path to the user.

After any naming, project-file, resource, or build-script change, search the repository for stale references to:

```text
BGMmrOverlayPlugin
BGMmrOverlayPlugin.dll
BGMmrOverlayPlugin.csproj
BGMmrOverlayPlugin.sln
BGMMRPlugin.dll
```

Any remaining occurrence must be reviewed and either updated or intentionally documented.

---

## 4. Current repository structure

Current primary files:

```text
AGENTS.md
BGMMRPlugin.sln
BGMMRPlugin.csproj
Plugin.cs
Build.bat
find_hdt_assembly.ps1
README.md
CHANGELOG.md
LICENSE.txt
THIRD_PARTY_NOTICES.txt
Assets/
Game/
Services/
UI/
Util/
lib/
dist/
```

### `Plugin.cs`

This file contains the HDT `IPlugin` implementation and coordinates the plugin lifecycle.

Current responsibilities include:

- plugin metadata;
- `OnLoad`, `OnUnload`, `OnButtonPress`, and `OnUpdate`;
- enabling and disabling the overlay;
- resolving a new Battlegrounds lobby;
- starting the public leaderboard download;
- completing the asynchronous leaderboard load;
- building the eight display slots;
- tracking the current or next opponent;
- tracking the last completed opponent;
- resetting match state;
- dispatching WPF updates to the HDT overlay thread.

Do not move unrelated logic into this file.

### `Game/LobbyPlayer.cs`

Contains the current lobby and display models, including fields such as:

```text
Name
PlayerId
HeroCardId
LeaderboardPlace
TavernTier
IsLocalPlayer
IsCurrentOpponent
IsLastOpponent
IsDead
RatingText
```

Keep the distinction between:

- persistent lobby identity;
- current leaderboard slot;
- transient display state.

### `Game/LobbyTracker.cs`

Resolves and maintains lobby-player identity.

Current responsibilities include:

- resolving `BattlegroundsLobbyInfo` metadata;
- associating player names with player IDs;
- ignoring fake or placeholder players where detectable;
- scanning only new `Power.log` lines;
- attaching authoritative hero entities;
- updating `PLAYER_LEADERBOARD_PLACE`;
- updating `PLAYER_TECH_LEVEL`;
- preserving the last valid Tavern Tier during brief entity replacement states;
- latching player death;
- refreshing hero card IDs.

This logic is fragile because Hearthstone may replace hero entities during a match. Preserve the existing fallbacks unless evidence supports a targeted change.

### `Services/OfficialBoardClient.cs`

Downloads and parses public Battlegrounds leaderboard data.

Current behavior includes:

- primary source: `https://bgrank.fly.dev`;
- fallback mirror: the configured GitHub raw mirror;
- in-memory cache duration: 15 minutes;
- offline cache maximum age: 48 hours;
- exact-case name lookup first;
- case-insensitive lookup second;
- preserving the first duplicate name from a leaderboard sorted by descending rating;
- separate solo and Duos board keys;
- regions: US, EU, AP, and CN.

Network or parse failure must not crash HDT.

### `Services/HoveredPlayerNameReader.cs`

Provides a narrow manual-hover compatibility fallback when
`BattlegroundsLobbyInfo` contains one opponent with no usable name.

Rules:

- use it only for a player still represented by the `...` placeholder;
- require the user to hover the corresponding native Hearthstone leaderboard
  portrait;
- associate the hovered entity with the placeholder by `PLAYER_ID`;
- never replace an already known player name;
- never simulate mouse movement or clicks;
- never log the recovered name or account identifier;
- dispose ScryDotNet objects when the plugin unloads;
- fail safely and leave `...` visible when the UI value cannot be read.

### `UI/PlayerMmrOverlay.cs`

Creates and updates the WPF elements attached to:

```csharp
Core.OverlayCanvas
```

Current responsibilities include:

- eight player label containers;
- player-name and MMR text;
- Tavern Tier images;
- last-opponent images;
- color and opacity rules;
- current-opponent horizontal offset;
- loading embedded PNG assets through WPF pack URIs;
- scaling and positioning all elements.

The current resolution-scaling implementation has a known compatibility limitation documented later in this file.

### `Util/PluginLogger.cs`

Provides local plugin logging and cache-directory paths.

Logging failures must never crash HDT.

### `BGMMRPlugin.csproj`

Current technical target:

```text
TargetFramework: net472
OutputType: Library
UseWPF: true
PlatformTarget: x64
Platforms: x64
LangVersion: 10
Nullable: disable
```

Local HDT references:

```text
lib\HearthstoneDeckTracker.exe
lib\HearthDb.dll
lib\HearthMirror.dll
lib\untapped-scry-dotnet.dll
```

Embedded assets:

```xml
<None Remove="Assets\*.png" />
<Resource Include="Assets\*.png" />
```

These local build dependencies and generated outputs must not be committed.

### `Build.bat`

The Windows build script:

1. accepts an HDT installation directory or `HearthstoneDeckTracker.exe` path;
2. checks common HDT installation locations through environment variables;
3. invokes `find_hdt_assembly.ps1`;
4. locates the real managed HDT assembly;
5. copies `HearthstoneDeckTracker.exe`, `HearthDb.dll`, `HearthMirror.dll`,
   and `untapped-scry-dotnet.dll` to `lib`;
6. locates MSBuild;
7. compiles `Release|x64`;
8. copies the final DLL to `dist`.

It must never contain a developer's personal absolute path.

### `find_hdt_assembly.ps1`

Searches for the real managed:

```text
HearthstoneDeckTracker.exe
```

It must avoid selecting a non-managed launcher or unrelated executable.

### `README.md`

Public GitHub documentation.

It must describe the actual plugin behavior and use:

```text
HDT-BGMMRPlugin.dll
```

in build and installation instructions.

---

## 5. Technical constraints

Required platform:

- Windows;
- .NET Framework 4.7.2;
- WPF;
- x64;
- C# 10;
- Hearthstone Deck Tracker plugin API;
- HearthDb;
- HearthMirror;
- the `untapped-scry-dotnet` runtime already distributed with HDT;
- `System.Net.Http`.

Do not migrate the project to:

- .NET 6, 7, 8, 9, or later;
- WinUI;
- Avalonia;
- Electron;
- a standalone desktop application;
- a Windows service;

unless the user explicitly requests and approves that architectural change.

Do not add a NuGet dependency unless it is clearly necessary and approved.

Prefer framework APIs and existing HDT APIs.

Unlike FinalStatsPlugin, this plugin currently requires Internet access for fresh exact public MMR data. It must still degrade safely when offline by using valid cached data or displaying the existing fallback value.

The plugin must never modify the user's HDT installation beyond normal plugin loading and local build-dependency copying performed by `Build.bat`.

Never commit:

- personal absolute paths;
- Windows usernames;
- access tokens;
- private identifiers;
- local HDT binaries;
- generated DLLs;
- PDB files;
- personal logs;
- cached leaderboard files;
- temporary files.

---

## 6. User communication

The main user is French-speaking and is a beginner in C#/.NET development.

Source code, identifiers, technical comments, commit messages, and this file should normally be written in English.

Reports to the user should normally be written in French.

When reporting work:

- explain exactly what changed;
- avoid unnecessary jargon;
- give exact commands when manual action is required;
- distinguish between static review, compilation, and in-game testing;
- never claim that a build succeeded unless it was actually run successfully;
- never claim that overlay alignment is fixed without testing the affected setup;
- clearly list remaining manual tests;
- explain errors in a way a beginner can follow.

Do not provide only isolated snippets when the task requires a complete repository change. Modify the appropriate files coherently.

---

## 7. Build instructions

Preferred Windows build command from the repository root:

```bat
Build.bat "PATH_TO_HDT_INSTALLATION"
```

The argument may also be the full path to:

```text
HearthstoneDeckTracker.exe
```

Interactive execution is allowed:

```bat
Build.bat
```

The script should automatically check generic locations such as:

```text
%LOCALAPPDATA%\HearthstoneDeckTracker
%ProgramFiles%\Hearthstone Deck Tracker
%ProgramFiles(x86)%\Hearthstone Deck Tracker
```

These are Windows environment variables, not personal paths.

Expected final artifact:

```text
dist\HDT-BGMMRPlugin.dll
```

Before building, ensure that the build script can obtain:

```text
lib\HearthstoneDeckTracker.exe
lib\HearthDb.dll
lib\HearthMirror.dll
lib\untapped-scry-dotnet.dll
```

### Build failure procedure

When compilation fails:

1. read the complete MSBuild output;
2. identify the first real compiler or project error;
3. fix that error rather than hiding it;
4. rebuild;
5. verify the exact DLL path;
6. report the real result.

Do not suppress errors simply to produce a file.

If the environment does not have Windows, MSBuild, Visual Studio Build Tools, or the HDT assemblies:

- run all possible static checks;
- inspect the diff carefully;
- state explicitly that the HDT build was not executed;
- do not fabricate a successful build result.

---

## 8. General code-change rules

### 8.1 Prefer small changes

Make changes that are:

- focused;
- isolated;
- reviewable;
- reversible;
- testable.

Do not combine an unrelated refactor with a layout fix or tracking fix.

### 8.2 Protect stable behavior

A task involving one feature must not silently change another feature.

Examples:

- a resolution fix must not change MMR lookup;
- a Tavern Tier fix must not change opponent tracking;
- a last-opponent fix must not change leaderboard ordering;
- a build-script fix must not change the runtime assembly namespace;
- a visual change must not change player identity matching.

### 8.3 Avoid unsafe global replacements

Never use broad text replacement without reviewing every result.

Before renaming a method, field, class, namespace, resource URI, or output filename:

1. search all occurrences;
2. inspect declarations and call sites;
3. change only intended references;
4. review the final diff;
5. search for stale and duplicated names.

WPF pack URIs must match the compiled assembly name exactly.

### 8.4 Preserve readability

Use:

- 4-space indentation;
- braces on separate lines;
- `PascalCase` for types and methods;
- `_camelCase` for private fields;
- clear local variable names;
- explicit state transitions;
- short useful comments;
- `CultureInfo.InvariantCulture` for stable technical numeric formats when applicable.

Do not introduce `dynamic` unless there is no reasonable typed alternative.

### 8.5 Error handling and performance

The plugin must not crash HDT.

For frequent update paths:

- catch exceptions at an appropriate boundary;
- log useful context;
- do not throw intentionally from `OnUpdate()`;
- avoid empty `catch` blocks except for a final shutdown fallback where HDT may already be closing;
- do not perform slow blocking network or disk work every update;
- keep the current asynchronous leaderboard-loading behavior;
- preserve the current update throttle unless a measured problem requires changing it.

`OnUpdate()` currently limits its main refresh work to approximately once every 250 milliseconds. Do not remove this throttle casually.

---

## 9. HDT lifecycle and match state

Current lifecycle methods:

```text
OnLoad
OnUnload
OnButtonPress
OnUpdate
```

Important state fields include:

```text
_trackedOpponentPlayerId
_combatOpponentPlayerId
_lastOpponentPlayerId
_pluginEnabled
_wasInMatch
_wasCombatPhase
_nextUpdateAt
```

Rules:

- create and attach the WPF overlay once during plugin loading;
- dispose the HTTP client during unloading;
- hide the overlay outside a running Battlegrounds match;
- reset match-specific state when leaving the match;
- avoid carrying the last opponent into the next lobby;
- do not start multiple leaderboard download tasks for the same lobby;
- keep lobby identification stable through transient entity replacements;
- make reset operations idempotent;
- do not recreate all WPF controls every update;
- preserve the user's Show/Hide toggle during the active match unless explicitly changing that behavior.

Required lifecycle scenarios:

1. HDT starts in the menu;
2. plugin is enabled;
3. a normal Battlegrounds match begins;
4. a Duos Battlegrounds match begins;
5. lobby metadata arrives late;
6. exact leaderboard data loads successfully;
7. network access fails;
8. cached data is available;
9. no valid cached data is available;
10. multiple Tavern and combat phases occur;
11. the match ends;
12. the game returns to the menu;
13. a new match begins without restarting HDT;
14. the plugin is hidden and shown again;
15. the plugin is disabled or unloaded;
16. HDT closes while the overlay dispatcher is shutting down.

---

## 10. Lobby and player identity rules

Player identity is central to every displayed feature.

Do not rely only on the current leaderboard slot because leaderboard positions move.

Preferred identity signals include:

```text
BattlegroundsLobbyInfo
GameUuid
PlayerId
player name without BattleTag suffix
HeroCardId
PLAYER_ID
PLAYER_LEADERBOARD_PLACE
```

Rules:

- keep one persistent `LobbyPlayer` object per resolved player;
- treat `LeaderboardPlace` as a changing display position;
- ignore invalid places outside 1 through 8;
- tolerate temporary duplicate places while Hearthstone replaces entities;
- keep the first authoritative slot for one update tick, as the current code does;
- do not match players solely by hero card ID because duplicate heroes or transformations may exist;
- strip the `#1234` BattleTag suffix before public leaderboard lookup;
- handle duplicate visible player names deterministically;
- do not expose full BattleTags or private account identifiers unnecessarily;
- preserve fake-player filtering used for ghost or placeholder entities;
- do not rescan the entire `Power.log` on every update.

When changing player resolution, add targeted diagnostics and test a complete match with moving leaderboard positions.

---

## 11. Public MMR semantics

Do not change the meaning of the displayed MMR without an explicit task.

Current intended behavior:

- an exact public rating is shown only when the visible player name is found in the public leaderboard data;
- exact-case lookup is attempted first;
- a case-insensitive fallback is attempted second;
- when the same visible name appears more than once, the established parser retains the first entry from the descending leaderboard source;
- when no exact public rating is available, display:

```text
< 8000
```

Do not:

- invent an exact value below the public leaderboard threshold;
- estimate MMR from placement, hero, combat results, or prior matches;
- show stale cached data older than the configured maximum age as if it were fresh;
- block the HDT UI while downloading the leaderboard;
- perform one Blizzard request per player;
- silently upload lobby or user data.

### Data-source resilience

Current fetch order:

1. BGrank primary endpoint;
2. configured GitHub raw mirror;
3. valid local offline cache;
4. stale in-memory board from the current session when appropriate;
5. normal `< 8000` fallback when no exact board is available.

Any future source change must update:

- `OfficialBoardClient.cs`;
- `README.md`;
- `THIRD_PARTY_NOTICES.txt` when required;
- privacy and network behavior documentation;
- tests for parse format and failure behavior.

Do not add an untrusted remote executable, script, or binary dependency.

---

## 12. Current-opponent tracking

The current or next opponent is displayed with:

- red player-name text;
- a horizontal shift of 30 reference pixels to the right;
- all attached elements following the same shifted player block.

The primary signal is the Battlegrounds next-opponent player ID exposed by HDT or Hearthstone entities.

Rules:

- track the player by `PlayerId`, not by the current leaderboard slot;
- preserve the opponent ID through combat and Tavern transitions when the direct signal briefly disappears;
- apply the red state to only one authoritative player;
- do not permanently mark an opponent red after the target changes;
- keep local-player green lower priority than current-opponent red, as in the current display rules;
- keep the 30-pixel value in 1920 × 1080 reference coordinates and scale it with the overlay.

Required tests:

- next opponent selected in Tavern phase;
- same opponent during combat;
- opponent changes after combat;
- ghost opponent;
- opponent dies;
- leaderboard order changes;
- local player becomes visually aligned with different slots;
- Duos match.

---

## 13. Last-completed-opponent tracking

The last-completed-opponent marker uses:

```text
Assets\Last.PNG
```

Current intended behavior:

- remember the opponent during combat;
- promote that player to `lastOpponentPlayerId` only when the combat phase ends;
- display no last-opponent marker before the first combat has completed;
- keep the marker on that player until the next combat completes;
- follow the player's changing `PLAYER_LEADERBOARD_PLACE`;
- remain independent from the red current-opponent state;
- remain visible for a dead player with reduced opacity;
- reset at the next match.

Visual behavior:

- `Last.PNG` uses a 35 × 35 reference-pixel display area;
- it is horizontally aligned with the Tavern Tier icon;
- it is placed directly below the Tavern Tier icon;
- vertical gap: 0 reference pixels.

Do not update the last opponent at combat start. It must represent a completed combat.

Required tests:

- before first combat;
- after first combat;
- after a draw;
- after a ghost combat;
- after fighting the same player twice;
- after the marked player moves in the leaderboard;
- after the marked player dies;
- when the current opponent is also the last opponent;
- new match reset.

---

## 14. Tavern Tier tracking

Tavern Tier is primarily derived from:

```text
GameTag.PLAYER_TECH_LEVEL
```

Current behavior:

- check the authoritative hero entity first;
- fall back to another entity with the same `PLAYER_ID` and a valid `PLAYER_TECH_LEVEL`;
- accept values 1 through 7;
- preserve the last valid value when Hearthstone briefly reports zero during entity replacement;
- load `T1.png` through `T7.png` from embedded resources;
- hide only the missing or invalid icon rather than crashing the plugin.

Do not infer a Tavern Tier from turn number, hero level, board strength, or visual recognition.

Required tests:

- every Tavern Tier from 1 through 6;
- Tier 7 when available in the current Battlegrounds ruleset;
- upgrade during Tavern phase;
- leaderboard reorder;
- player death;
- hero transformation or entity replacement;
- missing PNG asset;
- Duos match.

---

## 15. WPF overlay behavior

Current reference visual values:

```text
Slot count: 8
Reference resolution: 1920 × 1080
Label width: 90 px
Label height: 28 px
Current-opponent horizontal offset: 30 px
Tavern icon height: 35 px
Tavern icon source aspect ratio: 129 × 134
MMR-to-Tavern horizontal gap: 0 px
Last-opponent icon area: 35 × 35 px
Tavern-to-Last vertical gap: 0 px
Dead-player opacity: 0.65
```

Current calibrated slot coordinates:

```text
X: 255.00, 252.14, 249.29, 246.43, 243.57, 240.71, 237.86, 235.00
Y: 168, 260, 355, 445, 540, 633, 727, 822
```

Current colors:

- normal player name: near-white;
- local player: green;
- current opponent: red;
- MMR: gold/orange;
- dead player: gray.

Rules:

- create and update WPF controls through `Core.OverlayCanvas.Dispatcher`;
- do not update WPF controls from a non-UI thread;
- keep all overlay elements non-interactive;
- keep `IsHitTestVisible = false` for images and containers unless an explicit interactive feature is approved;
- never block Hearthstone clicks;
- attach controls once and update their values and positions;
- detach all controls cleanly when unloading;
- preserve high-quality bitmap scaling;
- missing assets must disable only the affected icon;
- all player-related elements must move together when the leaderboard slot or opponent offset changes.

---

## 16. Resolution compatibility — current known issue

The current v1.0.0 layout implementation treats the usable Hearthstone content area as a centered **16:9** rectangle:

```text
contentWidth  = min(overlayWidth, overlayHeight × 16/9)
contentHeight = min(overlayHeight, contentWidth × 9/16)
```

It then maps the 1920 × 1080 coordinates into that rectangle.

This is a known compatibility risk.

A real user reported all frames shifted while using:

```text
Two monitors
2560 × 1440 display
Hearthstone windowed mode
Windowed Borderless Gaming (WBG)
Windows scaling reported as disabled / 100%
```

The exact effective Hearthstone client rectangle and WBG position were not yet confirmed.

### Why this matters

HDT itself positions many game-relative elements using a centered **4:3** logical board area and a scale based primarily on overlay height.

Relevant HDT concepts include behavior equivalent to:

```csharp
ScreenRatio = (4.0 / 3.0) / (Width / Height);
```

and:

```csharp
scaledX = (width * ratio * normalizedX)
          + (width * (1 - ratio) / 2);
```

The current plugin's 16:9 transformation and HDT's 4:3 transformation can coincide at the 1920 × 1080 calibration point but diverge on:

- 16:10 displays;
- manually resized windows;
- borderless-window tools;
- unusual client-area sizes;
- mixed-DPI multi-monitor setups.

### Rules for the compatibility fix

Do not apply arbitrary per-resolution offsets.

A compatibility change should:

1. preserve the current 1920 × 1080 positions exactly;
2. derive coordinates from the actual HDT overlay dimensions;
3. prefer HDT's own logical coordinate model or an equivalent verified formula;
4. distinguish physical screen resolution from the actual Hearthstone client rectangle;
5. avoid hard-coding a specific monitor width;
6. avoid relying on the primary monitor when Hearthstone is on another monitor;
7. handle window movement and resize while the plugin is running;
8. keep all label and icon sizes proportional;
9. keep the 30-pixel opponent offset in reference coordinates;
10. log enough geometry information to diagnose remaining failures without logging personal paths.

Potential geometry diagnostics:

```text
overlayWidth
overlayHeight
windowLeft
windowTop
DPI scale X and Y when safely accessible
computed logical board width
computed logical board left
computed scale
first-slot final X and Y
```

Do not log a full screenshot automatically without explicit approval.

### Required resolution and setup tests

Baseline:

```text
1920 × 1080, 100%, fullscreen or borderless
```

Additional tests:

```text
1280 × 720
2560 × 1440
3840 × 2160
1920 × 1200
2560 × 1600
3440 × 1440
5120 × 1440
windowed Hearthstone
borderless Hearthstone
Windowed Borderless Gaming
single monitor
multiple monitors
100% Windows scaling
125% Windows scaling
150% Windows scaling
mixed DPI between monitors
```

For every layout test verify:

- all eight labels align with the correct portraits;
- the top and bottom slots remain aligned;
- the horizontal slope across the eight slots is preserved;
- Tavern icons stay attached to labels;
- `Last.PNG` remains directly below the Tavern icon;
- the current-opponent offset remains correct;
- resizing or moving the window updates the layout;
- no element blocks input.

Do not claim broad resolution compatibility after testing only one screenshot.

---

## 17. Embedded assets

Required asset filenames:

```text
Assets\T1.png
Assets\T2.png
Assets\T3.png
Assets\T4.png
Assets\T5.png
Assets\T6.png
Assets\T7.png
Assets\Last.PNG
```

Keep filenames exactly as documented, including the capital letters in:

```text
Last.PNG
```

Current expected Tavern source dimensions:

```text
129 × 134 px
```

Rules:

- embed PNG assets into the DLL;
- do not require a separate Assets folder after installation;
- keep pack URIs synchronized with `BGMMRPlugin` assembly naming;
- preserve transparency;
- do not silently stretch Tavern icons to a different aspect ratio;
- use `Stretch.Uniform`;
- do not include copyrighted Hearthstone assets without confirming distribution rights;
- record third-party asset attribution when required.

After an asset-resource change, test loading from the compiled DLL rather than only from the source tree.

---

## 18. Power.log processing

`LobbyTracker` processes selected lines from:

```text
Core.Game.PowerLog
```

Rules:

- process only new lines using the retained log index;
- reset the index safely if the log is replaced or shortened;
- do not rescan the full log every update;
- protect against incomplete lines;
- use player IDs and account markers where possible;
- avoid depending on localized text beyond stable HDT log syntax;
- keep regex patterns focused;
- ignore fake-player records when they are identified;
- do not expose account IDs in normal logs;
- reset match-specific maps at the correct lifecycle point.

When a parser changes, test against real log samples when available.

Do not assume Hearthstone logging is stable across patches.

---

## 19. Diagnostics and local data

The plugin logger currently stores logs and cache files under the user's HDT application-data area in a plugin-specific directory.

Rules:

- diagnostics must never crash HDT;
- cache failures must never crash HDT;
- the final logging method must be protected by `try/catch`;
- log transitions and decisions, not every 250-millisecond update;
- avoid writing the same message continuously;
- do not commit personal log files or cache files;
- rotate or limit log growth;
- do not log full BattleTags, account IDs, tokens, or personal absolute paths unless explicitly required for a local diagnostic build and approved by the user.

Preferred format:

```text
EVENT NAME | key=value | key=value
```

Useful diagnostics for this plugin should explain:

- which lobby was resolved without exposing private identifiers;
- how many players were resolved;
- region and solo/Duos mode;
- whether the public leaderboard loaded;
- whether the primary, mirror, or local cache source was used;
- why an exact MMR lookup succeeded or failed;
- current overlay geometry for resolution debugging;
- accepted current-opponent and last-opponent transitions;
- missing embedded assets.

---

## 20. Tribe / predominant-minion-type feature

A predominant tribe feature was investigated but is not part of the current plugin.

Hearthstone internally receives real-time battlefield race counts, but the currently used public HDT/HearthMirror API does not expose a simple supported method for retrieving that exact per-player data from a standard plugin.

Rules:

- do not implement invasive process-memory reading;
- do not intercept internal network packets;
- do not patch HearthMirror;
- do not reconstruct uncertain current compositions and label them as exact;
- do not add this feature without an explicit user request and a reviewed technical design.

A future implementation must clearly distinguish:

- exact game-provided tribe counts;
- last-seen board reconstruction;
- unknown data;
- ties;
- dual-tribe minions;
- `ALL` minions.

Until then, leave the feature absent.

---

## 21. Versioning

The plugin version is defined in the `IPlugin` implementation:

```csharp
public Version Version => new Version(MAJOR, MINOR, PATCH);
```

Current release baseline:

```text
1.0.4
```

Rules:

- increment the version for every distributed or user-tested build;
- bug fixes and compatibility fixes normally increment `PATCH`;
- do not silently replace an already distributed version with different code;
- keep version references consistent across:
  - `Plugin.cs`;
  - `BGMMRPlugin.csproj`;
  - assembly version and file version when used;
  - HTTP user agent;
  - `Build.bat` title and output;
  - README;
  - changelog;
  - release title;
  - archive name;
- do not increment the version for analysis-only work with no file changes.

Suggested release naming:

```text
HDT-BGMMRPlugin_v1.0.4.zip
```

The release archive should contain the correctly named:

```text
HDT-BGMMRPlugin.dll
```

For source archives, use an unambiguous name such as:

```text
HDT-BGMMRPlugin_v1.0.4_Source.zip
```

---

## 22. Git and GitHub rules

Before editing:

```bash
git status
git branch --show-current
```

After editing:

```bash
git diff --check
git diff
git status
```

Rules:

- do not overwrite unrelated user changes;
- do not delete untracked files without permission;
- do not use `git reset --hard`;
- do not force-push;
- do not rewrite published history;
- use a separate branch for significant features when practical;
- do not commit, push, or open a pull request unless the user asks;
- keep commits focused and descriptive.

Example commit messages:

```text
fix: align leaderboard overlay with HDT coordinate scaling
fix: preserve the last opponent across leaderboard reordering
fix: make HDT assembly discovery portable
feat: add adaptive multi-resolution overlay diagnostics
docs: document public MMR fallback behavior
```

Do not commit:

```text
bin/
obj/
dist/*.dll
lib/HearthstoneDeckTracker.exe
lib/HearthDb.dll
lib/HearthMirror.dll
lib/untapped-scry-dotnet.dll
*.pdb
*.log
leaderboard_*.txt
.vs/
```

Respect `.gitignore`.

---

## 23. Required checks before delivery

For every C# change:

1. inspect the complete diff;
2. run `git diff --check`;
3. verify braces and syntax;
4. verify declarations and call sites;
5. search for accidental duplicate names;
6. search for stale plugin names;
7. compile with `Build.bat` when possible;
8. confirm creation of:

```text
dist\HDT-BGMMRPlugin.dll
```

9. confirm that no old DLL name was generated;
10. list all changed files;
11. update version and documentation when distributing a test build.

### For DLL, project, namespace, or resource changes

Search for:

```text
BGMMRPlugin
BGMmrOverlayPlugin
pack://application
AssemblyName
RootNamespace
```

Expected final state:

- project and solution use `BGMMRPlugin`;
- source namespaces use `BGMMRPlugin`;
- distributed artifact is `HDT-BGMMRPlugin.dll`;
- pack URIs identify the compiled `HDT-BGMMRPlugin` assembly;
- README and Build.bat use `HDT-BGMMRPlugin.dll`;
- no personal path exists.

### For MMR changes

Test:

- exact-case player-name match;
- case-insensitive fallback;
- duplicate visible names;
- player below the public threshold;
- primary source failure;
- mirror source failure;
- valid offline cache;
- expired offline cache;
- solo and Duos region keys;
- no UI blocking.

### For lobby or opponent changes

Test:

- eight players resolved;
- fake player ignored;
- leaderboard reordering;
- duplicate temporary slot;
- hero entity replacement;
- current opponent changes;
- combat starts and ends;
- last opponent updates once;
- dead-player state;
- new-match reset.

### For overlay changes

Test:

- all eight slots;
- normal names;
- local player green;
- current opponent red;
- current-opponent offset;
- exact MMR and `< 8000`;
- Tavern Tier icons;
- last-opponent icon;
- dead-player opacity;
- Show/Hide button behavior;
- no blocked Hearthstone clicks;
- attach, detach, and HDT shutdown;
- all relevant resolutions and window modes listed in section 16.

If in-game testing is not possible, provide a precise checklist for the user.

---

## 24. Definition of done

A task is complete only when:

- the requested behavior is implemented;
- unrelated stable behavior was preserved;
- the diff is focused and clean;
- naming is consistent;
- compilation succeeded, or the inability to compile is explicitly stated;
- executed tests are listed;
- remaining manual tests are listed;
- version and documentation are coherent when required;
- no generated or private files were committed;
- the report is understandable to a beginner.

A layout task is not complete merely because the code compiles. It requires at least one in-game baseline test and targeted testing of the affected resolution or window configuration.

---

## 25. Gradual refactoring

The current project is already separated into several responsibilities:

```text
Plugin.cs
Game/LobbyPlayer.cs
Game/LobbyTracker.cs
Services/HoveredPlayerNameReader.cs
Services/OfficialBoardClient.cs
UI/PlayerMmrOverlay.cs
Util/PluginLogger.cs
```

Do not collapse these responsibilities back into one large file.

Possible future structure may include:

```text
Game/OpponentTracker.cs
Game/LobbyIdentityResolver.cs
UI/OverlayGeometry.cs
UI/OverlayDiagnostics.cs
Services/LeaderboardCache.cs
Models/OverlaySettings.cs
```

This is a direction, not a command to refactor immediately.

Rules:

- extract one responsibility at a time;
- preserve behavior during extraction;
- compile after each meaningful extraction;
- do not hide logic changes inside a refactor;
- do not create unnecessary abstraction;
- do not split a small cohesive method without a clear benefit.

The adaptive-resolution work is a reasonable point to introduce a small, independently testable geometry helper, provided the existing 1920 × 1080 output remains identical.

---

## 26. Project priorities

Priority order:

1. never crash HDT;
2. never block Hearthstone interactions;
3. keep every displayed player attached to the correct leaderboard identity;
4. keep overlay positions accurate across supported resolutions and window modes;
5. never invent an exact MMR;
6. preserve current-opponent and last-opponent state correctly;
7. preserve live Tavern Tier accuracy;
8. degrade safely when the network or cache is unavailable;
9. produce `HDT-BGMMRPlugin.dll` reproducibly without personal paths;
10. keep the code understandable and reviewable;
11. maintain a compact, legible overlay;
12. add optional features only after core stability.

When priorities conflict, choose stability, identity correctness, and truthful data over additional features.
