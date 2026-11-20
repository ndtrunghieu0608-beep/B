namespace SchoolFacilities;

/// <summary>Giao diện: LoginForm.Designer.cs (mở bằng Form Designer). Tại đây chỉ xử lý đăng nhập.</summary>
public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void LoginForm_Shown(object? sender, EventArgs e) => txtUser.Input.Focus();

    void txtUser_EnterPressed(object? sender, EventArgs e) => txtPass.Input.Focus();

    void txtPass_EnterPressed(object? sender, EventArgs e) => DoLogin();

    void btnLogin_Click(object? sender, EventArgs e) => DoLogin();

    void lblExit_Click(object? sender, EventArgs e) => DialogResult = DialogResult.Cancel;

    void DoLogin()
    {
        var username = txtUser.Text.Trim();
        if (username.Length == 0 || txtPass.Text.Length == 0)
        {
            MessageBox.Show(this, "Vui lòng nhập tên đăng nhập và mật khẩu.", Text,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var dt = Db.Query(@"SELECT UserId, Username, FullName, Role, PasswordHash
                                FROM Users WHERE Username = @u AND IsActive = 1", ("@u", username));

            if (dt.Rows.Count == 0 ||
                !string.Equals((string)dt.Rows[0]["PasswordHash"], Security.Hash(txtPass.Text), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Sai tên đăng nhập hoặc mật khẩu.", Text,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var r = dt.Rows[0];
            Session.UserId = (int)r["UserId"];
            Session.Username = (string)r["Username"];
            Session.FullName = (string)r["FullName"];
            Session.Role = (string)r["Role"];
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Không kết nối được cơ sở dữ liệu.\n\n" +
                "Hãy kiểm tra SQL Server đã chạy, đã chạy script Database/schema.sql và chuỗi kết nối trong connection.txt.\n\n" +
                ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
