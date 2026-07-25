using System.Collections.Generic;

namespace BGMMRPlugin.Game
{
    public sealed class LobbyPlayer
    {
        public string Name { get; set; }

        public string HeroCardId { get; set; }

        public int PlayerId { get; set; }

        public int LeaderboardPlace { get; set; }

        public int TavernTier { get; set; }

        public bool IsLocalPlayer { get; set; }

        public bool IsDead { get; set; }
    }

    public sealed class LobbyState
    {
        public List<LobbyPlayer> Players { get; set; }

        public string GameUuid { get; set; }
    }

    public sealed class PlayerDisplayData
    {
        public int Place { get; set; }

        public string Name { get; set; }

        public string RatingText { get; set; }

        public int TavernTier { get; set; }

        public bool IsVisible { get; set; }

        public bool IsLocalPlayer { get; set; }

        public bool IsCurrentOpponent { get; set; }

        public bool IsLastOpponent { get; set; }

        public bool IsDead { get; set; }

        public static PlayerDisplayData Hidden(int place)
        {
            return new PlayerDisplayData
            {
                Place = place,
                IsVisible = false
            };
        }
    }
}
