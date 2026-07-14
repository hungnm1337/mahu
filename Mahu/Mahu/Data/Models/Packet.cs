namespace Mahu.Data.Models;

/// <summary>
/// Gói từ vựng thuộc về một Category.
/// </summary>
public class Packet
{
    /// <summary>GUID định danh duy nhất.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID danh mục chứa Packet này.</summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>Tên Packet.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả Packet.</summary>
    public string? Description { get; set; }

    /// <summary>Đã đánh dấu yêu thích.</summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>Ghim lên Dashboard.</summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>Đã hoàn thành toàn bộ từ trong Packet.</summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>Thời điểm tạo.</summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Thời điểm cập nhật gần nhất.</summary>
    public string UpdatedAt { get; set; } = string.Empty;
}
