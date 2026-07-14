namespace Mahu.Data.Models;

/// <summary>
/// Thống kê của một phiên học cụ thể.
/// </summary>
public class StudySession
{
    /// <summary>GUID định danh duy nhất.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Ngày học (yyyy-MM-dd).</summary>
    public string SessionDate { get; set; } = string.Empty;

    /// <summary>Thời lượng phiên học (phút).</summary>
    public int Duration { get; set; } = 0;

    /// <summary>Tổng số từ đã ôn trong phiên.</summary>
    public int WordsReviewed { get; set; } = 0;

    /// <summary>Số từ mới học lần đầu.</summary>
    public int NewWordCount { get; set; } = 0;

    /// <summary>Số từ ôn tập lại.</summary>
    public int ReviewWordCount { get; set; } = 0;

    /// <summary>XP nhận được trong phiên này.</summary>
    public int XPEarned { get; set; } = 0;

    /// <summary>Ghi chú tùy chọn cho phiên học.</summary>
    public string? Note { get; set; }

    /// <summary>Thời điểm tạo bản ghi.</summary>
    public string CreatedAt { get; set; } = string.Empty;
}
