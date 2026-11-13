using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Downloads;

/// <summary>
/// Downloads large files, such as local models, so an interrupted download resumes where it stopped instead of starting
/// over.
/// </summary>
/// <remarks>Adapted from HomiliaMessenger's FileDownload.</remarks>
internal static class FileDownload
{
    internal const int BUFFER_SIZE = 1024 * 1024;

    // A download that receives nothing for this long is treated as failed (and can be resumed).
    internal static readonly TimeSpan STALL_TIMEOUT = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Downloads <paramref name="url"/> into <paramref name="path"/>, continuing a partial file with a range request.
    /// The partial file is kept when the download fails or is canceled, so the next attempt resumes it.
    /// </summary>
    /// <exception cref="TimeoutException">Nothing was received for <see cref="STALL_TIMEOUT"/>.</exception>
    /// <exception cref="InvalidDataException">The server sent more than <paramref name="size"/> bytes.</exception>
    internal static async Task DownloadAsync(HttpClient client, Uri url, string path, long size, DownloadProgress progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(progress);

        long existing = ExistingLength(path);

        if (existing == size)
        {
            return;
        }

        if (existing > size)
        {
            File.Delete(path);
            // Counted toward progress at its expected size.
            progress.Add(-size);
            existing = 0;
        }

        using HttpRequestMessage request = new(HttpMethod.Get, url);

        if (existing > 0)
        {
            request.Headers.Range = new RangeHeaderValue(existing, null);
        }

        // One timer for the whole download, restarted whenever data arrives.
        using CancellationTokenSource stall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stall.CancelAfter(STALL_TIMEOUT);

        try
        {
            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stall.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // A server that ignores the range sends the whole file, so start the file over.
            bool append = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;

            if (existing > 0 && !append)
            {
                progress.Add(-existing);
                existing = 0;
            }

            await using Stream source = await response.Content.ReadAsStreamAsync(stall.Token).ConfigureAwait(false);
            await using FileStream target = new(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, useAsync: true);
            byte[] buffer = new byte[BUFFER_SIZE];
            long written = existing;
            int read;

            while ((read = await source.ReadAsync(buffer, stall.Token).ConfigureAwait(false)) > 0)
            {
                stall.CancelAfter(STALL_TIMEOUT);
                written += read;

                if (written > size)
                {
                    throw new InvalidDataException("The download is larger than expected.");
                }

                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                progress.Add(read);
                progress.Report();
            }
        }
        catch (OperationCanceledException) when (stall.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The download stopped receiving data.");
        }
    }

    /// <summary>
    /// A message saying there is not enough disk space for <paramref name="bytesNeeded"/> more bytes, or
    /// <see langword="null"/> when there is enough.
    /// </summary>
    internal static string? MissingSpace(string directory, long bytesNeeded, string what)
    {
        long available = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(directory))!).AvailableFreeSpace;

        return available < bytesNeeded
            ? $"{what} needs {FormatSize(bytesNeeded)} more disk space; {FormatSize(available)} is free."
            : null;
    }

    internal static async Task<string> HashAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, useAsync: true);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    internal static long ExistingLength(string path)
    {
        FileInfo info = new(path);
        return info.Exists ? info.Length : 0;
    }

    internal static string FormatSize(long bytes)
    {
        return bytes >= 1_000_000_000 ? $"{bytes / 1_000_000_000d:0.0} GB" : $"{bytes / 1_000_000d:0} MB";
    }
}

/// <summary>
/// Overall progress of a download, reported at most every <see cref="INTERVAL"/> so the UI is not flooded.
/// </summary>
internal sealed class DownloadProgress
{
    internal static readonly TimeSpan INTERVAL = TimeSpan.FromMilliseconds(200);

    private readonly Action<long, long>? _report;
    private readonly long _total;
    private readonly Stopwatch _sinceReport = Stopwatch.StartNew();
    private long _downloaded;

    /// <param name="report">Receives the bytes downloaded so far and the total.</param>
    internal DownloadProgress(Action<long, long>? report, long total, long downloaded)
    {
        _report = report;
        _total = total;
        _downloaded = downloaded;
        Report(force: true);
    }

    internal void Add(long bytes)
    {
        _downloaded += bytes;
    }

    internal void Report(bool force = false)
    {
        if (_report is not null && (force || _sinceReport.Elapsed >= INTERVAL))
        {
            _report(_downloaded, _total);
            _sinceReport.Restart();
        }
    }
}
