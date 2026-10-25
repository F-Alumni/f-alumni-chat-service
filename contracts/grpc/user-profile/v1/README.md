# Hợp đồng gRPC Tra cứu Hồ sơ Người dùng (User Profile gRPC Contract)

Thư mục này chứa định nghĩa Protocol Buffers (protobuf) phiên bản 1 cho giao tiếp đồng bộ nội bộ giữa **Chat Service** (Client) và **User Service** (Server).

---

## 📌 Thông tin Hợp đồng

- **Đường dẫn tệp**: [`contracts/grpc/user-profile/v1/user_profile.proto`](file:///c:/Use/University/PRN232/ASM/guideline/contracts/grpc/user-profile/v1/user_profile.proto)
- **Package**: `falumni.userprofile.v1`
- **C# Namespace**: `Contracts.Grpc.UserProfile.V1`
- **Dịch vụ gRPC**: `UserProfileService`
- **Phương thức RPC**:
  - `GetUserSummary(GetUserSummaryRequest)` $\rightarrow$ `GetUserSummaryResponse`
- **Đơn vị sở hữu chính (Primary Owner)**: **User Service** (Backend Dev 1)
- **Đơn vị tiêu thụ (Consumer)**: **Chat Service** (Backend Dev 3)

---

## 🛡️ Dữ liệu Trả về (Minimal Public Summary)

Theo nguyên tắc đóng gói và bảo mật thông tin tối thiểu:
- `user_id`: Chuỗi UUID định danh người dùng.
- `display_name`: Tên hiển thị công khai (ví dụ "Nguyen Van A").
- `avatar_url`: Đường dẫn ảnh đại diện.
- `status`: Trạng thái cơ bản ("Active", "Alumni", "Student", "Suspended").

> [!CAUTION]
> **Không trả về dữ liệu nhạy cảm**: Hợp đồng gRPC này **KHÔNG BAO GIỜ** được chứa mật khẩu đã băm (password hash), số điện thoại cá nhân, mã OTP xác thực, hoặc Refresh Token.

---

## 🔒 Quy định Ranh giới & Bảo mật

1. **Chỉ dùng cho giao tiếp nội bộ (Internal Service-to-Service Only)**:
   - Cổng gRPC chỉ lắng nghe trên mạng nội bộ (hoặc Kestrel HTTP/2 cluster internal port).
   - Tuyệt đối **không mở công khai ra Internet** và không định tuyến qua API Gateway cho Web/Flutter gọi trực tiếp.
2. **Quy tắc phiên bản hóa (Versioning Policy)**:
   - Đặt trong thư mục `v1/` và package `v1`.
   - Nếu có thay đổi phá vỡ tương thích (Breaking Change, ví dụ xóa trường, đổi số thứ tự field tag), phải tạo thư mục mới `contracts/grpc/user-profile/v2/` và package `falumni.userprofile.v2`.
3. **Phụ thuộc lỏng lẻo**:
   - `Chat Service` chỉ tham chiếu tệp `.proto` này để sinh ra mã client.
   - Cấm `Chat Service` tham chiếu trực tiếp project `User.Repositories`, `User.Services` hay file `.cs` thực thi của User Service.

Chi tiết xem tại tài liệu hướng dẫn: [docs/09-grpc.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/09-grpc.md).
