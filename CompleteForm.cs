using System.Data;

namespace SchoolFacilities;

/// <summary>Hoàn thành một yêu cầu bảo trì. Giao diện: CompleteForm.Designer.cs.</summary>
public partial class CompleteForm : RecordForm
{
    public CompleteForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        SetDate(dtpCompleted, null);
        txtCost.Text = "";
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryDecimal(txtCost, "Chi phí (đ)", false, out var cost)) return false;

        v["CompletedDate"] = GetDate(dtpCompleted);
        v["Cost"] = cost;
        return true;
    }
}
