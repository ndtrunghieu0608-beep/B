/* ============================================================
   QUẢN LÝ CƠ SỞ VẬT CHẤT TRƯỜNG HỌC - Script tạo cơ sở dữ liệu
   SQL Server 2016 trở lên (bản Express dùng được).
   CẢNH BÁO: chạy lại script sẽ XÓA và tạo lại toàn bộ dữ liệu.
   Tài khoản mẫu: admin / admin123  (Admin)
                  staff / staff123  (Staff)
   ============================================================ */
IF DB_ID(N'SchoolFacilities') IS NULL
    CREATE DATABASE SchoolFacilities COLLATE Vietnamese_CI_AS;
GO
USE SchoolFacilities;
GO

DROP TABLE IF EXISTS EquipmentTransfers;
DROP TABLE IF EXISTS Maintenance;
DROP TABLE IF EXISTS Equipment;
DROP TABLE IF EXISTS EquipmentTypes;
DROP TABLE IF EXISTS Rooms;
DROP TABLE IF EXISTS Buildings;
DROP TABLE IF EXISTS SchoolInfo;
DROP TABLE IF EXISTS Users;
GO

/* ---------- Bảng ---------- */
CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash VARCHAR(64)   NOT NULL,              -- SHA-256 dạng HEX (chỉ để tham khảo)
    FullName     NVARCHAR(100) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL CHECK (Role IN (N'Admin', N'Staff')),
    IsActive     BIT           NOT NULL DEFAULT 1
);

CREATE TABLE SchoolInfo (
    KeyName NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Value   NVARCHAR(255) NULL
);

CREATE TABLE Buildings (
    BuildingId INT IDENTITY(1,1) PRIMARY KEY,
    Code       NVARCHAR(20)  NOT NULL UNIQUE,
    Name       NVARCHAR(100) NOT NULL,
    Floors     INT           NOT NULL CHECK (Floors > 0),
    Note       NVARCHAR(255) NULL
);

CREATE TABLE Rooms (
    RoomId     INT IDENTITY(1,1) PRIMARY KEY,
    Code       NVARCHAR(20)  NOT NULL UNIQUE,
    Name       NVARCHAR(100) NOT NULL,
    BuildingId INT           NOT NULL REFERENCES Buildings(BuildingId),
    Floor      INT           NOT NULL,
    RoomType   NVARCHAR(50)  NOT NULL,
    Capacity   INT           NULL,
    Status     NVARCHAR(30)  NOT NULL DEFAULT N'Đang sử dụng'
        CHECK (Status IN (N'Đang sử dụng', N'Đang sửa chữa', N'Ngừng sử dụng'))
);

CREATE TABLE EquipmentTypes (
    TypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name   NVARCHAR(100) NOT NULL UNIQUE,
    Note   NVARCHAR(255) NULL
);

CREATE TABLE Equipment (
    EquipmentId  INT IDENTITY(1,1) PRIMARY KEY,
    AssetCode    NVARCHAR(30)  NOT NULL UNIQUE,
    Name         NVARCHAR(150) NOT NULL,
    TypeId       INT           NOT NULL REFERENCES EquipmentTypes(TypeId),
    RoomId       INT           NULL REFERENCES Rooms(RoomId),
    PurchaseDate DATE          NULL,
    Price        DECIMAL(18,0) NULL,
    Status       NVARCHAR(30)  NOT NULL DEFAULT N'Tốt'
        CHECK (Status IN (N'Tốt', N'Đang bảo trì', N'Hỏng', N'Đã thanh lý')),
    Note         NVARCHAR(255) NULL
);

CREATE TABLE Maintenance (
    MaintenanceId INT IDENTITY(1,1) PRIMARY KEY,
    EquipmentId   INT           NOT NULL REFERENCES Equipment(EquipmentId),
    ReportDate    DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Description   NVARCHAR(500) NOT NULL,
    Status        NVARCHAR(30)  NOT NULL DEFAULT N'Chờ xử lý'
        CHECK (Status IN (N'Chờ xử lý', N'Đang xử lý', N'Hoàn thành')),
    CompletedDate DATE          NULL,
    Cost          DECIMAL(18,0) NULL,
    ReportedBy    INT           NULL REFERENCES Users(UserId)
);

CREATE TABLE EquipmentTransfers (
    TransferId   INT IDENTITY(1,1) PRIMARY KEY,
    EquipmentId  INT           NOT NULL REFERENCES Equipment(EquipmentId),
    FromRoomId   INT           NULL REFERENCES Rooms(RoomId),
    ToRoomId     INT           NULL REFERENCES Rooms(RoomId),
    TransferDate DATETIME      NOT NULL DEFAULT GETDATE(),
    ByUser       INT           NULL REFERENCES Users(UserId),
    Note         NVARCHAR(255) NULL
);
GO

/* ---------- Dữ liệu mẫu ---------- */
INSERT INTO Users (Username, PasswordHash, FullName, Role) VALUES
 (N'admin', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'admin123'), 2), N'Quản trị viên',    N'Admin'),
 (N'staff', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'staff123'), 2), N'Nguyễn Văn Bình', N'Staff');

INSERT INTO SchoolInfo (KeyName, Value) VALUES
 (N'SchoolName', N'Trường THPT Mẫu'),
 (N'Address',    N'Số 10 đường Lê Lợi, phường Trung Tâm, thành phố Mẫu'),
 (N'SchoolYear', N'2026 - 2027'),
 (N'Principal',  N'Nguyễn Văn An');

