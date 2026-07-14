namespace Mahu.Data.Models;

/// <summary>
/// Tiến trình học của một từ vựng theo thuật toán SM-2 (Spaced Repetition).
/// Mỗi từ chỉ có đúng một bản ghi WordProgress.
/// </summary>
public class WordProgress
{
    /// <summary>ID từ vựng (đồng thời là Primary Key và Foreign Key).</summary>
    public string VocabularyId { get; set; } = string.Empty;

    /// <summary>
    /// Hệ số Ease Factor - điều chỉnh khoảng cách ôn tập.
    /// Giá trị mặc định SM-2: 2.5. Tối thiểu: 1.3.
    /// </summary>
    public double EaseFactor { get; set; } = 2.5;

    /// <summary>Khoảng cách ôn tập hiện tại (ngày).</summary>
    public int Interval { get; set; } = 0;

    /// <summary>Số lần ôn tập thành công liên tiếp.</summary>
    public int Repetitions { get; set; } = 0;

    /// <summary>Ngày cần ôn tập tiếp theo (ISO 8601).</summary>
    public string NextReviewDate { get; set; } = string.Empty;

    /// <summary>Ngày ôn tập gần nhất (ISO 8601).</summary>
    public string? LastReviewedDate { get; set; }

    /// <summary>
    /// Trạng thái học:
    /// 0 = New (chưa học),
    /// 1 = Learning (đang học),
    /// 2 = Review (ôn tập),
    /// 3 = Mastered (đã thành thạo).
    /// </summary>
    public WordProgressState State { get; set; } = WordProgressState.New;

    /// <summary>Tổng số lần trả lời đúng.</summary>
    public int CorrectCount { get; set; } = 0;

    /// <summary>Tổng số lần trả lời sai.</summary>
    public int WrongCount { get; set; } = 0;
}

/// <summary>Trạng thái tiến trình học của một từ.</summary>
public enum WordProgressState
{
    New      = 0,
    Learning = 1,
    Review   = 2,
    Mastered = 3
}
