using System.ComponentModel;

namespace SchoolFacilities;

/// <summary>
/// Form và UserControl được thiết kế trong Visual Studio Designer theo kích thước 96 DPI.
/// Gọi DpiScaler.Apply(this) ngay sau InitializeComponent() để nhân kích thước, lề và
/// các dòng/cột cố định với hệ số DPI của màn hình (một lần duy nhất).
/// Không có tác dụng khi đang ở chế độ thiết kế hoặc khi màn hình là 100%.
/// </summary>
public static class DpiScaler
{
    public static void Apply(Control root)
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        float f = Theme.Scale;
        if (Math.Abs(f - 1f) < 0.01f) return;

        root.SuspendLayout();
        if (root is Form form)
        {
            if (!form.AutoSize) form.ClientSize = Sz(form.ClientSize, f);
            form.MinimumSize = Sz(form.MinimumSize, f);
            form.Padding = Pad(form.Padding, f);
        }
        else
        {
            ScaleOne(root, f, isRoot: true);
        }
        ScaleChildren(root, f);
        root.ResumeLayout(true);
    }

    /// <summary>
    /// Co giãn một control (và các control con) được thêm sau khi lớp cha đã co giãn.
    /// Dùng trong trang kế thừa (ví dụ EquipmentControl) khi Designer của lớp con thêm nút vào thanh công cụ của lớp cha.
    /// </summary>
    public static void ApplyControl(Control control)
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        float f = Theme.Scale;
        if (Math.Abs(f - 1f) < 0.01f) return;

        ScaleOne(control, f, isRoot: false);
        ScaleChildren(control, f);
    }

    static void ScaleChildren(Control parent, float f)
    {
        // Ô nhập bo tròn tự bố trí control con bên trong; lưới dữ liệu không có control thiết kế.
        if (parent is PillTextBox || parent is DataGridView) return;

        foreach (Control c in parent.Controls)
        {
            ScaleOne(c, f, isRoot: false);
            ScaleChildren(c, f);
        }
    }

    static void ScaleOne(Control c, float f, bool isRoot)
    {
        c.Padding = Pad(c.Padding, f);
        c.Margin = Pad(c.Margin, f);
        c.MinimumSize = Sz(c.MinimumSize, f);
        c.MaximumSize = Sz(c.MaximumSize, f);

        if (!isRoot)
        {
            switch (c.Dock)
            {
                case DockStyle.Top:
                case DockStyle.Bottom:
                    if (!c.AutoSize) c.Height = R(c.Height, f);
                    break;
                case DockStyle.Left:
                case DockStyle.Right:
                    if (!c.AutoSize) c.Width = R(c.Width, f);
                    break;
                case DockStyle.None:
                    c.Location = new Point(R(c.Left, f), R(c.Top, f));
                    if (!c.AutoSize) c.Size = Sz(c.Size, f);
                    break;
            }
        }

        if (c is TableLayoutPanel t)
        {
            foreach (ColumnStyle s in t.ColumnStyles)
                if (s.SizeType == SizeType.Absolute) s.Width *= f;
            foreach (RowStyle s in t.RowStyles)
                if (s.SizeType == SizeType.Absolute) s.Height *= f;
        }
    }

    static int R(int v, float f) => (int)Math.Round(v * f);
    static Size Sz(Size s, float f) => new(R(s.Width, f), R(s.Height, f));
    static Padding Pad(Padding p, float f) => new(R(p.Left, f), R(p.Top, f), R(p.Right, f), R(p.Bottom, f));
}
