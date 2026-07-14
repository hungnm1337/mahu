namespace Mahu.Data.Models;

/// <summary>
/// Một từ vựng thuộc về một Packet.
/// </summary>
public class Vocabulary
{
    /// <summary>GUID định danh duy nhất.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID Packet chứa từ này.</summary>
    public string PacketId { get; set; } = string.Empty;

    /// <summary>Từ vựng (tiếng Anh).</summary>
    public string Word { get; set; } = string.Empty;

    /// <summary>Loại từ: noun, verb, adjective, adverb, ...</summary>
    public string? WordType { get; set; }

    /// <summary>Nghĩa của từ.</summary>
    public string Meaning { get; set; } = string.Empty;

    /// <summary>Phiên âm IPA.</summary>
    public string? Phonetic { get; set; }

    /// <summary>Câu ví dụ minh họa.</summary>
    public string? Example { get; set; }

    /// <summary>Nghĩa của câu ví dụ.</summary>
    public string? ExampleMeaning { get; set; }

    /// <summary>
    /// Độ khó từ 1 → 5.
    /// 1 = Rất dễ, 5 = Rất khó.
    /// </summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>Số lần từ này được xem/ôn tập.</summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>Thời điểm tạo.</summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Thời điểm cập nhật gần nhất.</summary>
    public string UpdatedAt { get; set; } = string.Empty;
}
