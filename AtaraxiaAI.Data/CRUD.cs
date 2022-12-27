using AtaraxiaAI.Data.Base;
using AtaraxiaAI.Data.Domains;
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

        public static List<GrammarChoice> CreateDefaultGrammarChoices(ILogger logger)
        {
            List<GrammarChoice> grammarChoices = new List<GrammarChoice>
            {
                new GrammarChoice() { Word = "Hey Robot" },
                new GrammarChoice() { Word = "tell me a joke" },
                new GrammarChoice() { Word = "tell me a dark joke" },
                new GrammarChoice() { Word = "Move", Classification = GrammarChoice.Classifications.Verb },
                new GrammarChoice() { Word = "Go", Classification = GrammarChoice.Classifications.Verb },
                new GrammarChoice() { Word = "Start", Classification = GrammarChoice.Classifications.Verb },
                new GrammarChoice() { Word = "Stop", Classification = GrammarChoice.Classifications.Verb },
                new GrammarChoice() { Word = "Get", Classification = GrammarChoice.Classifications.Verb },
                new GrammarChoice() { Word = "Left", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place },
                new GrammarChoice() { Word = "Right", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place },
                new GrammarChoice() { Word = "Up", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place },
                new GrammarChoice() { Word = "Down", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place },
                new GrammarChoice() { Word = "North", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place },
                new GrammarChoice() { Word = "South", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place },
                new GrammarChoice() { Word = "West", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place },
                new GrammarChoice() { Word = "East", Classification = GrammarChoice.Classifications.Noun, NounType = GrammarChoice.NounTypes.Place }
            };

            Task.Run(() => { UpdateDomainsAsync<GrammarChoice>(grammarChoices, logger).Wait(); });

            return grammarChoices;
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
