using System.Data;

namespace SchoolFacilities;

/// <summary>Báo hỏng thiết bị. Giao diện: BrokenForm.Designer.cs.</summary>
public partial class BrokenForm : RecordForm
{
    public BrokenForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e) => txtDescription.Text = "";

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtDescription, "Mô tả sự cố", true, out var description)) return false;

        v["Description"] = description;
        return true;
    }
}
