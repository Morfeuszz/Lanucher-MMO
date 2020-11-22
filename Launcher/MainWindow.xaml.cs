using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
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
        public List<string> filesToCheck = new List<string>();
        public List<string> filesToUpdate = new List<string>();
        Dictionary<string, string> hashList = new Dictionary<string, string>();
        public MainWindow()
        {
            if (Environment.GetCommandLineArgs().Contains("finalizeUpdate"))
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
            Button_Play.IsEnabled = false;
        }
        void selfCheck()
        {
            label.Text = "Checking for updates...";
            Button_Play.IsEnabled = false;
            List<string> filesToCheck = new List<string>();
            foreach (string file in Directory.GetFiles(Globals.exePath, "*", SearchOption.AllDirectories).Where(x => !x.StartsWith(Globals.exefPath)))
            {
                filesToCheck.Add(file);
            }
            Dictionary<string, string> hashList = new Dictionary<string, string>();
            getHashArray(filesToCheck, hashList);
            foreach (KeyValuePair<string,string> hash in hashList)
            {
                Console.WriteLine(hash);
            }
            var json = new WebClient().DownloadString(Constants.releaseFiles + "launcher/hashlist.json");
            var serverHashList = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var diff = serverHashList.Where(x => !hashList.Contains(x));
            foreach (KeyValuePair<string, string> file in diff)
            {
                filesToUpdate.Add(file.Key.Replace("./", "/"));
            }
            if (filesToUpdate.Count > 0)
            {
                selfUpdate(filesToUpdate);
            } else
            {
                
                Check_Update();
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
                        hashList.Add("." + file.Replace(Globals.exePath, "/").Replace(@"build\", "").Replace(@"\","/"), BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
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
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(Globals.exePath);
            Process.Start(Globals.exefPath + "mmo.exe");
        }
        public void Check_Update()
        {
            Button_Play.IsEnabled = false;
            if (!Directory.Exists("./build/"))
            {
                Directory.CreateDirectory("./build/");
            }
            filesToCheck.Clear();
            foreach (string file in Directory.GetFiles(Globals.exefPath, "*", SearchOption.AllDirectories))
            {
                filesToCheck.Add(file);
            }
            hashList.Clear();
            
            getHashArray(filesToCheck, hashList);
            foreach(KeyValuePair<string,string> x in hashList)
            {
                Console.WriteLine(x);
            }
            var json = new WebClient().DownloadString(Constants.releaseFiles + "build/hashlist.json");
            var serverHashList = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var diff = serverHashList.Where(x => !hashList.Contains(x));

            filesToUpdate.Clear();
            foreach (KeyValuePair<string, string> file in diff)
            {
                filesToUpdate.Add(file.Key.Replace("./", "/"));
            }
            if (filesToUpdate.Count > 0)
            {
                buildUpdate(filesToUpdate);
            }
            else
            {
                ready();
            }
        }
        void buildUpdate(List<string> filesToUpdate)
        {
            label.Text = "Downloading update...";
            downloaderCount = 0;
            //temp.Clear();
            temp = filesToUpdate;
            if (!Directory.Exists(Globals.exefPath + "temp"))
            {
                Directory.CreateDirectory(Globals.exefPath + "temp");
            }
            foreach (string file in filesToUpdate)
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(buildCompleted);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                //label.Text = file;
                new System.IO.FileInfo(Globals.exefPath + "temp/" + file).Directory.Create();
                //webClient.OpenRead(Constants.releaseFiles + "build" + file);
                //Int64 bytes_total = Convert.ToInt64(webClient.ResponseHeaders["Content-Lenght"]);
                //Console.WriteLine(bytes_total.ToString() + " Bytes.");
                webClient.DownloadFileAsync(new Uri(Constants.releaseFiles + "build" + file), Globals.exefPath + "temp/" + file);
            }
        }
        private void buildCompleted(object sender, EventArgs e)
        {
            label.Text = "Downloading update... " + downloaderCount + "/" + temp.Count();
            downloaderCount++;;
            if (downloaderCount >= temp.Count())
            {
                
                build_finalizeUpdate();

            }
        }
        void build_finalizeUpdate()
        {
            foreach (string file in Directory.GetFiles(Globals.exefPath + "temp/", "*", SearchOption.AllDirectories))
            {
                string filePath = file.Replace(Globals.exefPath + "temp/", Globals.exefPath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    File.Move(file, filePath);
                }
                else
                {
                    new System.IO.FileInfo(filePath).Directory.Create();
                    File.Move(file, filePath);
                }
            }
            label.Text = "Download completed!";
            Check_Update();
        }
        private void launcherCompleted(object sender, EventArgs e)
        {
            label.Text = "Download completed!";
            downloaderCount++;
            if (downloaderCount >= temp.Count)
            {
                Process.Start(Globals.exePath + "Launcher.exe", "finalizeUpdate");
                System.Windows.Application.Current.Shutdown();
            }
        }
        public void ready()
        {
            label.Text = "Ready to play!";
            Button_Play.IsEnabled = true;
            progressBar.Opacity = 0;
            //check_ver();
        }
    }
}