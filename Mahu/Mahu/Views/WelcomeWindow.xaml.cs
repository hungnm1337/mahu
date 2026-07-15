using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Mahu.Data.Repositories;

namespace Mahu.Views
{
    public partial class WelcomeWindow : Wpf.Ui.Controls.FluentWindow
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            txtName.Focus();
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveAndClose();
        }

        private async void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SaveAndClose();
            }
        }

        private async Task SaveAndClose()
        {
            string name = txtName.Text.Trim();
            
            if (string.IsNullOrEmpty(name))
            {
                ShowStatus("Vui lòng nhập tên của bạn!", Colors.Red);
                return;
            }
            
            btnSave.IsEnabled = false;
            txtName.IsEnabled = false;

            bool isSuccess = AppSettingsRepository.SaveDisplayName(name);

            if (isSuccess)
            {
                ShowStatus("Lưu thành công!", Colors.Green);
                await Task.Delay(1000); // Chờ 1 giây để người dùng thấy thông báo
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ShowStatus("Có lỗi xảy ra khi lưu, vui lòng thử lại.", Colors.Red);
                btnSave.IsEnabled = true;
                txtName.IsEnabled = true;
                txtName.Focus();
            }
        }

        private void ShowStatus(string message, Color color)
        {
            txtStatus.Text = message;
            txtStatus.Foreground = new SolidColorBrush(color);
            txtStatus.Visibility = Visibility.Visible;
        }
    }
}
