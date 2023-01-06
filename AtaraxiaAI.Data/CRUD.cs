using AtaraxiaAI.Data.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

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
        public static void CreateModels(ILogger logger)
        {
            string voskZipPath = Path.Combine(VOSK_CONTENT_DIRECTORY, $"{VOSK_MODEL}.zip");
            if (!File.Exists(voskZipPath))
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        logger.Information("Beginning to download Vosk model.");
                        client.DownloadFile(new Uri(VOSK_DOWNLOAD_URL), voskZipPath);
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
                    using (WebClient client = new WebClient())
                    {
                        logger.Information("Beginning to download YOLO model.");
                        client.DownloadFile(new Uri(YOLO_WEIGHTS_DOWNLOAD_URL), YOLO_WEIGHTS_CONTENT_PATH);
                        logger.Information("YOLO model download complete.");
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to download YOLO model: {e.Message}");
                }
            }
        }

        public static async Task<T> ReadDataAsync<T>(ILogger logger)
        {
            logger.Information("Reading from file system.");

            try
            {
                string filePath = Path.Combine(GetAppDirectoryPath(), GetJsonFileNameForType<T>());

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

        public static async Task<IEnumerable<T>> ReadDomainsAsync<T>(ILogger logger)
        {
            logger.Information("Reading from file system.");

            try
            {
                string filePath = Path.Combine(GetAppDirectoryPath(), GetJsonFileNameForType<T>());

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

        public static async Task UpdateDataAsync<T>(object data, ILogger logger)
        {
            logger.Information("Writing to file system.");

            try
            {
                FileInfo file = new FileInfo(Path.Combine(GetAppDirectoryPath(), GetJsonFileNameForType<T>()));

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

        public static async Task UpdateDomainsAsync<T>(IEnumerable<object> domains, ILogger logger)
        {
            logger.Information("Writing to file system.");

            try
            {
                FileInfo file = new FileInfo(Path.Combine(GetAppDirectoryPath(), GetJsonFileNameForType<T>()));

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
            string typeName = typeof(T).ToString();
            int pos = typeName.LastIndexOf('.') + 1;
            string objectName = typeName.Substring(pos, typeName.Length - pos);

            return $"{objectName}.json";
        }
    }
}