INSERT INTO Buildings (Code, Name, Floors, Note) VALUES
 (N'A', N'Dãy nhà A',                    3, N'Khu phòng học'),
 (N'B', N'Dãy nhà B',                    3, N'Khu thực hành - thí nghiệm - tin học'),
 (N'C', N'Nhà hành chính - thư viện',    2, NULL);

INSERT INTO Rooms (Code, Name, BuildingId, Floor, RoomType, Capacity, Status) VALUES
 (N'A101', N'Phòng học 10A1',        1, 1, N'Phòng học',        45, N'Đang sử dụng'),
 (N'A102', N'Phòng học 10A2',        1, 1, N'Phòng học',        45, N'Đang sử dụng'),
 (N'A201', N'Phòng học 11A1',        1, 2, N'Phòng học',        45, N'Đang sử dụng'),
 (N'B101', N'Phòng thí nghiệm Lý - Hóa', 2, 1, N'Phòng thí nghiệm', 30, N'Đang sử dụng'),
 (N'B201', N'Phòng Tin học 1',       2, 2, N'Phòng tin học',    40, N'Đang sử dụng'),
 (N'B202', N'Phòng Tin học 2',       2, 2, N'Phòng tin học',    40, N'Đang sử dụng'),
 (N'C101', N'Thư viện',              3, 1, N'Thư viện',         80, N'Đang sử dụng'),
 (N'C201', N'Phòng Hội đồng',        3, 2, N'Phòng làm việc',   30, N'Đang sử dụng');

INSERT INTO EquipmentTypes (Name, Note) VALUES
 (N'Máy chiếu',           NULL),
 (N'Máy tính',            NULL),
 (N'Bàn ghế',             N'Bàn ghế học sinh, giáo viên'),
 (N'Bảng',                NULL),
 (N'Thiết bị thí nghiệm', NULL),
 (N'Điều hòa',            NULL);

INSERT INTO Equipment (AssetCode, Name, TypeId, RoomId, PurchaseDate, Price, Status, Note) VALUES
 (N'TS-0001', N'Máy chiếu Epson EB-X06',            1, 1,    '2022-08-15', 12500000, N'Tốt',          NULL),
 (N'TS-0002', N'Máy chiếu Panasonic PT-LB306',      1, 2,    '2021-09-01', 11800000, N'Tốt',          NULL),
 (N'TS-0003', N'Máy chiếu Sony VPL-DX221',          1, 3,    '2023-01-10', 13200000, N'Hỏng',         NULL),
 (N'TS-0004', N'Bảng từ trắng 1.2 x 3 m',           4, 1,    '2022-08-15',  1800000, N'Tốt',          NULL),
 (N'TS-0005', N'Bảng từ trắng 1.2 x 3 m',           4, 2,    '2022-08-15',  1800000, N'Tốt',          NULL),
 (N'TS-0006', N'Bộ bàn ghế học sinh (45 bộ)',       3, 1,    '2022-08-15', 27000000, N'Tốt',          NULL),
 (N'TS-0007', N'Điều hòa Daikin 18000 BTU',         6, 1,    '2022-08-20', 17500000, N'Tốt',          NULL),
 (N'TS-0008', N'Điều hòa Daikin 18000 BTU',         6, 3,    '2022-08-20', 17500000, N'Đang bảo trì', NULL),
 (N'TS-0009', N'Máy tính Dell OptiPlex 3080',       2, 5,    '2023-03-05', 14500000, N'Tốt',          NULL),
 (N'TS-0010', N'Máy tính Dell OptiPlex 3080',       2, 5,    '2023-03-05', 14500000, N'Tốt',          NULL),
 (N'TS-0011', N'Máy tính Dell OptiPlex 3080',       2, 5,    '2023-03-05', 14500000, N'Tốt',          NULL),
 (N'TS-0012', N'Máy tính Dell OptiPlex 3080',       2, 5,    '2023-03-05', 14500000, N'Tốt',          NULL),
 (N'TS-0013', N'Máy tính HP ProDesk 400',           2, 6,    '2023-03-05', 13900000, N'Tốt',          NULL),
 (N'TS-0014', N'Bộ thí nghiệm điện học',            5, 4,    '2020-11-20',  9500000, N'Tốt',          NULL),
 (N'TS-0015', N'Kính hiển vi quang học',            5, 4,    '2020-11-20',  6200000, N'Tốt',          NULL),
 (N'TS-0016', N'Điều hòa Panasonic 12000 BTU',      6, 8,    '2021-06-10', 11000000, N'Tốt',          NULL),
 (N'TS-0017', N'Máy tính để bàn cũ',                2, NULL, '2019-05-02',  8000000, N'Đã thanh lý',  N'Thanh lý theo biên bản 2026');

INSERT INTO Maintenance (EquipmentId, ReportDate, Description, Status, CompletedDate, Cost, ReportedBy) VALUES
 (3, '2026-10-20', N'Máy chiếu không lên hình, nghi hỏng bóng đèn',        N'Chờ xử lý', NULL,         NULL,   2),
 (8, '2026-10-25', N'Điều hòa chảy nước, cần vệ sinh và nạp gas',           N'Đang xử lý', NULL,         NULL,   2),
 (2, '2026-09-10', N'Thay bóng đèn máy chiếu',                              N'Hoàn thành', '2026-09-12',  850000, 2);

INSERT INTO EquipmentTransfers (EquipmentId, FromRoomId, ToRoomId, TransferDate, ByUser, Note) VALUES
 (13, 5, 6, '20260905', 1, N'Chuyển sang phòng Tin học 2');
GO
