# Changelog

All notable user-facing changes are documented here. Installable builds are
available from the [GitHub Releases page](https://github.com/Reign-in-blood/HDT-BGMMRPlugin/releases).

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
