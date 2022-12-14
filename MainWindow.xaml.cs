using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.Net.Http;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media.Effects;

namespace Launcher
{
    [Serializable]
    public struct VersionData
    {
        public string LauncherVersion { get; set; }
        public string GameVersion { get; set; }
        public string LauncherPath { get; set; }
    }
    public enum LauncherStatus
    {
        require_self_update,
        require_game_update,
        require_game_install,
        ready
    }
    public partial class MainWindow : Window
    {
        public LauncherStatus status;
        public const string DriveVersionLink = "https://drive.google.com/uc?id=1hFp9NRlm6lU3FFn3GQo9nU-2rRLitlBq&usp=download";
        public const string DriveGameLink = "https://drive.google.com/uc?id=1ajpkw8vmy4str7-dPovm_nwdCI4-_xZ0&usp=download";
        public const string DriveUpdaterLink = "https://drive.google.com/uc?id=1qf-2MM0sYyfTgMGmZVdt_5iIsikJpJpf&usp=download&confirm=no_antivirus";

        public const string GameTitle = "OpenGL-Voxel-Game";
        public string mainDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\"+GameTitle;
        public string launcherBasePath = AppDomain.CurrentDomain.BaseDirectory;
        public string LauncherPath => mainDataPath + "\\Launcher";
        public string LauncherUpdaterPath => LauncherPath + "\\UpdateHandler.exe";
        public string GamePath => mainDataPath + "\\Game";
        public string GameExecutablePath => GamePath + "\\" + GameTitle + "\\";
        public string LauncherExecutablePath = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".exe";
        public string DownloadedGamePath => GamePath + "\\" + GameTitle + ".zip";
        public string LauncherVersionsDataPath => LauncherPath + "\\VersionData.json";
        public bool initialized = false;
        
        public VersionData VersionData
        {
            get
            {
                if (!File.Exists(LauncherVersionsDataPath))
                {
                    var file = File.Create(LauncherVersionsDataPath);
                    file.Close();
                    VersionData data = new VersionData { LauncherVersion = "test", GameVersion = "", LauncherPath = AppDomain.CurrentDomain.BaseDirectory};
                    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(LauncherVersionsDataPath, json);
                    return data;

                }

                return JsonSerializer.Deserialize<VersionData>(File.ReadAllText(LauncherVersionsDataPath));
            }
            set
            {
                string json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LauncherVersionsDataPath, json);
            }
        }
        
        public MainWindow()
        {
            InitializeComponent();
            RunStartupChecks();
            VersionData data = VersionData;
            data.LauncherPath = AppDomain.CurrentDomain.BaseDirectory;
            VersionData = data;

        }
        public void RunStartupChecks()
        {
            Directory.CreateDirectory(mainDataPath);
            Directory.CreateDirectory(LauncherPath);
            Directory.CreateDirectory(GamePath);
            if (!File.Exists(LauncherUpdaterPath))
            {
                DownloadingWindow window = new DownloadingWindow("Installing Updater");
                WebClient client = new WebClient();
                var address = new Uri(DriveUpdaterLink);
                client.DownloadProgressChanged += window.OnProgressChanged;
                client.DownloadFileAsync(address, LauncherUpdaterPath);
                Effect = new BlurEffect();
                window.ShowDialog();
                Effect = null;
            }
            VerifyVersion();
            initialized = true;
        }


        public async void VerifyVersion()
        {
            var version = await GetLatestVersionData();
            LauncherVersionText.Text = "v" + VersionData.LauncherVersion;
            GameVersionText.Text = "Installed Version\n" + VersionData.GameVersion;
            if (version.LauncherVersion != VersionData.LauncherVersion)
            {
                Launch.Content = "Update Launcher";
                status = LauncherStatus.require_self_update;
            }
            else if (VersionData.GameVersion == "")
            {
                Launch.Content = "Install";
                status = LauncherStatus.require_game_install;
            }
            else if (version.GameVersion != VersionData.GameVersion)
            {
                Launch.Content = "Update";
                status = LauncherStatus.require_game_update;
            }
            else
            {
                Launch.Content = "Launch";
                status = LauncherStatus.ready;
            }
            Launch.IsEnabled = true;

        }
        public async Task<VersionData> GetLatestVersionData()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(DriveVersionLink);
            string responseBody = await response.Content.ReadAsStringAsync();
            VersionData version = JsonSerializer.Deserialize<VersionData>(responseBody);
            return version;
        }
        public async void Install()
        {

            DownloadingWindow window = new DownloadingWindow("Installing");
            WebClient client = new WebClient();
            var address = new Uri(DriveGameLink);
            client.DownloadProgressChanged += window.OnProgressChanged;
            client.DownloadFileCompleted += DownloadFileCompleted;
            if(Directory.Exists(GameExecutablePath))
                Directory.Delete(GameExecutablePath, true);
            client.DownloadFileAsync(address, DownloadedGamePath);
            Effect = new BlurEffect();
            window.ShowDialog();
            Effect = null;
            VersionData data = await GetLatestVersionData();
            VersionData = data;
            RunStartupChecks();
        }
        public async void UpdateLauncher()
        {
            FileInfo launcherUpdaterFile = new FileInfo(LauncherUpdaterPath);
            Process updateHandler = new Process();
            updateHandler.StartInfo.FileName = launcherUpdaterFile.FullName;
            updateHandler.StartInfo.WorkingDirectory = launcherUpdaterFile.DirectoryName;
            updateHandler.Start();
        }

        private void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            ZipFile.ExtractToDirectory(DownloadedGamePath, GameExecutablePath);
            File.Delete(DownloadedGamePath);
        }
        private void LaunchGame(object sender, RoutedEventArgs e)
        {
            switch (status)
            {
                case LauncherStatus.require_self_update: UpdateLauncher(); break;
                case LauncherStatus.require_game_update:
                case LauncherStatus.require_game_install: Install(); break;
                case LauncherStatus.ready:
                    Process.Start(GameExecutablePath + "\\" + GameTitle + ".exe");
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
