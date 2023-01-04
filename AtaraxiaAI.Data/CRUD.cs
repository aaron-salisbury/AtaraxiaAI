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
        private const string HC_CLASSIFIER_CONTENT_PATH = "./Detection/Vision/HaarCascades/haarcascade_frontalface_default.xml";
        private const string YOLO_CFG_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3-tiny.cfg";
        private const string YOLO_WEIGHTS_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3-tiny.weights";
        private const string COCO_NAMES_CONTENT_PATH = "./Detection/Vision/YOLO/coco.names";
        private const string VOSK_CONTENT_DIRECTORY = "./Detection/Voice/Vosk/";
        private const string VOSK_MODEL = "vosk-model-small-en-us-0.15";

        //TODO: Perhaps download the 128mb version of the vosk model, similar to the yolo file process. - https://alphacephei.com/vosk/models   https://alphacephei.com/vosk/models/vosk-model-en-us-0.22-lgraph.zip

        //TODO: Update vision engine to use the full model and cfg. Maybe have the tiny/small yolo and Vosk models still for backup logic if downloads were to fail.
        private const string YOLO_FULL_WEIGHTS_CONTENT_PATH = "./Detection/Vision/YOLO/yolov3.weights";

        /// <summary>
        /// Download and extract zipped ML models as necessary.
        /// </summary>
        public static void CreateModels(ILogger logger)
        {
            if (!Directory.Exists(Path.Combine(VOSK_CONTENT_DIRECTORY, VOSK_MODEL)))
            {
                try
                {
                    ZipFile.ExtractToDirectory(Path.Combine(VOSK_CONTENT_DIRECTORY, $"{VOSK_MODEL}.zip"), VOSK_CONTENT_DIRECTORY);
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to extract Vosk model: {e.Message}");
                }
            }

            if (!File.Exists(YOLO_FULL_WEIGHTS_CONTENT_PATH))
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        //TODO: I might want to block thread if I'm waiting for this to finish before letting the user use vision.
                        //client.DownloadFileAsync(new Uri("https://pjreddie.com/media/files/yolov3.weights"), YOLO_FULL_WEIGHTS_CONTENT_PATH);
                        client.DownloadFile(new Uri("https://pjreddie.com/media/files/yolov3.weights"), YOLO_FULL_WEIGHTS_CONTENT_PATH);
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to download YOLOv3 model: {e.Message}");
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
