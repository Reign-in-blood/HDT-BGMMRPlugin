using BGMMRPlugin.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BGMMRPlugin.Services
{
    public sealed class OfficialBoard
    {
        private readonly Dictionary<string, int> _exact;
        private readonly Dictionary<string, int> _folded;

        public int Count => _exact.Count;

        public OfficialBoard(
            Dictionary<string, int> exact,
            Dictionary<string, int> folded)
        {
            _exact = exact;
            _folded = folded;
        }

        public bool TryGetRating(
            string playerName,
            out int rating)
        {
            rating = 0;

            if (string.IsNullOrWhiteSpace(playerName))
                return false;

            // Preserve Blizzard's exact casing first. A case-insensitive
            // fallback helps when another source changed capitalization.
            return _exact.TryGetValue(playerName, out rating)
                   || _folded.TryGetValue(playerName, out rating);
        }
    }

    /// <summary>
    /// Downloads IBM5100's public mirror of Blizzard's complete official
    /// Battlegrounds leaderboards. The mirror avoids hundreds of direct
    /// Blizzard API requests from every plugin user.
    /// </summary>
    public sealed class OfficialBoardClient : IDisposable
    {
        private const string PrimaryBaseUrl =
            "https://bgrank.fly.dev";

        private const string MirrorBaseUrl =
            "https://raw.githubusercontent.com/"
            + "lowerman/bg-board-mirror/mirror";

        private static readonly TimeSpan MemoryCacheDuration =
            TimeSpan.FromMinutes(15);

        private static readonly TimeSpan OfflineCopyMaximumAge =
            TimeSpan.FromHours(48);

        private readonly HttpClient _httpClient;
        private readonly string _cacheDirectory;

        private readonly Dictionary<string, CacheEntry> _memoryCache =
            new Dictionary<string, CacheEntry>(
                StringComparer.OrdinalIgnoreCase
            );

        public OfficialBoardClient(string cacheDirectory)
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12;

            _cacheDirectory = cacheDirectory;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };

            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "BGMMRPlugin/1.0.0"
            );
        }

        public async Task<OfficialBoard> GetBoardAsync(
            string region,
            bool duos)
        {
            string key = MapRegion(region);

            if (key == null)
                return null;

            if (duos)
                key += "_duo";

            lock (_memoryCache)
            {
                if (
                    _memoryCache.TryGetValue(
                        key,
                        out CacheEntry cached
                    )
                    && DateTime.UtcNow - cached.LoadedAtUtc
                       < MemoryCacheDuration
                )
                {
                    return cached.Board;
                }
            }

            string text = await DownloadBoardText(key)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(text))
                return GetStaleMemoryBoard(key);

            OfficialBoard board = Parse(text);

            if (board == null || board.Count == 0)
                return GetStaleMemoryBoard(key);

            lock (_memoryCache)
            {
                _memoryCache[key] = new CacheEntry
                {
                    Board = board,
                    LoadedAtUtc = DateTime.UtcNow
                };
            }

            return board;
        }

        private async Task<string> DownloadBoardText(
            string key)
        {
            try
            {
                string text = await _httpClient.GetStringAsync(
                    $"{PrimaryBaseUrl}/{key}/"
                ).ConfigureAwait(false);

                if (LooksLikeBoard(text))
                {
                    SaveOfflineCopy(key, text);
                    return text;
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    $"Primary leaderboard fetch failed for {key}: "
                    + ex.Message
                );
            }

            try
            {
                string text = await _httpClient.GetStringAsync(
                    $"{MirrorBaseUrl}/{key}.txt"
                ).ConfigureAwait(false);

                if (LooksLikeBoard(text))
                {
                    SaveOfflineCopy(key, text);
                    return text;
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    $"GitHub leaderboard mirror failed for {key}: "
                    + ex.Message
                );
            }

            return LoadOfflineCopy(key);
        }

        private static OfficialBoard Parse(string text)
        {
            Dictionary<string, int> exact =
                new Dictionary<string, int>(
                    StringComparer.Ordinal
                );

            Dictionary<string, int> folded =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                );

            string normalized = text
                .Replace("\r", string.Empty)
                .Replace("\n<br />", "<br />");

            string[] lines = normalized.Split(
                new[] { "<br />", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                int separator = line.LastIndexOf(' ');
                if (separator <= 0)
                    continue;

                string name = line
                    .Substring(0, separator)
                    .Trim();

                if (
                    name.Length == 0
                    || !int.TryParse(
                        line.Substring(separator + 1).Trim(),
                        out int rating
                    )
                )
                {
                    continue;
                }

                // The mirror is sorted by rating descending. Retaining the
                // first duplicate implements the established BGrank rule:
                // identical names use the highest listed rating.
                if (!exact.ContainsKey(name))
                    exact[name] = rating;

                if (!folded.ContainsKey(name))
                    folded[name] = rating;
            }

            return new OfficialBoard(exact, folded);
        }

        private static bool LooksLikeBoard(string text)
        {
            return !string.IsNullOrWhiteSpace(text)
                   && text.Contains("<br />")
                   && text.Length > 100;
        }

        private string OfflinePath(string key)
        {
            return Path.Combine(
                _cacheDirectory,
                $"leaderboard_{key}.txt"
            );
        }

        private void SaveOfflineCopy(
            string key,
            string text)
        {
            try
            {
                Directory.CreateDirectory(_cacheDirectory);
                File.WriteAllText(OfflinePath(key), text);
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    "Unable to save leaderboard cache: "
                    + ex.Message
                );
            }
        }

        private string LoadOfflineCopy(string key)
        {
            try
            {
                string path = OfflinePath(key);

                if (!File.Exists(path))
                    return null;

                DateTime modifiedUtc =
                    File.GetLastWriteTimeUtc(path);

                if (
                    DateTime.UtcNow - modifiedUtc
                    > OfflineCopyMaximumAge
                )
                {
                    return null;
                }

                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                PluginLogger.Debug(
                    "Unable to read leaderboard cache: "
                    + ex.Message
                );

                return null;
            }
        }

        private OfficialBoard GetStaleMemoryBoard(
            string key)
        {
            lock (_memoryCache)
            {
                return _memoryCache.TryGetValue(
                    key,
                    out CacheEntry cached
                )
                    ? cached.Board
                    : null;
            }
        }

        private static string MapRegion(string region)
        {
            switch (region?.ToUpperInvariant())
            {
                case "US":
                case "NA":
                    return "US";

                case "EU":
                    return "EU";

                case "AP":
                case "ASIA":
                    return "AP";

                case "CN":
                    return "CN";

                default:
                    return null;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private sealed class CacheEntry
        {
            public OfficialBoard Board { get; set; }

            public DateTime LoadedAtUtc { get; set; }
        }
    }
}
