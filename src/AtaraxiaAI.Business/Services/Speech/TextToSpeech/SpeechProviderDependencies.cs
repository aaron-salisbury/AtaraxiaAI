using AtaraxiaAI.Business.Persistence;
using RunnethOverStudio.AppToolkit.Modules.Access;
using Serilog;
using System;

namespace AtaraxiaAI.Business.Services;

// Dependencies for a single application's speech providers. Providers never reach into AI's static state.
public sealed class SpeechProviderDependencies
{
    public SpeechProviderDependencies(Func<AppData> appData, IHttpRequester httpRequester, ILogger logger)
    {
        _appData = appData ?? throw new ArgumentNullException(nameof(appData));
        HttpRequester = httpRequester ?? throw new ArgumentNullException(nameof(httpRequester));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private readonly Func<AppData> _appData;
    public AppData AppData => _appData();
    public IHttpRequester HttpRequester { get; }
    public ILogger Logger { get; }
}
