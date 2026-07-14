namespace Mahu.Data.Models;

/// <summary>
/// Cài đặt và thông tin người dùng của ứng dụng.
/// Chỉ tồn tại đúng 1 bản ghi (Id = 1).
/// </summary>
public class AppSettings
{
    // -------------------------------------------------------
    // PRIMARY KEY
    // -------------------------------------------------------

    public int Id { get; set; } = 1;

    // -------------------------------------------------------
    // PROFILE
    // -------------------------------------------------------

    /// <summary>Tên hiển thị của người dùng.</summary>
    public string DisplayName { get; set; } = "User";

    /// <summary>Đường dẫn ảnh đại diện.</summary>
    public string? Avatar { get; set; }

    // -------------------------------------------------------
    // LEARNING
    // -------------------------------------------------------

    /// <summary>Tổng XP tích lũy.</summary>
    public int TotalXP { get; set; } = 0;

    /// <summary>Level hiện tại.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Chuỗi ngày học liên tiếp.</summary>
    public int CurrentStreak { get; set; } = 0;

    /// <summary>Ngày học gần nhất (ISO 8601).</summary>
    public string? LastActiveDate { get; set; }

    /// <summary>Tổng thời gian học (phút).</summary>
    public int TotalStudyTime { get; set; } = 0;

    /// <summary>Tổng số từ đã học.</summary>
    public int TotalWordsLearned { get; set; } = 0;

    /// <summary>Tổng số lượt Quiz đã chơi.</summary>
    public int TotalQuizPlayed { get; set; } = 0;

    /// <summary>Tổng câu trả lời đúng.</summary>
    public int TotalCorrectAnswers { get; set; } = 0;

    /// <summary>Tổng câu trả lời sai.</summary>
    public int TotalWrongAnswers { get; set; } = 0;

    // -------------------------------------------------------
    // DAILY GOAL
    // -------------------------------------------------------

    /// <summary>Mục tiêu số từ học mỗi ngày.</summary>
    public int DailyWordGoal { get; set; } = 20;

    /// <summary>Mục tiêu thời gian học mỗi ngày (phút).</summary>
    public int DailyStudyTimeGoal { get; set; } = 30;

    // -------------------------------------------------------
    // APPEARANCE
    // -------------------------------------------------------

    /// <summary>Giao diện: "Light", "Dark", "System".</summary>
    public string Theme { get; set; } = "System";

    /// <summary>Ngôn ngữ: "vi-VN", "en-US".</summary>
    public string Language { get; set; } = "en-US";

    // -------------------------------------------------------
    // PRONUNCIATION
    // -------------------------------------------------------

    /// <summary>Tự động phát âm khi xem từ (1 = bật).</summary>
    public bool AutoPlayPronunciation { get; set; } = true;

    /// <summary>Tốc độ đọc (0.5 → 2.0).</summary>
    public double SpeechRate { get; set; } = 1.0;

    /// <summary>Âm lượng phát âm (0 → 100).</summary>
    public int SpeechVolume { get; set; } = 100;

    /// <summary>Giọng đọc TTS.</summary>
    public string? SpeechVoice { get; set; }

    // -------------------------------------------------------
    // QUIZ
    // -------------------------------------------------------

    /// <summary>Số câu Quiz mặc định mỗi lần chơi.</summary>
    public int DefaultQuizQuestionCount { get; set; } = 20;

    /// <summary>Trộn câu hỏi ngẫu nhiên (1 = bật).</summary>
    public bool ShuffleQuestions { get; set; } = true;

    /// <summary>Trộn đáp án ngẫu nhiên (1 = bật).</summary>
    public bool ShuffleAnswers { get; set; } = true;

    // -------------------------------------------------------
    // BACKUP
    // -------------------------------------------------------

    /// <summary>Tự động sao lưu DB (1 = bật).</summary>
    public bool AutoBackup { get; set; } = false;

    /// <summary>Thư mục lưu backup.</summary>
    public string? BackupFolder { get; set; }

    // -------------------------------------------------------
    // REMINDER
    // -------------------------------------------------------

    /// <summary>Bật nhắc nhở học (1 = bật).</summary>
    public bool EnableReminder { get; set; } = false;

    /// <summary>Giờ nhắc nhở (HH:mm), mặc định "20:00".</summary>
    public string? ReminderTime { get; set; } = "20:00";

    // -------------------------------------------------------
    // SYSTEM
    // -------------------------------------------------------

    /// <summary>Phiên bản schema DB hiện tại.</summary>
    public int DatabaseVersion { get; set; } = 1;

    /// <summary>Thời điểm tạo bản ghi.</summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Thời điểm cập nhật gần nhất.</summary>
    public string UpdatedAt { get; set; } = string.Empty;
}
