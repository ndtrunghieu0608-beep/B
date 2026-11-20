using System.Data;

namespace SchoolFacilities;

/// <summary>
/// Quản lý thiết bị: kế thừa CrudControl, thêm nút "Điều chuyển phòng" và "Báo hỏng".
/// Hai nút được đặt trong EquipmentControl.Designer.cs (thêm vào thanh công cụ của lớp cha).
/// </summary>
public partial class EquipmentControl : CrudControl
{
    const string ListSqlText =
        @"SELECT e.EquipmentId AS Id, e.AssetCode AS [Mã tài sản], e.Name AS [Tên thiết bị], t.Name AS [Loại],
                 ISNULL(r.Code + N' - ' + r.Name, N'(Chưa gán phòng)') AS [Phòng],
                 e.PurchaseDate AS [Ngày mua], e.Price AS [Giá trị], e.Status AS [Tình trạng]
          FROM Equipment e
          JOIN EquipmentTypes t ON t.TypeId = e.TypeId
          LEFT JOIN Rooms r ON r.RoomId = e.RoomId
          ORDER BY e.AssetCode";

    public EquipmentControl()
        : base("Quản lý thiết bị / tài sản", "thiết bị", "Equipment", "EquipmentId", ListSqlText,
               () => new EquipmentForm(), Session.IsAdmin)
    {
        InitializeComponent();
        DpiScaler.ApplyControl(btnTransfer);
        DpiScaler.ApplyControl(btnBroken);
    }

    // Ghi lịch sử điều chuyển khi đổi phòng bằng hộp thoại Sửa.
    protected override void AfterSave(int? id, DataRow? oldRow, Dictionary<string, object?> v, bool isNew)
    {
        if (isNew || id == null || oldRow == null) return;
        object? oldRoom = oldRow["RoomId"] is DBNull ? null : oldRow["RoomId"];
        v.TryGetValue("RoomId", out var newRoom);
        if (object.Equals(oldRoom, newRoom)) return;

        Db.Exec(@"INSERT INTO EquipmentTransfers (EquipmentId, FromRoomId, ToRoomId, ByUser, Note)
                  VALUES (@e, @f, @t, @u, N'Cập nhật từ biểu mẫu sửa thiết bị')",
                ("@e", id.Value), ("@f", oldRoom), ("@t", newRoom), ("@u", Session.UserId));
    }

    void btnTransfer_Click(object? sender, EventArgs e)
    {
        var id = RequireSelection();
        if (id == null) return;

        try
        {
            using var dlg = new TransferForm();
            dlg.LoadValues(null);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            Db.Exec(@"SET XACT_ABORT ON; BEGIN TRAN;
                      INSERT INTO EquipmentTransfers (EquipmentId, FromRoomId, ToRoomId, ByUser, Note)
                      SELECT EquipmentId, RoomId, @to, @uid, @note FROM Equipment WHERE EquipmentId = @id;
                      UPDATE Equipment SET RoomId = @to WHERE EquipmentId = @id;
                      COMMIT;",
                    ("@id", id.Value), ("@to", dlg.Values["ToRoomId"]), ("@uid", Session.UserId), ("@note", dlg.Values["Note"]));
            LoadData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    void btnBroken_Click(object? sender, EventArgs e)
    {
        var id = RequireSelection();
        if (id == null) return;

        try
        {
            using var dlg = new BrokenForm();
            dlg.LoadValues(null);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            Db.Exec(@"SET XACT_ABORT ON; BEGIN TRAN;
                      INSERT INTO Maintenance (EquipmentId, ReportDate, Description, Status, ReportedBy)
                      VALUES (@id, CAST(GETDATE() AS DATE), @d, N'Chờ xử lý', @uid);
                      UPDATE Equipment SET Status = N'Hỏng' WHERE EquipmentId = @id;
                      COMMIT;",
                    ("@id", id.Value), ("@d", dlg.Values["Description"]), ("@uid", Session.UserId));
            LoadData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }
}
