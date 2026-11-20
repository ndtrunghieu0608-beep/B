using System.Data;

namespace SchoolFacilities;

/// <summary>Thêm/sửa phòng. Giao diện: RoomForm.Designer.cs.</summary>
public partial class RoomForm : RecordForm
{
    public RoomForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        txtCode.Text = Str(e, "Code");
        txtName.Text = Str(e, "Name");
        FillCombo(cboBuilding, Sql.BuildingCombo, false, Cur(e, "BuildingId"));
        txtFloor.Text = Str(e, "Floor");
        FillCombo(cboRoomType, Lists.RoomTypes, Cur(e, "RoomType"));
        txtCapacity.Text = Str(e, "Capacity");
        FillCombo(cboStatus, Lists.RoomStatus, Cur(e, "Status"));
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryText(txtCode, "Mã phòng", true, out var code)) return false;
        if (!TryText(txtName, "Tên phòng", true, out var name)) return false;
        if (!TryCombo(cboBuilding, "Tòa nhà", true, out var building)) return false;
        if (!TryInt(txtFloor, "Tầng", true, out var floor)) return false;
        if (!TryCombo(cboRoomType, "Loại phòng", true, out var roomType)) return false;
        if (!TryInt(txtCapacity, "Sức chứa", false, out var capacity)) return false;
        if (!TryCombo(cboStatus, "Tình trạng", true, out var status)) return false;

        v["Code"] = code;
        v["Name"] = name;
        v["BuildingId"] = building;
        v["Floor"] = floor;
        v["RoomType"] = roomType;
        v["Capacity"] = capacity;
        v["Status"] = status;
        return true;
    }
}
