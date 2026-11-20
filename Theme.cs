using System.Drawing.Drawing2D;

namespace SchoolFacilities;

public enum IconKind { Grid, Home, Search, Bell, Building, Room, Category, Device, Wrench, Chart, Users, Key }

/// <summary>Bảng màu, font và hệ số co giãn theo DPI của toàn ứng dụng.</summary>
public static class Theme
{
    public static readonly Color Bg = Color.FromArgb(233, 238, 243);
    public static readonly Color Ink = Color.FromArgb(55, 65, 81);
    public static readonly Color Heading = Color.FromArgb(71, 85, 105);
    public static readonly Color Muted = Color.FromArgb(120, 130, 145);
    public static readonly Color Line = Color.FromArgb(229, 233, 238);
    public static readonly Color Primary = Color.FromArgb(24, 160, 245);
    public static readonly Color PrimaryDark = Color.FromArgb(10, 120, 205);
    public static readonly Color Danger = Color.FromArgb(220, 53, 69);
    public static readonly Color Warn = Color.FromArgb(245, 158, 11);
    public static readonly Color Ok = Color.FromArgb(16, 185, 129);
    public static readonly Color Gray = Color.FromArgb(156, 163, 175);
    public static readonly Color TileBlueBg = Color.FromArgb(200, 238, 252);
    public static readonly Color TileBlueFg = Color.FromArgb(14, 150, 215);
    public static readonly Color TileOrangeBg = Color.FromArgb(254, 237, 200);
    public static readonly Color TileOrangeFg = Color.FromArgb(240, 150, 20);

    static float? _scale;

    /// <summary>Hệ số DPI (1.0 = 96 dpi). Dùng S() cho mọi kích thước tính bằng pixel.</summary>
    public static float Scale
    {
        get
        {
            if (_scale == null)
            {
                using var g = Graphics.FromHwnd(IntPtr.Zero);
                _scale = g.DpiX / 96f;
            }
            return _scale.Value;
        }
    }

    public static int S(int px) => (int)Math.Round(px * Scale);
    public static float S(float px) => px * Scale;

    public static Font F(float pt, FontStyle style = FontStyle.Regular) => new("Segoe UI", pt, style);
    public static Font FL(float pt) => new("Segoe UI Light", pt);
}

