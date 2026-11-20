using System.Data;

namespace SchoolFacilities;

/// <summary>Đổi mật khẩu. Giao diện: ChangePasswordForm.Designer.cs. Việc kiểm tra mật khẩu cũ do nơi gọi truyền vào qua Verify.</summary>
public partial class ChangePasswordForm : RecordForm
{
    public ChangePasswordForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (txtOld.Text.Length == 0) return Fail("Vui lòng nhập \"Mật khẩu hiện tại\".", txtOld);
        if (txtNew.Text.Length == 0) return Fail("Vui lòng nhập \"Mật khẩu mới\".", txtNew);
        if (txtConfirm.Text.Length == 0) return Fail("Vui lòng nhập \"Nhập lại mật khẩu mới\".", txtConfirm);

        v["Old"] = txtOld.Text;
        v["New"] = txtNew.Text;
        v["Confirm"] = txtConfirm.Text;
        return true;
    }
}
