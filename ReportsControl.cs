using System.Data;
using System.Globalization;
using System.Text;

namespace SchoolFacilities;

/// <summary>Báo cáo thống kê, có xuất file CSV (mở được bằng Excel). Giao diện: ReportsControl.Designer.cs.</summary>
public partial class ReportsControl : UserControl
{
    static readonly (string Name, string Sql)[] Reports =
    {
        ("Thiết bị theo phòng",
         @"SELECT b.Name AS [Tòa nhà], r.Code AS [Mã phòng], r.Name AS [Tên phòng],
                  COUNT(e.EquipmentId) AS [Số thiết bị], ISNULL(SUM(e.Price), 0) AS [Tổng giá trị]
           FROM Rooms r
           JOIN Buildings b ON b.BuildingId = r.BuildingId
           LEFT JOIN Equipment e ON e.RoomId = r.RoomId AND e.Status <> N'Đã thanh lý'
           GROUP BY b.Name, r.Code, r.Name
           ORDER BY b.Name, r.Code"),

        ("Thiết bị theo loại và tình trạng",
         @"SELECT t.Name AS [Loại thiết bị], COUNT(*) AS [Tổng],
                  SUM(CASE WHEN e.Status = N'Tốt' THEN 1 ELSE 0 END)           AS [Tốt],
                  SUM(CASE WHEN e.Status = N'Đang bảo trì' THEN 1 ELSE 0 END)  AS [Đang bảo trì],
                  SUM(CASE WHEN e.Status = N'Hỏng' THEN 1 ELSE 0 END)          AS [Hỏng],
                  SUM(CASE WHEN e.Status = N'Đã thanh lý' THEN 1 ELSE 0 END)   AS [Đã thanh lý]
           FROM Equipment e JOIN EquipmentTypes t ON t.TypeId = e.TypeId
           GROUP BY t.Name ORDER BY t.Name"),

        ("Thiết bị hỏng và đang bảo trì",
         @"SELECT e.AssetCode AS [Mã tài sản], e.Name AS [Tên thiết bị], t.Name AS [Loại],
                  ISNULL(r.Code + N' - ' + r.Name, N'(Chưa gán phòng)') AS [Phòng], e.Status AS [Tình trạng]
           FROM Equipment e
           JOIN EquipmentTypes t ON t.TypeId = e.TypeId
           LEFT JOIN Rooms r ON r.RoomId = e.RoomId
           WHERE e.Status IN (N'Hỏng', N'Đang bảo trì')
           ORDER BY e.Status, e.AssetCode"),

        ("Lịch sử điều chuyển",
         @"SELECT tr.TransferDate AS [Thời gian], e.AssetCode AS [Mã tài sản], e.Name AS [Thiết bị],
                  ISNULL(rf.Code, N'(không)') AS [Từ phòng], ISNULL(rt.Code, N'(không)') AS [Đến phòng],
                  u.FullName AS [Người thực hiện], tr.Note AS [Ghi chú]
           FROM EquipmentTransfers tr
           JOIN Equipment e ON e.EquipmentId = tr.EquipmentId
           LEFT JOIN Rooms rf ON rf.RoomId = tr.FromRoomId
           LEFT JOIN Rooms rt ON rt.RoomId = tr.ToRoomId
           LEFT JOIN Users u ON u.UserId = tr.ByUser
           ORDER BY tr.TransferDate DESC"),

        ("Chi phí bảo trì theo tháng",
         @"SELECT YEAR(CompletedDate) AS [Năm], MONTH(CompletedDate) AS [Tháng],
                  COUNT(*) AS [Số lượt], SUM(ISNULL(Cost, 0)) AS [Tổng chi phí]
           FROM Maintenance
           WHERE Status = N'Hoàn thành' AND CompletedDate IS NOT NULL
           GROUP BY YEAR(CompletedDate), MONTH(CompletedDate)
           ORDER BY 1 DESC, 2 DESC"),
    };

    DataTable? _table;

    public ReportsControl()
    {
        InitializeComponent();
        DpiScaler.Apply(this);

        UiHelper.StyleGrid(dgv);
        cboReport.Items.AddRange(Reports.Select(r => (object)r.Name).ToArray());
        cboReport.SelectedIndex = 0;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (!DesignMode) Run();
    }

    void cboReport_SelectedIndexChanged(object? sender, EventArgs e) => Run();

    void btnView_Click(object? sender, EventArgs e) => Run();

    void btnExport_Click(object? sender, EventArgs e) => Export();

    void Run()
    {
        if (cboReport.SelectedIndex < 0) return;
        try
        {
            _table = Db.Query(Reports[cboReport.SelectedIndex].Sql);
            dgv.DataSource = _table;
            UiHelper.FormatColumns(dgv);
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

    void Export()
    {
        if (_table == null || _table.Rows.Count == 0)
        {
            MessageBox.Show(FindForm(), "Báo cáo chưa có dữ liệu để xuất.", "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"{cboReport.Text}_{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", _table.Columns.Cast<DataColumn>().Select(c => Csv(c.ColumnName))));
            foreach (DataRow row in _table.Rows)
            {
                sb.AppendLine(string.Join(",", row.ItemArray.Select(x =>
                    Csv(x is DateTime d ? d.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                                        : Convert.ToString(x, CultureInfo.CurrentCulture)))));
            }
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            MessageBox.Show(FindForm(), "Đã xuất file:\n" + dlg.FileName, "Hoàn tất",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
