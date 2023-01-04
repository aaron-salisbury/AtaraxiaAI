﻿using AtaraxiaAI.Business.Services.Base.Domains;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Data;
using AtaraxiaAI.Data.Base;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    // https://icanhazdadjoke.com/api
    internal class CanHazDadJokeService : IJokeService
    {
        private const string URL = "https://icanhazdadjoke.com/";

        private string _userAgent;

        internal CanHazDadJokeService(string userAgent = null)
        {
            _userAgent = userAgent ?? "AtaraxiaAI (compatible; https://github.com/aaron-salisbury/AtaraxiaAI)";
        }

        async Task<Joke> IJokeService.GetJokeAsync()
        {
            AI.Log.Logger.Information("Acquiring dad joke.");

            Joke joke = null;

            string json = await WebRequests.SendHTTPJsonRequestAsync(URL, AI.Log.Logger, userAgent: _userAgent);

            if (!string.IsNullOrEmpty(json))
            {
                CanHazDadJoke canHazDadJoke = await Json.ToObjectAsync<CanHazDadJoke>(json);

                if (canHazDadJoke != null)
                {
                    joke = Joke.ConvertFromCanHazDadJoke(canHazDadJoke);
                }
            }

            if (joke == null)
            {
                AI.Log.Logger.Error("Failed to aquire joke.");
            }

            return joke;
        }
    }
}