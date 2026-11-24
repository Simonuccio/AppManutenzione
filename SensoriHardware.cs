#nullable disable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace AppManutenzione;

public static class SensoriHardware
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public static string LeggiRAM()
    {
        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        if (GlobalMemoryStatusEx(ref memStatus))
        {
            double totalRamGB = memStatus.ullTotalPhys / 1073741824.0;
            double availRamGB = memStatus.ullAvailPhys / 1073741824.0;
            return $"📊 RAM Usata: {totalRamGB - availRamGB:F1} GB / {totalRamGB:F1} GB";
        }
        return "📊 RAM: Errore lettura";
    }

    public static string LeggiTopProcessi()
    {
        try 
        {
            // Lista dei processi vitali di Windows da escludere assolutamente dall'analisi
            string[] processiVitali = {
                "svchost", "explorer", "System", "Idle", "csrss", "smss", "wininit",
                "services", "lsass", "winlogon", "fontdrvhost", "dwm", "spoolsv",
                "taskhostw", "RuntimeBroker", "SearchHost", "StartMenuExperienceHost",
                "sihost", "conhost", "dllhost", "Memory Compression", "TiWorker",
                "MsMpEng", "SecurityHealthService", "WmiPrvSE", "WUDFHost",
                "AppManutenzione", "Code", "devenv", "Taskmgr", "SearchApp", "Registry"
            };

            // Filtra solo le app NON vitali e che consumano più di 30 MB di RAM
            var processi = Process.GetProcesses()
                .Where(p => !processiVitali.Any(sys => p.ProcessName.Equals(sys, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p.WorkingSet64 > 30 * 1048576) // > 30 MB
                .OrderByDescending(p => p.WorkingSet64)
                .Take(3);

            string info = "App sacrificabili più pesanti:\n";
            bool trovati = false;

            foreach (var p in processi) 
            {
                info += $"• {p.ProcessName}: {p.WorkingSet64 / 1048576.0:F0} MB\n";
                trovati = true;
            }
            
            return trovati ? info : "Nessuna app pesante superflua.";
        } 
        catch { return ""; }
    }

    public static string LeggiDischi()
    {
        try {
            string diskInfo = "💾 Spazio su Disco:\n\n";
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed)) {
                diskInfo += $"Disco {drive.Name}\nLiberi: {drive.AvailableFreeSpace / 1073741824.0:F1} GB su {drive.TotalSize / 1073741824.0:F0} GB\n\n";
            }
            return diskInfo;
        } catch { return "💾 Lettura dischi fallita"; }
    }

    public static string LeggiTemperature(Computer monitor)
    {
        try {
            string infoTemp = "🌡️ Sensori Hardware:\n";
            bool cpuTrovata = false;
            foreach (var hardware in monitor.Hardware) {
                hardware.Update();
                if (hardware.HardwareType == HardwareType.Cpu || hardware.HardwareType.ToString().Contains("Gpu") || hardware.HardwareType == HardwareType.Motherboard || hardware.HardwareType == HardwareType.EmbeddedController) {
                    var tempSensors = hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 0);
                    if (tempSensors.Any()) {
                        float maxTemp = tempSensors.Max(s => s.Value.Value);
                        if (hardware.HardwareType == HardwareType.Motherboard && maxTemp < 30) continue;
                        
                        string shortName = hardware.Name.Replace("with Radeon Graphics", "").Replace("NVIDIA GeForce", "").Replace("Laptop GPU", "").Replace("AMD Ryzen", "Ryzen").Trim();
                        string icona = hardware.HardwareType.ToString().Contains("Gpu") ? "🎮 GPU:" : (hardware.HardwareType == HardwareType.Motherboard ? "🎛️ MB:" : (hardware.HardwareType == HardwareType.EmbeddedController ? "💻 CPU (EC):" : "💻 CPU:"));
                        if (hardware.HardwareType == HardwareType.EmbeddedController) shortName = "Sensore Portatile";
                        
                        infoTemp += $"\n{icona} {shortName}\n{Math.Round(maxTemp)} °C\n";
                        if (icona.Contains("CPU")) cpuTrovata = true;
                    }
                }
            }
            if (!cpuTrovata) infoTemp += "\n💻 CPU:\nSensore bloccato dal costruttore.";
            return infoTemp;
        } catch { return "🌡️ Lettura sensori fallita"; }
    }
}