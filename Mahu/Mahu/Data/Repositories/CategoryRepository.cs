using Mahu.Data.Models;
using Microsoft.Data.Sqlite;

namespace Mahu.Data.Repositories;

/// <summary>
/// Repository cho bảng Categories.
/// </summary>
public static class CategoryRepository
{
    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    /// <summary>Lấy toàn bộ danh mục, sắp xếp theo DisplayOrder.</summary>
    public static List<Category> GetAll()
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Categories ORDER BY DisplayOrder ASC, Name ASC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Category>();
        while (reader.Read()) list.Add(MapReader(reader));
        return list;
    }

    /// <summary>Lấy một danh mục theo Id.</summary>
    public static Category? GetById(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Categories WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReader(reader) : null;
    }

    /// <summary>Kiểm tra tên danh mục đã tồn tại chưa (dùng trước khi thêm/sửa).</summary>
    public static bool IsNameExists(string name, string? excludeId = null)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        if (excludeId == null)
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Categories WHERE Name = $name COLLATE NOCASE";
            cmd.Parameters.AddWithValue("$name", name);
        }
        else
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Categories WHERE Name = $name COLLATE NOCASE AND Id != $excludeId";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$excludeId", excludeId);
        }
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // ----------------------------------------------------------------
    // WRITE
    // ----------------------------------------------------------------

    /// <summary>Thêm danh mục mới. Id được tự sinh GUID nếu rỗng.</summary>
    public static void Insert(Category category)
    {
        if (string.IsNullOrEmpty(category.Id))
            category.Id = Guid.NewGuid().ToString();

        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Categories (Id, Name, Description, Color, Icon, DisplayOrder)
            VALUES ($Id, $Name, $Description, $Color, $Icon, $DisplayOrder)
            """;
        cmd.Parameters.AddWithValue("$Id",           category.Id);
        cmd.Parameters.AddWithValue("$Name",         category.Name);
        cmd.Parameters.AddWithValue("$Description",  (object?)category.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Color",        (object?)category.Color       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Icon",         (object?)category.Icon        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$DisplayOrder", category.DisplayOrder);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Cập nhật thông tin danh mục.</summary>
    public static void Update(Category category)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Categories SET
                Name         = $Name,
                Description  = $Description,
                Color        = $Color,
                Icon         = $Icon,
                DisplayOrder = $DisplayOrder
            WHERE Id = $Id
            """;
        cmd.Parameters.AddWithValue("$Id",           category.Id);
        cmd.Parameters.AddWithValue("$Name",         category.Name);
        cmd.Parameters.AddWithValue("$Description",  (object?)category.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Color",        (object?)category.Color       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$Icon",         (object?)category.Icon        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$DisplayOrder", category.DisplayOrder);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Xóa danh mục (CASCADE → Packets → Vocabularies → WordProgress).</summary>
    public static void Delete(string id)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Categories WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Cập nhật thứ tự hiển thị cho nhiều danh mục cùng lúc.
    /// Truyền vào danh sách (categoryId, newOrder).
    /// </summary>
    public static void UpdateOrder(IEnumerable<(string Id, int Order)> items)
    {
        using var conn = AppDbContext.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE Categories SET DisplayOrder = $order WHERE Id = $id";

        var pId    = cmd.Parameters.Add("$id",    SqliteType.Text);
        var pOrder = cmd.Parameters.Add("$order", SqliteType.Integer);

        foreach (var (id, order) in items)
        {
            pId.Value    = id;
            pOrder.Value = order;
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // ----------------------------------------------------------------
    // PRIVATE
    // ----------------------------------------------------------------

    private static Category MapReader(SqliteDataReader r) => new()
    {
        Id           = r.GetString(r.GetOrdinal("Id")),
        Name         = r.GetString(r.GetOrdinal("Name")),
        Description  = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        Color        = r.IsDBNull(r.GetOrdinal("Color"))       ? null : r.GetString(r.GetOrdinal("Color")),
        Icon         = r.IsDBNull(r.GetOrdinal("Icon"))        ? null : r.GetString(r.GetOrdinal("Icon")),
        DisplayOrder = r.GetInt32(r.GetOrdinal("DisplayOrder")),
        CreatedAt    = r.GetString(r.GetOrdinal("CreatedAt")),
    };
}
