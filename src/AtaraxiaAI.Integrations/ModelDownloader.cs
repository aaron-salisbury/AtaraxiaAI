using RunnethOverStudio.AppToolkit.Modules.Access;
using Serilog;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations
{
    internal static class ModelDownloader
    {
        private const string VOSK_DOWNLOAD_URL = "https://alphacephei.com/vosk/models/vosk-model-en-us-0.22-lgraph.zip";
        private const string YOLO_WEIGHTS_DOWNLOAD_URL = "https://pjreddie.com/media/files/yolov3.weights";
        private const string VOSK_CONTENT_DIRECTORY = "./Detection/Voice/Vosk/";
        private const string VOSK_MODEL = "vosk-model-en-us-0.22-lgraph";
        private const string YOLO_WEIGHTS_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3.weights";

        public static async Task CreateModels(IHttpRequester requester, ILogger logger)
        {
            string voskZipPath = Path.Combine(VOSK_CONTENT_DIRECTORY, $"{VOSK_MODEL}.zip");
            Directory.CreateDirectory(VOSK_CONTENT_DIRECTORY);
            Directory.CreateDirectory(Path.GetDirectoryName(YOLO_WEIGHTS_CONTENT_PATH));
            if (!Directory.Exists(Path.Combine(VOSK_CONTENT_DIRECTORY, VOSK_MODEL)))
            {
                try
                {
                    logger.Information("Beginning to download Vosk model.");
                    await requester.DownloadFileAsync(VOSK_DOWNLOAD_URL, voskZipPath, deletePreexisting: true);
                    logger.Information("Vosk model download complete.");

                    logger.Information("Beginning to extract Vosk model.");
                    ZipFile.ExtractToDirectory(voskZipPath, VOSK_CONTENT_DIRECTORY);
                    logger.Information("Vosk model extraction complete.");
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to download and extract Vosk model: {e.Message}");

                    try
                    {
                        File.Delete(voskZipPath);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to clean-up Vosk model after previous extract failure: {ex.Message}");
                    }
                }
            }

            if (!File.Exists(YOLO_WEIGHTS_CONTENT_PATH))
            {
                try
                {
                    logger.Information("Beginning to download YOLO model.");
                    await requester.DownloadFileAsync(YOLO_WEIGHTS_DOWNLOAD_URL, YOLO_WEIGHTS_CONTENT_PATH);
                    logger.Information("YOLO model download complete.");
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to download YOLO model: {e.Message}");
                }
            }
        }

    }
}
