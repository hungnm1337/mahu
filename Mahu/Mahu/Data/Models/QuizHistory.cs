namespace Mahu.Data.Models;

/// <summary>
/// Lịch sử một lần chơi Quiz.
/// </summary>
public class QuizHistory
{
    /// <summary>GUID định danh duy nhất.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID Packet được quiz (null nếu quiz toàn bộ hoặc spaced repetition).</summary>
    public string? PacketId { get; set; }

    /// <summary>
    /// Phạm vi Quiz:
    /// "Packet"           - quiz một Packet cụ thể,
    /// "SpacedRepetition" - quiz theo lịch ôn tập SM-2,
    /// "All"              - quiz toàn bộ từ vựng.
    /// </summary>
    public string QuizScope { get; set; } = "Packet";

    /// <summary>
    /// Loại Quiz:
    /// 1 = Word → Meaning (cho từ, đoán nghĩa),
    /// 2 = Meaning → Word (cho nghĩa, đoán từ).
    /// </summary>
    public QuizType QuizType { get; set; }

    /// <summary>Tổng số câu hỏi trong lượt quiz.</summary>
    public int TotalQuestions { get; set; }

    /// <summary>Số câu trả lời đúng.</summary>
    public int CorrectAnswers { get; set; }

    /// <summary>Số câu trả lời sai.</summary>
    public int WrongAnswers { get; set; }

    /// <summary>Điểm số (0 → 100).</summary>
    public int Score { get; set; }

    /// <summary>Thời gian làm bài (giây).</summary>
    public int Duration { get; set; } = 0;

    /// <summary>XP nhận được từ lượt quiz này.</summary>
    public int XPEarned { get; set; } = 0;

    /// <summary>Thời điểm hoàn thành Quiz.</summary>
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>Loại câu hỏi trong Quiz.</summary>
public enum QuizType
{
    WordToMeaning = 1,
    MeaningToWord = 2
}