public static class Gfx
{
    public static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        float d = Math.Max(1f, radius * 2);
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>Vẽ thẻ trắng bo góc có bóng mềm (giống các card của cổng thông tin).</summary>
    public static void DrawCard(Graphics g, Size size, Color fill, int radius, bool lift = false)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(Theme.S(5), Theme.S(3), size.Width - Theme.S(10), size.Height - Theme.S(9));
        int alpha = lift ? 15 : 8;
        for (int i = 5; i >= 1; i--)
        {
            var sr = RectangleF.Inflate(rect, i, i);
            sr.Y += 2;
            using var sp = RoundRect(sr, radius + i);
            using var sb = new SolidBrush(Color.FromArgb(alpha, 15, 23, 42));
            g.FillPath(sb, sp);
        }
        using var path = RoundRect(rect, radius);
        using var br = new SolidBrush(fill);
        g.FillPath(br, path);
    }

    /// <summary>Vẽ biểu tượng nét mảnh bằng GDI+ (không cần file ảnh hay font biểu tượng).</summary>
    public static void DrawIcon(Graphics g, IconKind kind, RectangleF b, Color color, float stroke = 0)
    {
        var oldMode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float x = b.X, y = b.Y;
        float s = Math.Min(b.Width, b.Height);
        if (stroke <= 0) stroke = Math.Max(1.4f, s * 0.075f);

        using var pen = new Pen(color, stroke) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var br = new SolidBrush(color);

        PointF P(float u, float v) => new(x + u * s, y + v * s);
        RectangleF R(float u, float v, float w, float h) => new(x + u * s, y + v * s, w * s, h * s);
        void Box(RectangleF r) => g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
        void Dot(float u, float v, float sz) => g.FillRectangle(br, R(u, v, sz, sz));

        switch (kind)
        {
            case IconKind.Grid:
                foreach (var cu in new[] { 0.2f, 0.5f, 0.8f })
                    foreach (var cv in new[] { 0.2f, 0.5f, 0.8f })
                        g.FillEllipse(br, R(cu - 0.075f, cv - 0.075f, 0.15f, 0.15f));
                break;

            case IconKind.Home:
                g.DrawLines(pen, new[] { P(.08f, .5f), P(.5f, .12f), P(.92f, .5f) });
                g.DrawLines(pen, new[] { P(.2f, .44f), P(.2f, .88f), P(.8f, .88f), P(.8f, .44f) });
                g.DrawLines(pen, new[] { P(.42f, .88f), P(.42f, .62f), P(.58f, .62f), P(.58f, .88f) });
                break;

            case IconKind.Search:
                g.DrawEllipse(pen, R(.12f, .12f, .56f, .56f));
                g.DrawLine(pen, P(.6f, .6f), P(.88f, .88f));
                break;

            case IconKind.Bell:
                g.DrawArc(pen, R(.22f, .12f, .56f, .56f), 180, 180);
                g.DrawLines(pen, new[] { P(.22f, .4f), P(.22f, .68f), P(.12f, .8f), P(.88f, .8f), P(.78f, .68f), P(.78f, .4f) });
                g.DrawArc(pen, R(.4f, .78f, .2f, .16f), 0, 180);
                break;

            case IconKind.Building:
                Box(R(.22f, .1f, .56f, .8f));
                Dot(.34f, .22f, .09f); Dot(.57f, .22f, .09f);
                Dot(.34f, .42f, .09f); Dot(.57f, .42f, .09f);
                Box(R(.43f, .68f, .14f, .22f));
                break;

            case IconKind.Room:
                Box(R(.2f, .1f, .6f, .8f));
                Box(R(.32f, .22f, .36f, .56f));
                g.FillEllipse(br, R(.56f, .5f, .08f, .08f));
                break;

            case IconKind.Category:
                Box(R(.12f, .12f, .3f, .3f)); Box(R(.58f, .12f, .3f, .3f));
                Box(R(.12f, .58f, .3f, .3f)); Box(R(.58f, .58f, .3f, .3f));
                break;

            case IconKind.Device:
                Box(R(.1f, .14f, .8f, .55f));
                g.DrawLine(pen, P(.5f, .69f), P(.5f, .84f));
                g.DrawLine(pen, P(.3f, .86f), P(.7f, .86f));
                break;

            case IconKind.Wrench:
                g.DrawEllipse(pen, R(.5f, .1f, .36f, .36f));
                using (var thick = new Pen(color, stroke * 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(thick, P(.55f, .42f), P(.2f, .82f));
                break;

            case IconKind.Chart:
                g.DrawLine(pen, P(.1f, .9f), P(.9f, .9f));
                Box(R(.2f, .55f, .14f, .35f)); Box(R(.43f, .38f, .14f, .52f)); Box(R(.66f, .2f, .14f, .7f));
                break;

            case IconKind.Users:
                g.DrawEllipse(pen, R(.34f, .12f, .32f, .32f));
                g.DrawArc(pen, R(.18f, .5f, .64f, .7f), 180, 180);
                break;

            case IconKind.Key:
                g.DrawEllipse(pen, R(.1f, .3f, .32f, .32f));
                g.DrawLine(pen, P(.42f, .46f), P(.9f, .46f));
                g.DrawLine(pen, P(.74f, .46f), P(.74f, .64f));
                g.DrawLine(pen, P(.88f, .46f), P(.88f, .6f));
                break;
        }

        g.SmoothingMode = oldMode;
    }
}

public static class UiHelper
{
    public static Color StatusColor(string? s) => s switch
    {
        "Hỏng" or "Chờ xử lý" => Theme.Danger,
        "Đang bảo trì" or "Đang xử lý" or "Đang sửa chữa" => Color.FromArgb(217, 119, 6),
        "Đã thanh lý" or "Đã khóa" or "Ngừng sử dụng" => Theme.Gray,
        "Tốt" or "Hoàn thành" or "Đang hoạt động" or "Đang sử dụng" => Color.FromArgb(5, 150, 105),
        _ => Theme.Ink
    };

    public static Button MakeButton(string text, Color back)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(Theme.S(88), Theme.S(36)),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Font = Theme.F(10f),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, Theme.S(8), 0),
            Padding = new Padding(Theme.S(10), 0, Theme.S(10), 0),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.15f);
        return b;
    }

    public static void StyleGrid(DataGridView g)
    {
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.MultiSelect = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.RowHeadersVisible = false;
        g.BackgroundColor = Color.White;
        g.BorderStyle = BorderStyle.None;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.GridColor = Theme.Line;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Ink;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.Ink;
        g.ColumnHeadersDefaultCellStyle.Font = Theme.F(10f, FontStyle.Bold);
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(Theme.S(6), 0, 0, 0);
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersHeight = Theme.S(40);
        g.RowTemplate.Height = Theme.S(36);
        g.DefaultCellStyle.Font = Theme.F(10f);
        g.DefaultCellStyle.ForeColor = Theme.Ink;
        g.DefaultCellStyle.Padding = new Padding(Theme.S(6), 0, 0, 0);
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 238, 253);
        g.DefaultCellStyle.SelectionForeColor = Color.Black;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 251);
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        g.CellFormatting += (_, e) =>
        {
            if (e.CellStyle == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var header = g.Columns[e.ColumnIndex].HeaderText;
            if (e.Value is string s && (header == "Tình trạng" || header == "Trạng thái"))
            {
                e.CellStyle.ForeColor = StatusColor(s);
                e.CellStyle.Font = Theme.F(10f, FontStyle.Bold);
            }
        };
    }

    public static void FormatColumns(DataGridView g)
    {
        foreach (DataGridViewColumn col in g.Columns)
        {
            if (col.ValueType == typeof(DateTime)) col.DefaultCellStyle.Format = "dd/MM/yyyy";
            else if (col.ValueType == typeof(decimal)) col.DefaultCellStyle.Format = "N0";
            col.MinimumWidth = Theme.S(70);
        }
    }
}
