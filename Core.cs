using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace SchoolFacilities;

/// <summary>Truy cập SQL Server (ADO.NET, truy vấn có tham số).</summary>
public static class Db
{
    const string DbName = "SchoolFacilities";
    static string? _cs;

    public static string ConnectionString => _cs ??= FindConnectionString();

    // ---------- Tìm chuỗi kết nối ----------
    // Thứ tự: (1) lần chạy trước đã lưu  (2) connection.txt do người dùng tự điền  (3) tự dò các SQL Server trên máy.
    // Server nào có sẵn database SchoolFacilities (nơi người dùng vừa chạy schema.sql) sẽ được chọn.
    static string FindConnectionString()
    {
        var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SchoolFacilities", "server.txt");

        // 1. Kết nối đã lưu
        try
        {
            if (File.Exists(cache))
            {
                var saved = File.ReadAllText(cache).Trim();
                if (saved.Length > 0 && CanConnect(saved)) return saved;
            }
        }
        catch { }

        // 2. connection.txt (dòng đầu không bắt đầu bằng #)
        var fromFile = ReadConnectionFile();
        if (fromFile != null)
        {
            var cs = WithTimeout(fromFile);
            if (CanConnect(cs)) return Save(cache, cs);
        }

        // 3. Tự dò
        var found = AutoDetect();
        if (found != null) return Save(cache, found);

        throw new InvalidOperationException(
            "Không tìm thấy cơ sở dữ liệu SchoolFacilities trên máy này.\n\n" +
            "Hãy mở Database/schema.sql trong SSMS (kết nối vào SQL Server của máy) và bấm Execute, rồi chạy lại ứng dụng.\n" +
            "Nếu bạn dùng tài khoản SQL (sa...), hãy điền chuỗi kết nối vào connection.txt.");
    }

    static string Save(string cacheFile, string cs)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
            File.WriteAllText(cacheFile, cs);
        }
        catch { }
        return cs;
    }

    static string? ReadConnectionFile()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "connection.txt");
        if (!File.Exists(file)) return null;
        foreach (var line in File.ReadAllLines(file))
        {
            var t = line.Trim();
            if (t.Length > 0 && !t.StartsWith('#')) return t;
        }
        return null;
    }

    static string WithTimeout(string cs) =>
        cs.Contains("timeout", StringComparison.OrdinalIgnoreCase) ? cs : cs.TrimEnd().TrimEnd(';') + ";Connect Timeout=3;";

    static bool CanConnect(string cs)
    {
        try
        {
            using var cn = new SqlConnection(cs);
            cn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    static string? AutoDetect()
    {
        var servers = new List<string>();

        // Mọi instance SQL Server đã cài trên máy (đọc từ registry) - kể cả tên instance lạ.
        try
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
            foreach (var name in key?.GetValueNames() ?? Array.Empty<string>())
                servers.Add(name == "MSSQLSERVER" ? "." : @".\" + name);
        }
        catch { }

        // Các địa chỉ phổ biến khác.
        servers.AddRange(new[] { @".\SQLEXPRESS", @"(localdb)\MSSQLLocalDB", "(local)", "localhost" });

        var list = servers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Thử song song cho nhanh; giữ đúng thứ tự ưu tiên khi chọn kết quả.
        var tasks = list.Select(s => Task.Run<string?>(() =>
        {
            var cs = $"Server={s};Database={DbName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=3;";
            return CanConnect(cs) ? cs : null;
        })).ToArray();
        Task.WaitAll(tasks);

        return tasks.Select(t => t.Result).FirstOrDefault(r => r != null);
    }

    // ---------- Truy vấn ----------
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
