using Mahu.Data.Models;
using Mahu.Data.Repositories;
using Mahu.Services;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mahu.Views.Dialogs;

public partial class ImportExcelDialog : Window
{
    private List<ExcelImportService.SheetImportResult> _importResults = new();

    public ImportExcelDialog()
    {
        InitializeComponent();
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files|*.xlsx;*.xls",
            Title = "Chọn file Excel để import"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            txtFilePath.Text = openFileDialog.FileName;
            LoadPreview(openFileDialog.FileName);
        }
    }

    private void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            Title = "Lưu file mẫu",
            FileName = "vocabulary_template.xlsx"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                // In a real app, this file would be embedded or generated
                // We'll just generate the sample using ClosedXML here
                using var wb = new ClosedXML.Excel.XLWorkbook();
                var ws = wb.Worksheets.Add("TOEIC Basic");
                var headers = new[] { "Word", "Meaning", "WordType", "Phonetic", "Example", "ExampleMeaning", "Difficulty" };
                for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
                
                object[][] data = [
                    ["abandon", "từ bỏ, bỏ rơi", "verb", "/əˈbændən/", "He abandoned his car.", "Anh ấy bỏ xe.", 2],
                    ["accurate", "chính xác", "adj", "/ˈækjərət/", "Be accurate.", "Hãy chính xác.", 3]
                ];
                
                for (int r = 0; r < data.Length; r++)
                    for (int c = 0; c < data[r].Length; c++)
                        ws.Cell(r + 2, c + 1).Value = data[r][c]?.ToString() ?? "";

                wb.SaveAs(saveFileDialog.FileName);
                MessageBox.Show("Đã lưu file mẫu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu file mẫu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadPreview(string filePath)
    {
        try
        {
            _importResults = ExcelImportService.ParseExcelFile(filePath);
            
            tabPreview.Items.Clear();
            txtEmptyState.Visibility = Visibility.Collapsed;
            tabPreview.Visibility = Visibility.Visible;
            
            int totalWords = 0;

            foreach (var result in _importResults)
            {
                var tabItem = new TabItem
                {
                    Header = $"{result.SheetName} ({result.ValidWords.Count})"
                };

                var dataGrid = new DataGrid
                {
                    ItemsSource = result.ValidWords,
                    AutoGenerateColumns = true,
                    IsReadOnly = true,
                    HeadersVisibility = DataGridHeadersVisibility.Column,
                    GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.White,
                    RowBackground = Brushes.White,
                    AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9F9F9"))
                };

                tabItem.Content = dataGrid;
                tabPreview.Items.Add(tabItem);
                
                totalWords += result.ValidWords.Count;
            }

            if (_importResults.Count > 0)
            {
                tabPreview.SelectedIndex = 0;
                btnImport.IsEnabled = true;
                txtStatus.Text = $"Sẵn sàng import: {_importResults.Count} packet, {totalWords} từ vựng.";
            }
            else
            {
                txtStatus.Text = "Không tìm thấy dữ liệu hợp lệ trong file.";
                btnImport.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi đọc file Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            txtEmptyState.Visibility = Visibility.Visible;
            tabPreview.Visibility = Visibility.Collapsed;
            btnImport.IsEnabled = false;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Lấy Category chung
            var chungCategory = CategoryRepository.GetAll().FirstOrDefault(c => c.Name == "Chung");
            string categoryId = chungCategory?.Id ?? Guid.NewGuid().ToString();
            
            if (chungCategory == null)
            {
                // Nếu không có, tạo mới
                CategoryRepository.Insert(new Category { Id = categoryId, Name = "Chung", Color = "#808080" });
            }

            int totalImported = 0;

            foreach (var sheet in _importResults)
            {
                if (sheet.ValidWords.Count == 0) continue;

                // Tìm packet trùng tên
                var packet = PacketRepository.FindByName(sheet.SheetName);
                if (packet != null)
                {
                    var result = MessageBox.Show(
                        $"Do you want to overwrite packet '{sheet.SheetName}'?",
                        "Trùng lặp",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Xoá từ cũ
                        VocabularyRepository.DeleteByPacketId(packet.Id);
                    }
                    else
                    {
                        // Bỏ qua packet này nếu người dùng chọn No
                        continue;
                    }
                }
                else
                {
                    // Tạo packet mới
                    packet = new Packet
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = sheet.SheetName,
                        CategoryId = categoryId,
                        Description = $"Imported from Excel on {DateTime.Now:dd/MM/yyyy}"
                    };
                    PacketRepository.Insert(packet);
                }

                // Gán PacketId cho các từ và insert
                foreach (var word in sheet.ValidWords)
                {
                    word.Id = Guid.NewGuid().ToString(); // Reset ID to ensure it's new
                    word.PacketId = packet.Id;
                }
                
                VocabularyRepository.InsertMany(sheet.ValidWords);
                totalImported += sheet.ValidWords.Count;
            }

            MessageBox.Show($"Đã import thành công {totalImported} từ vựng!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi import dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
