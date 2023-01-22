using AtaraxiaAI.Data.Base;
using AtaraxiaAI.Data.Domains;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using File = System.IO.File;

namespace AtaraxiaAI.Data
{
    public static class CRUD
    {
        private const string INTERNAL_DIRECTORY = "./";
        private const string VOSK_DOWNLOAD_URL = "https://alphacephei.com/vosk/models/vosk-model-en-us-0.22-lgraph.zip";
        private const string VOSK_CONTENT_DIRECTORY = "./Detection/Voice/Vosk/";
        private const string VOSK_MODEL = "vosk-model-en-us-0.22-lgraph";
        private const string YOLO_WEIGHTS_DOWNLOAD_URL = "https://pjreddie.com/media/files/yolov3.weights";
        private const string YOLO_CFG_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3.cfg";
        private const string YOLO_WEIGHTS_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3.weights";
        private const string COCO_NAMES_CONTENT_PATH = "./Detection/Vision/YOLO/coco.names";
        private const string HC_CLASSIFIER_CONTENT_PATH = "./Detection/Vision/HaarCascades/haarcascade_frontalface_default.xml";
        private const string TESSDATA_CONTENT_DIRECTORY = "./Detection/Vision/OCR/tessdata";

        // Small versions in case downloading the large models becomes no longer viable.
        private const string VOSK_SMALL_MODEL = "vosk-model-small-en-us-0.15";
        private const string YOLO_CFG_TINY_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3-tiny.cfg";
        private const string YOLO_WEIGHTS_TINY_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3-tiny.weights";

        /// <summary>
        /// Download and extract ML models as necessary.
        /// This is done the first time the app runs since Github's 
        /// file-size limit (100 MB) prevents them from being included in the project.
        /// </summary>
        public static async Task CreateModels(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            string voskZipPath = Path.Combine(VOSK_CONTENT_DIRECTORY, $"{VOSK_MODEL}.zip");
            if (!File.Exists(voskZipPath))
            {
                try
                {
                    logger.Information("Beginning to download Vosk model.");
                    await WebRequests.DownloadFileAsync(httpClientFactory, VOSK_DOWNLOAD_URL, voskZipPath);
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
                    await WebRequests.DownloadFileAsync(httpClientFactory, YOLO_WEIGHTS_DOWNLOAD_URL, YOLO_WEIGHTS_CONTENT_PATH);
                    logger.Information("YOLO model download complete.");
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to download YOLO model: {e.Message}");
                }
            }
        }

        public static async Task<T> ReadDataAsync<T>(string directoryPath, ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, GetJsonFileNameForType<T>());

                if (File.Exists(filePath))
                {
                    using (FileStream openStream = File.OpenRead(filePath))
                    {
                        return await JsonSerializer.DeserializeAsync<T>(openStream);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to read from file system: {e.Message}");
            }

            return default;
        }

        public static async Task<IEnumerable<T>> ReadDomainsAsync<T>(string directoryPath, ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, GetJsonFileNameForType<T>());

                if (File.Exists(filePath))
                {
                    using (FileStream openStream = File.OpenRead(filePath))
                    {
                        return await JsonSerializer.DeserializeAsync<IEnumerable<T>>(openStream);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to read from file system: {e.Message}");
            }

            return null;
        }

        public static string ReadHaarCascadesClassifierFaceContentPath()
        {
            return HC_CLASSIFIER_CONTENT_PATH;
        }

        public static string ReadVoskModelContentPath()
        {
            return Path.Combine(VOSK_CONTENT_DIRECTORY, VOSK_MODEL);
        }

        public static string ReadTessdataContentPath()
        {
            return TESSDATA_CONTENT_DIRECTORY;
        }

        public static byte[] ReadYoloCFGBuffer()
        {
            using (FileStream stream = File.OpenRead(YOLO_CFG_CONTENT_PATH))
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);

