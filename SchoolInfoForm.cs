using System.Data;

namespace SchoolFacilities;

/// <summary>Cập nhật thông tin trường (chỉ Admin). Giao diện: SchoolInfoForm.Designer.cs.</summary>
public partial class SchoolInfoForm : RecordForm
{
    public SchoolInfoForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        txtSchoolName.Text = Str(e, "SchoolName");
        txtAddress.Text = Str(e, "Address");
        txtSchoolYear.Text = Str(e, "SchoolYear");
        txtPrincipal.Text = Str(e, "Principal");
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtSchoolName, "Tên trường", true, out var name)) return false;

        v["SchoolName"] = name;
        v["Address"] = ReadText(txtAddress);
        v["SchoolYear"] = ReadText(txtSchoolYear);
        v["Principal"] = ReadText(txtPrincipal);
        return true;
    }
}
