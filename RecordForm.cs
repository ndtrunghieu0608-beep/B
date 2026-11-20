using System.Data;
using System.Globalization;

namespace SchoolFacilities;

/// <summary>
/// Lớp cơ sở cho các hộp thoại nhập liệu (BuildingForm, RoomForm, EquipmentForm...).
/// Lớp này không có control nào: mỗi form con được thiết kế bằng Form Designer, rồi chỉ cần viết
/// <see cref="LoadValues"/> (điền dữ liệu vào các ô) và <see cref="Collect"/> (đọc các ô vào từ điển giá trị,
/// khóa là tên cột trong cơ sở dữ liệu).
/// </summary>
public class RecordForm : Form
{
    /// <summary>Giá trị người dùng đã nhập (có sau khi bấm Lưu thành công). Khóa là tên cột.</summary>
    public Dictionary<string, object?> Values { get; } = new();

    /// <summary>Kiểm tra bổ sung trước khi đóng hộp thoại; trả về thông báo lỗi hoặc null nếu hợp lệ.</summary>
    public Func<Dictionary<string, object?>, string?>? Verify { get; set; }

    /// <summary>Điền dữ liệu có sẵn vào các ô nhập. <paramref name="existing"/> là null khi thêm mới.</summary>
    public virtual void LoadValues(DataRow? existing) { }

    /// <summary>Đọc các ô nhập vào <paramref name="values"/>. Trả về false nếu dữ liệu chưa hợp lệ (đã báo lỗi).</summary>
    protected virtual bool Collect(Dictionary<string, object?> values) => true;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Tiêu đề trong form (nhãn lblHeading) luôn bằng tiêu đề cửa sổ, để nơi gọi chỉ cần đặt Text.
        if (Controls.Find("lblHeading", true).FirstOrDefault() is Label heading) heading.Text = Text;
    }

    /// <summary>Gọi từ nút Lưu của form con.</summary>
    protected void Submit()
    {
        Values.Clear();
        if (!Collect(Values)) return;

        var error = Verify?.Invoke(Values);
        if (error != null)
        {
            MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
    }

    // ---------- Đọc dữ liệu có sẵn ----------

    protected static object? Cur(DataRow? row, string column) =>
        row != null && row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? row[column] : null;

    protected static string Str(DataRow? row, string column) => Cur(row, column)?.ToString() ?? "";

    // ---------- Đọc các ô nhập ----------

    protected bool Fail(string message, Control c)
    {
        MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        c.Focus();
        return false;
    }

    protected static string? ReadText(Control c)
    {
        var t = c.Text.Trim();
        return t.Length == 0 ? null : t;
    }

    protected bool TryText(TextBox box, string label, bool required, out string? value)
    {
        value = ReadText(box);
        if (required && value == null) return Fail($"Vui lòng nhập \"{label}\".", box);
        return true;
    }

    protected bool TryInt(TextBox box, string label, bool required, out int? value)
    {
        value = null;
        var t = box.Text.Trim();
        if (t.Length == 0)
            return !required || Fail($"Vui lòng nhập \"{label}\".", box);
        if (!int.TryParse(t, NumberStyles.Integer, CultureInfo.CurrentCulture, out var i))
            return Fail($"\"{label}\" phải là số nguyên.", box);
        value = i;
        return true;
    }

    protected bool TryDecimal(TextBox box, string label, bool required, out decimal? value)
    {
        value = null;
        var t = box.Text.Trim();
        if (t.Length == 0)
            return !required || Fail($"Vui lòng nhập \"{label}\".", box);
        if (!decimal.TryParse(t, NumberStyles.Number, CultureInfo.CurrentCulture, out var m) &&
            !decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out m))
            return Fail($"\"{label}\" phải là số.", box);
        value = m;
        return true;
    }

    protected bool TryCombo(ComboBox box, string label, bool required, out object? value)
    {
        value = ComboValue(box);
        if (required && value == null) return Fail($"Vui lòng chọn \"{label}\".", box);
        return true;
    }

    // ---------- Ô chọn (ComboBox) ----------

    /// <summary>Nạp ô chọn từ câu truy vấn trả về hai cột Id và Name.</summary>
    protected static void FillCombo(ComboBox box, string sql, bool allowEmpty, object? current = null)
    {
        var dt = Db.Query(sql);
        if (allowEmpty)
        {
            var empty = dt.NewRow();
            empty["Id"] = DBNull.Value;
            empty["Name"] = "";
            dt.Rows.InsertAt(empty, 0);
        }
        box.DataSource = dt;
        box.DisplayMember = "Name";
        box.ValueMember = "Id";
        if (current != null) box.SelectedValue = current;
        else if (box.Items.Count > 0) box.SelectedIndex = 0;
    }

    /// <summary>Nạp ô chọn từ danh sách cố định.</summary>
    protected static void FillCombo(ComboBox box, string[] items, object? current = null)
    {
        box.DataSource = null;
        box.Items.Clear();
        box.Items.AddRange(items);
        if (current != null) box.SelectedItem = current.ToString();
        if (box.SelectedIndex < 0 && box.Items.Count > 0) box.SelectedIndex = 0;
    }

    protected static object? ComboValue(ComboBox box)
    {
        if (box.DataSource != null)
            return box.SelectedValue is null || box.SelectedValue is DBNull ? null : box.SelectedValue;
        return box.SelectedItem;
    }

    // ---------- Ô ngày ----------

    protected static void SetDate(DateTimePicker picker, object? current)
    {
        if (current is DateTime d)
        {
            picker.Value = d;
            if (picker.ShowCheckBox) picker.Checked = true;
        }
        else if (picker.ShowCheckBox)
        {
            picker.Checked = false;
        }
        else
        {
            picker.Value = DateTime.Today;
        }
    }

    /// <summary>Ngày đã chọn; null nếu ô có hộp chọn và đang bỏ chọn.</summary>
    protected static object? GetDate(DateTimePicker picker) =>
        picker.ShowCheckBox && !picker.Checked ? null : picker.Value.Date;
}
