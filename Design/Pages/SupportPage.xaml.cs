using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace VRise.Pages
{
    [Obfuscation(Feature = "mutation", Exclude = false)]
    public partial class SupportPage : Page
    {
        public SupportPage()
        {
            InitializeComponent();
        }

        private void Links(object sender, RoutedEventArgs e)
        {
            switch ((e.Source as Button).Tag)
            {
                case "Discord":
                    System.Diagnostics.Process.Start("https://www.google.com");
                    break;

                case "Github":
                    System.Diagnostics.Process.Start("https://www.google.com");
                    break;

                case "Youtube":
                    System.Diagnostics.Process.Start("https://www.google.com");
                    break;
            }
        }
    }
}
