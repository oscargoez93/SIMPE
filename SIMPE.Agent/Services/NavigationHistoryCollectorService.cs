using Microsoft.Data.Sqlite;
using SIMPE.Agent.Models;

namespace SIMPE.Agent.Services
{
    public class NavigationHistoryCollectorService
    {
        private const int MaxDurationEstimateMinutes = 240;

        public NavigationHistoryResult GatherNavigationHistory(int limit = 2000)
        {
            limit = Math.Clamp(limit, 100, 5000);
            var entries = new List<NavigationHistoryEntry>();
            var scannedBrowsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in GetChromiumSources())
            {
                if (File.Exists(source.historyPath))
                {
                    entries.AddRange(ReadChromiumHistory(source.browser, source.profile, source.historyPath));
                    scannedBrowsers.Add(source.browser);
                }
            }

            foreach (var source in GetFirefoxSources())
            {
                if (File.Exists(source.historyPath))
                {
                    entries.AddRange(ReadFirefoxHistory(source.browser, source.profile, source.historyPath));
                    scannedBrowsers.Add(source.browser);
                }
            }

            EstimateMissingDurations(entries);

            var orderedEntries = entries
                .OrderByDescending(e => ParseVisitDate(e.visitedAt))
                .Take(limit)
                .ToList();

            return new NavigationHistoryResult
            {
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                totalEntries = entries.Count,
                returnedEntries = orderedEntries.Count,
                scannedBrowsers = scannedBrowsers.OrderBy(b => b).ToList(),
                entries = orderedEntries,
                notes = new List<string>
                {
                    "El modo incognito/privado no se puede reconstruir desde el historial porque los navegadores no lo guardan.",
                    "La duracion se muestra desde el navegador cuando existe; en otros casos se estima con la siguiente visita del mismo perfil."
                }
            };
        }

        private static IEnumerable<(string browser, string profile, string historyPath)> GetChromiumSources()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            foreach (var item in DiscoverChromiumProfiles("Google Chrome", Path.Combine(local, "Google", "Chrome", "User Data")))
            {
                yield return item;
            }
            foreach (var item in DiscoverChromiumProfiles("Microsoft Edge", Path.Combine(local, "Microsoft", "Edge", "User Data")))
            {
                yield return item;
            }
            foreach (var item in DiscoverChromiumProfiles("Brave", Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data")))
            {
                yield return item;
            }
            foreach (var item in DiscoverChromiumProfiles("Vivaldi", Path.Combine(local, "Vivaldi", "User Data")))
            {
                yield return item;
            }
            foreach (var item in DiscoverChromiumProfiles("Opera", Path.Combine(roaming, "Opera Software", "Opera Stable"), includeRoot: true))
            {
                yield return item;
            }
            foreach (var item in DiscoverChromiumProfiles("Opera GX", Path.Combine(roaming, "Opera Software", "Opera GX Stable"), includeRoot: true))
            {
                yield return item;
            }
        }

        private static IEnumerable<(string browser, string profile, string historyPath)> DiscoverChromiumProfiles(string browser, string userDataPath, bool includeRoot = false)
        {
            if (!Directory.Exists(userDataPath))
            {
                yield break;
            }

            if (includeRoot)
            {
                yield return (browser, "Default", Path.Combine(userDataPath, "History"));
            }

            foreach (var directory in Directory.EnumerateDirectories(userDataPath))
            {
                var profile = Path.GetFileName(directory);
                if (profile.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                    profile.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase) ||
                    profile.Equals("Guest Profile", StringComparison.OrdinalIgnoreCase))
                {
                    yield return (browser, profile, Path.Combine(directory, "History"));
                }
            }
        }

        private static IEnumerable<(string browser, string profile, string historyPath)> GetFirefoxSources()
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var profilesPath = Path.Combine(roaming, "Mozilla", "Firefox", "Profiles");
            if (!Directory.Exists(profilesPath))
            {
                yield break;
            }

