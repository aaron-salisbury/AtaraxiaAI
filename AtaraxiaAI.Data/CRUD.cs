using AtaraxiaAI.Data.Base;
using AtaraxiaAI.Data.Domains;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using File = System.IO.File;

namespace AtaraxiaAI.Data
{
    public static class CRUD
    {
        private const string VOSK_DOWNLOAD_URL = "https://alphacephei.com/vosk/models/vosk-model-en-us-0.22-lgraph.zip";
        private const string VOSK_CONTENT_DIRECTORY = "./Detection/Voice/Vosk/";
        private const string VOSK_MODEL = "vosk-model-en-us-0.22-lgraph";
        private const string YOLO_WEIGHTS_DOWNLOAD_URL = "https://pjreddie.com/media/files/yolov3.weights";
        private const string YOLO_CFG_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3.cfg";
        private const string YOLO_WEIGHTS_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3.weights";
        private const string COCO_NAMES_CONTENT_PATH = "./Detection/Vision/YOLO/coco.names";
        private const string HC_CLASSIFIER_CONTENT_PATH = "./Detection/Vision/HaarCascades/haarcascade_frontalface_default.xml";

        // Small versions incase downloading the large models becomes no longer viable.
        private const string VOSK_SMALL_MODEL = "vosk-model-small-en-us-0.15";
        private const string YOLO_CFG_TINY_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3-tiny.cfg";
        private const string YOLO_WEIGHTS_TINY_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3-tiny.weights";

        /// <summary>
        /// Download and extract ML models as necessary.
        /// This is done the first time the app runs since Github's 
        /// file-size limit (100 MB) prevents them from being included in the project.
        /// </summary>
        public static async Task CreateModels(ILogger logger)
        {
            string voskZipPath = Path.Combine(VOSK_CONTENT_DIRECTORY, $"{VOSK_MODEL}.zip");
            if (!File.Exists(voskZipPath))
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        logger.Information("Beginning to download Vosk model.");
                        await client.DownloadFileTaskAsync(new Uri(VOSK_DOWNLOAD_URL), voskZipPath);
                        logger.Information("Vosk model download complete.");
                    }

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
                    using (HttpClient client = new HttpClient())
                    {
                        logger.Information("Beginning to download YOLO model.");
                        await client.DownloadFileTaskAsync(new Uri(YOLO_WEIGHTS_DOWNLOAD_URL), YOLO_WEIGHTS_CONTENT_PATH);
                        logger.Information("YOLO model download complete.");
                    }
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

                string json = File.ReadAllText(filePath);

                if (!string.IsNullOrEmpty(json))
                {
                    return await Json.ToObjectAsync<T>(json);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to read from file system: {e.Message}");
            }

            return default(T);
        }

        public static async Task<IEnumerable<T>> ReadDomainsAsync<T>(string directoryPath, ILogger logger)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, GetJsonFileNameForType<T>());

                string json = File.ReadAllText(filePath);

                if (!string.IsNullOrEmpty(json))
                {
                    return await Json.ToObjectAsync<IEnumerable<T>>(json);
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

        public static async Task<InternalStorage> ReadInternalStorage(ILogger logger)
        {
            const string INTERNAL_DIRECTORY = "./";

            InternalStorage internalStorage = null;

            try
            {
                string filePath = Path.Combine(INTERNAL_DIRECTORY, GetJsonFileNameForType<InternalStorage>());

                if (File.Exists(filePath))
                {
                    internalStorage = await ReadDataAsync<InternalStorage>(INTERNAL_DIRECTORY, logger);
                }
                else
                {
                    internalStorage = new InternalStorage
                    {
                        UserStorageDirectory = GetAppDirectoryPath()
                    };

                    await UpdateDataAsync<InternalStorage>(internalStorage, INTERNAL_DIRECTORY, logger);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to read internal storage: {e.Message}");
            }

            return internalStorage;
        }

        public static async Task UpdateInternalStorage(InternalStorage internalStorage, ILogger logger, string newUserDirectory = null)
        {
            const string INTERNAL_DIRECTORY = "./";

            try
            {
                if (!string.IsNullOrEmpty(newUserDirectory))
                {
                    string previousUserStorageDirectory = internalStorage.UserStorageDirectory;

                    if (!string.IsNullOrEmpty(previousUserStorageDirectory))
                    {
                        foreach (string file in Directory.GetFiles(previousUserStorageDirectory))
                        {
                            string fileName = Path.GetFileName(file);
                            string newFilePath = Path.Combine(newUserDirectory, fileName);

                            if (!File.Exists(newFilePath))
                            {
                                Directory.Move(file, newFilePath);
                            }
                        }
                    }

                    internalStorage = new InternalStorage
                    {
                        UserStorageDirectory = newUserDirectory
                    };
                }

                await UpdateDataAsync<InternalStorage>(internalStorage, INTERNAL_DIRECTORY, logger);
            }
            catch (Exception e)
            {
                logger.Error($"Failed to update file: {e.Message}");
            }
        }

        public static async Task UpdateDataAsync<T>(object data, string directoryPath, ILogger logger)
        {
            try
            {
                FileInfo file = new FileInfo(Path.Combine(directoryPath, GetJsonFileNameForType<T>()));

                if (file != null && file.Directory != null)
                {
                    file.Directory.Create();
                    string json = await Json.StringifyAsync(data);

                    File.WriteAllText(file.FullName, json);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to write to file system: {e.Message}");
            }
        }

        public static async Task UpdateDomainsAsync<T>(IEnumerable<object> domains, string directoryPath, ILogger logger)
        {
            try
            {
                FileInfo file = new FileInfo(Path.Combine(directoryPath, GetJsonFileNameForType<T>()));

                if (file != null && file.Directory != null)
                {
                    file.Directory.Create();
                    string json = await Json.StringifyAsync(domains);

                    File.WriteAllText(file.FullName, json);
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
