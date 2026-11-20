using System.Data;

namespace SchoolFacilities;

/// <summary>Điều chuyển thiết bị sang phòng khác. Giao diện: TransferForm.Designer.cs.</summary>
public partial class TransferForm : RecordForm
{
    public TransferForm()
    {
        InitializeComponent();
        DpiScaler.Apply(this);
    }

    void btnOk_Click(object? sender, EventArgs e) => Submit();

    public override void LoadValues(DataRow? e)
    {
        FillCombo(cboToRoom, Sql.RoomCombo, false);
        txtNote.Text = "";
    }

    protected override bool Collect(Dictionary<string, object?> v)
    {
        if (!TryCombo(cboToRoom, "Chuyển đến phòng", true, out var room)) return false;

        v["ToRoomId"] = room;
        v["Note"] = ReadText(txtNote);
        return true;
    }
}
