using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using System.Threading;

namespace AtaraxiaAI.Business.Skills
{
    internal static class JokeSkill
    {
        internal static void TellMeAJoke(SpeechEngine speechEngine)
        {
            IJokeService jokeService = new Sv443JokeService();

            Joke joke = jokeService.GetJokeAsync().Result;

            SayJoke(joke, speechEngine);
        }

        internal static void TellMeADadJoke(SpeechEngine speechEngine)
        {
            IJokeService dadJokeService = new CanHazDadJokeService();

            Joke joke = dadJokeService.GetJokeAsync().Result;

            SayJoke(joke, speechEngine);
        }

        private static void SayJoke(Joke joke, SpeechEngine speechEngine)
        {
            if (joke != null)
            {
                if (joke.JokeType == Joke.JokeTypes.Single && !string.IsNullOrEmpty(joke.JokeLine))
                {
                    speechEngine.Speak(joke.JokeLine);
                }
                else if (joke.JokeType == Joke.JokeTypes.TwoPart && !string.IsNullOrEmpty(joke.Setup) && !string.IsNullOrEmpty(joke.Delivery))
                {
                    speechEngine.Speak(joke.Setup);
                    Thread.Sleep(500);
                    speechEngine.Speak(joke.Delivery);
                }
            }
        }
    }
}
