using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows.Forms;

namespace SimulatorWinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            if (IsUnderTestHost()) return;

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static bool IsUnderTestHost()
        {
            try
            {
                var self = Process.GetCurrentProcess();
                string parentName = "";
                string parentCmd = "";

                using var q = new ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {self.Id}");
                foreach (ManagementObject mo in q.Get())
                {
                    var ppid = (int)(uint)mo["ParentProcessId"];
                    using var parent = Process.GetProcessById(ppid);
                    parentName = parent.ProcessName ?? "";

                    using var q2 = new ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {ppid}");
                    foreach (ManagementObject mo2 in q2.Get())
                        parentCmd = (string)(mo2["CommandLine"] ?? "");
                }

                var s1 = parentName.ToLowerInvariant();
                var s2 = parentCmd.ToLowerInvariant();
                return
                    s1.Contains("servicehub.testwindow") ||
                    s1.Contains("vstest") ||
                    s1.Contains("testhost") ||
                    s2.Contains("vstest") ||
                    s2.Contains("testwindow");
            }
            catch
            {
                return false;
            }
        }
    }
}
