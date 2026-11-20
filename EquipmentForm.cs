using System.Data;

namespace SchoolFacilities;

/// <summary>Thêm/sửa thiết bị. Giao diện: EquipmentForm.Designer.cs.</summary>
public partial class EquipmentForm : RecordForm
{
    public EquipmentForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        txtAssetCode.Text = Str(e, "AssetCode");
        txtName.Text = Str(e, "Name");
        FillCombo(cboType, Sql.TypeCombo, false, Cur(e, "TypeId"));
        FillCombo(cboRoom, Sql.RoomCombo, true, Cur(e, "RoomId"));
        SetDate(dtpPurchase, Cur(e, "PurchaseDate"));
        txtPrice.Text = Cur(e, "Price") is decimal price ? price.ToString("0") : "";
        FillCombo(cboStatus, Lists.EquipmentStatus, Cur(e, "Status"));
        txtNote.Text = Str(e, "Note");
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtAssetCode, "Mã tài sản", true, out var code)) return false;
        if (!TryText(txtName, "Tên thiết bị", true, out var name)) return false;
        if (!TryCombo(cboType, "Loại thiết bị", true, out var type)) return false;
        if (!TryCombo(cboRoom, "Phòng đang đặt", false, out var room)) return false;
        if (!TryDecimal(txtPrice, "Giá trị (đ)", false, out var price)) return false;
        if (!TryCombo(cboStatus, "Tình trạng", true, out var status)) return false;

        v["AssetCode"] = code;
        v["Name"] = name;
        v["TypeId"] = type;
        v["RoomId"] = room;
        v["PurchaseDate"] = GetDate(dtpPurchase);
        v["Price"] = price;
        v["Status"] = status;
        v["Note"] = ReadText(txtNote);
        return true;
    }
}
