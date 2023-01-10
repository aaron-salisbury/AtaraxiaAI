using AtaraxiaAI.Business.Services;
using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace AtaraxiaAI.Business.Componants
{
    internal class SystemInfo
    {
        internal string OSDescription { get; set; }
        internal string OSArchitecture { get; set; }
        internal string IPAddress { get; set; }
        internal string Name { get; set; }
        internal string DeviceID { get; set; }
        internal string AdapterRAM { get; set; }
        internal string AdapterDACType { get; set; }
        internal string Monochrome { get; set; }
        internal string InstalledDisplayDrivers { get; set; }
        internal string DriverVersion { get; set; }
        internal string VideoProcessor { get; set; }
        internal string VideoArchitecture { get; set; }
        internal string VideoMemoryType { get; set; }
        internal string LogicalProcessors { get; set; }
        internal string Memory { get; set; }

        internal SystemInfo()
        {
            OSDescription = RuntimeInformation.OSDescription;
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString();

            IIPAddressService iPService = new IPIFYIPAddressService();
            IPAddress = iPService.GetPublicIPAddressAsync().Result;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
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

            return string.Format("{0:0.##} {1}", dblSByte, suffix[i]);
        }

        public override string ToString()
        {
            const string NEW_LINE_PREFIX = "               ";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"System: {OSDescription} ({OSArchitecture})");
            builder.AppendLine($"{NEW_LINE_PREFIX}Memory: {Memory}");
            builder.AppendLine($"{NEW_LINE_PREFIX}Logical Processors: {LogicalProcessors}");
            builder.AppendLine($"{NEW_LINE_PREFIX}Video Processor: {VideoProcessor}");
            builder.Append($"{NEW_LINE_PREFIX}IP Address: {IPAddress}");

            return builder.ToString();
        }
    }
}
