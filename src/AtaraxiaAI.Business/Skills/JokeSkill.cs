using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Skills
{
    internal static class JokeSkill
    {
        internal static async Task TellMeAJokeAsync(SpeechEngine speechEngine, IIntegrationFactory integrations, CancellationToken cancellationToken)
        {
            IJokeService jokeService = integrations.CreateJokeService();
            Joke joke = await jokeService.GetJokeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            await SayJokeAsync(joke, speechEngine, cancellationToken);
        }

        internal static async Task TellMeADadJokeAsync(SpeechEngine speechEngine, IIntegrationFactory integrations, CancellationToken cancellationToken)
        {
            IJokeService dadJokeService = integrations.CreateJokeService(dadJoke: true);
            Joke joke = await dadJokeService.GetJokeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            await SayJokeAsync(joke, speechEngine, cancellationToken);
        }

        private static async Task SayJokeAsync(Joke joke, SpeechEngine speechEngine, CancellationToken cancellationToken)
        {
            if (joke == null) return;
            if (joke.JokeType == Joke.JokeTypes.Single && !string.IsNullOrEmpty(joke.JokeLine))
            {
                await speechEngine.SpeakAsync(joke.JokeLine, cancellationToken);
            }
            else if (joke.JokeType == Joke.JokeTypes.TwoPart && !string.IsNullOrEmpty(joke.Setup) && !string.IsNullOrEmpty(joke.Delivery))
            {
                await speechEngine.SpeakAsync(joke.Setup, cancellationToken);
                await Task.Delay(500, cancellationToken);
                await speechEngine.SpeakAsync(joke.Delivery, cancellationToken);
            }
        }
    }
}
