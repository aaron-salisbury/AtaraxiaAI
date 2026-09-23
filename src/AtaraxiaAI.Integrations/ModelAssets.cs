using System.IO;

namespace AtaraxiaAI.Integrations
{
    internal static class ModelAssets
    {
        internal static string VoskModelPath => "./Detection/Voice/Vosk/vosk-model-en-us-0.22-lgraph";
        internal static string HaarCascadePath => "./Detection/Vision/HaarCascades/haarcascade_frontalface_default.xml";
        internal static string TessdataPath => "./Detection/Vision/OCR/tessdata";
        internal static byte[] YoloConfiguration => File.ReadAllBytes("./Detection/Vision/YOLO/yolov3-tiny.cfg");
        internal static byte[] YoloWeights => File.ReadAllBytes("./Detection/Vision/YOLO/yolov3-tiny.weights");
        internal static string[] CocoLabels => File.ReadAllLines("./Detection/Vision/YOLO/coco.names");
    }
}
