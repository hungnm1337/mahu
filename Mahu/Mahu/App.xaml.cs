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
            DatabaseInitializer.Initialize();
        }
    }

}