            foreach (var directory in Directory.EnumerateDirectories(profilesPath))
            {
                yield return ("Firefox", Path.GetFileName(directory), Path.Combine(directory, "places.sqlite"));
            }
        }

        private static List<NavigationHistoryEntry> ReadChromiumHistory(string browser, string profile, string historyPath)
        {
            var entries = new List<NavigationHistoryEntry>();
            var tempPath = CopyToTemporaryDatabase(historyPath);
            if (string.IsNullOrWhiteSpace(tempPath))
            {
                return entries;
            }

            try
            {
                using var connection = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly");
                connection.Open();

                var hasDuration = HasColumn(connection, "visits", "visit_duration");
                var durationColumn = hasDuration ? "visits.visit_duration" : "0";
                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT urls.url, urls.title, visits.visit_time, {durationColumn} AS visit_duration
                    FROM urls
                    INNER JOIN visits ON urls.id = visits.url
                    WHERE urls.url IS NOT NULL AND urls.url <> ''
                    ORDER BY visits.visit_time DESC";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var visitedAt = FromChromiumTime(ReadLong(reader, "visit_time"));
                    entries.Add(new NavigationHistoryEntry
                    {
                        browser = browser,
                        profile = profile,
                        title = ReadString(reader, "title"),
                        url = ReadString(reader, "url"),
                        visitedAt = visitedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        mode = "Normal",
                        duration = FormatDurationFromMicroseconds(ReadLong(reader, "visit_duration")),
                        durationEstimated = false
                    });
                }
            }
            catch
            {
            }
            finally
            {
                TryDelete(tempPath);
            }

            return entries;
        }

        private static List<NavigationHistoryEntry> ReadFirefoxHistory(string browser, string profile, string historyPath)
        {
            var entries = new List<NavigationHistoryEntry>();
            var tempPath = CopyToTemporaryDatabase(historyPath);
            if (string.IsNullOrWhiteSpace(tempPath))
            {
                return entries;
            }

            try
            {
                using var connection = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT moz_places.url, moz_places.title, moz_historyvisits.visit_date
                    FROM moz_places
                    INNER JOIN moz_historyvisits ON moz_places.id = moz_historyvisits.place_id
                    WHERE moz_places.url IS NOT NULL AND moz_places.url <> ''
                    ORDER BY moz_historyvisits.visit_date DESC";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var visitedAt = FromFirefoxTime(ReadLong(reader, "visit_date"));
                    entries.Add(new NavigationHistoryEntry
                    {
                        browser = browser,
                        profile = profile,
                        title = ReadString(reader, "title"),
                        url = ReadString(reader, "url"),
                        visitedAt = visitedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        mode = "Normal",
                        duration = "No disponible",
                        durationEstimated = false
                    });
                }
            }
            catch
            {
            }
            finally
            {
                TryDelete(tempPath);
            }

            return entries;
        }

        private static void EstimateMissingDurations(List<NavigationHistoryEntry> entries)
        {
            var groupedEntries = entries
                .GroupBy(e => $"{e.browser}|{e.profile}", StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedEntries)
            {
                var ordered = group
                    .OrderBy(e => ParseVisitDate(e.visitedAt))
                    .ToList();

                for (var i = 0; i < ordered.Count - 1; i++)
                {
                    if (ordered[i].duration != "No disponible")
                    {
                        continue;
                    }

                    var current = ParseVisitDate(ordered[i].visitedAt);
                    var next = ParseVisitDate(ordered[i + 1].visitedAt);
                    var estimate = next - current;
                    if (estimate.TotalSeconds > 0 && estimate.TotalMinutes <= MaxDurationEstimateMinutes)
                    {
                        ordered[i].duration = FormatTimeSpan(estimate);
                        ordered[i].durationEstimated = true;
                    }
                }
            }
        }

        private static string CopyToTemporaryDatabase(string sourcePath)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"simpe-history-{Guid.NewGuid():N}.sqlite");
                File.Copy(sourcePath, tempPath, overwrite: true);
                return tempPath;
            }
            catch
            {
                return "";
            }
        }

        private static bool HasColumn(SqliteConnection connection, string tableName, string columnName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName})";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (ReadString(reader, "name").Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static DateTime FromChromiumTime(long value)
        {
            try
            {
                return DateTime.SpecifyKind(new DateTime(1601, 1, 1), DateTimeKind.Utc)
                    .AddMilliseconds(value / 1000.0)
                    .ToLocalTime();
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static DateTime FromFirefoxTime(long value)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(value / 1000).LocalDateTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string FormatDurationFromMicroseconds(long value)
        {
            if (value <= 0)
            {
                return "No disponible";
            }

            return FormatTimeSpan(TimeSpan.FromMilliseconds(value / 1000.0));
        }

        private static string FormatTimeSpan(TimeSpan value)
        {
            if (value.TotalHours >= 1)
            {
                return $"{(int)value.TotalHours}h {value.Minutes}m";
            }

            if (value.TotalMinutes >= 1)
            {
                return $"{(int)value.TotalMinutes}m {value.Seconds}s";
            }

            return $"{Math.Max(1, value.Seconds)}s";
        }

        private static DateTime ParseVisitDate(string value)
        {
            return DateTime.TryParse(value, out var parsed) ? parsed : DateTime.MinValue;
        }

        private static long ReadLong(SqliteDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return 0;
            }

            return Convert.ToInt64(reader.GetValue(ordinal));
        }

        private static string ReadString(SqliteDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
