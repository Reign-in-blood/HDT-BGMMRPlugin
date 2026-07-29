using BGMMRPlugin.Util;
using ScryDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BGMMRPlugin.Services
{
    /// <summary>
    /// Reads the name text already populated by Hearthstone for the currently
    /// hovered Battlegrounds leaderboard tile.
    ///
    /// This is a narrow compatibility fallback for a lobby entry whose name
    /// is absent from HearthMirror BattlegroundsLobbyInfo. It is never used
    /// for players whose lobby name is already known.
    /// </summary>
    internal sealed class HoveredPlayerNameReader : IDisposable
    {
        private const string HearthstoneImageName =
            "Blizzard.T5.ServiceLocator";

        private const string UnityVersion = "2021.3.25.61228";

        private Process _process;
        private MonoScry _view;
        private MonoImage _root;
        private string _lastError;

        public string TryRead()
        {
            try
            {
                MonoImage root = GetRoot();
                if (root == null)
                    return null;

                // Hearthstone populates this text only after the player
                // manually hovers the native leaderboard tile.
                dynamic leaderboardManager =
                    root["PlayerLeaderboardManager"]?["s_instance"];

                dynamic hoveredTile =
                    leaderboardManager?["m_currentlyMousedOverTile"];

                string name =
                    hoveredTile?["m_overlay"]?
                        ["m_heroActor"]?
                        ["m_playerNameText"]?
                        ["m_Text"];

                _lastError = null;
                return name;
            }
            catch (Exception ex)
            {
                LogErrorOnce(ex);
                ResetConnection();
                return null;
            }
        }

        public void Dispose()
        {
            ResetConnection();
        }

        private MonoImage GetRoot()
        {
            if (_root != null)
                return _root;

            _process = Process
                .GetProcessesByName("Hearthstone")
                .FirstOrDefault();

            if (_process == null)
                return null;

            _view = new MonoScry(
                Scry.connect(_process.Id)
            );

            _root = _view.getImage(
                new List<string>
                {
                    HearthstoneImageName
                },
                UnityVersion
            );

            return _root;
        }

        private void ResetConnection()
        {
            try
            {
                _root?.Dispose();
            }
            catch
            {
                // The Hearthstone process may already have stopped.
            }

            try
            {
                _view?.Dispose();
            }
            catch
            {
                // The Hearthstone process may already have stopped.
            }

            try
            {
                _process?.Dispose();
            }
            catch
            {
                // The Hearthstone process may already have stopped.
            }

            _root = null;
            _view = null;
            _process = null;
        }

        private void LogErrorOnce(Exception ex)
        {
            string error =
                ex.GetType().Name + ": " + ex.Message;

            if (string.Equals(
                error,
                _lastError,
                StringComparison.Ordinal
            ))
            {
                return;
            }

            _lastError = error;

            PluginLogger.Debug(
                "HOVER NAME READ FAILED"
                + $" | errorType={ex.GetType().Name}"
            );
        }
    }
}
