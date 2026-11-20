using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text;
using static SchoolFacilities.Theme;

namespace SchoolFacilities;

/// <summary>Tiện ích vẽ chữ bằng TextRenderer (nét chữ sắc, hỗ trợ tiếng Việt tốt).</summary>
static class Tx
{
    const TextFormatFlags Base = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
    public const TextFormatFlags Left = Base | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
    public const TextFormatFlags Center = Base | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;
    public const TextFormatFlags CenterWrap = Base | TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter;

    public static int Width(string s, Font f) =>
        TextRenderer.MeasureText(s, f, new Size(int.MaxValue, int.MaxValue), Base).Width;

    public static void Draw(Graphics g, string s, Font f, Rectangle r, Color c, bool center = false) =>
        TextRenderer.DrawText(g, s, f, r, c, center ? Center : Left);

    public static void DrawWrapped(Graphics g, string s, Font f, Rectangle r, Color c) =>
        TextRenderer.DrawText(g, s, f, r, c, CenterWrap);
}

/// <summary>Lớp cơ sở cho control tự vẽ (chống nháy hình).</summary>
[DesignerCategory("Code")]
public abstract class PaintControl : Control
{
    protected PaintControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
    }
}

[DesignerCategory("Code")]
public class BufferedPanel : Panel
{
    public BufferedPanel() { DoubleBuffered = true; }
}

/// <summary>Thẻ bo góc có bóng mềm. BackColor là màu của thẻ (mặc định trắng), OuterColor là màu nền phía sau thẻ.</summary>
[DesignerCategory("Code")]
public class CardPanel : Panel
{
    Color _outer = Bg;

    [Category("Appearance")]
    public Color OuterColor
    {
        get => _outer;
        set { _outer = value; Invalidate(); }
    }

    [Category("Appearance"), DefaultValue(8)]
    public int Radius { get; set; } = 8;

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
        Margin = new Padding(8);
        Padding = new Padding(28, 20, 28, 18);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(_outer);
        if (Width < 20 || Height < 20) return;
        Gfx.DrawCard(e.Graphics, Size, BackColor, Radius);
    }
}

/// <summary>Tiêu đề thẻ: chữ đậm bên trái và đường kẻ mảnh bên dưới. Có thể đặt control khác lên trên (ví dụ liên kết, ô chọn).</summary>
[DesignerCategory("Code")]
public class SectionHeader : Panel
{
    static readonly Font FTitle = F(15.5f, FontStyle.Bold);
    string _title = "";
    int _lineY = 42;

    public SectionHeader()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        Height = 54;
    }

    [Category("Appearance"), DefaultValue("")]
    public string Title
    {
        get => _title;
        set { _title = value ?? ""; Invalidate(); }
    }

    /// <summary>Vị trí đường kẻ tính từ mép trên (đơn vị 96 DPI, tự co giãn theo màn hình).</summary>
    [Category("Appearance"), DefaultValue(42)]
    public int LineY
    {
        get => _lineY;
        set { _lineY = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int y = S(_lineY);
        Tx.Draw(g, _title, FTitle, new Rectangle(0, 0, Width, y), Heading);
        using var pen = new Pen(Line);
        g.DrawLine(pen, 0, y, Width, y);
    }
}

public class IconCircle : PaintControl
{
    public IconKind Kind { get; set; }
    public Color Stroke { get; set; } = Primary;
    public Color Fill { get; set; } = Color.Empty;
    public Color IconColor { get; set; } = Primary;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(1, 1, Width - 3, Height - 3);
        if (!Fill.IsEmpty)
        {
            using var br = new LinearGradientBrush(r, Color.FromArgb(48, 178, 250), PrimaryDark, 55f);
            g.FillEllipse(br, r);
        }
        else
        {
            using var pen = new Pen(Stroke, 1.5f);
            g.DrawEllipse(pen, r);
        }
        int pad = (int)(Width * 0.27);
        Gfx.DrawIcon(g, Kind, new RectangleF(pad, pad, Width - pad * 2, Height - pad * 2), IconColor);
    }
}

/// <summary>Ô truy cập nhanh (icon + tên chức năng) ở hàng giữa trang chủ.</summary>
public class ShortcutTile : PaintControl
{
    static readonly Font FCap = F(10.5f);
    bool _hover;

    public IconKind Kind { get; set; }
    public string Caption { get; set; } = "";

    public ShortcutTile()
    {
        BackColor = Bg;
        Cursor = Cursors.Hand;
        Margin = new Padding(8);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        if (Width < 30 || Height < 30) return;
        Gfx.DrawCard(g, Size, Color.White, S(8), _hover);

        int icon = S(40);
        Gfx.DrawIcon(g, Kind, new RectangleF((Width - icon) / 2f, Height * 0.2f, icon, icon), Primary);
        Tx.DrawWrapped(g, Caption, FCap, new Rectangle(S(12), (int)(Height * 0.2f) + icon + S(14), Width - S(24), S(40)),
                       _hover ? PrimaryDark : Muted);
    }
}

