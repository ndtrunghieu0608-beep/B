using System.Data;
using static SchoolFacilities.Theme;

namespace SchoolFacilities;

/// <summary>
/// Trang chủ kiểu cổng thông tin. Bố cục (thẻ, ô thống kê, lối tắt, biểu đồ) nằm ở DashboardControl.Designer.cs;
/// tại đây chỉ nạp dữ liệu và xử lý nhấp chuột.
/// </summary>
public partial class DashboardControl : UserControl
{
    readonly INavigator? _nav;
    bool _loading;

    /// <summary>Hàm khởi tạo không tham số dành cho Designer.</summary>
    public DashboardControl()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
        UiHelper.StyleGrid(dgvRecent);
    }

    public DashboardControl(INavigator nav) : this()
    {
        _nav = nav;

        // Quản trị viên: ô cuối là "Người dùng"; nhân viên: ô cuối là "Tài khoản" (thông tin cá nhân + đổi mật khẩu).
        if (!Session.IsAdmin)
        {
            tileLast.Kind = IconKind.Users;
            tileLast.Caption = "Tài khoản";
            tileLast.Tag = "profile";
        }
        lnkEditSchool.Visible = Session.IsAdmin;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (!DesignMode && _nav != null) Reload();
    }

    // ================= Sự kiện nhấp chuột =================

    void tile_Click(object? sender, EventArgs e)
    {
        if (sender is not Control { Tag: string key }) return;
        if (key == "profile") _nav?.EditProfile();
        else _nav?.Open(key);
    }

    void statTotal_Click(object? sender, EventArgs e) => _nav?.Open("equipment");

    void statAttention_Click(object? sender, EventArgs e) => _nav?.Open("maintenance");

    void lnkBuildings_Click(object? sender, EventArgs e) => _nav?.Open("buildings");

    void lnkRemindMore_Click(object? sender, EventArgs e) => _nav?.Open("maintenance");

    void lnkEditSchool_Click(object? sender, EventArgs e) => _nav?.EditSchoolInfo();

    void dgvRecent_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0) _nav?.Open("maintenance");
    }

    void cboBuilding_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_loading) LoadBars();
    }

    // ================= Nạp dữ liệu =================

    void Reload()
    {
        try
        {
            _loading = true;
            LoadInfo();
            LoadBuildingFilter();
            LoadDonut();
            LoadBars();
            LoadList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), "Không tải được dữ liệu trang chủ:\n" + ex.Message, "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    void LoadInfo()
    {
        var school = SchoolInfo.Load();
        var r = Db.Query(@"SELECT
                (SELECT COUNT(*) FROM Buildings)                                              AS Buildings,
                (SELECT COUNT(*) FROM Rooms)                                                  AS Rooms,
                (SELECT COUNT(*) FROM Equipment WHERE Status <> N'Đã thanh lý')               AS Total,
                (SELECT COUNT(*) FROM Equipment WHERE Status IN (N'Hỏng', N'Đang bảo trì'))   AS Attention,
                (SELECT COUNT(*) FROM Maintenance WHERE Status <> N'Hoàn thành')              AS Pending,
                (SELECT ISNULL(SUM(Price), 0) FROM Equipment WHERE Status <> N'Đã thanh lý')  AS Value").Rows[0];

        kvLeft.Items = new List<(string, string)>
        {
            ("Tên trường", school["SchoolName"]),
            ("Địa chỉ", school["Address"]),
            ("Năm học", school["SchoolYear"]),
            ("Hiệu trưởng", school["Principal"]),
        };
        kvRight.Items = new List<(string, string)>
        {
            ("Số tòa nhà", Convert.ToInt32(r["Buildings"]).ToString("N0")),
            ("Số phòng", Convert.ToInt32(r["Rooms"]).ToString("N0")),
            ("Số thiết bị", Convert.ToInt32(r["Total"]).ToString("N0")),
            ("Tổng giá trị", Convert.ToDecimal(r["Value"]).ToString("N0") + " đ"),
        };
        kvLeft.Invalidate();
        kvRight.Invalidate();

        statTotal.Value = Convert.ToInt32(r["Total"]).ToString("N0");
        statAttention.Value = Convert.ToInt32(r["Attention"]).ToString("N0");
        statTotal.Invalidate();
        statAttention.Invalidate();

        lblRemindNumber.Text = Convert.ToInt32(r["Pending"]).ToString();

        var recent = Db.Query(@"SELECT TOP 3 e.Name + N' – ' + m.Description AS Txt
                                FROM Maintenance m JOIN Equipment e ON e.EquipmentId = m.EquipmentId
                                WHERE m.Status <> N'Hoàn thành'
                                ORDER BY m.ReportDate DESC, m.MaintenanceId DESC");
        lblRemindText.Text = recent.Rows.Count == 0
            ? "Không có yêu cầu bảo trì nào đang chờ xử lý."
            : string.Join("  •  ", recent.Rows.Cast<DataRow>().Select(x => (string)x["Txt"]));
    }

    void LoadBuildingFilter()
    {
        var dt = Db.Query("SELECT BuildingId AS Id, Name AS Name FROM Buildings ORDER BY Name");
        var all = dt.NewRow();
        all["Id"] = DBNull.Value;
        all["Name"] = "Tất cả tòa nhà";
        dt.Rows.InsertAt(all, 0);
        cboBuilding.DataSource = dt;
        cboBuilding.DisplayMember = "Name";
        cboBuilding.ValueMember = "Id";
        if (cboBuilding.Items.Count > 0) cboBuilding.SelectedIndex = 0;
    }

    void LoadDonut()
    {
        var counts = Db.Query("SELECT Status, COUNT(*) AS N FROM Equipment GROUP BY Status").Rows
            .Cast<DataRow>().ToDictionary(r => (string)r["Status"], r => Convert.ToDouble(r["N"]));

        double Get(string k) => counts.TryGetValue(k, out var v) ? v : 0;

        donut.Data = new List<(string, double, Color)>
        {
            ("Tốt", Get("Tốt"), Ok),
            ("Đang bảo trì", Get("Đang bảo trì"), Warn),
            ("Hỏng", Get("Hỏng"), Danger),
            ("Đã thanh lý", Get("Đã thanh lý"), Gray),
        };
        donut.Invalidate();
    }

    void LoadBars()
    {
        object? buildingId = cboBuilding.SelectedValue is DBNull ? null : cboBuilding.SelectedValue;
        var dt = Db.Query(@"SELECT t.Name AS Name, COUNT(e.EquipmentId) AS N
                            FROM EquipmentTypes t
                            LEFT JOIN Equipment e ON e.TypeId = t.TypeId AND e.Status <> N'Đã thanh lý'
                                 AND (CAST(@b AS INT) IS NULL OR e.RoomId IN (SELECT RoomId FROM Rooms WHERE BuildingId = CAST(@b AS INT)))
                            GROUP BY t.Name
                            ORDER BY COUNT(e.EquipmentId) DESC, t.Name",
                          ("@b", buildingId));
        bars.Data = dt.Rows.Cast<DataRow>()
            .Take(8)
            .Select(r => ((string)r["Name"], Convert.ToDouble(r["N"])))
            .ToList();
        bars.Invalidate();
    }

    /// <summary>Danh sách yêu cầu bảo trì gần đây (lưới dữ liệu đặt sẵn trong Designer, ba cột Code/Title/Status).</summary>
    void LoadList()
    {
        var dt = Db.Query(@"SELECT TOP 5 e.AssetCode AS Code, e.Name AS Title, m.Status AS Status
                            FROM Maintenance m JOIN Equipment e ON e.EquipmentId = m.EquipmentId
                            WHERE m.Status <> N'Hoàn thành'
                            ORDER BY m.ReportDate DESC, m.MaintenanceId DESC");

        dgvRecent.DataSource = dt;
        dgvRecent.Visible = dt.Rows.Count > 0;
        lblRecentEmpty.Visible = dt.Rows.Count == 0;
    }
}
