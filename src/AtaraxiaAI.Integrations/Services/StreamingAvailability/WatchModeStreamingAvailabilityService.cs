using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.DTOs;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using RunnethOverStudio.AppToolkit.Modules.Access;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    // https://api.watchmode.com/docs/#api-reference
    internal class WatchModeStreamingAvailabilityService : IStreamingAvailabilityService
    {
        private readonly IntegrationDependencies _dependencies;
        private const string API_KEY = null; //TODO: Use your own key.
        private const int API_USAGE_MAX = 1000;
        private const string INTERNAL_DIRECTORY = "./";
        private const string FILE_ADDRESS = "https://api.watchmode.com/datasets/title_id_map.csv";
        private const string URL_SOURCES_FORMAT = "https://api.watchmode.com/v1/title/{0}/sources/?apiKey={1}"; //{0}Title ID, {1}API Key

        private DateTime? _lastIDPullDate;
        private string _iDsPath;

        internal WatchModeStreamingAvailabilityService(IntegrationDependencies dependencies)
        {
            _dependencies = dependencies;
            // A missing API key must not trigger a network download during startup.
            if (!string.IsNullOrWhiteSpace(API_KEY)) RefreshIDs();
        }

        public async Task<IEnumerable<string>> GetMovieStreamOfferingsAsync(string title)
        {
            return await GetSubscriptionStreamOfferings(title, isMovie: true);
        }

        public async Task<IEnumerable<string>> GetTVShowStreamOfferingsAsync(string title)
        {
            return await GetSubscriptionStreamOfferings(title, isMovie: false);
        }

        private async Task<IEnumerable<string>> GetSubscriptionStreamOfferings(string title, bool isMovie)
        {
            if (!IsAvailable())
            {
                return null;
            }

            IEnumerable<string> sources = null;

            string watchmodeID = GetIDForTitle(title, isMovie);

            if (!string.IsNullOrEmpty(watchmodeID))
            {
                string url = string.Format(URL_SOURCES_FORMAT, watchmodeID, API_KEY);

                string json = await _dependencies.HttpRequester.SendHTTPJsonRequestAsync(url);

                if (!string.IsNullOrEmpty(json))
                {
                    List<WatchmodeSource> wSources = JsonSerializer.Deserialize<List<WatchmodeSource>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (wSources != null)
                    {
                        sources = wSources
                            .Where(ws => string.Equals(ws.Type, "sub"))
                            .Select(ws => ws.Name);
                    }
                }

                _dependencies.AppData.WatchmodeCurrentAPIUsage++;
            }

            return sources;
        }

        private bool IsAvailable()
        {
            //TODO: Check if Watchmode reset date is null.
            // If this is the first time, might need to be added to AppData.
            // If it is null and the API key is not null, query the service for the reset date and current API usage.
            // This query itself will probably increment the usage. Should check if what it returns accounts this.
            // Set, then save.

            return !string.IsNullOrEmpty(API_KEY) &&
                API_USAGE_MAX > _dependencies.AppData.WatchmodeCurrentAPIUsage;
        }

        private void RefreshIDs()
        {
            DateTime currentDate = DateTime.Now.Date;

            if (_lastIDPullDate == null || _lastIDPullDate != currentDate)
            {
                string filename = Path.GetFileName(new Uri(FILE_ADDRESS).AbsolutePath);

                _iDsPath = Path.Combine(INTERNAL_DIRECTORY, filename);
                _lastIDPullDate = currentDate;

                if (!File.Exists(_iDsPath) || File.GetCreationTime(_iDsPath).Date != currentDate)
                {
                    _dependencies.HttpRequester.DownloadFileAsync(FILE_ADDRESS, _iDsPath, true).Wait();
                }
            }
        }

        private string GetIDForTitle(string title, bool isMovie)
        {
            string watchModeID = null;

            RefreshIDs();

            CsvConfiguration conf = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null
            };

            using (var reader = new StreamReader(_iDsPath))
            using (var csv = new CsvReader(reader, conf))
            {
                watchModeID = csv.GetRecords<TitleIDMap>()
                    .Where(tim =>
                        string.Equals(tim.TMDBType, isMovie ? "movie" : "tv") &&
                        string.Equals(tim.Title, title, StringComparison.OrdinalIgnoreCase) && //TODO: Like instead of equal.
                        short.TryParse(tim.Year, out short year))
                    .OrderByDescending(tim => Convert.ToInt32(tim.Year))
                    .Select(tim => tim.WatchModeID)
                    .FirstOrDefault();
            }

            return watchModeID;
        }
    }

    [HasHeaderRecord(true)]
    [Delimiter(",")]
    internal class TitleIDMap
    {
        TitleIDMap() { }

        [Name("Watchmode ID")]
        public string WatchModeID { get; set; }

        [Name("IMDB ID")]
        public string IMDBID { get; set; }

        [Name("TMDB ID")]
        public string TMDBID { get; set; }

        [Name("TMDB Type")]
        public string TMDBType { get; set; }

        [Name("Title")]
        public string Title { get; set; }

        [Name("Year")]
        public string Year { get; set; }
    }
}
