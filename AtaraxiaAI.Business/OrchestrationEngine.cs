﻿using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Skills;

namespace AtaraxiaAI.Business
{
    public class OrchestrationEngine
    {
        private SpeechEngine _speechEngine;

        public OrchestrationEngine(SpeechEngine speechEngine)
        {
            _speechEngine = speechEngine;
        }

        public void Heard(string message)
        {
            AI.Log.Logger.Information($"Heard \"{message}\".");

            switch (message.ToLower())
            {
                case "tell me a joke":
                    Sv443Joke joke = Sv443Jokes.GetJokeAsync().Result;
                    if (joke != null && !string.IsNullOrEmpty(joke.Joke))
                    {
                        _speechEngine.Speak(joke.Joke);
                    }
                    break;
                case "tell me a dark joke":
                    Sv443Joke darkJoke = Sv443Jokes.GetDarkJokeAsync().Result;
                    if (darkJoke != null && !string.IsNullOrEmpty(darkJoke.Joke))
                    {
                        _speechEngine.Speak(darkJoke.Joke);
                    }
                    break;
            }
        }
    }
}
