using Mahu.Data.Models;
using Mahu.Data.Repositories;
using Mahu.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace Mahu.Views.Pages;

public class PacketViewModel
{
    public Packet Packet { get; set; } = new();
    public string CategoryName { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public bool IsFavorite => Packet.IsFavorite;
}

public partial class PacketPage : Page
{
    private List<PacketViewModel> _allPackets = new();

    public PacketPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadCategories();
        LoadData();
    }

    private void LoadCategories()
    {
        var categories = CategoryRepository.GetAll();
        var filterList = new List<Category> { new Category { Id = "ALL", Name = "Tất cả danh mục" } };
        filterList.AddRange(categories);
        
        cboCategory.ItemsSource = filterList;
        cboCategory.SelectedIndex = 0;
    }

    private void LoadData()
    {
        var packets = PacketRepository.GetAll();
        var categories = CategoryRepository.GetAll().ToDictionary(c => c.Id, c => c.Name);
        
        _allPackets = packets.Select(p => new PacketViewModel
        {
            Packet = p,
            CategoryName = categories.TryGetValue(p.CategoryId, out var catName) ? catName : "Chung",
            WordCount = PacketRepository.GetWordCount(p.Id)
        }).ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var searchText = txtSearch.Text.Trim().ToLower();
        var selectedCategory = cboCategory.SelectedValue?.ToString();

        var filtered = _allPackets.AsEnumerable();

        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(p => p.Packet.Name.ToLower().Contains(searchText));
        }

        if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "ALL")
        {
            filtered = filtered.Where(p => p.Packet.CategoryId == selectedCategory);
        }

        icPackets.ItemsSource = filtered.ToList();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void CboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void BtnAddPacket_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PacketDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            LoadData();
        }
    }

    private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ImportExcelDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            LoadData();
        }
    }

    private void BtnEditPacket_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Packet packet)
        {
            var dialog = new PacketDialog(packet) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }
    }

    private void BtnDeletePacket_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Packet packet)
        {
            var wordCount = PacketRepository.GetWordCount(packet.Id);
            var msg = $"Xoá packet '{packet.Name}'?\nToàn bộ {wordCount} từ vựng bên trong sẽ bị xoá vĩnh viễn.";
            
            var result = MessageBox.Show(msg, "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                PacketRepository.Delete(packet.Id);
                LoadData();
            }
        }
    }
}