/// <summary>Ô thống kê màu (xanh nhạt / cam nhạt) có số lớn và liên kết "Xem chi tiết".</summary>
public class StatTile : PaintControl
{
    static readonly Font FCap = F(10.5f);
    static readonly Font FNum = FL(32f);
    static readonly Font FLink = F(10.5f);

    public string Caption { get; set; } = "";
    public string Value { get; set; } = "0";
    public string LinkText { get; set; } = "Xem chi tiết";
    public IconKind Kind { get; set; }
    public Color Back { get; set; } = TileBlueBg;
    public Color Fore { get; set; } = TileBlueFg;

    public StatTile()
    {
        BackColor = Bg;
        Cursor = Cursors.Hand;
        Margin = new Padding(8);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        if (Width < 60 || Height < 60) return;
        Gfx.DrawCard(g, Size, Back, S(8));

        int x = S(26);
        Tx.Draw(g, Caption, FCap, new Rectangle(x, S(18), Width - S(110), S(24)), Fore);
        Tx.Draw(g, Value, FNum, new Rectangle(x, S(44), Width - S(110), S(60)), Fore);
        Tx.Draw(g, LinkText, FLink, new Rectangle(x, Height - S(46), Width - S(110), S(24)), Fore);

        int d = S(44);
        var circle = new Rectangle(Width - S(28) - d, (Height - d) / 2, d, d);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var pen = new Pen(Fore, 1.5f)) g.DrawEllipse(pen, circle);
        int pad = (int)(d * 0.27);
        Gfx.DrawIcon(g, Kind, new RectangleF(circle.X + pad, circle.Y + pad, d - pad * 2, d - pad * 2), Fore);
    }
}

/// <summary>Danh sách "Khóa: Giá trị" nhiều dòng (khối thông tin ở thẻ đầu trang).</summary>
public class KeyValueList : PaintControl
{
    static readonly Font FKey = F(10.5f);
    static readonly Font FVal = F(10.5f, FontStyle.Bold);

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<(string Key, string Value)> Items { get; set; } = new();

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int rowH = S(34), y = S(2);
        foreach (var (key, value) in Items)
        {
            int kw = Tx.Width(key + ":", FKey);
            Tx.Draw(g, key + ":", FKey, new Rectangle(0, y, kw + S(2), rowH), Muted);
            Tx.Draw(g, value, FVal, new Rectangle(kw + S(8), y, Math.Max(10, Width - kw - S(10)), rowH), Ink);
            y += rowH;
        }
    }
}

public class DonutChart : PaintControl
{
    static readonly Font FNum = F(20f, FontStyle.Bold);
    static readonly Font FSmall = F(9f);
    static readonly Font FLeg = F(10f);

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<(string Label, double Value, Color Color)> Data { get; set; } = new();
    public string CenterCaption { get; set; } = "thiết bị";

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        double total = Data.Sum(x => x.Value);
        int dia = Math.Min(Height - S(16), (int)(Width * 0.52));
        if (dia < S(60)) return;

        int th = (int)(dia * 0.17);
        int left = S(4);
        var ring = new Rectangle(left + th / 2, (Height - dia) / 2 + th / 2, dia - th, dia - th);

        using (var bgPen = new Pen(Color.FromArgb(238, 241, 244), th))
            g.DrawEllipse(bgPen, ring);

        var visible = Data.Where(x => x.Value > 0).ToList();
        if (total > 0)
        {
            float start = -90f;
            foreach (var item in visible)
            {
                float sweep = (float)(item.Value / total * 360.0);
                using var pen = new Pen(item.Color, th) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
                g.DrawArc(pen, ring, start, visible.Count == 1 ? 359.9f : Math.Max(0.5f, sweep - 1.5f));
                start += sweep;
            }
        }

        var mid = new Rectangle(ring.X, ring.Y + ring.Height / 2 - S(24), ring.Width, S(30));
        Tx.Draw(g, total.ToString("N0"), FNum, mid, Ink, true);
        Tx.Draw(g, CenterCaption, FSmall, new Rectangle(ring.X, mid.Bottom - S(2), ring.Width, S(18)), Muted, true);

        int lx = left + dia + S(18);
        int ly = (Height - Data.Count * S(30)) / 2;
        foreach (var item in Data)
        {
            using var br = new SolidBrush(item.Color);
            g.FillEllipse(br, lx, ly + S(9), S(12), S(12));
            Tx.Draw(g, $"{item.Label}: {item.Value:N0}", FLeg,
                    new Rectangle(lx + S(20), ly, Math.Max(10, Width - lx - S(24)), S(30)), Ink);
            ly += S(30);
        }
    }
}

