using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mahu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RootNavigation.SetPageProviderService(new SimplePageService());
            RootNavigation.Navigate(typeof(Views.Pages.AnalysisPage));
            CheckNewUserAndLoadData();
        }

        private void CheckNewUserAndLoadData()
        {
            if (Data.Repositories.AppSettingsRepository.IsNewUser())
            {
                var welcomeWindow = new Views.WelcomeWindow();
                welcomeWindow.Owner = this;
                if (welcomeWindow.ShowDialog() == true)
                {
                    // Nếu lưu tên thành công, tải lại dữ liệu Header
                    LoadHeaderData();
                }
            }
            else
            {
                // Nếu đã có tên, tải dữ liệu Header
                LoadHeaderData();
            }
        }

        private void LoadHeaderData()
        {
            var settings = Data.Repositories.AppSettingsRepository.Get();
            if (settings != null)
            {
                txtDisplayName.Text = string.IsNullOrEmpty(settings.DisplayName) ? "User" : settings.DisplayName;
                txtXP.Text = $"{settings.TotalXP:N0} XP";
                txtStreak.Text = $"{settings.CurrentStreak} Days";
            }
        }
    }
}