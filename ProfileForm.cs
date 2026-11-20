using System.Data;

namespace SchoolFacilities;

/// <summary>
/// Thông tin tài khoản: xem tên đăng nhập / vai trò (khóa), sửa họ tên, và đổi mật khẩu (tùy chọn).
/// Giao diện: ProfileForm.Designer.cs. Việc kiểm tra mật khẩu cũ do nơi gọi truyền vào qua Verify.
/// Để trống ba ô mật khẩu nghĩa là không đổi mật khẩu.
/// </summary>
public partial class ProfileForm : RecordForm
{
    public ProfileForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        txtUsername.Text = Str(e, "Username");
        txtFullName.Text = Str(e, "FullName");
        txtRole.Text = Str(e, "Role") == "Admin" ? "Quản trị viên (Admin)" : "Nhân viên (Staff)";
        txtOld.Text = "";
        txtNew.Text = "";
        txtConfirm.Text = "";
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtFullName, "Họ tên", true, out var fullName)) return false;

        // Chỉ khi người dùng nhập vào bất kỳ ô mật khẩu nào thì mới yêu cầu nhập đủ cả ba ô.
        bool changing = txtOld.Text.Length > 0 || txtNew.Text.Length > 0 || txtConfirm.Text.Length > 0;
        if (changing)
        {
            if (txtOld.Text.Length == 0) return Fail("Vui lòng nhập \"Mật khẩu hiện tại\" để đổi mật khẩu.", txtOld);
            if (txtNew.Text.Length == 0) return Fail("Vui lòng nhập \"Mật khẩu mới\".", txtNew);
            if (txtConfirm.Text.Length == 0) return Fail("Vui lòng nhập \"Nhập lại mật khẩu mới\".", txtConfirm);
        }

        v["FullName"] = fullName;
        v["Old"] = txtOld.Text;
        v["New"] = txtNew.Text;
        v["Confirm"] = txtConfirm.Text;
        return true;
    }
}
