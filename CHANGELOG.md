# Changelog

All notable user-facing changes are documented here. Installable builds are
available from the [GitHub Releases page](https://github.com/Reign-in-blood/HDT-BGMMRPlugin/releases).

## 1.0.9

- Pair Duos players through `BACON_DUO_TEAMMATE_PLAYER_ID` instead of relying
  on transient individual leaderboard places.
- Latch teammate relationships so an early concession cannot split a team or
  mix players from two groups.
- Keep leaderboard places as team-ordering signals only, with safe fallbacks
  while Hearthstone reports temporary ties or uneven group sizes.

## 1.0.8

- Order both players in each Duos group using
  `BACON_DUO_PLAYER_FIGHTS_FIRST_NEXT_COMBAT`.
- Track `NEXT_OPPONENT_TEAMMATE_PLAYER_ID` so both members of the next opposing
  team receive the red state and the 30-pixel horizontal offset.
- Keep all Solo ordering and opponent behavior unchanged.

## 1.0.7

- Stop treating `BACON_DUO_TEAM_ID` as a shared partner key after in-game
  diagnostics showed that it could split one leaderboard pair.
- Keep all eight resolved Duos players visible while leaderboard places arrive
  progressively or temporarily contain ties.
- Use the shared leaderboard place only to order Duos players, with unranked
  players retained at the end instead of hidden.

## 1.0.6

- Preserve both partners in Duos when they share the same team leaderboard
  place instead of treating the second player as a transient duplicate.
- Group the eight Duos players into four stable two-player team rows.
- Add a separate first-pass Duos overlay layout without changing Solo
  coordinates.
- Add anonymized Duos team-structure diagnostics for in-game calibration.

## 1.0.5

- Wait for at least seven usable player names before accepting an eight-entry
  lobby.
- Avoid freezing transient EU lobby metadata containing two incomplete player
  identities, which could leave one leaderboard frame without a valid place.
- Keep the manual-hover fallback for the single remaining unavailable name.
- Do not resume lobby metadata polling after the stabilized lobby is accepted.

## 1.0.4

- Added a compatibility fallback for an EU lobby name that is absent from
  `BattlegroundsLobbyInfo` but visible in Hearthstone.
- Read only the currently hovered leaderboard tile, and only while the
  corresponding player is still displayed as `...`.
- Associate the recovered name with the hovered hero's `PLAYER_ID`.
- Keep all known player names on the existing HDT/HearthMirror metadata path.
- Reuse the ScryDotNet runtime already distributed with HDT; no additional
  installation file is required.
- Stop re-resolving lobby metadata every 250 milliseconds after the initial
  eight-player lobby has been accepted.

## 1.0.3

- Continuously reconcile a placeholder lobby player with refreshed
  HearthMirror metadata.
- Replace `...` automatically when the missing EU player name becomes
  available later in the match.
- Match the recovered identity by player ID first and by a unique normalized
  hero ID only as a safe fallback.
- Log successful recovery without writing the player's name or account ID.

## 1.0.2

- Restored `HDT-BGMMRPlugin.dll` as the canonical release filename.
- Displayed partial eight-player lobbies with an honest placeholder for an unavailable name.
- Allowed a missing name to be refreshed later from newly available Power.log data.
- Allowed the plugin to be reloaded during a running match without rejecting the current lobby as stale.

## 1.0.1

- Made Battlegrounds lobby resolution tolerate invalid metadata entries.
- Added anonymized, transition-only diagnostics for incomplete lobby data.
- Preserved the existing Power.log fallback and all display behavior.

## 1.0.0

- Renamed the release assembly to `BGMMRPlugin.dll`.
- Renamed the project and solution to `BGMMRPlugin`.
- Updated plugin metadata to version 1.0.0.
- Translated build scripts and documentation to English.
- Removed redundant development comments.
- Reworked `Build.bat` to use portable HDT discovery with no personal absolute path.
- Preserved all v0.1.11 runtime behavior, coordinates, colors, asset sizes, and tracking logic.

## 0.1.x development history

- Added public MMR display and `< 8000` fallback.
- Added leaderboard-place tracking.
- Added local-player and opponent highlighting.
- Added the 30 px next-opponent offset.
- Added live tavern-tier icons.
- Added the last-completed-opponent marker.
- Calibrated eight independent leaderboard positions.
- Added centered 16:9 scaling for wider displays.
