using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using VRise.Tools;

namespace VRise.Settings
{
    /// <summary>
    /// Handles automatic updates for ao-bin-dumps and ITEMS assets
    /// </summary>
    public class AssetsUpdater
    {
        private const string AO_BIN_DUMPS_REPO = "https://github.com/ao-data/ao-bin-dumps.git";
        private const string AO_BIN_DUMPS_DIR = "ao-bin-dumps";
        private const string ITEMS_DIR = "ITEMS";
        private const string PYTHON_SCRIPT = "download_missing_items.py";

        public delegate void UpdateProgressHandler(string message, UpdateStatus status);
        public event UpdateProgressHandler OnUpdateProgress;

        public enum UpdateStatus
        {
            Info,
            Success,
            Warning,
            Error,
            InProgress
        }

        /// <summary>
        /// Check if Git is installed on the system
        /// </summary>
        public bool IsGitInstalled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit(5000); // 5 second timeout
                return process.ExitCode == 0;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Git not found in PATH
                OnUpdateProgress?.Invoke($"Git not found: {ex.Message}", UpdateStatus.Error);
                return false;
            }
            catch (Exception ex)
            {
                OnUpdateProgress?.Invoke($"Error checking Git: {ex.Message}", UpdateStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// Check if Python is installed on the system
        /// </summary>
        public bool IsPythonInstalled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit(5000); // 5 second timeout
                return process.ExitCode == 0;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Python not found in PATH
                OnUpdateProgress?.Invoke($"Python not found: {ex.Message}", UpdateStatus.Error);
                return false;
            }
            catch (Exception ex)
            {
                OnUpdateProgress?.Invoke($"Error checking Python: {ex.Message}", UpdateStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// Check for ao-bin-dumps updates (async)
        /// </summary>
        public async Task<bool> CheckAoBinDumpsUpdateAsync()
        {
            return await Task.Run(() => CheckAoBinDumpsUpdate());
        }

        /// <summary>
        /// Check for ao-bin-dumps updates
        /// </summary>
        public bool CheckAoBinDumpsUpdate()
        {
            OnUpdateProgress?.Invoke("Checking ao-bin-dumps for updates...", UpdateStatus.InProgress);

            string aoBinDumpsPath = Path.Combine(Pathfinder.mainFolder, AO_BIN_DUMPS_DIR);

            if (!Directory.Exists(aoBinDumpsPath))
            {
                OnUpdateProgress?.Invoke("ao-bin-dumps directory not found. Clone it first using Git.", UpdateStatus.Warning);
                return false;
            }

            // Check if it's a git repository
            if (!Directory.Exists(Path.Combine(aoBinDumpsPath, ".git")))
            {
                OnUpdateProgress?.Invoke("ao-bin-dumps is not a git repository.\n" +
                    "To fix: Delete 'ao-bin-dumps' folder and run:\n" +
                    "git clone --depth 1 https://github.com/ao-data/ao-bin-dumps.git", UpdateStatus.Warning);
                return false;
            }

            // Check Git installation first
            if (!IsGitInstalled())
            {
                OnUpdateProgress?.Invoke("Git is not installed or not in PATH.\n" +
                    "Download from: https://git-scm.com/downloads", UpdateStatus.Error);
                return false;
            }

            try
            {
                // Run git fetch to check for updates
                var fetchResult = RunGitCommand(aoBinDumpsPath, "fetch origin");
                if (!fetchResult.success)
                {
                    OnUpdateProgress?.Invoke($"Git fetch failed: {fetchResult.error}", UpdateStatus.Error);
                    return false;
                }

                // Check if local is behind remote
                var statusResult = RunGitCommand(aoBinDumpsPath, "status -uno");
                if (statusResult.success && statusResult.output.Contains("behind"))
                {
                    OnUpdateProgress?.Invoke("Updates available for ao-bin-dumps!", UpdateStatus.Success);
                    return true;
                }
                else
                {
                    OnUpdateProgress?.Invoke("ao-bin-dumps is up to date", UpdateStatus.Success);
                    return false;
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                OnUpdateProgress?.Invoke($"Cannot execute Git command: {ex.Message}\n" +
                    "Make sure Git is installed and in your PATH.", UpdateStatus.Error);
                return false;
            }
            catch (Exception ex)
            {
                OnUpdateProgress?.Invoke($"Error checking updates: {ex.Message}", UpdateStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// Update ao-bin-dumps from GitHub (async)
        /// </summary>
        public async Task<bool> UpdateAoBinDumpsAsync()
        {
            return await Task.Run(() => UpdateAoBinDumps());
        }

        /// <summary>
        /// Update ao-bin-dumps from GitHub
        /// </summary>
        public bool UpdateAoBinDumps()
        {
            OnUpdateProgress?.Invoke("Updating ao-bin-dumps...", UpdateStatus.InProgress);

            string aoBinDumpsPath = Path.Combine(Pathfinder.mainFolder, AO_BIN_DUMPS_DIR);

            if (!IsGitInstalled())
            {
                OnUpdateProgress?.Invoke("Git is not installed. Please install Git first.", UpdateStatus.Error);
                return false;
            }

            // If directory doesn't exist, clone it
            if (!Directory.Exists(aoBinDumpsPath))
            {
                OnUpdateProgress?.Invoke("Cloning ao-bin-dumps repository...", UpdateStatus.InProgress);
                var cloneResult = RunGitCommand(Pathfinder.mainFolder, $"clone --depth 1 {AO_BIN_DUMPS_REPO} {AO_BIN_DUMPS_DIR}");

                if (cloneResult.success)
                {
                    OnUpdateProgress?.Invoke("ao-bin-dumps cloned successfully!", UpdateStatus.Success);
                    return true;
                }
                else
                {
                    OnUpdateProgress?.Invoke($"Clone failed: {cloneResult.error}", UpdateStatus.Error);
                    return false;
                }
            }

            // If it's not a git repo, show warning
            if (!Directory.Exists(Path.Combine(aoBinDumpsPath, ".git")))
            {
                OnUpdateProgress?.Invoke("ao-bin-dumps is not a git repository. Cannot update automatically.", UpdateStatus.Warning);
                return false;
            }

            // Pull latest changes
            var pullResult = RunGitCommand(aoBinDumpsPath, "pull origin master");

            if (pullResult.success)
            {
                OnUpdateProgress?.Invoke("ao-bin-dumps updated successfully!", UpdateStatus.Success);
                return true;
            }
            else
            {
                OnUpdateProgress?.Invoke($"Update failed: {pullResult.error}", UpdateStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// Download missing ITEMS images (async)
        /// </summary>
        public async Task<bool> DownloadMissingItemsAsync(bool dryRun = false)
        {
            return await Task.Run(() => DownloadMissingItems(dryRun));
        }

        /// <summary>
        /// Download missing ITEMS images using Python script
        /// </summary>
        public bool DownloadMissingItems(bool dryRun = false)
        {
            OnUpdateProgress?.Invoke("Checking for missing item images...", UpdateStatus.InProgress);

            string scriptPath = Path.Combine(Pathfinder.mainFolder, PYTHON_SCRIPT);

            if (!File.Exists(scriptPath))
            {
                OnUpdateProgress?.Invoke($"{PYTHON_SCRIPT} not found", UpdateStatus.Error);
                return false;
            }

            if (!IsPythonInstalled())
            {
                OnUpdateProgress?.Invoke("Python is not installed. Please install Python first.", UpdateStatus.Error);
                return false;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = dryRun ? $"\"{scriptPath}\" --dry-run" : $"\"{scriptPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Pathfinder.mainFolder
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OnUpdateProgress?.Invoke(e.Data, UpdateStatus.Info);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OnUpdateProgress?.Invoke(e.Data, UpdateStatus.Warning);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    OnUpdateProgress?.Invoke(dryRun ? "Check completed" : "Items download completed!", UpdateStatus.Success);
                    return true;
                }
                else
                {
                    OnUpdateProgress?.Invoke("Items download failed", UpdateStatus.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnUpdateProgress?.Invoke($"Error: {ex.Message}", UpdateStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// Run a Git command and return output
        /// </summary>
        private (bool success, string output, string error) RunGitCommand(string workingDirectory, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(30000); // 30 second timeout

                bool success = process.ExitCode == 0;
                return (success, output, error);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Git executable not found
                return (false, string.Empty, $"Git not found in PATH: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Get last update date of ao-bin-dumps key files
        /// </summary>
        public string GetAoBinDumpsLastUpdate()
        {
            try
            {
                string itemsXml = Path.Combine(Pathfinder.mainFolder, AO_BIN_DUMPS_DIR, "items.xml");
                if (File.Exists(itemsXml))
                {
                    DateTime lastWrite = File.GetLastWriteTime(itemsXml);
                    return lastWrite.ToString("yyyy-MM-dd HH:mm");
                }
                return "Unknown";
            }
            catch
            {
                return "Error";
            }
        }

        /// <summary>
        /// Check all assets and return status summary
        /// </summary>
        public string GetAssetsStatus()
        {
            bool hasAoBinDumps = Directory.Exists(Path.Combine(Pathfinder.mainFolder, AO_BIN_DUMPS_DIR));
            bool hasItems = Directory.Exists(Path.Combine(Pathfinder.mainFolder, ITEMS_DIR));
            bool hasGit = IsGitInstalled();
            bool hasPython = IsPythonInstalled();

            return $"ao-bin-dumps: {(hasAoBinDumps ? "✓" : "✗")} | " +
                   $"ITEMS: {(hasItems ? "✓" : "✗")} | " +
                   $"Git: {(hasGit ? "✓" : "✗")} | " +
                   $"Python: {(hasPython ? "✓" : "✗")}";
        }
    }
}
