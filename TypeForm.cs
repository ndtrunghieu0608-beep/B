using System.Data;

namespace SchoolFacilities;

/// <summary>Thêm/sửa loại thiết bị. Giao diện: TypeForm.Designer.cs.</summary>
public partial class TypeForm : RecordForm
{
    public TypeForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        txtName.Text = Str(e, "Name");
        txtNote.Text = Str(e, "Note");
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtName, "Tên loại", true, out var name)) return false;

        v["Name"] = name;
        v["Note"] = ReadText(txtNote);
        return true;
    }
}
