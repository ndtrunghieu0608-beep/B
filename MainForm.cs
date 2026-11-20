using System.Data;
using System.Globalization;
using System.Text;

namespace SchoolFacilities;

/// <summary>
/// Khung chính. Giao diện (thanh trên, menu) nằm ở MainForm.Designer.cs.
/// Tại đây chỉ có điều hướng giữa các trang và các hộp thoại tài khoản.
/// </summary>
public partial class MainForm : Form, INavigator
{
    public bool LoggedOut { get; private set; }

    public MainForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);

        logo.SchoolName = SchoolInfo.Load()["SchoolName"];
        userChip.FullName = Session.FullName;
        mnuSchoolInfo.Visible = Session.IsAdmin;
        mnuUsers.Visible = Session.IsAdmin;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ShowHome();
    }

    // ================= Sự kiện của giao diện =================

    void btnMenu_Click(object? sender, EventArgs e) => ctxModules.Show(btnMenu, new Point(0, btnMenu.Height));

    void navHome_Click(object? sender, EventArgs e) => ShowHome();

    void navBell_Click(object? sender, EventArgs e) => Open("maintenance");

    void userChip_Click(object? sender, EventArgs e) => ctxUser.Show(userChip, new Point(0, userChip.Height));

    void txtSearch_EnterPressed(object? sender, EventArgs e) => RunSearch(txtSearch.Text);

    /// <summary>Mọi mục menu chức năng dùng chung một hàm; khóa trang nằm ở thuộc tính Tag.</summary>
    void mnuModule_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripItem { Tag: string key }) return;
        if (key == "home") ShowHome();
        else Open(key);
    }

    void mnuChangePassword_Click(object? sender, EventArgs e) => ChangePassword();

    void mnuSchoolInfo_Click(object? sender, EventArgs e) => EditSchoolInfo();

    void mnuLogout_Click(object? sender, EventArgs e)
    {
        LoggedOut = true;
        Close();
    }

    // ================= Điều hướng =================

    public void Open(string key, string? search = null)
    {
        var module = AppModules.All.FirstOrDefault(x => x.Key == key);
        if (module == null || (module.AdminOnly && !Session.IsAdmin)) return;

        var page = module.Create();
        if (page is CrudControl crud && !string.IsNullOrEmpty(search)) crud.SetSearch(search);
        SetPage(page);
    }

    public void ShowHome() => SetPage(new DashboardControl(this));

    void SetPage(Control page)
    {
        pnlBody.SuspendLayout();
        foreach (var old in pnlBody.Controls.Cast<Control>().ToList())
        {
            pnlBody.Controls.Remove(old);
            // Giải phóng sau khi sự kiện Click hiện tại kết thúc (control cũ có thể là nơi phát sinh click).
            BeginInvoke(new Action(old.Dispose));
        }
        page.Dock = DockStyle.Fill;
        pnlBody.Controls.Add(page);
        pnlBody.ResumeLayout(true);
        RefreshBadge();
    }

    void RefreshBadge()
    {
        try
        {
            navBell.Badge = Convert.ToInt32(
                Db.Scalar("SELECT COUNT(*) FROM Maintenance WHERE Status <> N'Hoàn thành'") ?? 0);
        }
        catch
        {
            navBell.Badge = 0;
        }
    }

    /// <summary>Gõ tên chức năng để mở trang đó; nếu không khớp thì tìm trong danh sách thiết bị.</summary>
    void RunSearch(string text)
    {
        text = text.Trim();
        if (text.Length == 0) return;

        var q = Fold(text);
        var module = AppModules.All.FirstOrDefault(m => (!m.AdminOnly || Session.IsAdmin) && Fold(m.Title).Contains(q));
        if (module != null)
        {
            Open(module.Key);
            return;
        }
        Open("equipment", text);
    }

    static string Fold(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch == 'đ' ? 'd' : ch == 'Đ' ? 'D' : ch);
        }
        return sb.ToString().ToLowerInvariant();
    }

    // ================= Tài khoản / thông tin trường =================

    public void ChangePassword()
    {
        using var dlg = new ChangePasswordForm();
        dlg.Verify = v =>
        {
            var current = Convert.ToString(Db.Scalar("SELECT PasswordHash FROM Users WHERE UserId = @u", ("@u", Session.UserId)));
            if (!string.Equals(current, Security.Hash((string)v["Old"]!), StringComparison.OrdinalIgnoreCase))
                return "Mật khẩu hiện tại không đúng.";
            if (((string)v["New"]!).Length < 6)
                return "Mật khẩu mới phải có ít nhất 6 ký tự.";
            if ((string)v["New"]! != (string)v["Confirm"]!)
                return "Mật khẩu nhập lại không khớp.";
            return null;
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Db.Exec("UPDATE Users SET PasswordHash = @h WHERE UserId = @u",
                    ("@h", Security.Hash((string)dlg.Values["New"]!)), ("@u", Session.UserId));
            MessageBox.Show(this, "Đã đổi mật khẩu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void EditSchoolInfo()
    {
        if (!Session.IsAdmin) return;

        var info = SchoolInfo.Load();
        var table = new DataTable();
        foreach (var k in SchoolInfo.Keys) table.Columns.Add(k, typeof(string));
        var row = table.NewRow();
        foreach (var k in SchoolInfo.Keys) row[k] = info[k];
        table.Rows.Add(row);

        using var dlg = new SchoolInfoForm();
        dlg.LoadValues(row);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            SchoolInfo.Save(dlg.Values);
            logo.SchoolName = (string)dlg.Values["SchoolName"]!;
            ShowHome();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
