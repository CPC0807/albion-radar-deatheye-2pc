using VRise.Settings;
using VRise.Tools;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;

namespace VRise.Pages
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public partial class ConfigPage : Page
    {
        MainWindow MainWindow;
        private AssetsUpdater assetsUpdater;
        private bool isUpdating = false;

        public ConfigPage(MainWindow MainWindow)
        {
            InitializeComponent();
            this.MainWindow = MainWindow;

            ConfigList.ItemsSource = ConfigHandler.Source.ConfigList;

            // Initialize AssetsUpdater
            assetsUpdater = new AssetsUpdater();
            assetsUpdater.OnUpdateProgress += AssetsUpdater_OnUpdateProgress;

            // Load auto-update preference
            AutoUpdateCheckbox.IsChecked = LoadAutoUpdatePreference();

            // Display initial status
            UpdateStatusDisplay();
        }

        private void ConfigList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConfigList.SelectedItem != null)

            ActiveConfigTB.PlaceholderText = ConfigList.SelectedItem.ToString() + " *";
            ImportConfigTB.PlaceholderText = ConfigList.SelectedItem.ToString() + " *";
        }

        private void Actions(object sender, RoutedEventArgs e)
        {
            string configname = ActiveConfigTB.PlaceholderText.Substring(0, ActiveConfigTB.PlaceholderText.Length - 2);

            switch ((e.Source as Button).Tag)
            {
                case "Rename":

                    if (ActiveConfigTB.Text.Length == 0) return;

                    if (ConfigHandler.Source.ConfigList.Exists(x => x.Contains(ActiveConfigTB.Text))) return;

                    File.Move(Pathfinder.mainFolder + "\\" + configname + ".cfg", Pathfinder.mainFolder + "\\" + ActiveConfigTB.Text + ".cfg");

                    ConfigHandler.Source.ConfigList.Add(ActiveConfigTB.Text);
                    ConfigHandler.Source.ConfigList.Remove(configname);

                    if (ConfigHandler.Source.selectedConfig == configname) MainWindow.ConfigCB.SelectedItem = ActiveConfigTB.Text;
                    ConfigList.SelectedItem = ActiveConfigTB.Text;

                    ConfigList.Items.Refresh();
                    MainWindow.ConfigCB.Items.Refresh();
                    ActiveConfigTB.Text = string.Empty;
                    break;

                case "Copy":

                    if (configname.Contains(" (Copy)")) return;

                    if (!ConfigHandler.Source.ConfigList.Exists(x=> x.Contains(configname + " (Copy)")))
                    {
                        File.Copy(Pathfinder.mainFolder + "\\" + configname + ".cfg", Pathfinder.mainFolder + "\\" + configname + " (Copy)" + ".cfg");
                        ConfigHandler.Source.ConfigList.Add(configname + " (Copy)");
                        ConfigList.Items.Refresh();
                        MainWindow.ConfigCB.Items.Refresh();
                    }

                    break;

                case "Delete":

                    if (ConfigHandler.Source.ConfigList.Exists(x => x.Contains(configname)) && ConfigHandler.Source.ConfigList.Count() > 1)
                    {
                        ConfigHandler.Source.ConfigList.Remove(configname);
                        File.Delete(Pathfinder.mainFolder + "\\" + configname + ".cfg");

                        if (ConfigHandler.Source.selectedConfig == configname) MainWindow.ConfigCB.SelectedItem = ConfigHandler.Source.ConfigList.First();

                        ConfigList.SelectedItem = ConfigHandler.Source.ConfigList.First();
                        ConfigList.Items.Refresh();
                        MainWindow.ConfigCB.Items.Refresh();
                    }

                    break;

                case "Export":
                    Clipboard.SetText(Convert.ToBase64String(Encoding.UTF8.GetBytes(File.ReadAllText(Pathfinder.mainFolder + "\\" + configname + ".cfg"))));
                    break;
            }
        }

        private void Import(object sender, RoutedEventArgs e)
        {
            if (ImportKey.Text.Length < 0) return;

            string configname = ActiveConfigTB.PlaceholderText.Substring(0, ActiveConfigTB.PlaceholderText.Length - 2);

            try
            {
                var json = JsonConvert.DeserializeObject<Config>(Encoding.UTF8.GetString(Convert.FromBase64String(ImportKey.Text)));

                if (ImportConfigTB.Text.Length == 0)
                {
                    File.WriteAllText(Pathfinder.mainFolder + "\\" + configname + ".cfg", JsonConvert.SerializeObject(json, Formatting.Indented));
                    if (ConfigHandler.Source.selectedConfig == configname) ConfigHandler.Source.LoadConfig();
                }
                else
                {
                    File.WriteAllText(Pathfinder.mainFolder + "\\" + ImportConfigTB.Text + ".cfg", JsonConvert.SerializeObject(json, Formatting.Indented));

                    if (!ConfigHandler.Source.ConfigList.Exists(x => x.Contains(ImportConfigTB.Text)))
                    {
                        ConfigHandler.Source.ConfigList.Add(ImportConfigTB.Text);
                        ConfigList.Items.Refresh();
                        MainWindow.ConfigCB.Items.Refresh();
                    }
                    
                    if (ConfigHandler.Source.selectedConfig == ImportConfigTB.Text) ConfigHandler.Source.LoadConfig();
                }

                MainWindow.loadingCfg = true;
                MainWindow.UpdateSettings();
                MainWindow.loadingCfg = false;

                ImportKey.Text = "Imported!";
            }
            catch
            {
                ImportKey.Text = "Error!";
            }
        }

        private void Create(object sender, RoutedEventArgs e)
        {
            if (CreateConfigTB.Text.Length < 0) return;
            if (ConfigHandler.Source.ConfigList.Exists(x => x.Contains(CreateConfigTB.Text))) return;

            ConfigHandler.Source.CreateConfig(CreateConfigTB.Text);
            ConfigHandler.Source.ConfigList.Add(CreateConfigTB.Text);
            ConfigList.Items.Refresh();
            MainWindow.ConfigCB.Items.Refresh();
        }

        #region Assets Updater

        private void UpdateStatusDisplay()
        {
            string status = assetsUpdater.GetAssetsStatus();
            string lastUpdate = assetsUpdater.GetAoBinDumpsLastUpdate();
            AssetsStatusTB.Text = $"Status: {status}\nLast update: {lastUpdate}";
        }

        private async void CheckUpdate(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;

            isUpdating = true;
            CheckUpdateBtn.IsEnabled = false;
            AssetsStatusTB.Text = "Checking for updates...";

            try
            {
                bool hasUpdates = await assetsUpdater.CheckAoBinDumpsUpdateAsync();

                // Enable update buttons based on check result
                UpdateAoBinDumpsBtn.IsEnabled = hasUpdates;
                UpdateItemsBtn.IsEnabled = true; // Items can always be updated

                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                AssetsStatusTB.Text = $"Error: {ex.Message}";
            }
            finally
            {
                CheckUpdateBtn.IsEnabled = true;
                isUpdating = false;
            }
        }

        private async void UpdateAssets(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;

            var button = sender as Button;
            string tag = button.Tag.ToString();

            isUpdating = true;
            button.IsEnabled = false;
            CheckUpdateBtn.IsEnabled = false;

            try
            {
                bool success = false;

                if (tag == "AoBinDumps")
                {
                    success = await assetsUpdater.UpdateAoBinDumpsAsync();
                    if (success)
                    {
                        // After updating ao-bin-dumps, suggest updating items
                        var result = MessageBox.Show(
                            "ao-bin-dumps updated successfully!\n\nDo you want to download missing item images now?",
                            "Update Items?",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                        if (result == MessageBoxResult.Yes)
                        {
                            await assetsUpdater.DownloadMissingItemsAsync();
                        }
                    }
                }
                else if (tag == "Items")
                {
                    success = await assetsUpdater.DownloadMissingItemsAsync();
                }

                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                AssetsStatusTB.Text = $"Error: {ex.Message}";
            }
            finally
            {
                button.IsEnabled = true;
                CheckUpdateBtn.IsEnabled = true;
                isUpdating = false;
            }
        }

        private void AutoUpdateChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = AutoUpdateCheckbox.IsChecked ?? false;
            SaveAutoUpdatePreference(isChecked);
        }

        private void AssetsUpdater_OnUpdateProgress(string message, AssetsUpdater.UpdateStatus status)
        {
            // Update UI on dispatcher thread
            Dispatcher.Invoke(() =>
            {
                string prefix = "";
                switch (status)
                {
                    case AssetsUpdater.UpdateStatus.Success:
                        prefix = "[✓] ";
                        break;
                    case AssetsUpdater.UpdateStatus.Error:
                        prefix = "[✗] ";
                        break;
                    case AssetsUpdater.UpdateStatus.Warning:
                        prefix = "[!] ";
                        break;
                    case AssetsUpdater.UpdateStatus.InProgress:
                        prefix = "[...] ";
                        break;
                }

                AssetsStatusTB.Text = prefix + message;
            });
        }

        private bool LoadAutoUpdatePreference()
        {
            try
            {
                string prefFile = Path.Combine(Pathfinder.mainFolder, "auto_update.cfg");
                if (File.Exists(prefFile))
                {
                    string content = File.ReadAllText(prefFile);
                    return content.Trim().ToLower() == "true";
                }
            }
            catch { }
            return false;
        }

        private void SaveAutoUpdatePreference(bool enabled)
        {
            try
            {
                string prefFile = Path.Combine(Pathfinder.mainFolder, "auto_update.cfg");
                File.WriteAllText(prefFile, enabled.ToString());
            }
            catch { }
        }

        #endregion
    }
}
