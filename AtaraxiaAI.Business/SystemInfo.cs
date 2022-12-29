using AtaraxiaAI.Business.Services;
using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace AtaraxiaAI.Business
{
    public class SystemInfo
    {
        public string OSDescription { get; set; }
        public string OSArchitecture { get; set; }
        public string IPAddress { get; set; }
        public string Name { get; set; }
        public string DeviceID { get; set; }
        public string AdapterRAM { get; set; }
        public string AdapterDACType { get; set; }
        public string Monochrome { get; set; }
        public string InstalledDisplayDrivers { get; set; }
        public string DriverVersion { get; set; }
        public string VideoProcessor { get; set; }
        public string VideoArchitecture { get; set; }
        public string VideoMemoryType { get; set; }
        public string LogicalProcessors { get; set; }
        public string Memory { get; set; }

        public SystemInfo()
        {
            OSDescription = RuntimeInformation.OSDescription;
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString();

            IIPAddressService iPService = new IPIFYIPAddressService();
            IPAddress = iPService.GetPublicIPAddressAsync().Result;

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    Name = obj["Name"].ToString();
                    DeviceID = obj["DeviceID"].ToString();
                    AdapterRAM = obj["AdapterRAM"].ToString();
                    AdapterDACType = obj["AdapterDACType"].ToString();
                    Monochrome = obj["Monochrome"].ToString();
                    InstalledDisplayDrivers = obj["InstalledDisplayDrivers"].ToString();
                    DriverVersion = obj["DriverVersion"].ToString();
                    VideoProcessor = obj["VideoProcessor"].ToString();
                    VideoArchitecture = obj["VideoArchitecture"].ToString();
                    VideoMemoryType = obj["VideoMemoryType"].ToString();
                }
            }

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    Memory = FormatBytes(Convert.ToInt64(obj["TotalPhysicalMemory"])).ToString();
                    LogicalProcessors = obj["NumberOfLogicalProcessors"].ToString();
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            double dblSByte = bytes;
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };

            int i;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, suffix[i]);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"System: {OSDescription} ({OSArchitecture})");
            builder.AppendLine($"               Memory: {Memory}");
            builder.AppendLine($"               Logical Processors: {LogicalProcessors}");
            builder.AppendLine($"               Video Processor: {VideoProcessor}");
            builder.Append($"               IP Address: {IPAddress}");

            return builder.ToString();
        }
    }
}
