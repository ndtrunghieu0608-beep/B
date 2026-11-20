namespace SchoolFacilities;

internal static class Sql
{
    public const string BuildingCombo = "SELECT BuildingId AS Id, Name AS Name FROM Buildings ORDER BY Name";
    public const string RoomCombo = "SELECT RoomId AS Id, Code + N' - ' + Name AS Name FROM Rooms ORDER BY Code";
    public const string TypeCombo = "SELECT TypeId AS Id, Name AS Name FROM EquipmentTypes ORDER BY Name";
    public const string EquipmentCombo =
        "SELECT EquipmentId AS Id, AssetCode + N' - ' + Name AS Name FROM Equipment WHERE Status<>N'Đã thanh lý' ORDER BY AssetCode";
}

internal static class Lists
{
    public static readonly string[] RoomTypes =
        { "Phòng học", "Phòng thí nghiệm", "Phòng tin học", "Thư viện", "Phòng thể chất", "Phòng làm việc", "Kho", "Khác" };
    public static readonly string[] RoomStatus = { "Đang sử dụng", "Đang sửa chữa", "Ngừng sử dụng" };
    public static readonly string[] EquipmentStatus = { "Tốt", "Đang bảo trì", "Hỏng", "Đã thanh lý" };
    public static readonly string[] MaintenanceStatus = { "Chờ xử lý", "Đang xử lý", "Hoàn thành" };
}

/// <summary>Các module (trang chức năng) của ứng dụng, dùng cho menu và các ô truy cập nhanh.</summary>
public static class AppModules
{
    public static readonly List<AppModule> All = new()
    {
        new("buildings",   "Tòa nhà",       IconKind.Building, () => Pages.Buildings()),
        new("rooms",       "Phòng",         IconKind.Room,     () => Pages.Rooms()),
        new("types",       "Loại thiết bị", IconKind.Category, () => Pages.EquipmentTypes()),
        new("equipment",   "Thiết bị",      IconKind.Device,   () => new EquipmentControl()),
        new("maintenance", "Bảo trì",       IconKind.Wrench,   () => new MaintenanceControl()),
        new("reports",     "Báo cáo",       IconKind.Chart,    () => new ReportsControl()),
        new("users",       "Người dùng",    IconKind.Users,    () => new UsersControl(), AdminOnly: true),
    };
}

/// <summary>
/// Các trang quản lý đơn giản: chỉ cần tên bảng, câu truy vấn danh sách và form nhập liệu (đã thiết kế trong Designer).
/// Muốn thêm trang mới: tạo form nhập liệu kế thừa RecordForm, rồi khai báo thêm một hàm ở đây.
/// </summary>
public static class Pages
{
    public static CrudControl Buildings() => new(
        "Quản lý tòa nhà", "tòa nhà", "Buildings", "BuildingId",
        @"SELECT BuildingId AS Id, Code AS [Mã tòa nhà], Name AS [Tên tòa nhà], Floors AS [Số tầng], Note AS [Ghi chú]
          FROM Buildings ORDER BY Code",
        () => new BuildingForm(),
        Session.IsAdmin);

    public static CrudControl Rooms() => new(
        "Quản lý phòng", "phòng", "Rooms", "RoomId",
        @"SELECT r.RoomId AS Id, r.Code AS [Mã phòng], r.Name AS [Tên phòng], b.Name AS [Tòa nhà],
                 r.Floor AS [Tầng], r.RoomType AS [Loại phòng], r.Capacity AS [Sức chứa], r.Status AS [Tình trạng],
                 (SELECT COUNT(*) FROM Equipment e WHERE e.RoomId = r.RoomId AND e.Status <> N'Đã thanh lý') AS [Số thiết bị]
          FROM Rooms r JOIN Buildings b ON b.BuildingId = r.BuildingId
          ORDER BY b.Name, r.Code",
        () => new RoomForm(),
        Session.IsAdmin);

    public static CrudControl EquipmentTypes() => new(
        "Loại thiết bị", "loại thiết bị", "EquipmentTypes", "TypeId",
        @"SELECT t.TypeId AS Id, t.Name AS [Tên loại], t.Note AS [Ghi chú],
                 (SELECT COUNT(*) FROM Equipment e WHERE e.TypeId = t.TypeId) AS [Số thiết bị]
          FROM EquipmentTypes t ORDER BY t.Name",
        () => new TypeForm(),
        Session.IsAdmin);
}

/// <summary>Quản lý người dùng (chỉ Admin). Mật khẩu được băm trước khi lưu.</summary>
public class UsersControl : CrudControl
{
    const string ListSqlText =
        @"SELECT UserId AS Id, Username AS [Tên đăng nhập], FullName AS [Họ tên], Role AS [Vai trò],
                 CASE WHEN IsActive = 1 THEN N'Đang hoạt động' ELSE N'Đã khóa' END AS [Trạng thái]
          FROM Users ORDER BY Username";

    public UsersControl()
        : base("Quản lý người dùng", "người dùng", "Users", "UserId", ListSqlText, () => new UserForm(), true)
    {
    }

    protected override string? ValidateValues(Dictionary<string, object?> v, bool isNew)
    {
        var pw = v["Password"] as string;
        if (isNew && string.IsNullOrEmpty(pw)) return "Vui lòng nhập mật khẩu cho người dùng mới.";
        if (!string.IsNullOrEmpty(pw) && pw.Length < 6) return "Mật khẩu phải có ít nhất 6 ký tự.";
        return null;
    }

    protected override Dictionary<string, object?> Transform(Dictionary<string, object?> v, bool isNew)
    {
        var copy = new Dictionary<string, object?>(v);
        var pw = copy["Password"] as string;
        copy.Remove("Password");
        if (!string.IsNullOrEmpty(pw)) copy["PasswordHash"] = Security.Hash(pw);
        return copy;
    }

    protected override string? CanDelete(int id) =>
        id == Session.UserId ? "Bạn không thể xóa chính tài khoản đang đăng nhập." : null;
}