public class BarChart : PaintControl
{
    static readonly Font FLab = F(10f);
    static readonly Font FVal = F(10f, FontStyle.Bold);

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<(string Label, double Value)> Data { get; set; } = new();
    public Color BarColor { get; set; } = Primary;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (Data.Count == 0)
        {
            Tx.Draw(g, "Chưa có dữ liệu", FLab, new Rectangle(0, 0, Width, Height), Muted, true);
            return;
        }

        int rows = Data.Count;
        int rowH = Math.Min(S(38), Height / rows);
        int labelW = (int)(Width * 0.38);
        int valW = S(44);
        int trackW = Math.Max(10, Width - labelW - valW);
        double max = Math.Max(1, Data.Max(x => x.Value));
        int barH = S(12);
        int y = S(4);

        foreach (var (label, value) in Data)
        {
            Tx.Draw(g, label, FLab, new Rectangle(0, y, labelW - S(10), rowH), Ink);

            var track = new RectangleF(labelW, y + (rowH - barH) / 2f, trackW, barH);
            using (var tp = Gfx.RoundRect(track, barH / 2f))
            using (var tb = new SolidBrush(Color.FromArgb(240, 243, 246)))
                g.FillPath(tb, tp);

            float w = (float)(trackW * value / max);
            if (w >= 2)
            {
                var bar = new RectangleF(labelW, track.Y, Math.Max(w, barH), barH);
                using var bp = Gfx.RoundRect(bar, barH / 2f);
                using var bb = new SolidBrush(BarColor);
                g.FillPath(bb, bp);
            }

            Tx.Draw(g, value.ToString("N0"), FVal, new Rectangle(Width - valW + S(8), y, valW - S(8), rowH), Ink);
            y += rowH;
        }
    }
}

/// <summary>Liên kết trên thanh trên cùng: biểu tượng + chữ (+ huy hiệu số).</summary>
public class NavLink : PaintControl
{
    static readonly Font FText = F(10.5f);
    static readonly Font FBadge = F(7.5f, FontStyle.Bold);
    bool _hover;
    string _caption = "";
    int _badge;

    public IconKind Kind { get; set; }

    public string Caption
    {
        get => _caption;
        set { _caption = value; Invalidate(); }
    }

    public int Badge
    {
        get => _badge;
        set { _badge = value; Invalidate(); }
    }

    public NavLink()
    {
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        var color = _hover ? Primary : Ink;
        int icon = S(20);
        var iconRect = new RectangleF(S(14), (Height - icon) / 2f, icon, icon);
        Gfx.DrawIcon(g, Kind, iconRect, color);
        Tx.Draw(g, _caption, FText, new Rectangle(S(14) + icon + S(8), 0, Width - S(44), Height), color);

        if (_badge > 0)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int d = S(17);
            var r = new Rectangle((int)iconRect.Right - d / 2 - S(2), (int)iconRect.Y - S(6), d, d);
            using (var br = new SolidBrush(Danger)) g.FillEllipse(br, r);
            Tx.Draw(g, _badge > 99 ? "99+" : _badge.ToString(), FBadge, r, Color.White, true);
        }
    }
}

/// <summary>Ảnh đại diện tròn + tên người dùng + mũi tên (mở menu tài khoản).</summary>
public class UserChip : PaintControl
{
    static readonly Font FName = F(11f);
    static readonly Font FInit = F(10f, FontStyle.Bold);
    string _name = "";
    bool _hover;

    public string FullName
    {
        get => _name;
        set { _name = (value ?? "").ToUpperInvariant(); Fit(); }
    }

    public UserChip()
    {
        Cursor = Cursors.Hand;
    }

    void Fit() => Width = S(6) + S(38) + S(10) + Tx.Width(_name, FName) + S(10) + S(16) + S(8);

    string Initials()
    {
        var words = _name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return "?";
        var sb = new StringBuilder();
        if (words.Length > 1) sb.Append(words[^2][0]);
        sb.Append(words[^1][0]);
        return sb.ToString();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int d = S(38);
        var av = new Rectangle(S(6), (Height - d) / 2, d, d);
        using (var br = new LinearGradientBrush(av, Color.FromArgb(48, 178, 250), PrimaryDark, 55f))
            g.FillEllipse(br, av);
        Tx.Draw(g, Initials(), FInit, av, Color.White, true);

        int tx = av.Right + S(10);
        Tx.Draw(g, _name, FName, new Rectangle(tx, 0, Width - tx - S(24), Height), _hover ? Primary : Ink);

        int cx = Width - S(14), cy = Height / 2;
        using var pen = new Pen(_hover ? Primary : Ink, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pen, new[] { new Point(cx - S(4), cy - S(2)), new Point(cx, cy + S(2)), new Point(cx + S(4), cy - S(2)) });
    }
}

