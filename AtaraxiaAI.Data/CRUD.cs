using AtaraxiaAI.Data.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AtaraxiaAI.Data
{
    public static class CRUD
    {
        private const string YOLO_CFG_PATH = "AtaraxiaAI.Data.Detection.PWCVision.yolov3-tiny.cfg";
        private const string YOLO_WEIGHTS_PATH = "AtaraxiaAI.Data.Detection.PWCVision.yolov3-tiny.weights";
        private const string COCO_NAMES_PATH = "AtaraxiaAI.Data.Detection.PWCVision.coco.names";

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

        public static byte[] ReadYoloCFGBuffer()
        {
            using (Stream stream = GetEmbeddedResourceStream(YOLO_CFG_PATH))
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);

                return memoryStream.GetBuffer();
            }
        }

        public static byte[] ReadYoloWeightsBuffer()
        {
            using (Stream stream = GetEmbeddedResourceStream(YOLO_WEIGHTS_PATH))
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);

                return memoryStream.GetBuffer();
            }
        }

        public static string[] ReadCOCOClassLabels()
        {
            List<string> labels = new List<string>();

            using (Stream stream = GetEmbeddedResourceStream(COCO_NAMES_PATH))
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
