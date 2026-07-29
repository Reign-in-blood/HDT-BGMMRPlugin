using BGMMRPlugin.Util;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BGMMRPlugin.Game
{
    /// <summary>
    /// Resolves the Battlegrounds lobby without requiring portrait hovering.
    ///
    /// Primary source:
    /// Core.Game.MetaData.BattlegroundsLobbyInfo, populated by HDT through
    /// HearthMirror.
    ///
    /// Fallback:
    /// PlayerID / PlayerName lines already exposed by HDT in Core.Game.PowerLog.
    ///
    /// Hero entities provide PLAYER_ID and PLAYER_LEADERBOARD_PLACE, allowing
    /// the overlay text to follow the moving leaderboard slots.
    /// </summary>
    public sealed class LobbyTracker
    {
        private const string UnknownPlayer = "UNKNOWN HUMAN PLAYER";
        private const int MinimumUsableLobbyNames = 7;

        private static readonly Regex PlayerLineRegex = new Regex(
            @"PlayerID=(?<id>\d+), PlayerName=(?<name>.+?)\s*$",
            RegexOptions.Compiled
        );

        private static readonly Regex AccountLineRegex = new Regex(
            @"Player EntityID=\d+ PlayerID=(?<id>\d+) "
            + @"GameAccountId=\[hi=(?<hi>\d+) lo=(?<lo>\d+)\]",
            RegexOptions.Compiled
        );

        private readonly Dictionary<int, string> _namesByPlayerId =
            new Dictionary<int, string>();

        private readonly HashSet<int> _fakePlayerIds =
            new HashSet<int>();

        private int _powerLogIndex;
        private string _consumedGameUuid;
        private string _lastResolutionDiagnostic;

        public void Reset()
        {
            _powerLogIndex = 0;
            _namesByPlayerId.Clear();
            _fakePlayerIds.Clear();
            _lastResolutionDiagnostic = null;
        }

        public void MarkCurrentLobbyInfoStale()
        {
            try
            {
                string uuid =
                    Core.Game?.MetaData?.BattlegroundsLobbyInfo?.GameUuid;

                if (!string.IsNullOrWhiteSpace(uuid))
                    _consumedGameUuid = uuid;
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    "MarkCurrentLobbyInfoStale: " + ex.Message
                );
            }
        }

        public LobbyState TryResolveLobby()
        {
            try
            {
                ScanPowerLog();

                string localName = StripTag(Core.Game.Player?.Name);

                List<LobbyPlayer> players =
                    TryFromLobbyInfo(
                        localName,
                        out string gameUuid,
                        out string metadataState,
                        out int metadataTotal,
                        out int metadataUsable,
                        out int metadataIgnored
                    );

                if (players == null)
                {
                    gameUuid = null;
                    players = TryFromPowerLog(localName);
                }

                if (players == null || players.Count < 8)
                {
                    LogResolutionDiagnostic(
                        metadataState,
                        metadataTotal,
                        metadataUsable,
                        metadataIgnored
                    );

                    return null;
                }

                if (string.Equals(
                    metadataState,
                    "partial",
                    StringComparison.Ordinal
                ))
                {
                    LogResolutionDiagnostic(
                        metadataState,
                        metadataTotal,
                        metadataUsable,
                        metadataIgnored
                    );
                }
                else
                {
                    _lastResolutionDiagnostic = null;
                }

                AttachHeroEntities(players, gameUuid);

                return new LobbyState
                {
                    Players = players,
                    GameUuid = gameUuid
                };
            }
            catch (Exception ex)
            {
                PluginLogger.Error(
                    "LobbyTracker.TryResolveLobby failed.",
                    ex
                );

                return null;
            }
        }

        private List<LobbyPlayer> TryFromLobbyInfo(
            string localName,
            out string gameUuid,
            out string metadataState,
            out int metadataTotal,
            out int metadataUsable,
            out int metadataIgnored)
        {
            gameUuid = null;
            metadataState = "unavailable";
            metadataTotal = 0;
            metadataUsable = 0;
            metadataIgnored = 0;

            var lobbyInfo =
                Core.Game.MetaData?.BattlegroundsLobbyInfo;

            var lobbyPlayers = lobbyInfo?.Players;

            if (lobbyPlayers == null)
                return null;

            metadataTotal = lobbyPlayers.Count;

            // HDT can retain the preceding game's lobby briefly.
            if (
                !string.IsNullOrWhiteSpace(lobbyInfo.GameUuid)
                && string.Equals(
                    lobbyInfo.GameUuid,
                    _consumedGameUuid,
                    StringComparison.Ordinal
                )
            )
            {
                metadataState = "stale";
                return null;
            }

            metadataState = "incomplete";

            List<LobbyPlayer> result = new List<LobbyPlayer>();
            bool localAssigned = false;
            var localAccountId = Core.Game.MetaData?.AccountId;

            foreach (var info in lobbyPlayers)
            {
                if (info == null)
                {
                    metadataIgnored++;
                    continue;
                }

                string name = StripTag(info.Name);
                bool isNamePlaceholder = false;

                if (
                    string.IsNullOrWhiteSpace(name)
                    || string.Equals(
                        name,
                        UnknownPlayer,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    metadataIgnored++;

                    bool isLocalAccount =
                        info.AccountId != null
                        && localAccountId != null
                        && info.AccountId.Hi == localAccountId.Hi
                        && info.AccountId.Lo == localAccountId.Lo;

                    if (
                        isLocalAccount
                        && !string.IsNullOrWhiteSpace(localName)
                    )
                    {
                        name = localName;
                    }
                    else
                    {
                        name = "...";
                        isNamePlaceholder = true;
                    }
                }

                bool isLocal =
                    !localAssigned
                    && !string.IsNullOrWhiteSpace(localName)
                    && string.Equals(
                        name,
                        localName,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (isLocal)
                    localAssigned = true;

                result.Add(new LobbyPlayer
                {
                    Name = name,
                    IsNamePlaceholder = isNamePlaceholder,
                    HeroCardId = info.HeroCardId,
                    IsLocalPlayer = isLocal,
                    PlayerId = FindUnusedPlayerId(
                        name,
                        result
                    )
                });
            }

            metadataUsable = result.Count(
                player => !player.IsNamePlaceholder
            );

            metadataState =
                metadataUsable >= 8
                    ? "complete"
                    : "partial";

            if (
                result.Count < 8
                || metadataUsable < MinimumUsableLobbyNames
            )
            {
                return null;
            }

            gameUuid = lobbyInfo.GameUuid;
            return result;
        }

        private List<LobbyPlayer> TryFromPowerLog(
            string localName)
        {
            if (_namesByPlayerId.Count < 8)
                return null;

            return _namesByPlayerId
                .Where(pair => !_fakePlayerIds.Contains(pair.Key))
                .OrderBy(pair => pair.Key)
                .Take(8)
                .Select(pair => new LobbyPlayer
                {
                    Name = pair.Value,
                    PlayerId = pair.Key,
                    IsLocalPlayer =
                        !string.IsNullOrWhiteSpace(localName)
                        && string.Equals(
                            pair.Value,
                            localName,
                            StringComparison.OrdinalIgnoreCase
                        )
                })
                .ToList();
        }

        private void LogResolutionDiagnostic(
            string metadataState,
            int metadataTotal,
            int metadataUsable,
            int metadataIgnored)
        {
            int powerLogUsable = _namesByPlayerId.Count(
                pair => !_fakePlayerIds.Contains(pair.Key)
            );

            string diagnostic =
                "LOBBY WAIT"
                + $" | metadataState={metadataState}"
                + $" | metadataTotal={metadataTotal}"
                + $" | metadataUsable={metadataUsable}"
                + $" | metadataIgnored={metadataIgnored}"
                + $" | powerLogUsable={powerLogUsable}";

            if (string.Equals(
                diagnostic,
                _lastResolutionDiagnostic,
                StringComparison.Ordinal
            ))
            {
                return;
            }

            _lastResolutionDiagnostic = diagnostic;
            PluginLogger.Info(diagnostic);
        }

        public void AttachHeroEntities(
            List<LobbyPlayer> players,
            string gameUuid)
        {
            if (players == null || players.Count == 0)
                return;

            try
            {
                RefreshHeroCardIds(players, gameUuid);
                ScanPowerLog();

                foreach (LobbyPlayer player in players)
                {
                    if (
                        player.PlayerId == 0
                        && player.IsLocalPlayer
                        && Core.Game.Player != null
                    )
                    {
                        player.PlayerId = Core.Game.Player.Id;
                    }

                    if (player.PlayerId == 0)
                    {
                        player.PlayerId = FindUnusedPlayerId(
                            player.Name,
                            players
                        );
                    }
                }

                List<Entity> entities =
                    Core.Game.Entities.Values.ToList();

                // The lobby may expose a base hero while the entity uses a skin.
                // Card-id matching is the reliable bridge to PLAYER_ID.
                foreach (Entity entity in entities)
                {
                    if (
                        entity == null
                        || !entity.IsHero
                        || !entity.HasTag(GameTag.PLAYER_ID)
                    )
                    {
                        continue;
                    }

                    string entityHero =
                        NormalizeHeroCardId(entity.CardId);

                    if (entityHero == null)
                        continue;

                    LobbyPlayer player = players.FirstOrDefault(
                        candidate =>
                            string.Equals(
                                entityHero,
                                NormalizeHeroCardId(
                                    candidate.HeroCardId
                                ),
                                StringComparison.OrdinalIgnoreCase
                            )
                    );

                    if (player == null)
                        continue;

                    // Ghost hero copies can exist. An entity carrying a real
                    // leaderboard place is the authoritative one.
                    if (
                        entity.HasTag(
                            GameTag.PLAYER_LEADERBOARD_PLACE
                        )
                        || player.PlayerId == 0
                    )
                    {
                        player.PlayerId =
                            entity.GetTag(GameTag.PLAYER_ID);
                    }
                }

                foreach (LobbyPlayer player in players)
                {
                    Entity hero = FindAuthoritativeHero(
                        entities,
                        player.PlayerId
                    );

                    if (hero == null)
                        continue;

                    int place = hero.GetTag(
                        GameTag.PLAYER_LEADERBOARD_PLACE
                    );

                    if (place >= 1 && place <= 8)
                        player.LeaderboardPlace = place;

                    int tavernTier = ResolveTavernTier(
                        hero,
                        entities,
                        player.PlayerId
                    );

                    // Keep the last valid value when Hearthstone briefly
                    // reports zero while replacing or transforming an entity.
                    if (tavernTier >= 1 && tavernTier <= 7)
                        player.TavernTier = tavernTier;

                    if (
                        hero.HasTag(GameTag.HEALTH)
                        && hero.Health <= 0
                    )
                    {
                        // Death is latched. A zero-damage ghost copy may later
                        // appear, but a Battlegrounds player cannot resurrect.
                        player.IsDead = true;
                    }

                    if (string.IsNullOrWhiteSpace(player.HeroCardId))
                        player.HeroCardId = hero.CardId;
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    "AttachHeroEntities: " + ex.Message
                );
            }
        }

        private static int ResolveTavernTier(
            Entity hero,
            IEnumerable<Entity> entities,
            int playerId)
        {
            if (
                hero != null
                && hero.HasTag(GameTag.PLAYER_TECH_LEVEL)
            )
            {
                int heroTier =
                    hero.GetTag(GameTag.PLAYER_TECH_LEVEL);

                if (heroTier > 0)
                    return heroTier;
            }

            if (playerId <= 0)
                return 0;

            Entity taggedEntity = entities.FirstOrDefault(
                entity =>
                    entity != null
                    && entity.HasTag(GameTag.PLAYER_ID)
                    && entity.GetTag(GameTag.PLAYER_ID) == playerId
                    && entity.HasTag(GameTag.PLAYER_TECH_LEVEL)
                    && entity.GetTag(GameTag.PLAYER_TECH_LEVEL) > 0
            );

            return taggedEntity?.GetTag(
                GameTag.PLAYER_TECH_LEVEL
            ) ?? 0;
        }

        private static Entity FindAuthoritativeHero(
            IEnumerable<Entity> entities,
            int playerId)
        {
            if (playerId <= 0)
                return null;

            List<Entity> candidates = entities
                .Where(entity =>
                    entity != null
                    && entity.IsHero
                    && entity.HasTag(GameTag.PLAYER_ID)
                    && entity.GetTag(GameTag.PLAYER_ID) == playerId
                    && entity.HasTag(
                        GameTag.PLAYER_LEADERBOARD_PLACE
                    )
                )
                .ToList();

            if (candidates.Count == 0)
                return null;

            // Prefer an in-play entity, then the last enumerated candidate.
            return candidates.FirstOrDefault(entity => entity.IsInPlay)
                   ?? candidates[candidates.Count - 1];
        }

        private void RefreshHeroCardIds(
            List<LobbyPlayer> players,
            string gameUuid)
        {
            if (string.IsNullOrWhiteSpace(gameUuid))
                return;

            try
            {
                var lobbyInfo =
                    Core.Game.MetaData?.BattlegroundsLobbyInfo;

                if (
                    lobbyInfo?.Players == null
                    || !string.Equals(
                        lobbyInfo.GameUuid,
                        gameUuid,
                        StringComparison.Ordinal
                    )
                )
                {
                    return;
                }

                Dictionary<string, int> occurrence =
                    new Dictionary<string, int>(
                        StringComparer.OrdinalIgnoreCase
                    );

                foreach (var info in lobbyInfo.Players)
                {
                    string name = StripTag(info.Name);

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    occurrence.TryGetValue(name, out int index);
                    occurrence[name] = index + 1;

                    if (string.IsNullOrWhiteSpace(info.HeroCardId))
                        continue;

                    LobbyPlayer player = players
                        .Where(candidate =>
                            string.Equals(
                                candidate.Name,
                                name,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .Skip(index)
                        .FirstOrDefault();

                    if (player != null)
                        player.HeroCardId = info.HeroCardId;
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    "RefreshHeroCardIds: " + ex.Message
                );
            }
        }

        private int FindUnusedPlayerId(
            string name,
            IEnumerable<LobbyPlayer> assigned)
        {
            HashSet<int> used = new HashSet<int>(
                assigned
                    .Where(player => player != null)
                    .Select(player => player.PlayerId)
                    .Where(id => id > 0)
            );

            foreach (
                KeyValuePair<int, string> pair
                in _namesByPlayerId
            )
            {
                if (
                    !used.Contains(pair.Key)
                    && string.Equals(
                        pair.Value,
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return pair.Key;
                }
            }

            return 0;
        }

        private void ScanPowerLog()
        {
            var log = Core.Game.PowerLog;
            if (log == null)
                return;

            if (_powerLogIndex > log.Count)
                _powerLogIndex = 0;

            for (int index = _powerLogIndex; index < log.Count; index++)
            {
                string line = log[index];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Contains("GameAccountId=["))
                {
                    Match accountMatch =
                        AccountLineRegex.Match(line);

                    if (
                        accountMatch.Success
                        && accountMatch.Groups["hi"].Value == "0"
                        && accountMatch.Groups["lo"].Value == "0"
                    )
                    {
                        int fakeId = int.Parse(
                            accountMatch.Groups["id"].Value
                        );

                        _fakePlayerIds.Add(fakeId);
                        _namesByPlayerId.Remove(fakeId);
                    }

                    continue;
                }

                if (
                    !line.Contains("DebugPrintGame()")
                    || !line.Contains("PlayerID=")
                )
                {
                    continue;
                }

                Match playerMatch =
                    PlayerLineRegex.Match(line);

                if (!playerMatch.Success)
                    continue;

                int playerId = int.Parse(
                    playerMatch.Groups["id"].Value
                );

                if (_fakePlayerIds.Contains(playerId))
                    continue;

                string playerName = StripTag(
                    playerMatch.Groups["name"].Value
                );

                if (
                    string.IsNullOrWhiteSpace(playerName)
                    || string.Equals(
                        playerName,
                        UnknownPlayer,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                _namesByPlayerId[playerId] = playerName;
            }

            _powerLogIndex = log.Count;
        }

        internal static string NormalizeHeroCardId(
            string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return null;

            try
            {
                cardId =
                    Hearthstone_Deck_Tracker.Hearthstone
                        .BattlegroundsUtils
                        .GetOriginalHeroId(cardId)
                    ?? cardId;
            }
            catch
            {
                // HDT remote data may not be ready yet.
            }

            int skinIndex = cardId.IndexOf(
                "_SKIN_",
                StringComparison.OrdinalIgnoreCase
            );

            return skinIndex > 0
                ? cardId.Substring(0, skinIndex)
                : cardId;
        }

        internal static string StripTag(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim();

            int tagIndex = name.IndexOf('#');

            return tagIndex > 0
                ? name.Substring(0, tagIndex)
                : name;
        }
    }
}
