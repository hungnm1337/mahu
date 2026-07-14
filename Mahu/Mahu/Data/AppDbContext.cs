using System.IO;
using Microsoft.Data.Sqlite;

namespace Mahu.Data;

/// <summary>
/// Quản lý connection string và cung cấp SqliteConnection cho toàn ứng dụng.
/// DB file đặt cùng thư mục với file thực thi (mahu.db).
/// </summary>
public static class AppDbContext
{
    /// <summary>Đường dẫn tuyệt đối tới file database.</summary>
    public static readonly string DbPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mahu.db");

    /// <summary>Connection string SQLite.</summary>
    public static string ConnectionString =>
        new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            ForeignKeys = true
        }.ToString();

    /// <summary>
    /// Tạo và trả về một SqliteConnection mới (chưa mở).
    /// Gọi using() hoặc tự dispose sau khi dùng.
    /// </summary>
    public static SqliteConnection CreateConnection()
        => new SqliteConnection(ConnectionString);
}