/// <summary>Nút tròn chứa biểu tượng (nút lưới chức năng bên trái).</summary>
public class IconButton : PaintControl
{
    bool _hover;
    public IconKind Kind { get; set; }

    public IconButton() { Cursor = Cursors.Hand; }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (_hover)
        {
            using var br = new SolidBrush(Color.FromArgb(238, 243, 248));
            g.FillEllipse(br, 0, 0, Width - 1, Height - 1);
        }
        int pad = (int)(Width * 0.27);
        Gfx.DrawIcon(g, Kind, new RectangleF(pad, pad, Width - pad * 2, Height - pad * 2), Ink);
    }
}

/// <summary>Logo (khối vuông xanh + biểu tượng) kèm tên trường và tên hệ thống.</summary>
public class LogoControl : PaintControl
{
    static readonly Font FSchool = F(11.5f, FontStyle.Bold);
    static readonly Font FApp = F(8.5f, FontStyle.Bold);
    string _school = "TRƯỜNG HỌC";

    public string SchoolName
    {
        get => _school;
        set { _school = string.IsNullOrWhiteSpace(value) ? "TRƯỜNG HỌC" : value.ToUpperInvariant(); Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int m = S(42);
        var mark = new Rectangle(0, (Height - m) / 2, m, m);
        using (var path = Gfx.RoundRect(mark, S(11)))
        using (var br = new LinearGradientBrush(mark, Color.FromArgb(48, 178, 250), PrimaryDark, 45f))
            g.FillPath(br, path);
        int pad = S(9);
        Gfx.DrawIcon(g, IconKind.Building, new RectangleF(mark.X + pad, mark.Y + pad, m - pad * 2, m - pad * 2), Color.White);

        int tx = m + S(12);
        Tx.Draw(g, _school, FSchool, new Rectangle(tx, 0, Width - tx, Height / 2 + S(2)), Color.FromArgb(30, 58, 120));
        Tx.Draw(g, "QUẢN LÝ CƠ SỞ VẬT CHẤT", FApp, new Rectangle(tx, Height / 2 + S(2), Width - tx, Height / 2 - S(2)), Primary);
    }
}

/// <summary>Ô nhập liệu bo tròn (dùng cho tìm kiếm và đăng nhập).</summary>
public class PillTextBox : Control
{
    public readonly TextBox Input = new() { BorderStyle = BorderStyle.None, BackColor = Color.White, Font = F(10.5f) };
    IconKind? _left, _right;
    bool _focus;

    public int Radius { get; set; } = 20;
    public event EventHandler? EnterPressed;

    public IconKind? LeftIcon { get => _left; set { _left = value; DoLayout(); Invalidate(); } }
    public IconKind? RightIcon { get => _right; set { _right = value; DoLayout(); Invalidate(); } }

    public override string Text { get => Input.Text; set => Input.Text = value; }
    public string PlaceholderText { get => Input.PlaceholderText; set => Input.PlaceholderText = value; }
    public bool Password { get => Input.UseSystemPasswordChar; set => Input.UseSystemPasswordChar = value; }

    public PillTextBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = 40;
        Controls.Add(Input);
        Input.GotFocus += (_, _) => { _focus = true; Invalidate(); };
        Input.LostFocus += (_, _) => { _focus = false; Invalidate(); };
        Input.KeyDown += (_, ke) =>
        {
            if (ke.KeyCode == Keys.Enter)
            {
                ke.SuppressKeyPress = true;
                ke.Handled = true;
                EnterPressed?.Invoke(this, EventArgs.Empty);
            }
        };
        Click += (_, _) => Input.Focus();
        DoLayout();
    }

    void DoLayout()
    {
        int l = _left != null ? S(46) : S(18);
        int r = _right != null ? S(46) : S(18);
        Input.SetBounds(l, Math.Max(0, (Height - Input.Height) / 2), Math.Max(10, Width - l - r), Input.Height);
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); DoLayout(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using (var path = Gfx.RoundRect(r, Math.Min(S(Radius), Height / 2f)))
        {
            using var fill = new SolidBrush(Color.White);
            g.FillPath(fill, path);
            using var pen = new Pen(_focus ? Primary : Color.FromArgb(220, 226, 232), 1.3f);
            g.DrawPath(pen, path);
        }

        int ic = S(20);
        var color = _focus ? Primary : Muted;
        if (_left != null)
            Gfx.DrawIcon(g, _left.Value, new RectangleF(S(15), (Height - ic) / 2f, ic, ic), color);
        if (_right != null)
            Gfx.DrawIcon(g, _right.Value, new RectangleF(Width - S(15) - ic, (Height - ic) / 2f, ic, ic), color);
    }
}
