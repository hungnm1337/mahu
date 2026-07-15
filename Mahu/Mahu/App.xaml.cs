using System.Windows;
using Mahu.Data;

namespace Mahu
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Khởi tạo theme WPF UI programmatically để tránh lỗi XamlParseException
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                Wpf.Ui.Appearance.ApplicationTheme.Light,
                Wpf.Ui.Controls.WindowBackdropType.Mica,
                true
            );

            // Nạp ControlsDictionary qua code thay vì XAML
            try {
                Application.Current.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
            } catch {}

            DatabaseInitializer.Initialize();
        }
    }

}
