using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Presentation.Desktop.Models;

/// <summary>
/// The progress shown while a chosen source, such as a speech or recognizer source, is prepared, which may include
/// downloading its model. Starting another preparation cancels the unfinished one.
/// </summary>
public partial class SourcePreparation : ObservableObject
{
    /// <summary>Whether a source is being prepared.</summary>
    [ObservableProperty]
    private bool _isPreparing;

    /// <summary>Whether <see cref="Progress"/> reports a download; otherwise preparation has no known length.</summary>
    [ObservableProperty]
    private bool _isProgressKnown;

    /// <summary>Download progress from 0 to 100.</summary>
    [ObservableProperty]
    private double _progress;

    /// <summary>What is happening, or why the last preparation failed; <see langword="null"/> when there is nothing to say.</summary>
    [ObservableProperty]
    private string? _status;

    private CancellationTokenSource? _current;

    /// <summary>
    /// Prepares the source named <paramref name="name"/>, cancelling any unfinished preparation, and reports its progress.
    /// </summary>
    /// <param name="prepare">Prepares the source, reporting download progress, and returns whether it is ready.</param>
    /// <returns>
    /// Whether the source is ready, or <see langword="null"/> when another preparation started meanwhile and reports the
    /// outcome instead.
    /// </returns>
    public async Task<bool?> RunAsync(string name, Func<IProgress<double>, CancellationToken, Task<bool>> prepare)
    {
        ArgumentNullException.ThrowIfNull(prepare);

        _current?.Cancel();
        CancellationTokenSource preparation = new();
        _current = preparation;

        IsPreparing = true;
        IsProgressKnown = false;
        Progress = 0;
        Status = $"Preparing {name}…";

        Progress<double> progress = new(percent =>
        {
            if (ReferenceEquals(_current, preparation))
            {
                IsProgressKnown = true;
                Progress = percent;
                Status = percent < 100 ? $"Downloading {name}… {percent:0}%" : $"Installing {name}…";
            }
        });

        try
        {
            bool ready = await prepare(progress, preparation.Token);
            if (!ReferenceEquals(_current, preparation))
            {
                return null;
            }

            Status = ready ? null : $"{name} is unavailable. See the logs for details.";
            return ready;
        }
        catch (OperationCanceledException) when (preparation.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception error)
        {
            Log.Error(error, "Failed to prepare {Source}.", name);
            if (!ReferenceEquals(_current, preparation))
            {
                return null;
            }

            Status = $"Could not change to {name}: {error.Message}";
            return false;
        }
        finally
        {
            if (ReferenceEquals(_current, preparation))
            {
                _current = null;
                IsPreparing = false;
            }

            preparation.Dispose();
        }
    }
}