                return memoryStream.GetBuffer();
            }
        }

        public static byte[] ReadYoloWeightsBuffer()
        {
            using (FileStream stream = File.OpenRead(YOLO_WEIGHTS_CONTENT_PATH))
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);

                return memoryStream.GetBuffer();
            }
        }

        public static string[] ReadCOCOClassLabels()
        {
            List<string> labels = new List<string>();

            using (FileStream stream = File.OpenRead(COCO_NAMES_CONTENT_PATH))
            using (StreamReader streamReader = new StreamReader(stream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    labels.Add(line);
                }
            }

            return labels.ToArray();
        }

        public static async Task<AppData> ReadAppData(string appDirectory, ILogger logger)
        {
            AppData appData = null;

            try
            {
                string filePath = Path.Combine(appDirectory, GetJsonFileNameForType<AppData>());

                if (File.Exists(filePath))
                {
                    using (FileStream openStream = File.OpenRead(filePath))
                    {
                        appData = await JsonSerializer.DeserializeAsync<AppData>(openStream);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to read internal storage: {e.Message}");
            }

            bool updatedAppData = false;
            DateTime currentDate = DateTime.Now.Date;
            int currentMonth = currentDate.Month;

            if (appData == null)
            {
                appData = new AppData { MonthOfLastCloudServicesRoll = currentMonth };
                updatedAppData = true;
            }
            else
            {
                if (appData.MonthOfLastCloudServicesRoll != currentMonth)
                {
                    appData.MonthOfLastCloudServicesRoll = currentMonth;
                    appData.MicrosoftAzureSpeechToTextCharCount = 0;
                    appData.GoogleCloudSpeechToTextByteCount = 0;
                    updatedAppData = true;
                }

                if (appData.WatchmodeQuotaResetsOn != null && appData.WatchmodeQuotaResetsOn < currentDate)
                {
                    while (appData.WatchmodeQuotaResetsOn < currentDate)
                    {
                        appData.WatchmodeQuotaResetsOn.Value.AddMonths(1);
                    }
                    appData.WatchmodeCurrentAPIUsage = 0;
                    updatedAppData = true;
                }
            }

            if (updatedAppData)
            {
                await UpdateDataAsync<AppData>(appData, appDirectory, logger);
            }

            return appData;
        }

        public static async Task<InternalStorage> ReadInternalStorage(ILogger logger)
        {
            InternalStorage internalStorage = null;

            try
            {
                string filePath = Path.Combine(INTERNAL_DIRECTORY, GetJsonFileNameForType<InternalStorage>());

                if (File.Exists(filePath))
                {
                    using (FileStream openStream = File.OpenRead(filePath))
                    {
                        internalStorage = await JsonSerializer.DeserializeAsync<InternalStorage>(openStream);
                    }
                }
                else
                {
                    internalStorage = new InternalStorage { UserStorageDirectory = GetAppDirectoryPath() };

                    await UpdateDataAsync<InternalStorage>(internalStorage, INTERNAL_DIRECTORY, logger);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to read internal storage: {e.Message}");
            }

            return internalStorage;
        }

        public static async Task<InternalStorage> UpdateInternalStorage(InternalStorage internalStorage, ILogger logger, string newUserDirectory = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(newUserDirectory))
                {
                    string appDataFileName = GetJsonFileNameForType<AppData>();
                    string oldAppDataFilePath = Path.Combine(internalStorage.UserStorageDirectory, appDataFileName);
                    string newAppDataFilePath = Path.Combine(newUserDirectory, appDataFileName);

                    if (File.Exists(oldAppDataFilePath) && !File.Exists(newAppDataFilePath))
                    {
                        Directory.Move(oldAppDataFilePath, newAppDataFilePath);
                    }

                    internalStorage = new InternalStorage { UserStorageDirectory = newUserDirectory };
                }

                await UpdateDataAsync<InternalStorage>(internalStorage, INTERNAL_DIRECTORY, logger);
            }
            catch (Exception e)
            {
                logger.Error($"Failed to update file: {e.Message}");
            }

            return internalStorage;
        }

        public static async Task UpdateDataAsync<T>(T data, string directoryPath, ILogger logger)
        {
            try
            {
                FileInfo file = new FileInfo(Path.Combine(directoryPath, GetJsonFileNameForType<T>()));

                if (file != null && file.Directory != null)
                {
                    file.Directory.Create();

                    using (FileStream createStream = File.Create(file.FullName))
                    {
                        await JsonSerializer.SerializeAsync<T>(createStream, data);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to write to file system: {e.Message}");
            }
        }

        public static async Task UpdateDomainsAsync<T>(IEnumerable<T> domains, string directoryPath, ILogger logger)
        {
            try
            {
                FileInfo file = new FileInfo(Path.Combine(directoryPath, GetJsonFileNameForType<T>()));

                if (file != null && file.Directory != null)
                {
                    file.Directory.Create();

                    using (FileStream createStream = File.Create(file.FullName))
                    {
                        await JsonSerializer.SerializeAsync<IEnumerable<T>>(createStream, domains);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to write to file system: {e.Message}");
            }
        }

        public static void DeleteDomain<T>(string directoryPath, ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, GetJsonFileNameForType<T>());

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to delete file: {e.Message}");
            }
        }

        private static Stream GetEmbeddedResourceStream(string filename)
        {
            return Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream(filename);
        }

        private static string GetEmbeddedResourceText(string filename)
        {
            string result = string.Empty;

            using (Stream stream = GetEmbeddedResourceStream(filename))
            using (StreamReader streamReader = new StreamReader(stream))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }

        private static string GetAppDirectoryPath()
        {
            string appPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string appName = assemblyName.Substring(0, assemblyName.IndexOf('.'));

            return Path.Combine(appPath, appName);
        }

        private static string GetJsonFileNameForType<T>()
        {
            // I like to name the main application data file of my projects after the solution.
            // This file is usually stored in a local app folder and isn't unrealistic that the user
            // would interact with it, or configure it to instead be stored in a network location.
            // All other data can just be named after its type, assuming no more than one file for each.

            string objectName;
            if (typeof(T) == typeof(AppData))
            {
                string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                objectName = assemblyName.Substring(0, assemblyName.IndexOf('.'));
            }
            else
            {
                string typeName = typeof(T).ToString();
                int pos = typeName.LastIndexOf('.') + 1;
                objectName = typeName.Substring(pos, typeName.Length - pos);
            }

            return $"{objectName}.json";
        }
    }
}
