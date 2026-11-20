using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SchoolFacilities;

/// <summary>Truy cập SQL Server (ADO.NET, truy vấn có tham số).</summary>
public static class Db
{
    static string? _cs;

    public static string ConnectionString => _cs ??= LoadConnectionString();

    static string LoadConnectionString()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "connection.txt");
        if (File.Exists(file))
        {
            foreach (var line in File.ReadAllLines(file))
            {
                var t = line.Trim();
                if (t.Length > 0 && !t.StartsWith('#')) return t;
            }
        }
        return @"Server=.\SQLEXPRESS;Database=SchoolFacilities;Integrated Security=True;TrustServerCertificate=True;";
    }

    static SqlCommand Build(SqlConnection cn, string sql, (string Name, object? Value)[] p)
    {
        var cmd = new SqlCommand(sql, cn);
        foreach (var (name, value) in p)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    public static DataTable Query(string sql, params (string Name, object? Value)[] p)
    {
        using var cn = new SqlConnection(ConnectionString);
        using var cmd = Build(cn, sql, p);
        var table = new DataTable();
        using var adapter = new SqlDataAdapter(cmd);
        adapter.Fill(table);
        return table;
    }

    public static int Exec(string sql, params (string Name, object? Value)[] p)
    {
        using var cn = new SqlConnection(ConnectionString);
        cn.Open();
        using var cmd = Build(cn, sql, p);
        return cmd.ExecuteNonQuery();
    }

    public static object? Scalar(string sql, params (string Name, object? Value)[] p)
    {
        using var cn = new SqlConnection(ConnectionString);
        cn.Open();
        using var cmd = Build(cn, sql, p);
        var v = cmd.ExecuteScalar();
        return v == DBNull.Value ? null : v;
    }
}

public static class Session
{
    public static int UserId;
    public static string Username = "";
    public static string FullName = "";
    public static string Role = "";
    public static bool IsAdmin => Role == "Admin";
}

public static class Security
{
    // Chỉ để tham khảo: dự án thật nên dùng băm có muối (PBKDF2 / bcrypt / Argon2).
    public static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}

public record AppModule(string Key, string Title, IconKind Icon, Func<Control> Create, bool AdminOnly = false);

public interface INavigator
{
    void Open(string key, string? search = null);
    void ShowHome();
    void ChangePassword();
    void EditSchoolInfo();
}

public static class SchoolInfo
{
    public static readonly string[] Keys = { "SchoolName", "Address", "SchoolYear", "Principal" };

    public static Dictionary<string, string> Load()
    {
        var d = Keys.ToDictionary(k => k, _ => "");
        try
        {
            foreach (DataRow r in Db.Query("SELECT KeyName, Value FROM SchoolInfo").Rows)
                d[(string)r[0]] = r[1] as string ?? "";
        }
        catch
        {
            // Bảng chưa có hoặc chưa kết nối được: dùng giá trị rỗng.
        }
        return d;
    }

    public static void Save(Dictionary<string, object?> values)
    {
        foreach (var key in Keys)
        {
            values.TryGetValue(key, out var v);
            Db.Exec(@"UPDATE SchoolInfo SET Value=@v WHERE KeyName=@k;
                      IF @@ROWCOUNT = 0 INSERT INTO SchoolInfo(KeyName, Value) VALUES (@k, @v);",
                    ("@k", key), ("@v", v));
        }
    }
}
