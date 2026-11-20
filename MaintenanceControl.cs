using System.Data;

namespace SchoolFacilities;

/// <summary>
/// Quản lý bảo trì: kế thừa CrudControl, thêm nút "Đánh dấu hoàn thành" (MaintenanceControl.Designer.cs)
/// và đồng bộ tình trạng thiết bị theo trạng thái yêu cầu.
/// </summary>
public partial class MaintenanceControl : CrudControl
{
    const string ListSqlText =
        @"SELECT m.MaintenanceId AS Id, e.AssetCode AS [Mã tài sản], e.Name AS [Thiết bị],
                 m.ReportDate AS [Ngày báo], m.Description AS [Mô tả sự cố], m.Status AS [Trạng thái],
                 m.CompletedDate AS [Ngày hoàn thành], m.Cost AS [Chi phí], u.FullName AS [Người báo]
          FROM Maintenance m
          JOIN Equipment e ON e.EquipmentId = m.EquipmentId
          LEFT JOIN Users u ON u.UserId = m.ReportedBy
          ORDER BY m.ReportDate DESC, m.MaintenanceId DESC";

    public MaintenanceControl()
        : base("Quản lý bảo trì", "yêu cầu bảo trì", "Maintenance", "MaintenanceId", ListSqlText,
               () => new MaintenanceForm(), Session.IsAdmin)
    {
        InitializeComponent();
        DpiScaler.ApplyControl(btnComplete);
    }

    protected override string? ValidateValues(Dictionary<string, object?> v, bool isNew)
    {
        if ((string?)v["Status"] == "Hoàn thành" && v["CompletedDate"] == null)
            return "Trạng thái \"Hoàn thành\" cần có ngày hoàn thành (tích vào ô bên cạnh ô ngày).";
        return null;
    }

    protected override Dictionary<string, object?> Transform(Dictionary<string, object?> v, bool isNew)
    {
        var copy = new Dictionary<string, object?>(v);
        if (isNew) copy["ReportedBy"] = Session.UserId;
        return copy;
    }

    protected override void AfterSave(int? id, DataRow? oldRow, Dictionary<string, object?> v, bool isNew)
    {
        var equipmentStatus = (string?)v["Status"] switch
        {
            "Hoàn thành" => "Tốt",
            "Đang xử lý" => "Đang bảo trì",
            _ => "Hỏng"
        };
        Db.Exec("UPDATE Equipment SET Status = @s WHERE EquipmentId = @e AND Status <> N'Đã thanh lý'",
                ("@s", equipmentStatus), ("@e", v["EquipmentId"]));
    }

    void btnComplete_Click(object? sender, EventArgs e)
    {
        var id = RequireSelection();
        if (id == null) return;

        try
        {
            using var dlg = new CompleteForm();
            dlg.LoadValues(null);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            Db.Exec(@"SET XACT_ABORT ON; BEGIN TRAN;
                      UPDATE Maintenance SET Status = N'Hoàn thành', CompletedDate = @d, Cost = @c WHERE MaintenanceId = @id;
                      UPDATE Equipment SET Status = N'Tốt'
                       WHERE EquipmentId = (SELECT EquipmentId FROM Maintenance WHERE MaintenanceId = @id)
                         AND Status <> N'Đã thanh lý';
                      COMMIT;",
                    ("@id", id.Value), ("@d", dlg.Values["CompletedDate"]), ("@c", dlg.Values["Cost"]));
            LoadData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }
}
