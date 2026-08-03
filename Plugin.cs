using BGMMRPlugin.Game;
using BGMMRPlugin.Services;
using BGMMRPlugin.UI;
using BGMMRPlugin.Util;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BGMMRPlugin
{
    public sealed class Plugin : IPlugin
    {
        public string Name => "BGMMRPlugin";

        public string Description =>
            "Displays ratings, tavern tiers, next-opponent highlighting, and the last completed opponent.";

        public string ButtonText => "Show / hide";

        public string Author => "Benito";

        public Version Version => new Version(1, 0, 9);

        public MenuItem MenuItem => null;

        private readonly LobbyTracker _lobbyTracker = new LobbyTracker();

        private OfficialBoardClient _boardClient;
        private HoveredPlayerNameReader _hoveredNameReader;
        private PlayerMmrOverlay _overlay;

        private LobbyState _lobby;
        private OfficialBoard _officialBoard;
        private Task<OfficialBoard> _boardLoadTask;

        private int _trackedOpponentPlayerId;
        private int _trackedOpponentTeammatePlayerId;
        private int _combatOpponentPlayerId;
        private int _lastOpponentPlayerId;
        private bool _pluginEnabled = true;
        private bool _wasInMatch;
        private bool _wasCombatPhase;
        private string _lastHoverEntityError;
        private string _lastDuosStructureDiagnostic;
        private DateTime _nextUpdateAt = DateTime.MinValue;

        public void OnLoad()
        {
            PluginLogger.Info("Plugin loaded.");

            _boardClient = new OfficialBoardClient(
                PluginLogger.CacheDirectory
            );

            _hoveredNameReader =
                new HoveredPlayerNameReader();

            Core.OverlayCanvas.Dispatcher.Invoke(() =>
            {
                _overlay = new PlayerMmrOverlay();
                _overlay.Attach();
                _overlay.HideAll();
            });
        }

        public void OnUnload()
        {
            try
            {
                Core.OverlayCanvas.Dispatcher.Invoke(() =>
                {
                    _overlay?.Detach();
                    _overlay = null;
                });
            }
            catch
            {
                // HDT may already be closing its overlay window.
            }

            _boardClient?.Dispose();
            _boardClient = null;

            _hoveredNameReader?.Dispose();
            _hoveredNameReader = null;

            // Keep the current lobby eligible when HDT reloads the plugin
            // during a running match.
            ResetMatchState(markLobbyAsStale: false);
            PluginLogger.Info("Plugin unloaded.");
        }

        public void OnButtonPress()
        {
            _pluginEnabled = !_pluginEnabled;

            if (!_pluginEnabled)
            {
                Core.OverlayCanvas.Dispatcher.Invoke(
                    () => _overlay?.HideAll()
                );
            }
        }

        public void OnUpdate()
        {
            try
            {
                bool isBattlegroundsMatch =
                    Core.Game.IsRunning
                    && !Core.Game.IsInMenu
                    && Core.Game.IsBattlegroundsMatch;

                if (!isBattlegroundsMatch)
                {
                    if (_wasInMatch)
                    {
                        Core.OverlayCanvas.Dispatcher.Invoke(
                            () => _overlay?.HideAll()
                        );

                        ResetMatchState(markLobbyAsStale: true);
                    }

                    _wasInMatch = false;
                    return;
                }

                _wasInMatch = true;

                if (!_pluginEnabled)
                    return;

                if (DateTime.UtcNow < _nextUpdateAt)
                    return;

                _nextUpdateAt = DateTime.UtcNow.AddMilliseconds(250);

                ResolveLobbyIfNeeded();
                FinishLeaderboardLoadIfReady();

                if (_lobby == null)
                {
                    Core.OverlayCanvas.Dispatcher.Invoke(
                        () => _overlay?.HideAll()
                    );
                    return;
                }

                _lobbyTracker.AttachHeroEntities(
                    _lobby.Players,
                    _lobby.GameUuid
                );

                TryRecoverHoveredPlayerName();

                int opponentPlayerId =
                    ResolveTrackedOpponentPlayerId();

                UpdateLastOpponentTracking(
                    opponentPlayerId
                );

                PlayerDisplayData[] display =
                    BuildDisplayData(
                        _lobby.Players,
                        opponentPlayerId,
                        _trackedOpponentTeammatePlayerId,
                        _lastOpponentPlayerId,
                        Core.Game.IsBattlegroundsDuosMatch
                    );

                Core.OverlayCanvas.Dispatcher.Invoke(() =>
                {
                    if (_overlay == null)
                        return;

                    _overlay.Display(
                        display,
                        Core.Game.IsBattlegroundsDuosMatch
                    );
                    _overlay.UpdateLayout();
                });
            }
            catch (Exception ex)
            {
                PluginLogger.Error("OnUpdate failed.", ex);
            }
        }

        private void ResolveLobbyIfNeeded()
        {
            if (_lobby != null)
                return;

            LobbyState resolved = _lobbyTracker.TryResolveLobby();
            if (resolved == null)
                return;

            string region = MapRegion();
            if (region == null)
            {
                PluginLogger.Info(
                    "The current Hearthstone region could not be identified."
                );
                return;
            }

            _lobby = resolved;
            _officialBoard = null;

            bool duos = Core.Game.IsBattlegroundsDuosMatch;

            PluginLogger.Info(
                $"Lobby resolved: {_lobby.Players.Count} players, "
                + $"region={region}, duos={duos}."
            );

            _boardLoadTask = _boardClient.GetBoardAsync(
                region,
                duos
            );
        }

        private void FinishLeaderboardLoadIfReady()
        {
            if (_boardLoadTask == null || !_boardLoadTask.IsCompleted)
                return;

            try
            {
                if (_boardLoadTask.Status == TaskStatus.RanToCompletion)
                {
                    _officialBoard = _boardLoadTask.Result;

                    PluginLogger.Info(
                        _officialBoard == null
                            ? "Official leaderboard unavailable."
                            : $"Official leaderboard loaded: "
                              + $"{_officialBoard.Count} entries."
                    );
                }
                else if (_boardLoadTask.IsFaulted)
                {
                    PluginLogger.Error(
                        "Official leaderboard task failed.",
                        _boardLoadTask.Exception
                    );
                }
            }
            finally
            {
                _boardLoadTask = null;
            }
        }

        private void TryRecoverHoveredPlayerName()
        {
            if (
                _hoveredNameReader == null
                || _lobby?.Players == null
                || !_lobby.Players.Any(
                    player => player.IsNamePlaceholder
                )
            )
            {
                return;
            }

            int? hoveredEntityId;

            try
            {
                hoveredEntityId =
                    HearthMirror.Reflection.Client
                        .GetBattlegroundsLeaderboardHoveredEntityId();

                _lastHoverEntityError = null;
            }
            catch (Exception ex)
            {
                string errorType = ex.GetType().Name;

                if (!string.Equals(
                    errorType,
                    _lastHoverEntityError,
                    StringComparison.Ordinal
                ))
                {
                    _lastHoverEntityError = errorType;

                    PluginLogger.Debug(
                        "HOVER ENTITY READ FAILED"
                        + $" | errorType={errorType}"
                    );
                }

                return;
            }

            if (
                !hoveredEntityId.HasValue
                || !Core.Game.Entities.TryGetValue(
                    hoveredEntityId.Value,
                    out var hoveredEntity
                )
                || !hoveredEntity.HasTag(GameTag.PLAYER_ID)
            )
            {
                return;
            }

            int playerId = hoveredEntity.GetTag(
                GameTag.PLAYER_ID
            );

            if (playerId <= 0)
                return;

            LobbyPlayer player = _lobby.Players.FirstOrDefault(
                candidate =>
                    candidate.IsNamePlaceholder
                    && candidate.PlayerId == playerId
            );

            if (player == null)
                return;

            string recoveredName = LobbyTracker.StripTag(
                _hoveredNameReader.TryRead()
            );

            if (!IsUsableHoveredName(recoveredName))
                return;

            player.Name = recoveredName;
            player.IsNamePlaceholder = false;

            PluginLogger.Info(
                "LOBBY NAME RECOVERED"
                + " | source=manualHover"
                + " | playerIdKnown=True"
            );
        }

        private static bool IsUsableHoveredName(string name)
        {
            if (
                string.IsNullOrWhiteSpace(name)
                || string.Equals(
                    name,
                    "...",
                    StringComparison.Ordinal
                )
                || string.Equals(
                    name,
                    "UNKNOWN HUMAN PLAYER",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return false;
            }

            string[] unavailableNames =
            {
                "Your Opponent",
                "Votre adversaire",
                "Euer Gegner"
            };

            return !unavailableNames.Contains(
                name,
                StringComparer.OrdinalIgnoreCase
            );
        }

        private PlayerDisplayData[] BuildDisplayData(
            IReadOnlyList<LobbyPlayer> players,
            int opponentPlayerId,
            int opponentTeammatePlayerId,
            int lastOpponentPlayerId,
            bool duos)
        {
            PlayerDisplayData[] result =
                Enumerable.Range(1, 8)
                    .Select(place => PlayerDisplayData.Hidden(place))
                    .ToArray();

            if (duos)
            {
                return BuildDuosDisplayData(
                    players,
                    result,
                    opponentPlayerId,
                    opponentTeammatePlayerId,
                    lastOpponentPlayerId
                );
            }

            HashSet<int> occupiedPlaces = new HashSet<int>();

            foreach (LobbyPlayer player in players)
            {
                int place = player.LeaderboardPlace;
                if (place < 1 || place > 8)
                    continue;

                // A temporary duplicate may occur while Hearthstone is replacing
                // a hero entity. Keep the first authoritative slot for this tick.
                if (!occupiedPlaces.Add(place))
                    continue;

                string ratingText;

                if (player.IsNamePlaceholder)
                {
                    ratingText = "...";
                }
                else if (_officialBoard == null)
                {
                    ratingText = "...";
                }
                else if (_officialBoard.TryGetRating(
                    player.Name,
                    out int rating
                ))
                {
                    ratingText = rating.ToString(
                        "N0",
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                }
                else
                {
                    // Blizzard's public leaderboard does not expose the exact
                    // rating below its cutoff.
                    ratingText = "< 8000";
                }

                result[place - 1] = new PlayerDisplayData
                {
                    Place = place,
                    Name = player.Name,
                    RatingText = ratingText,
                    TavernTier = player.TavernTier,
                    IsVisible = true,
                    IsLocalPlayer = player.IsLocalPlayer,
                    IsCurrentOpponent =
                        opponentPlayerId > 0
                        && player.PlayerId == opponentPlayerId,
                    IsLastOpponent =
                        lastOpponentPlayerId > 0
                        && player.PlayerId == lastOpponentPlayerId,
                    IsDead = player.IsDead
                };
            }

            return result;
        }

        private PlayerDisplayData[] BuildDuosDisplayData(
            IReadOnlyList<LobbyPlayer> players,
            PlayerDisplayData[] result,
            int opponentPlayerId,
            int opponentTeammatePlayerId,
            int lastOpponentPlayerId)
        {
            var rankedGroups = players
                .Where(player =>
                    player.LeaderboardPlace >= 1
                    && player.LeaderboardPlace <= 8
                )
                .GroupBy(player => player.LeaderboardPlace)
                .OrderBy(group => group.Key)
                .ToList();

            int unrankedCount = players.Count(player =>
                player.LeaderboardPlace < 1
                || player.LeaderboardPlace > 8
            );

            string diagnostic = "DUOS STRUCTURE"
                + $" | players={players.Count}"
                + $" | places={rankedGroups.Count}"
                + " | members="
                + string.Join(
                    ",",
                    rankedGroups.Select(group =>
                        $"{group.Key}:{group.Count()}"
                    )
                )
                + $" | unranked={unrankedCount}";

            List<List<LobbyPlayer>> teams =
                BuildStableDuosTeams(players);

            int linkedTeamCount = teams.Count(team =>
                team.Count == 2
                && (
                    team[0].TeammatePlayerId == team[1].PlayerId
                    || team[1].TeammatePlayerId == team[0].PlayerId
                )
            );

            diagnostic += $" | linkedTeams={linkedTeamCount}";

            if (!string.Equals(
                diagnostic,
                _lastDuosStructureDiagnostic,
                StringComparison.Ordinal
            ))
            {
                _lastDuosStructureDiagnostic = diagnostic;
                PluginLogger.Info(diagnostic);
            }

            LobbyPlayer[] orderedPlayers = teams
                .OrderBy(team => team
                    .Select(player => player.LeaderboardPlace)
                    .Where(place => place >= 1 && place <= 8)
                    .DefaultIfEmpty(9)
                    .Min()
                )
                .SelectMany(team => team
                    .OrderByDescending(player =>
                        player.FightsFirstNextCombat
                    )
                    .ThenBy(player =>
                        player.PlayerId <= 0
                            ? int.MaxValue
                            : player.PlayerId
                    )
                )
                .Take(8)
                .ToArray();

            for (int index = 0; index < orderedPlayers.Length; index++)
            {
                result[index] = CreateDisplayData(
                    orderedPlayers[index],
                    index + 1,
                    opponentPlayerId,
                    opponentTeammatePlayerId,
                    lastOpponentPlayerId
                );
            }

            return result;
        }

        private static List<List<LobbyPlayer>> BuildStableDuosTeams(
            IReadOnlyList<LobbyPlayer> players)
        {
            List<LobbyPlayer> remaining = players
                .Where(player => player != null)
                .ToList();

            List<List<LobbyPlayer>> teams =
                new List<List<LobbyPlayer>>();

            // Resolve every explicit relationship first so a player cannot be
            // consumed by a temporary-place fallback before their partner.
            foreach (LobbyPlayer player in players)
            {
                if (player == null || !remaining.Contains(player))
                    continue;

                LobbyPlayer linkedTeammate = remaining
                    .FirstOrDefault(candidate =>
                        !ReferenceEquals(candidate, player)
                        && (
                            (
                                player.TeammatePlayerId > 0
                                && candidate.PlayerId
                                    == player.TeammatePlayerId
                            )
                            || (
                                candidate.TeammatePlayerId > 0
                                && candidate.TeammatePlayerId
                                    == player.PlayerId
                            )
                        )
                    );

                if (linkedTeammate == null)
                    continue;

                teams.Add(new List<LobbyPlayer>
                {
                    player,
                    linkedTeammate
                });

                remaining.Remove(player);
                remaining.Remove(linkedTeammate);
            }

            // Fall back only for relationships not exposed yet at match start.
            while (remaining.Count > 0)
            {
                LobbyPlayer player = remaining[0];
                LobbyPlayer teammate = remaining
                    .Skip(1)
                    .FirstOrDefault(candidate =>
                        player.LeaderboardPlace >= 1
                        && player.LeaderboardPlace
                            == candidate.LeaderboardPlace
                    );

                if (teammate == null && remaining.Count > 1)
                    teammate = remaining[1];

                List<LobbyPlayer> team =
                    new List<LobbyPlayer> { player };

                remaining.Remove(player);

                if (teammate != null)
                {
                    team.Add(teammate);
                    remaining.Remove(teammate);
                }

                teams.Add(team);
            }

            return teams;
        }

        private PlayerDisplayData CreateDisplayData(
            LobbyPlayer player,
            int displayPlace,
            int opponentPlayerId,
            int opponentTeammatePlayerId,
            int lastOpponentPlayerId)
        {
            string ratingText;

            if (player.IsNamePlaceholder || _officialBoard == null)
            {
                ratingText = "...";
            }
            else if (_officialBoard.TryGetRating(player.Name, out int rating))
            {
                ratingText = rating.ToString(
                    "N0",
                    System.Globalization.CultureInfo.InvariantCulture
                );
            }
            else
            {
                ratingText = "< 8000";
            }

            return new PlayerDisplayData
            {
                Place = displayPlace,
                Name = player.Name,
                RatingText = ratingText,
                TavernTier = player.TavernTier,
                IsVisible = true,
                IsLocalPlayer = player.IsLocalPlayer,
                IsCurrentOpponent =
                    player.PlayerId > 0
                    && (
                        player.PlayerId == opponentPlayerId
                        || player.PlayerId == opponentTeammatePlayerId
                    ),
                IsLastOpponent =
                    lastOpponentPlayerId > 0
                    && player.PlayerId == lastOpponentPlayerId,
                IsDead = player.IsDead
            };
        }

        private void UpdateLastOpponentTracking(
            int currentOpponentPlayerId)
        {
            bool isCombatPhase =
                Core.Game.IsBattlegroundsCombatPhase;

            if (isCombatPhase)
            {
                if (currentOpponentPlayerId > 0)
                {
                    _combatOpponentPlayerId =
                        currentOpponentPlayerId;
                }
                else if (
                    Core.Game.Opponent != null
                    && Core.Game.Opponent.Id > 0
                )
                {
                    _combatOpponentPlayerId =
                        Core.Game.Opponent.Id;
                }
            }
            else if (_wasCombatPhase)
            {
                // The combat has just ended. Only now does this opponent
                // become the last completed opponent.
                if (_combatOpponentPlayerId > 0)
                {
                    _lastOpponentPlayerId =
                        _combatOpponentPlayerId;
                }

                _combatOpponentPlayerId = 0;
            }

            _wasCombatPhase = isCombatPhase;
        }

        private int ResolveTrackedOpponentPlayerId()
        {
            try
            {
                int nextOpponentId = 0;
                int nextOpponentTeammateId = 0;

                if (
                    Core.Game.PlayerEntity != null
                    && Core.Game.PlayerEntity.HasTag(
                        GameTag.NEXT_OPPONENT_PLAYER_ID
                    )
                )
                {
                    nextOpponentId =
                        Core.Game.PlayerEntity.GetTag(
                            GameTag.NEXT_OPPONENT_PLAYER_ID
                        );
                }

                if (
                    Core.Game.PlayerEntity != null
                    && Core.Game.PlayerEntity.HasTag(
                        GameTag.NEXT_OPPONENT_TEAMMATE_PLAYER_ID
                    )
                )
                {
                    nextOpponentTeammateId =
                        Core.Game.PlayerEntity.GetTag(
                            GameTag.NEXT_OPPONENT_TEAMMATE_PLAYER_ID
                        );
                }

                // Hearthstone updates this tag when the next matchup is
                // selected. Keeping the last valid value also covers combat,
                // where the same player remains the current opponent.
                if (
                    nextOpponentId > 0
                    && (
                        Core.Game.Player == null
                        || nextOpponentId != Core.Game.Player.Id
                    )
                )
                {
                    _trackedOpponentPlayerId = nextOpponentId;
                    _trackedOpponentTeammatePlayerId =
                        nextOpponentTeammateId > 0
                        && nextOpponentTeammateId != nextOpponentId
                            ? nextOpponentTeammateId
                            : 0;
                }
                else if (
                    _trackedOpponentPlayerId <= 0
                    && Core.Game.IsBattlegroundsCombatPhase
                    && Core.Game.Opponent != null
                    && Core.Game.Opponent.Id > 0
                )
                {
                    // Fallback for a combat that starts before the tag has
                    // been observed by this plugin.
                    _trackedOpponentPlayerId =
                        Core.Game.Opponent.Id;
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    "ResolveTrackedOpponentPlayerId: "
                    + ex.Message
                );
            }

            return _trackedOpponentPlayerId;
        }

        private static string MapRegion()
        {
            switch (Core.Game.CurrentRegion)
            {
                case Region.US:
                    return "US";

                case Region.EU:
                    return "EU";

                case Region.ASIA:
                    return "AP";

                case Region.CHINA:
                    return "CN";

                default:
                    return null;
            }
        }

        private void ResetMatchState(bool markLobbyAsStale)
        {
            if (markLobbyAsStale)
                _lobbyTracker.MarkCurrentLobbyInfoStale();

            _lobbyTracker.Reset();

            _lobby = null;
            _officialBoard = null;
            _boardLoadTask = null;
            _trackedOpponentPlayerId = 0;
            _trackedOpponentTeammatePlayerId = 0;
            _combatOpponentPlayerId = 0;
            _lastOpponentPlayerId = 0;
            _lastDuosStructureDiagnostic = null;
            _wasCombatPhase = false;
            _nextUpdateAt = DateTime.MinValue;
        }
    }
}
