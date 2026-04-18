using VRise.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace VRise
{
    public partial class App : Application
    {
        public App()
        {
            InstallGlobalExceptionLogging();
            this.DispatcherUnhandledException += (s, e) =>
            {
                LogCrash("Dispatcher.UnhandledException", e.Exception, terminating: false);
                e.Handled = true;
            };

            #region LANGUAGE

            InitializeComponent();
            LanguageChanged += App_LanguageChanged;

            m_Languages.Clear();
            m_Languages.Add(new CultureInfo("ru-RU"));
            m_Languages.Add(new CultureInfo("en-US"));
            Language = configHandler.config.Language;

            #endregion

            StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
        }

        private static readonly object _crashLogLock = new object();

        private static void InstallGlobalExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception, terminating: e.IsTerminating);

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogCrash("TaskScheduler.UnobservedTaskException", e.Exception, terminating: false);
                e.SetObserved();
            };
        }

        private static void LogCrash(string source, Exception ex, bool terminating)
        {
            try
            {
                string msg = $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} {source} (terminating={terminating}) ===\n" +
                             (ex != null ? FormatException(ex) : "<null exception>") + "\n";
                Console.WriteLine(msg);
                lock (_crashLogLock)
                {
                    File.AppendAllText("crash.log", msg);
                }
            }
            catch { /* never let the logger itself crash the process */ }
        }

        private static string FormatException(Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                sb.AppendLine($"{cur.GetType().FullName}: {cur.Message}");
                sb.AppendLine(cur.StackTrace);
                if (cur.InnerException != null) sb.AppendLine("--- Inner ---");
            }
            return sb.ToString();
        }

        #region LANGUAGE

        ConfigHandler configHandler = ConfigHandler.Source;

        private static List<CultureInfo> m_Languages = new List<CultureInfo>();
        public static List<CultureInfo> Languages
        {
            get
            {
                return m_Languages;
            }
        }


        public static event EventHandler LanguageChanged;

        public static CultureInfo Language
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value == null) value = System.Threading.Thread.CurrentThread.CurrentUICulture;

                System.Threading.Thread.CurrentThread.CurrentUICulture = value;

                ResourceDictionary dict = new ResourceDictionary();
                switch (value.Name)
                {
                    case "ru-RU":
                        dict.Source = new Uri(String.Format("Design/Lang/lang.{0}.xaml", value.Name), UriKind.Relative);
                        break;

                    default:
                        dict.Source = new Uri("Design/Lang/lang.xaml", UriKind.Relative);
                        break;
                }

                ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
                                              where d.Source != null && d.Source.OriginalString.StartsWith("Design/Lang/lang.")
                                              select d).First();
                if (oldDict != null)
                {
                    int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }

                LanguageChanged(Application.Current, new EventArgs());
            }
        }

        private void App_LanguageChanged(Object sender, EventArgs e)
        {
            ConfigHandler.Source.config.Language = Language;
        }

        #endregion
    }
}
