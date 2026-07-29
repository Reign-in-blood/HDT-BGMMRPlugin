# HDT-BGMMRPlugin

**HDT-BGMMRPlugin** is a compact plugin for **Hearthstone Deck Tracker**, designed specifically for **Battlegrounds**.

It displays useful information directly beside each player's avatar, including player names, high-level MMR ratings, Tavern Tiers and your most recent opponent.

The information is updated dynamically throughout the game, even when players change position in the leaderboard.

[Download the latest release](https://github.com/Reign-in-blood/HDT-BGMMRPlugin/releases/latest)
· [View the complete changelog](CHANGELOG.md)

<p align="center">
  <img src="Images\Capture d’écran 2026-07-25 150939.png" alt="HDT-BGMMRPlugin preview" width="500">
</p>

---

## Latest Update — v1.0.5

Version 1.0.5 restores reliable lobby display after a Hearthstone update
changed the player-name metadata returned for some EU Battlegrounds lobbies.

The plugin now:

* Waits for the lobby identities to stabilize before displaying the eight
  player frames.
* Prevents a transient incomplete player entry from leaving a frame missing.
* Displays `...` when Hearthstone does not initially provide one player name.
* Recovers that missing name when the corresponding native Hearthstone
  portrait is manually hovered.
* Preserves the existing behavior for US and other unaffected lobbies.

See [CHANGELOG.md](CHANGELOG.md) for the complete version history.

---

## About the Project

HDT-BGMMRPlugin was created to provide a cleaner and more practical way to identify opponents during a Battlegrounds match.

Instead of displaying information in a separate window, the plugin places it directly beside the corresponding player avatars.

The goal is to keep the interface compact, readable and visually integrated with the Battlegrounds leaderboard.

This project was also created as a personal programming challenge. I am not a professional programmer, so feedback, testing and contributions are welcome.

---

<p align="center">
  <img src="Images\Capture d’écran 2026-07-25 151141.png" alt="HDT-BGMMRPlugin preview" width="500">
</p>

## Features

### Player Names

* Displays player names directly beside their avatars.
* Displays your own player name in **green**.
* Displays your current opponent's name in **red**.
* Keeps player names associated with the correct avatars when leaderboard positions change.
* If the EU lobby metadata omits one name, manually hovering that player's
  Hearthstone portrait lets the plugin recover the name shown by the game.

### MMR Display

* Displays the MMR rating of opponents with a rating of **8,000 or higher**.
* Displays `< 8000` when an exact public MMR is unavailable.
* Keeps the interface compact by displaying only relevant high-level ratings.

### Tavern Tier Tracking

* Displays the current Tavern Tier of each opponent.
* Updates Tavern Tier information dynamically during the game.
* Keeps the information aligned with the correct player.

### Last Opponent Indicator

* Displays an icon beside your most recent opponent.
* Makes it easier to remember which player you fought during the previous combat.
* Helps track opponent rotations during the match.

### Dynamic Interface

* Tracks changes in the Battlegrounds leaderboard.
* Repositions player information when opponents move up or down.
* Updates displayed information in real time.
* Uses a compact interface designed to remain readable without covering the game board.

---

## For Twitch and YouTube Creators

HDT-BGMMRPlugin is particularly useful for **Twitch streamers**, **YouTube creators** and recorded gameplay.

Viewers can immediately see:

* The names of the players in the lobby.
* Which player is currently fighting the streamer.
* Which opponent was fought during the previous round.
* The Tavern Tier of each opponent.
* The MMR of high-ranked players.

This gives viewers more visual context.

---

## Installation

1. Install and configure **Hearthstone Deck Tracker**.
2. Open the **[latest release page](https://github.com/Reign-in-blood/HDT-BGMMRPlugin/releases)** section of this GitHub repository.
3. Download the latest published version of HDT-BGMMRPlugin.dll.
4. Extract the downloaded archive.
5. Copy HDT-BGMMRPlugin.dll into the Hearthstone Deck Tracker plugins directory:

```text
%AppData%\HearthstoneDeckTracker\Plugins
```

6. Restart Hearthstone Deck Tracker.
7. Open:

```text
Options → Tracker → Plugins
```

8. Find **HDT-BGMMRPlugin** and enable it.

The plugin should automatically appear when entering a Hearthstone Battlegrounds match.

---

## Support the Project

The best ways to support HDT-BGMMRPlugin are:

* Test the plugin during Battlegrounds matches.
* Report bugs with screenshots and detailed information.
* Suggest improvements.
* Share the plugin with other Battlegrounds players.
* Mention the plugin in Twitch or YouTube content.
* Contribute improvements through GitHub.

---

## Compatibility

HDT-BGMMRPlugin is designed for:

* Hearthstone Battlegrounds.
* Hearthstone Deck Tracker.
* Windows.

Compatibility may be affected by:

* Hearthstone updates.
* Hearthstone Deck Tracker updates.
* Different screen resolutions.
* Windows display scaling.
* Changes to the Battlegrounds interface.

---

## Updates and Release History

Important changes are tracked in [CHANGELOG.md](CHANGELOG.md). Installable
builds and their release notes are published on the
[GitHub Releases page](https://github.com/Reign-in-blood/HDT-BGMMRPlugin/releases).

The version is kept consistent in the plugin metadata, project assembly,
build script and network user-agent for every distributed build.

---

## Disclaimer

HDT-BGMMRPlugin is an independent, community-created project.

It is not affiliated with, endorsed by or sponsored by Blizzard Entertainment, Hearthstone, HSReplay.net or HearthSim.

Hearthstone and Blizzard Entertainment are trademarks or registered trademarks of Blizzard Entertainment, Inc.

Player information displayed by the plugin depends on the data available during the match. Some names, ratings or Tavern Tiers may be unavailable, delayed or incomplete.

---

## License

This project is licensed under the MIT License. See the [`LICENSE`](https://github.com/Reign-in-blood/HDT-BGMMRPlugin/blob/master/LICENSE) file for details.



