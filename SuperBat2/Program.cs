using Ionic.Zip;
using SuperBat2.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SuperBat2
{
    class Program
    {
        static readonly string _RemoteServerFolder = Path.Combine(Settings.Default.RemotePublishFolderPath, "FossDoc");

        static void Main(string[] args)
        {
            _ArchiveLog();

            // This is reserved for Twitter debugging purposes. Do not delete this line! Okay

            // TwitterHelper.SendTweet("This is test tweet!");
            // return;
            // 
            try
            {
                var dirs = Directory.GetDirectories(_RemoteServerFolder, "FossDoc*", SearchOption.TopDirectoryOnly);
                if (dirs == null || dirs.Length == 0)
                {
                    Trace.TraceInformation("Directory was not found!");
                    return;
                }




                Trace.TraceInformation("Directory was found: {0}", dirs[0]);

                var localServerFile = new DirectoryInfo(Settings.Default.LocalServerFolderPath)
                    .GetFiles("*.msi")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (localServerFile == null)
                {
                    Trace.TraceInformation("Local server file was not found!");
                    return;
                }

                var remoteServerFile = new DirectoryInfo(Path.Combine(dirs[0], @"Server\"))
                    .GetFiles("*.msi")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (remoteServerFile == null || localServerFile.LastWriteTime >= remoteServerFile.LastWriteTime)
                {
                    Trace.TraceInformation("Remote server file was not found or it has greater or equal local server file!");
                    return;
                }

                File.Copy(remoteServerFile.FullName, localServerFile.FullName, true);

                Trace.TraceInformation("Starting server update...");

                var process = new Process();
                process.StartInfo.FileName = "msiexec.exe";
                process.StartInfo.Arguments = "/i \"" + localServerFile.FullName + "\" /passive";
                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    TwitterHelper.SendTweet(String.Format("Хозяин, похоже сервер уже был обновлен! Пишет какой-то return code received {0}", process.ExitCode));
                    Trace.TraceError("Server is up to date! No reason to update? return code: {0}", process.ExitCode);
                    return;
                }


                System.Threading.Thread.Sleep(120000);//2 min sleep for Configurator

                Trace.TraceInformation("Server has been updated!");

                Trace.TraceInformation("Starting service...");
                SrartService(Settings.Default.ServiceName, 15 * 60);
                Trace.TraceInformation("Service has been started!");
            }
            catch (Exception exception)
            {
                Trace.TraceError("Fatal error: {0}", exception.ToString());
                TwitterHelper.SendTweet("Error: " + exception.Message);
            }
        }

        private static void _ArchiveLog()
        {
            var logFilePath = "Trace.log";
            if (!File.Exists(logFilePath))
                return;

            try
            {
                var newLogFilePath = Path.GetInvalidFileNameChars().Union(new char[] { '-', '.', ' ', 'Z' }).Aggregate(DateTime.Now.ToString("u"), (current, c) => current.Replace(c.ToString(), "")) + ".log";
                File.Move(logFilePath, newLogFilePath);

                using (var zip = new ZipFile("Archive.zip"))
                {
                    zip.AddFile(newLogFilePath);
                    zip.Save();
                }

                File.Delete(newLogFilePath);
            }
            catch (Exception exception)
            {
                Trace.TraceError("Alert! Acrhiving error: " + exception.Message);
            }
        }

        public static void SrartService(string serviceName, double timeout)
        {
            using (var service = new ServiceController(serviceName))
            {
                if (service.Status == ServiceControllerStatus.Running || service.Status == ServiceControllerStatus.StartPending)
                {
                    TwitterHelper.SendTweet("Хозяин, похоже сервер был уже запущен раньше!");
                    return;
                }

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeout));
                TwitterHelper.SendTweet("Хозяин, сервак обновлен и запущен! Где печенька?");
            }
        }
    }
}