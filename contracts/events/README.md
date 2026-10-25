# Thư mục Hợp đồng Sự kiện Kafka (Kafka Event Contracts)

Thư mục này định nghĩa các hợp đồng sự kiện (Event Contracts) và JSON Schemas dùng chung giữa các dịch vụ độc lập trong hệ thống **FPT Alumni Connect**.

---

## 📄 Danh mục Tệp tin

1. [`EventEnvelope.cs`](file:///c:/Use/University/PRN232/ASM/guideline/contracts/events/EventEnvelope.cs):
   - Lớp generic `EventEnvelope<T>` chứa phong bì siêu dữ liệu chuẩn (`eventId`, `eventType`, `version`, `occurredAt`, `correlationId`, `producer`, `data`).
2. [`UserRegisteredEventV1.cs`](file:///c:/Use/University/PRN232/ASM/guideline/contracts/events/UserRegisteredEventV1.cs):
   - C# Model đặc tả payload sự kiện `user.registered.v1` (`userId`, `email`, `fullName`, `verificationCode`, `registeredAt`).
3. [`user.registered.v1.json`](file:///c:/Use/University/PRN232/ASM/guideline/contracts/events/user.registered.v1.json):
   - JSON Schema (Draft 2020-12) chính thức của sự kiện `user.registered.v1`.

---

## 📌 Nguyên tắc Bất biến

- **Tính tương thích ngược**: Không sửa đổi hoặc xóa các trường hiện có trong một phiên bản sự kiện đã phát hành.
- **Tạo phiên bản mới**: Nếu cần thay đổi phá vỡ tương thích (breaking change), tạo schema mới (ví dụ: `v2`) và topic mới.
- **Khóa phân vùng**: Mọi producer phát sự kiện của thực thể nào phải đặt Partition Key bằng ID của thực thể đó (ví dụ `userId.ToString()`).

Chi tiết xem tại tài liệu hướng dẫn: [docs/08-kafka.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/08-kafka.md).
