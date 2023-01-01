using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Skills.Domains;
using System.Threading;

namespace AtaraxiaAI.Business.Skills
{
    public static class JokeSkill
    {
        public static void TellMeAJoke(SpeechEngine speechEngine)
        {
            Sv443Joke joke = Sv443JokesService.GetJokeAsync().Result;

            if (joke != null)
            {
                if (string.Equals(joke.Type.ToLower(), "single") &&
                    !string.IsNullOrEmpty(joke.Joke))
                {
                    speechEngine.Speak(joke.Joke);
                }
                else if (string.Equals(joke.Type.ToLower(), "twopart") &&
                    !string.IsNullOrEmpty(joke.Setup) &&
                    !string.IsNullOrEmpty(joke.Delivery))
                {
                    speechEngine.Speak(joke.Setup);
                    Thread.Sleep(1000);
                    speechEngine.Speak(joke.Delivery);
                }
            }
        }
    }
}
