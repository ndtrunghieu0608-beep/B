using System.Data;

namespace SchoolFacilities;

/// <summary>Thêm/sửa yêu cầu bảo trì. Giao diện: MaintenanceForm.Designer.cs.</summary>
public partial class MaintenanceForm : RecordForm
{
    public MaintenanceForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        FillCombo(cboEquipment, Sql.EquipmentCombo, false, Cur(e, "EquipmentId"));
        SetDate(dtpReport, Cur(e, "ReportDate"));
        txtDescription.Text = Str(e, "Description");
        FillCombo(cboStatus, Lists.MaintenanceStatus, Cur(e, "Status"));
        SetDate(dtpCompleted, Cur(e, "CompletedDate"));
        txtCost.Text = Cur(e, "Cost") is decimal cost ? cost.ToString("0") : "";
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryCombo(cboEquipment, "Thiết bị", true, out var equipment)) return false;
        if (!TryText(txtDescription, "Mô tả sự cố", true, out var description)) return false;
        if (!TryCombo(cboStatus, "Trạng thái", true, out var status)) return false;
        if (!TryDecimal(txtCost, "Chi phí (đ)", false, out var cost)) return false;

        v["EquipmentId"] = equipment;
        v["ReportDate"] = GetDate(dtpReport);
        v["Description"] = description;
        v["Status"] = status;
        v["CompletedDate"] = GetDate(dtpCompleted);
        v["Cost"] = cost;
        return true;
    }
}
