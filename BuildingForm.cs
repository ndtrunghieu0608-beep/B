using System.Data;

namespace SchoolFacilities;

/// <summary>Thêm/sửa tòa nhà. Giao diện: BuildingForm.Designer.cs.</summary>
public partial class BuildingForm : RecordForm
{
    public BuildingForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        txtCode.Text = Str(e, "Code");
        txtName.Text = Str(e, "Name");
        txtFloors.Text = Str(e, "Floors");
        txtNote.Text = Str(e, "Note");
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtCode, "Mã tòa nhà", true, out var code)) return false;
        if (!TryText(txtName, "Tên tòa nhà", true, out var name)) return false;
        if (!TryInt(txtFloors, "Số tầng", true, out var floors)) return false;

        v["Code"] = code;
        v["Name"] = name;
        v["Floors"] = floors;
        v["Note"] = ReadText(txtNote);
        return true;
    }
}
