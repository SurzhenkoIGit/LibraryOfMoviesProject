using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfMoviesProject.Services
{
    public class KinopoiskService
    {
        private readonly RestClient _client;
        private readonly string _apiKey;

        public KinopoiskService(string apiKey)
        {
            _client = new RestClient("https://kinopoiskapiunofficial.tech/api/v2.2");
            _apiKey = apiKey;
        }

        public async Task<JObject> GetFilmsAsync(string type = "ALL", int page = 1, string keyword = "", string order = "RATING", int? countryId = null, int? year = null)
        {
            var request = new RestRequest("/films", Method.Get);
            request.AddHeader("X-API-KEY", _apiKey);

            request.AddParameter("type", type);
            request.AddParameter("page", page);
            request.AddParameter("order", order);

            if (!string.IsNullOrWhiteSpace(keyword))
                request.AddParameter("keyword", keyword);

            if (countryId.HasValue)
                request.AddParameter("countries", countryId.Value);
           
            if(year.HasValue)
            {
                request.AddParameter("yearFrom", year.Value);
                request.AddParameter("yearTo", year.Value);
            }

            var response = await _client.ExecuteAsync(request);

            if (response.IsSuccessful)
                return JObject.Parse(response.Content);

            throw new Exception($"Ошибка API: {response.StatusCode} - {response.Content}");
        }

        public async Task<JObject> GetPopularFilmsAsync(int page = 1)
        {
            var request = new RestRequest("/films/top", Method.Get);
            request.AddHeader("X-API-KEY", _apiKey);

            request.AddParameter("type", "TOP_100_POPULAR_FILMS");
            request.AddParameter("page", page);

            var response = await _client.ExecuteAsync(request);

            if(response.IsSuccessful)
                return JObject.Parse(response.Content);

            throw new Exception($"Ошибка API: {response.StatusCode} - {response.Content}");
        }

        public async Task<JObject> GetFilmByIdAsync(int id)
        {
            var request = new RestRequest($"films/{id}", method: Method.Get);
            request.AddHeader("X-API-KEY", _apiKey);

            var response = await _client.ExecuteAsync(request);

            if(response.IsSuccessful)
                return JObject.Parse(response.Content);

            throw new Exception($"Ошибка API: {response.StatusCode} - {response.Content}");
        }

        public async Task<JObject> GetFiltersAsync()
        {
            var request = new RestRequest("/films/filters", Method.Get);

            request.AddHeader("X-API-KEY", _apiKey);

            var response = await _client.ExecuteAsync(request);

            if(response.IsSuccessful)
                return JObject.Parse(response.Content);

            throw new Exception($"Ошибка API: {response.StatusCode} - {response.Content}");
        }
        
        public async Task<JObject> GetFilmsByGenreAsync(int gernreId, int page = 1, string order = "RATING", int? countryId = null, int? year = null)
        {
            var request = new RestRequest("/films", Method.Get);
            request.AddHeader("X-API-KEY", _apiKey);

            request.AddParameter("type", "ALL");
            request.AddParameter("page", page);
            request.AddParameter("order", order);
            request.AddParameter("genres", gernreId);

            if (countryId.HasValue)
                request.AddParameter("countries", countryId.Value);

            if(year.HasValue)
            {
                request.AddParameter("yearFrom", year.Value);
                request.AddParameter("yearTo", year.Value);
            }

            var response = await _client.ExecuteAsync(request);

            if(response.IsSuccessful)
                return JObject.Parse(response.Content);

            throw new Exception($"Ошибка API: {response.StatusCode} - {response.Content}");
        }
    }
}
