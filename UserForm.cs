using System.Data;

namespace SchoolFacilities;

/// <summary>Thêm/sửa người dùng. Giao diện: UserForm.Designer.cs. Để trống mật khẩu khi sửa nghĩa là giữ mật khẩu cũ.</summary>
public partial class UserForm : RecordForm
{
    public UserForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        txtUsername.Text = Str(e, "Username");
        txtFullName.Text = Str(e, "FullName");
        FillCombo(cboRole, new[] { "Admin", "Staff" }, Cur(e, "Role"));
        txtPassword.Text = "";
        chkActive.Checked = e == null || Cur(e, "IsActive") is not false;
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtUsername, "Tên đăng nhập", true, out var username)) return false;
        if (!TryText(txtFullName, "Họ tên", true, out var fullName)) return false;
        if (!TryCombo(cboRole, "Vai trò", true, out var role)) return false;

        v["Username"] = username;
        v["FullName"] = fullName;
        v["Role"] = role;
        v["Password"] = txtPassword.Text.Length == 0 ? null : txtPassword.Text;
        v["IsActive"] = chkActive.Checked;
        return true;
    }
}
