using Mahu.Data.Models;
using Mahu.Data.Repositories;
using System.Windows;

namespace Mahu.Views.Dialogs;

public partial class PacketDialog : Window
{
    private Packet? _editingPacket;

    public Packet? ResultPacket { get; private set; }

    public PacketDialog(Packet? packet = null)
    {
        InitializeComponent();
        _editingPacket = packet;
        LoadCategories();
        LoadData();
    }

    private void LoadCategories()
    {
        var categories = CategoryRepository.GetAll();
        cboCategory.ItemsSource = categories;
        
        if (categories.Any())
        {
            var chungCategory = categories.FirstOrDefault(c => c.Name == "Chung");
            cboCategory.SelectedValue = chungCategory?.Id ?? categories.First().Id;
        }
    }

    private void LoadData()
    {
        if (_editingPacket != null)
        {
            Title = "Sửa Packet";
            txtName.Text = _editingPacket.Name;
            txtDescription.Text = _editingPacket.Description;
            cboCategory.SelectedValue = _editingPacket.CategoryId;
            chkFavorite.IsChecked = _editingPacket.IsFavorite;
        }
        else
        {
            Title = "Thêm Packet";
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string name = txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Vui lòng nhập tên packet.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string categoryId = cboCategory.SelectedValue?.ToString() ?? "";
        if (string.IsNullOrEmpty(categoryId))
        {
            MessageBox.Show("Vui lòng chọn danh mục.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check if name already exists
        var existingPacket = PacketRepository.FindByName(name, _editingPacket?.Id);
        if (existingPacket != null)
        {
            var result = MessageBox.Show(
                $"Do you want to overwrite packet '{name}'?",
                "Trùng lặp",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Update existing packet's properties
                existingPacket.Description = txtDescription.Text.Trim();
                existingPacket.CategoryId = categoryId;
                existingPacket.IsFavorite = chkFavorite.IsChecked ?? false;
                PacketRepository.Update(existingPacket);
                
                // Clear old words as requested for overwrite
                VocabularyRepository.DeleteByPacketId(existingPacket.Id);

                ResultPacket = existingPacket;
                DialogResult = true;
                Close();
            }
            return; // If No, stay on dialog
        }

        if (_editingPacket == null)
        {
            ResultPacket = new Packet
            {
                Name = name,
                Description = txtDescription.Text.Trim(),
                CategoryId = categoryId,
                IsFavorite = chkFavorite.IsChecked ?? false
            };
            PacketRepository.Insert(ResultPacket);
        }
        else
        {
            _editingPacket.Name = name;
            _editingPacket.Description = txtDescription.Text.Trim();
            _editingPacket.CategoryId = categoryId;
            _editingPacket.IsFavorite = chkFavorite.IsChecked ?? false;
            PacketRepository.Update(_editingPacket);
            ResultPacket = _editingPacket;
        }

        DialogResult = true;
        Close();
    }
}
