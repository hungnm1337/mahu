namespace Mahu.Data.Models;

/// <summary>
/// Danh mục phân loại Packet từ vựng.
/// Ví dụ: TOEIC, IELTS, Business, Travel, Daily Conversation.
/// </summary>
public class Category
{
    /// <summary>GUID định danh duy nhất.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Tên danh mục (duy nhất).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả danh mục.</summary>
    public string? Description { get; set; }

    /// <summary>Màu hiển thị (ví dụ: "#FF5722").</summary>
    public string? Color { get; set; }

    /// <summary>Icon hiển thị.</summary>
    public string? Icon { get; set; }

    /// <summary>Thứ tự hiển thị trong danh sách.</summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>Thời điểm tạo.</summary>
    public string CreatedAt { get; set; } = string.Empty;
}
