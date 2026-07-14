namespace Mahu.Data.Models;

/// <summary>
/// Thành tựu (badge) trong ứng dụng.
/// </summary>
public class Achievement
{
    /// <summary>GUID định danh duy nhất.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Mã thành tựu (duy nhất), dùng để tra cứu trong code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên thành tựu hiển thị.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả điều kiện mở khóa.</summary>
    public string? Description { get; set; }

    /// <summary>Icon hiển thị.</summary>
    public string? Icon { get; set; }

    /// <summary>XP thưởng khi mở khóa.</summary>
    public int RewardXP { get; set; } = 0;

    /// <summary>Đã mở khóa chưa.</summary>
    public bool IsUnlocked { get; set; } = false;

    /// <summary>Thời điểm mở khóa (null nếu chưa mở).</summary>
    public string? UnlockedAt { get; set; }
}
