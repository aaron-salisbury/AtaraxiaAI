using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services.Base.DTOs;
using AtaraxiaAI.Data;
using NAudio.Wave;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal class MicrosoftBingSynthesizer : ISynthesizer, IDisposable
    {
        // https://github.com/Noisyfox/ACT.FoxTTS/blob/master/ACT.FoxTTS/ACT.FoxTTS/engine/edge/EdgeTTSEngine.cs
        // https://github.com/Loskh/EdgeTTS.Net
        // https://github.com/rany2/edge-tts/blob/master/src/edge_tts/constants.py

        private const string URL_SPEECH_FORMAT = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken={0}&ConnectionId={1}";
        private const string PITCH = "+1350Hz";
        private const string RATE = "+50%";
        private const string VOLUME = "+100%";

        private SecureString _secureTrustedClientToken;
        private string _voice;
        private ClientWebSocket _webSocket;
        private SemaphoreSlim _slimlock;
        private bool _disposedValue;
        private Timer _connectionKeeper;

        internal MicrosoftBingSynthesizer(CultureInfo culture = null)
        {
            _secureTrustedClientToken = ScrapeEdgeClientToken().Result;

            if (_secureTrustedClientToken != null)
            {
                _voice = GetBingVoiceForCulture(culture ?? new CultureInfo("en-US"), _secureTrustedClientToken).Result;
                _webSocket = new ClientWebSocket();
                _slimlock = new SemaphoreSlim(1, 1);
                _disposedValue = false;
                _connectionKeeper = new Timer(ConnnectKeeper, null, 0, 1000);
            }
        }

        bool ISynthesizer.IsAvailable() => _secureTrustedClientToken != null && !string.IsNullOrEmpty(_voice);

        async Task<bool> ISynthesizer.SpeakAsync(string message)
        {
            var sendMessage = "X-Timestamp:" + Utils.GetFormatedDate() + "\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n";
            sendMessage += "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":" + "\"false\"" + ",\"wordBoundaryEnabled\":" + "\"false\"" + "}," + "\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}\r\n";

            var SSMLPayload = new SSML(message, _voice, PITCH, RATE, VOLUME);
            var retryTimes = 0;
            await _slimlock.WaitAsync();
            while (retryTimes < 3)
            {
                try
                {
                    var token = CancellationToken.None;

                    switch (_webSocket.State)
                    {
                        //Steal Fox's Code
                        case WebSocketState.None:
                            await EstablishConnectionAsync(token);
                            break;
                        case WebSocketState.Connecting:
                        case WebSocketState.Open:
                            // All good
                            break;
                        case WebSocketState.CloseSent:
                        case WebSocketState.CloseReceived:
                        case WebSocketState.Closed:
                            _webSocket.Abort();
                            _webSocket.Dispose();
                            _webSocket = new ClientWebSocket();
                            await EstablishConnectionAsync(token);
                            break;
                        case WebSocketState.Aborted:
                            _webSocket.Dispose();
                            _webSocket = new ClientWebSocket();
                            await EstablishConnectionAsync(token);
                            break;
                        default:
                            break;
                    }

                    Task<MemoryStream> audioTask = ReceiveAudioAsync(_webSocket, SSMLPayload.RequestID, token);
                    await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(sendMessage)), WebSocketMessageType.Text, true, token);
                    await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(SSMLPayload.ToString())), WebSocketMessageType.Text, true, token);
                    while (!audioTask.IsCompleted)
                    {
                        await Task.Delay(10);
                    }

                    if (audioTask.Result.Length == 0)
                        throw new IOException("Received Empty Audio!");

                    _slimlock.Release();

                    using (MemoryStream retMs = new MemoryStream())
                    using (MemoryStream ms = new MemoryStream(audioTask.Result.ToArray()))  //TODO: May be able to avoid the conversion by using an output format other than "audio-24khz-48kbitrate-mono-mp3".
                    using (Mp3FileReader reader = new Mp3FileReader(ms))
                    using (RawSourceWaveStream rs = new RawSourceWaveStream(reader, new WaveFormat(16000, 1)))
                    using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(rs))
                    {
                        WaveFileWriter.WriteWavFileToStream(retMs, pcmStream);
                        SpeechEngine.StreamSpeechToSpeaker(retMs.ToArray(), message);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    retryTimes++;

                    if (ex.InnerException != null && ex.InnerException is SocketException && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionReset)
                        Log.Error("[EdgeTTS]Azure服务器主动断开连接");
                    else
                        Log.Error("[EdgeTTS]Azure服务器连接异常");

                    Log.Error(ex.ToString());
                    Log.Information($"[EdegTTS]连接失败，开始第{retryTimes}次重试 :(");
                }
            }
            Log.Information($"[EdegTTS]重试失败 :(");
            _slimlock.Release();

            return false;
        }

        private static async Task<SecureString> ScrapeEdgeClientToken()
        {
            const string URL_CONSTANTS = "https://raw.githubusercontent.com/rany2/edge-tts/master/src/edge_tts/constants.py";
            const string TOKEN_CONSTANT = "TRUSTED_CLIENT_TOKEN = \"";

            SecureString scrapedEdgeClientToken = null;
            string error = null;

            try
            {
                using (StreamReader stream = new StreamReader(await WebRequests.GetWebRequestStreamAsync(URL_CONSTANTS, AI.HttpClientFactory, AI.Logger)))
                {
                    string response = stream.ReadToEnd();

                    if (!string.IsNullOrEmpty(response) && response.Contains(TOKEN_CONSTANT))
                    {
                        string responseSubset = response.Substring(response.LastIndexOf(TOKEN_CONSTANT) + TOKEN_CONSTANT.Length);

                        int charLocation = responseSubset.IndexOf("\"");
                        if (charLocation > 0)
                        {
                            scrapedEdgeClientToken = new NetworkCredential(string.Empty, responseSubset[..charLocation]).SecurePassword;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                scrapedEdgeClientToken = null;
            }

            if (scrapedEdgeClientToken == null)
            {
                string errorMessage = "Failed to parse Edge TTS trusted client token";
                if (string.IsNullOrEmpty(error))
                {
                    errorMessage += ".";
                }
                else
                {
                    errorMessage += $": {error}";
                }

                AI.Logger.Error(errorMessage);
            }

            return scrapedEdgeClientToken;
        }

        private static async Task<string> GetBingVoiceForCulture(CultureInfo culture, SecureString secureToken, bool isFemale = true)
        {
            const string URL_VOICES_FORMAT = "https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/voices/list?trustedclienttoken={0}";

            string voice = null;

            if (string.Equals(culture.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                voice = isFemale ? "en-US-JennyNeural" : "en-US-GuyNeural"; // Alt. Female: en-US-AriaNeural
            }
            else
            {
                string url = string.Format(URL_VOICES_FORMAT, new NetworkCredential(string.Empty, secureToken).Password);
                string json = await WebRequests.SendHTTPJsonRequestAsync(url, AI.HttpClientFactory, AI.Logger);
                List<BingVoice> voices = JsonSerializer.Deserialize<List<BingVoice>>(json);

                voice = voices
                    .Where(v => string.Equals(v.Gender, isFemale ? "Female" : "Male") && string.Equals(v.Locale, culture.Name, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault()?
                    .ShortName;
            }

            if (string.IsNullOrEmpty(voice))
            {
                AI.Logger.Error("Failed to set Bing synthesizer voice.");
            }

            return voice;
        }

        private void ConnnectKeeper(object state)
        {
            _slimlock.Wait();
#if DEBUG
            Debug.WriteLine($"[{DateTime.Now}] Keeper WS_STATE={_webSocket.State}");
#endif
            try
            {
                switch (_webSocket.State)
                {
                    case WebSocketState.Closed:
                        _webSocket.Abort();
                        _webSocket.Dispose();
                        _webSocket = new ClientWebSocket();
                        EstablishConnectionAsync(CancellationToken.None).Wait();
                        break;
                    case WebSocketState.Aborted:
                        _webSocket = new ClientWebSocket();
                        EstablishConnectionAsync(CancellationToken.None).Wait();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[{DateTime.Now}] Keeper {ex.Message}");
#endif
            }
            finally
            {
                _slimlock.Release();
            }
        }

        private async Task EstablishConnectionAsync(CancellationToken token)
        {
            string url = string.Format(URL_SPEECH_FORMAT, new NetworkCredential(string.Empty, _secureTrustedClientToken).Password, Utils.GetUUID());

            var options = _webSocket.Options;
            options.SetRequestHeader("Pragma", "no-cache");
            options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
            options.SetRequestHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            options.SetRequestHeader("Cache-Control", "no-cache");

            await _webSocket.ConnectAsync(new Uri(url), token);
        }

        private async Task<MemoryStream> ReceiveAudioAsync(WebSocket client, string requestId, CancellationToken token)
        {
            var buffer = new MemoryStream(10 * 1024);
            var audioBuffer = new MemoryStream();
            var state = SessionState.NotStarted;

            while (true)
            {
                if (client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable ||
                    client.CloseStatus == WebSocketCloseStatus.InternalServerError ||
                    client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
                {
                    return audioBuffer;
                }
                var array = new byte[5 * 1024];
                var receive = await client.ReceiveAsync(new ArraySegment<byte>(array), token);
                if (receive.Count == 0)
                    continue;

                buffer.Write(array, (int)buffer.Position, receive.Count);
                if (receive.EndOfMessage == false)
                {
                    continue;
                }

                array = buffer.ToArray();

                switch (receive.MessageType)
                {
                    case WebSocketMessageType.Text:
                        var content = Encoding.UTF8.GetString(array, 0, array.Length);
                        if (!content.StartsWith($"X-RequestId:{requestId}"))
                            throw new IOException($"Unexpected request id during streaming:{content}");
                        switch (state)
                        {
                            case SessionState.NotStarted:
                                if (content.Contains("Path:turn.start"))
                                {
                                    state = SessionState.TurnStarted;
                                }
                                break;
                            case SessionState.TurnStarted:
                                if (content.Contains("Path:turn.end"))
                                {
                                    throw new IOException("Unexpected turn.end");
                                }
                                else if (content.Contains("Path:turn.start"))
                                {
                                    throw new IOException("Turn already started");
                                }
                                break;
                            case SessionState.Streaming:
                                if (content.Contains("Path:turn.end"))
                                {
                                    // All done
                                    return audioBuffer;
                                }
                                else
                                {
                                    throw new IOException($"Unexpected message during streaming: {content}");
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;

                    case WebSocketMessageType.Binary:
                        if (array.Length < 2)
                            throw new IOException("Message too short");
                        var headerLen = (array[0] << 8) + array[1];
                        if (buffer.Length < 2 + headerLen)
                            throw new IOException("Message too short");
                        var header = Encoding.UTF8.GetString(array, 2, headerLen);
                        if (!header.StartsWith($"X-RequestId:{requestId}"))
                            throw new IOException("Unexpected request id during streaming");
                        switch (state)
                        {
                            case SessionState.NotStarted:
                                throw new IOException($"Unexpected Binary");
                            case SessionState.TurnStarted:
                            case SessionState.Streaming:
                                if (!header.EndsWith("Path:audio\r\n"))
                                {
                                    throw new IOException($"Unexpected Binary with header: {header}");
                                }
                                state = SessionState.Streaming;
                                audioBuffer.Write(array, headerLen + 2, receive.Count - headerLen - 2);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;

                    case WebSocketMessageType.Close:
                        throw new IOException("Unexpected closing of connection");
                }
                buffer = new MemoryStream();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _webSocket.Abort();
                    _webSocket.Dispose();
                }

                _connectionKeeper.Dispose();

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public enum SessionState
    {
        NotStarted,
        TurnStarted, // turn.start received
        Streaming, // audio binary started
    }

    public class Voice
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Gender { get; set; }
        public string Locale { get; set; }
        public string SuggestedCodec { get; set; }
        public string FriendlyName { get; set; }
        public string Status { get; set; }
    }

    internal class SSML
    {
        public string Voice;
        public string Pitch;
        public string Rate;
        public string Volume;
        public string Sentence;
        public string RequestID;

        public SSML(string sentence, string voice, string pitch, string rate, string volume)
        {
            Voice = voice;
            Pitch = pitch;
            Rate = rate;
            Volume = volume;
            Sentence = sentence;
            RequestID = Guid.NewGuid().ToString().Replace("-", "");
        }

        private static string GetFormatedDate()
        {
            return DateTime.UtcNow.ToString("ddd MMM yyyy H:m:s", CultureInfo.CreateSpecificCulture("en-GB")) + " GMT+0000 (Coordinated Universal Time)";
        }

        public override string ToString()
        {
            var message = "X-RequestId:" + RequestID + "\r\nContent-Type:application/ssml+xml\r\n";
            message += "X-Timestamp:" + GetFormatedDate() + "Z\r\nPath:ssml\r\n\r\n";
            message += "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>";
            message += "<voice  name='" + Voice + "'>" + "<prosody pitch='" + Pitch + "' rate ='" + Rate + "' volume='" + Volume + "'>";
            message += Sentence;
            message += "</prosody></voice></speak>";

            return message;
        }
    }

    public static class Utils
    {
        public static string GetUUID() => Guid.NewGuid().ToString().Replace("-", "");

        public static string GetFormatedDate()
        {
            return DateTime.UtcNow.ToString("ddd MMM yyyy H:m:s", CultureInfo.CreateSpecificCulture("en-GB")) + " GMT+0000 (Coordinated Universal Time)";
        }
    }
}
