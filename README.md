# BGMMRPlugin

**BGMMRPlugin** is a compact plugin for **Hearthstone Deck Tracker**, designed specifically for **Battlegrounds**.

It displays useful information directly beside each player's avatar, including player names, high-level MMR ratings, Tavern Tiers and your most recent opponent.

The information is updated dynamically throughout the game, even when players change position in the leaderboard.

---

## About the Project

BGMMRPlugin was created to provide a cleaner and more practical way to identify opponents during a Battlegrounds match.

Instead of displaying information in a separate window, the plugin places it directly beside the corresponding player avatars.

The goal is to keep the interface compact, readable and visually integrated with the Battlegrounds leaderboard.

This project was also created as a personal programming challenge. I am not a professional programmer, so feedback, testing and contributions are welcome.

---

## Features

### Player Names

* Displays player names directly beside their avatars.
* Displays your own player name in **green**.
* Displays your current opponent's name in **red**.
* Keeps player names associated with the correct avatars when leaderboard positions change.

### MMR Display

* Displays the MMR rating of opponents with a rating of **8,000 or higher**.
* Players below 8,000 MMR are not shown.
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

BGMMRPlugin is particularly useful for **Twitch streamers**, **YouTube creators** and recorded gameplay.

Viewers can immediately see:

* The names of the players in the lobby.
* Which player is currently fighting the streamer.
* Which opponent was fought during the previous round.
* The Tavern Tier of each opponent.
* The MMR of high-ranked players.

This gives viewers more context without requiring the streamer to constantly explain the state of the lobby.

---

## Installation

1. Install and configure **Hearthstone Deck Tracker**.
2. Open the **Releases** section of this GitHub repository.
3. Download the latest published version of BGMMRPlugin.
4. Extract the downloaded archive.
5. Copy the BGMMRPlugin folder into the Hearthstone Deck Tracker plugins directory:

```text
%AppData%\HearthstoneDeckTracker\Plugins
```

6. Restart Hearthstone Deck Tracker.
7. Open:

```text
Options → Tracker → Plugins
```

8. Find **BGMMRPlugin** and enable it.

The plugin should automatically appear when entering a Hearthstone Battlegrounds match.

> [!WARNING]
> Do not download the files named **Source code.zip** or **Source code.tar.gz** unless you want to compile the plugin yourself.
>
> Download the compiled plugin archive attached to the latest release.

---

## Usage

Once the plugin is installed and enabled:

1. Launch Hearthstone Deck Tracker.
2. Start Hearthstone.
3. Enter a Battlegrounds match.
4. BGMMRPlugin will automatically detect the lobby.
5. Player information will appear beside the corresponding avatars.

No manual activation is required during a match.

---

## Display Rules

BGMMRPlugin uses several visual indicators:

* **Green player name:** your own account.
* **Red player name:** your current opponent.
* **Standard player name:** another active opponent.
* **MMR value:** displayed only for players rated 8,000 or higher.
* **Tavern icon or value:** the player's current Tavern Tier.
* **Last opponent icon:** indicates the opponent you fought during the previous combat.

The displayed elements may evolve as the plugin is updated.

---

## Feedback and Bug Reports

BGMMRPlugin is still under development.

Bug reports are particularly useful for:

* Incorrect player positions.
* Information displayed beside the wrong avatar.
* Missing player names.
* Incorrect Tavern Tiers.
* MMR values not appearing.
* Display problems caused by screen resolution or interface scaling.
* Information not updating after leaderboard changes.

When reporting a problem, please include:

* Your Hearthstone Deck Tracker version.
* Your BGMMRPlugin version.
* Your screen resolution.
* Your Windows display scaling.
* Whether Hearthstone is running in fullscreen, windowed or borderless mode.
* A screenshot or video showing the issue.
* The relevant Hearthstone Deck Tracker logs when available.

Bug reports and suggestions can be submitted through the repository's **Issues** section.

---

## Support the Project

The best ways to support BGMMRPlugin are:

* Test the plugin during Battlegrounds matches.
* Report bugs with screenshots and detailed information.
* Suggest improvements.
* Share the plugin with other Battlegrounds players.
* Mention the plugin in Twitch or YouTube content.
* Contribute improvements through GitHub.

---

## To Do

Planned improvements include:

* Verify and improve compatibility with different screen resolutions and display scaling settings.
* Display the damage dealt by each hero.
* Add a victory or defeat indicator for the previous combat.

Additional features may be added based on player feedback and technical feasibility.

---

## Compatibility

BGMMRPlugin is designed for:

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

## Disclaimer

BGMMRPlugin is an independent, community-created project.

It is not affiliated with, endorsed by or sponsored by Blizzard Entertainment, Hearthstone, HSReplay.net or HearthSim.

Hearthstone and Blizzard Entertainment are trademarks or registered trademarks of Blizzard Entertainment, Inc.

Player information displayed by the plugin depends on the data available during the match. Some names, ratings or Tavern Tiers may be unavailable, delayed or incomplete.

---

## License

See the `LICENSE` file included in this repository for licensing information.
