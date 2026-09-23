using AtaraxiaAI.Business.Persistence;
using RunnethOverStudio.AppToolkit.Modules.Access;
using Serilog;
using System;

namespace AtaraxiaAI.Business.Services;

// Application-scoped dependencies shared by provider adapters. AppData is set after local data loads.
public sealed class IntegrationDependencies
{
    public IntegrationDependencies(IHttpRequester httpRequester, ILogger logger)
    {
        HttpRequester = httpRequester ?? throw new ArgumentNullException(nameof(httpRequester));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AppData AppData { get; set; }
    public IHttpRequester HttpRequester { get; }
    public ILogger Logger { get; }
}
