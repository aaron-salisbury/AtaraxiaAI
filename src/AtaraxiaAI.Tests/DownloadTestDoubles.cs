using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Tests;

/// <summary>Serves a model download from memory, and can fail, stall, or drop the connection partway.</summary>
/// <remarks>Adapted from HomiliaMessenger's Kokoro speech tests.</remarks>
internal sealed class FakeModelServer : HttpMessageHandler, IHttpClientFactory
{
    public FakeModelServer(byte[] content)
    {
        Content = content;
    }

    public byte[] Content { get; set; }
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public bool Hold { get; set; }
    public int? DropAfter { get; set; }
    public long? LastRangeStart { get; private set; }
    public int Requests { get; private set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public HttpClient CreateClient(string name)
    {
        return new(this, disposeHandler: false);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests++;
        Started.TrySetResult();
        if (Hold)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (Status != HttpStatusCode.OK)
        {
            return new(Status);
        }

        long start = request.Headers.Range?.Ranges.First().From ?? 0;
        LastRangeStart = start > 0 ? start : LastRangeStart;
        byte[] body = Content[(int)start..];
        HttpContent content = DropAfter is { } drop ? new StreamContent(new DroppingStream(body, drop)) : new ByteArrayContent(body);
        return new(start > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK) { Content = content };
    }
}

/// <summary>Sends the first bytes, then fails as a dropped connection does.</summary>
internal sealed class DroppingStream(byte[] content, int dropAfter) : MemoryStream(content)
{
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Position >= dropAfter)
        {
            throw new IOException("The connection was reset.");
        }

        return base.Read(buffer, offset, (int)Math.Min(count, dropAfter - Position));
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(Read(buffer, offset, count));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        byte[] chunk = new byte[buffer.Length];
        int read = Read(chunk, 0, chunk.Length);
        chunk.AsSpan(0, read).CopyTo(buffer.Span);
        return ValueTask.FromResult(read);
    }
}

internal sealed class TestFolder : IDisposable
{
    public TestFolder()
    {
        Directory.CreateDirectory(Path);
    }

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AtaraxiaModels-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
