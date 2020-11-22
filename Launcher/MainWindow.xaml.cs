using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Launcher
{
    public static class Globals
    {
        public static string exePath = AppDomain.CurrentDomain.BaseDirectory;
        public static string exefPath = exePath + "build\\";
    }
    public static class Constants
    {
        public const string releaseFiles = "http://18.192.38.56/release/";
        public const string checksum = "5d4a2b158c82af4b978f55b259a46590";
    }

    public class ver
    {
        public string version { get; set; }
        public string build { get; set; }
    }
    


    public partial class MainWindow : Window
    {
        public int downloaderCount = 0;
        public List<string> temp;
        public MainWindow()
        {
            string[] arguments = Environment.GetCommandLineArgs();

            if (arguments.Contains("finalizeUpdate"))
            {
                finalizeUpdate();
            }
            else 
            { 
                InitializeComponent();
                Start();
            }
        }
        public void Start()
        {
            if (Directory.Exists("./0/"))
            {
                Directory.Delete("./0/", true);
            }
            if (Directory.Exists("./updateTemp/"))
            {
                Directory.Delete("./updateTemp/", true);
            }
                check_ver();
        }
        public void check_ver()
        {

            if (File.Exists(Globals.exePath + "version.json"))
            {
                var j = new StreamReader(Globals.exePath + "version.json");
                var j1 = j.ReadToEnd();
                ver jj = JsonConvert.DeserializeObject<ver>(j1);
                versionLabel.Text = "Launcher Version: " + jj.version;
                buildLabel.Text = "Launcher Version: " + jj.build;
                j.Close();
            } else
            {
                using (FileStream fs = File.Create("version.json"))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes("{'version' : 0, 'build' : 0}");
                    fs.Write(info, 0, info.Length);
                }
            }
            selfCheck();
        }

        void selfCheck()
        {
            label.Text = "Checking for updates...";
            List<string> filesToCheck = new List<string>();
            foreach (string file in Directory.GetFiles(Globals.exePath, "*", SearchOption.AllDirectories).Where(x => !x.StartsWith(Globals.exefPath)))
            {
                filesToCheck.Add(file);

            }
            Dictionary<string, string> hashList = new Dictionary<string, string>();
            getHashArray(filesToCheck, hashList);
            var json = new WebClient().DownloadString(Constants.releaseFiles + "launcher/hashlist.json");
            var serverHashList = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var diff = serverHashList.Where(x => !hashList.Contains(x));
            List<string> filesToUpdate = new List<string>();
            foreach (KeyValuePair<string, string> file in diff)
            {
                filesToUpdate.Add(file.Key.Replace("./", "/"));
            }
            if (filesToUpdate.Count > 0)
            {
                selfUpdate(filesToUpdate);
            } else
            {
                //Check_Update();
            }
        }

        void getHashArray(List<string> filesToCheck, Dictionary<string, string> hashList)
        {
            foreach (string file in filesToCheck)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(file))
                    {
                        var hash = md5.ComputeHash(stream);
                        hashList.Add("." + file.Replace(Globals.exePath, "/"), BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
                    }
                }
            }
        }

        void selfUpdate(List<string> filesToUpdate)
        {
            temp = filesToUpdate;
            label.Text = "Downloading update...";
            if (!Directory.Exists("./updateTemp/"))
            {
                Directory.CreateDirectory("./updateTemp/");
            }
            foreach (string file in filesToUpdate)
            {
                Console.WriteLine(file);
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(launcherCompleted);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                webClient.DownloadFileAsync(new Uri(Constants.releaseFiles + "launcher" + file), Globals.exePath + "updateTemp/" + file);
            }
        }

        void finalizeUpdate()
        {
            foreach (string file in Directory.GetFiles(Globals.exePath + "updateTemp/", "*", SearchOption.AllDirectories))
            {
                string filePath = file.Replace("\\updateTemp", "");
                if (File.Exists(filePath)){
                    try
                    {
                        File.Delete(filePath);
                        File.Move(file, filePath);
                    }
                    catch
                    {
                        if (!Directory.Exists("./0/"))
                        {
                            Directory.CreateDirectory("./0/");
                        }
                        File.Move(filePath,"./0/" + Path.GetFileName(filePath));
                        File.Move(file, filePath);
                    }
                } else
                {
                File.Move(file, filePath);
                }
            }
            Process.Start(Globals.exePath + "Launcher.exe");
            System.Windows.Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(Globals.exePath);
            Process.Start(Globals.exefPath + "mmo.exe");
        }
        public void Check_Update()
        {
            label.Text = "Checking for updates...";
            var json = new WebClient().DownloadString(Constants.releaseFiles + "version.json");
            var j = new StreamReader(Globals.exePath + "version.json");
            var j1 = j.ReadToEnd();
            j.Close();
            ver jj = JsonConvert.DeserializeObject<ver>(j1);
            ver Server = JsonConvert.DeserializeObject<ver>(json);
            if (jj.version == Server.version)
            {
                var chksm = CreateDirectoryMd5(Globals.exefPath);
                Console.WriteLine(chksm);
                if (chksm != Constants.checksum)
                {
                    label.Text = "File verification failed. Repairing...";
                    Dl();
                }
                else { ready(); }
            }
            else
            {
                Button_Play.IsEnabled = false;
                Dl();
            }
        }
        public void Dl()
        {
            //var exePath = AppDomain.CurrentDomain.BaseDirectory;
            WebClient webClient = new WebClient();
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
            webClient.DownloadFileAsync(new Uri(Constants.releaseFiles), Globals.exePath + "build.zip");
        }
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
        private void Completed(object sender, EventArgs e)
        {
            label.Text = "Download completed!";
           // extract();
        }
        private void launcherCompleted(object sender, EventArgs e)
        {
            label.Text = "Download completed!";
            downloaderCount++;
            if (downloaderCount >= temp.Count)
            {
                Process.Start(Globals.exePath + "Launcher.exe", "finializeUpdate ");
                System.Windows.Application.Current.Shutdown();
            }
        }
        public void extract()
        {
            label.Text = "Extracting update";
            try
            {
                if (Directory.Exists(Globals.exefPath))
                {
                    Directory.Delete(Globals.exefPath, true);
                }
            }
            finally
            {
                ZipFile.ExtractToDirectory(Globals.exePath + "build.zip", Globals.exePath);
                File.Delete(Globals.exePath + "build.zip");
                var json = new WebClient().DownloadString(Constants.releaseFiles + "version.json");
                var j = new StreamReader(Globals.exePath + "version.json");
                var j1 = j.ReadToEnd();
                ver jj = JsonConvert.DeserializeObject<ver>(j1);
                j.Close();
                var chksm = CreateDirectoryMd5(Globals.exefPath);
                Console.WriteLine(chksm);
                if (chksm != Constants.checksum)
                {
                    label.Text = "File verification failed. Repairing...";
                    Dl();
                }
                else 
                {
                    ready(); 
                }
            }
        }
        public static string CreateDirectoryMd5(string srcPath)
        {
            var filePaths = Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();
            using (var md5 = MD5.Create())
            {
                foreach (var filePath in filePaths)
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(filePath);
                    md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);
                    byte[] contentBytes = File.ReadAllBytes(filePath);
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }
                md5.TransformFinalBlock(new byte[0], 0, 0);
                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
        }
        public void ready()
        {
            label.Text = "Ready to play!";
            Button_Play.IsEnabled = true;
            progressBar.Opacity = 0;
            check_ver();
        }
    }
}