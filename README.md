# Quản lý cơ sở vật chất trường học (C# WinForms + SQL Server)

Ứng dụng tham khảo, giao diện lấy cảm hứng từ cổng thông tin sinh viên: thanh trên trắng, thẻ trắng bo góc có bóng, ô thống kê xanh/cam, hàng lối tắt và các biểu đồ.

## Yêu cầu
- Windows 10/11
- Visual Studio 2022 (khối lượng công việc ".NET desktop development") hoặc .NET 8 SDK
- SQL Server (bản Express hoặc LocalDB đều được) và SSMS (hoặc Azure Data Studio)

## Cài đặt
1. Mở `Database/schema.sql` trong SSMS rồi bấm Execute. Script tạo database `SchoolFacilities`, các bảng và dữ liệu mẫu.
   **Chạy lại script sẽ xóa toàn bộ dữ liệu cũ.**
2. Mở `connection.txt`, sửa chuỗi kết nối cho đúng máy của bạn:
   - SQL Express: `Server=.\SQLEXPRESS;Database=SchoolFacilities;Integrated Security=True;TrustServerCertificate=True;`
   - LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=SchoolFacilities;Integrated Security=True;TrustServerCertificate=True;`
3. Mở `SchoolFacilities.csproj` bằng Visual Studio, bấm F5. Hoặc chạy `dotnet run` trong thư mục dự án.

## Tài khoản mẫu
| Tên đăng nhập | Mật khẩu   | Vai trò | Quyền |
|---------------|------------|---------|-------|
| admin         | admin123   | Admin   | Toàn quyền, gồm xóa dữ liệu, quản lý người dùng, sửa thông tin trường |
| staff         | staff123   | Staff   | Thêm/sửa dữ liệu, không xóa, không vào trang Người dùng |

## Các trang
- **Trang chủ:** thông tin trường, số liệu tổng quan, yêu cầu bảo trì chưa xong, biểu đồ theo tình trạng và theo loại (lọc theo tòa nhà), danh sách bảo trì gần đây
- **Tòa nhà, Phòng, Loại thiết bị**
- **Thiết bị:** thêm/sửa/xóa, điều chuyển phòng (có lịch sử), báo hỏng
- **Bảo trì:** theo dõi yêu cầu, đánh dấu hoàn thành (tự cập nhật tình trạng thiết bị)
- **Báo cáo:** 5 báo cáo, xuất CSV mở bằng Excel
- **Người dùng** (chỉ Admin), đổi mật khẩu, đăng xuất

## Thiết kế giao diện bằng Designer
Toàn bộ form và trang đều có file `.Designer.cs`, mở được bằng **Form Designer** của Visual Studio (nhấp đúp file `.cs` hoặc chuột phải > View Designer).

| Nhóm | Form / trang |
|------|--------------|
| Khung ứng dụng | `LoginForm` (đăng nhập), `MainForm` (thanh trên + menu) |
| Trang | `DashboardControl` (trang chủ), `ReportsControl` (báo cáo), `CrudControl` (khung chung của các trang danh sách), `EquipmentControl` và `MaintenanceControl` (kế thừa `CrudControl`, thêm nút riêng) |
| Form nhập liệu | `BuildingForm`, `RoomForm`, `TypeForm`, `EquipmentForm`, `MaintenanceForm`, `UserForm`, `TransferForm`, `BrokenForm`, `CompleteForm`, `ChangePasswordForm`, `SchoolInfoForm` |

Lưu ý khi dùng Designer:
- **Build dự án một lần (Ctrl+Shift+B) trước khi mở Designer** để các control tùy chỉnh (`CardPanel`, `SectionHeader`, `StatTile`, `PillTextBox`, `NavLink`...) hiện trong Toolbox và trên màn hình thiết kế.
- Kích thước trong Designer tính theo **96 DPI**. Khi chạy, `DpiScaler.Apply(this)` (gọi sau `InitializeComponent()`) tự co giãn theo màn hình, nên **không** đổi `AutoScaleMode` (để `None`).
- Form nhập liệu kế thừa `RecordForm`. Muốn thêm một ô nhập: kéo control vào form trong Designer, rồi bổ sung một dòng trong `LoadValues` (điền dữ liệu) và `Collect` (đọc dữ liệu, khóa là tên cột trong cơ sở dữ liệu).
- Thêm trang quản lý mới: tạo form nhập liệu mới (kế thừa `RecordForm`) và khai báo một hàm trong `Pages.cs`.
- Thêm nút riêng cho một trang: mở `EquipmentControl` hoặc `MaintenanceControl` trong Designer, kéo `Button` vào thanh công cụ (`flowToolbar`, thuộc `CrudControl`), nhớ gọi `DpiScaler.ApplyControl(nút)` trong hàm khởi tạo.
- Các biểu tượng, biểu đồ và ô thống kê là control tự vẽ (`Controls.cs`), vẫn đặt và chỉnh trong Designer qua cửa sổ Properties (Kind, Caption, Back, Fore...).

## Cấu trúc mã nguồn
| File | Nội dung |
|------|----------|
| `*.Designer.cs` | Phần giao diện do Form Designer quản lý (`InitializeComponent`) |
| `DpiScaler.cs` | Co giãn giao diện thiết kế theo DPI màn hình |
| `Theme.cs`, `Controls.cs` | Bảng màu, font, hàm vẽ GDI+ và các control tự vẽ (thẻ bóng, ô thống kê, biểu đồ, ô nhập bo tròn) |
| `Core.cs` | Truy cập SQL Server, phiên đăng nhập, băm mật khẩu |
| `RecordForm.cs` | Lớp cơ sở của các form nhập liệu (đọc/kiểm tra ô nhập, nạp ô chọn) |
| `CrudControl.cs`, `Pages.cs` | Trang danh sách dùng chung (tìm kiếm, thêm/sửa/xóa) và khai báo các trang |
| `EquipmentControl.cs`, `MaintenanceControl.cs` | Nghiệp vụ riêng: điều chuyển, báo hỏng, hoàn thành bảo trì |
| `DashboardControl.cs`, `ReportsControl.cs`, `LoginForm.cs`, `MainForm.cs`, `*Form.cs` | Code-behind (nạp dữ liệu, xử lý sự kiện) |

## Lưu ý
- Mật khẩu băm SHA-256 không muối để đơn giản. Dự án thật nên dùng PBKDF2, bcrypt hoặc Argon2.
- Logo và ảnh đại diện được vẽ bằng mã. Muốn dùng logo trường, thay phần vẽ trong `LogoControl` (Controls.cs).
