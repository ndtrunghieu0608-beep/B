using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SchoolFacilities;

/// <summary>
/// Trang quản lý dùng chung: lưới dữ liệu + tìm kiếm + Thêm / Sửa / Xóa.
/// Giao diện nằm ở CrudControl.Designer.cs; các trang cụ thể (Pages.cs) chỉ truyền tên bảng, câu truy vấn và form nhập liệu (RecordForm).
/// Cột đầu tiên của ListSql phải đặt bí danh là Id (khóa chính, được ẩn đi).
/// </summary>
public partial class CrudControl : UserControl
{
    string _entityName = "";
    string _table = "";
    string _keyColumn = "";
    string _listSql = "";
    Func<RecordForm>? _createForm;
    DataTable? _data;

    /// <summary>Hàm khởi tạo không tham số dành cho Designer.</summary>
    public CrudControl()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
        UiHelper.StyleGrid(dgv);
        txtSearch.Input.TextChanged += (_, _) => ApplyFilter();
    }

    public CrudControl(string title, string entityName, string table, string keyColumn,
                       string listSql, Func<RecordForm> createForm, bool allowDelete) : this()
    {
        _entityName = entityName;
        _table = table;
        _keyColumn = keyColumn;
        _listSql = listSql;
        _createForm = createForm;
        hdr.Title = title;
        btnDelete.Visible = allowDelete;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (!DesignMode) LoadData();
    }

    // ---------- Móc mở rộng cho lớp con ----------
    protected virtual string? ValidateValues(Dictionary<string, object?> v, bool isNew) => null;
    protected virtual Dictionary<string, object?> Transform(Dictionary<string, object?> v, bool isNew) => v;
    protected virtual void AfterSave(int? id, DataRow? oldRow, Dictionary<string, object?> v, bool isNew) { }
    protected virtual string? CanDelete(int id) => null;

    // ---------- Giao diện ----------

    /// <summary>Thêm một nút chức năng riêng của trang (ví dụ "Điều chuyển phòng") vào thanh công cụ.</summary>
    public Button AddButton(string text, EventHandler onClick, Color? back = null)
    {
        var b = UiHelper.MakeButton(text, back ?? Color.FromArgb(100, 116, 139));
        b.Click += onClick;
        flowToolbar.Controls.Add(b);
        return b;
    }

    public void SetSearch(string text) => txtSearch.Text = text;

    void btnAdd_Click(object? sender, EventArgs e) => AddItem();

    void btnEdit_Click(object? sender, EventArgs e) => EditItem();

    void btnDelete_Click(object? sender, EventArgs e) => DeleteItem();

    void btnRefresh_Click(object? sender, EventArgs e) => LoadData();

    void dgv_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0) EditItem();
    }

    protected int? SelectedId
    {
        get
        {
            var row = dgv.CurrentRow;
            if (row == null || !dgv.Columns.Contains("Id")) return null;
            return row.Cells["Id"].Value is int i ? i : null;
        }
    }

    protected int? RequireSelection()
    {
        var id = SelectedId;
        if (id == null)
            MessageBox.Show(FindForm(), "Vui lòng chọn một dòng trong danh sách.", "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return id;
    }

    // ---------- Dữ liệu ----------
    public void LoadData()
    {
        if (string.IsNullOrEmpty(_listSql)) return;

        try
        {
            _data = Db.Query(_listSql);
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return;
        }

        dgv.DataSource = _data;
        if (dgv.Columns.Contains("Id")) dgv.Columns["Id"].Visible = false;
        UiHelper.FormatColumns(dgv);
        ApplyFilter();
    }

    void ApplyFilter()
    {
        if (_data == null) return;
        var text = txtSearch.Text.Trim();
        if (text.Length == 0)
        {
            _data.DefaultView.RowFilter = "";
            return;
        }

        var esc = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch is '[' or ']' or '%' or '*') esc.Append('[').Append(ch).Append(']');
            else if (ch == '\'') esc.Append("''");
            else esc.Append(ch);
        }

        var parts = _data.Columns.Cast<DataColumn>()
            .Where(c => c.ColumnName != "Id")
            .Select(c => $"Convert([{c.ColumnName}], 'System.String') LIKE '%{esc}%'");
        try { _data.DefaultView.RowFilter = string.Join(" OR ", parts); }
        catch (DataException) { _data.DefaultView.RowFilter = ""; }
    }

    // ---------- Thêm / Sửa / Xóa ----------
    void AddItem()
    {
        if (_createForm == null) return;

        try
        {
            using var dlg = _createForm();
            dlg.Text = $"Thêm {_entityName}";
            dlg.Verify = v => ValidateValues(v, true);
            dlg.LoadValues(null);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            var values = Transform(dlg.Values, true);
            var cols = values.Keys.ToList();
            var sql = $"INSERT INTO {_table} ({string.Join(", ", cols)}) " +
                      $"VALUES ({string.Join(", ", cols.Select(c => "@" + c))})";
            Db.Exec(sql, values.Select(kv => ("@" + kv.Key, kv.Value)).ToArray());
            AfterSave(null, null, dlg.Values, true);
            LoadData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    void EditItem()
    {
        var id = RequireSelection();
        if (id == null || _createForm == null) return;

        try
        {
            var dt = Db.Query($"SELECT * FROM {_table} WHERE {_keyColumn}=@id", ("@id", id.Value));
            if (dt.Rows.Count == 0) { LoadData(); return; }
            var old = dt.Rows[0];

            using var dlg = _createForm();
            dlg.Text = $"Sửa {_entityName}";
            dlg.Verify = v => ValidateValues(v, false);
            dlg.LoadValues(old);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            var values = Transform(dlg.Values, false);
            if (values.Count > 0)
            {
                var set = string.Join(", ", values.Keys.Select(c => $"{c}=@{c}"));
                var args = values.Select(kv => ("@" + kv.Key, kv.Value)).Append(("@__id", (object?)id.Value)).ToArray();
                Db.Exec($"UPDATE {_table} SET {set} WHERE {_keyColumn}=@__id", args);
            }
            AfterSave(id, old, dlg.Values, false);
            LoadData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    void DeleteItem()
    {
        var id = RequireSelection();
        if (id == null) return;

        var blocked = CanDelete(id.Value);
        if (blocked != null)
        {
            MessageBox.Show(FindForm(), blocked, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ask = MessageBox.Show(FindForm(), $"Bạn có chắc muốn xóa {_entityName} đang chọn?", "Xác nhận xóa",
                                  MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (ask != DialogResult.Yes) return;

        try
        {
            Db.Exec($"DELETE FROM {_table} WHERE {_keyColumn}=@id", ("@id", id.Value));
            LoadData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    protected void ShowError(Exception ex)
    {
        string msg = ex is SqlException s
            ? s.Number switch
            {
                2627 or 2601 => "Dữ liệu bị trùng (mã hoặc tên đã tồn tại).",
                547 => "Không thể thực hiện vì dữ liệu đang được sử dụng ở nơi khác hoặc giá trị không hợp lệ.",
                _ => s.Message
            }
            : ex.Message;
        MessageBox.Show(FindForm(), msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
