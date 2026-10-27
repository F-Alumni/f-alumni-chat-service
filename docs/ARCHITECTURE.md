# 17. Sơ Đồ Kiến Trúc Hệ Thống Chi Tiết (System Architecture & ERD — BE1 Trương Chí Trung)

> **Người thực hiện**: Trương Chí Trung (`chstrg2208` — Backend 1)  
> **Phạm vi**:
> 1. Sơ đồ Kiến trúc Tổng thể Hệ thống (Vector SVG + Draw.io + Mermaid).
> 2. Kiến trúc 3 tầng nội bộ `.NET 10` (`Api -> Services -> Repositories`).
> 3. Sơ đồ ERD độc lập của `FAlumni_User_Db` (`f-alumni-user-service`) và `FAlumni_Chat_Db` (`f-alumni-chat-service`).
> 4. Sơ đồ Tuần tự (Sequence Diagrams) cho luồng **gRPC Profile Lookup**, **SignalR Real-time Messaging**, và **Apache Kafka Event Streaming**.
> 5. Sơ đồ Triển khai Cloud trên Render & Cơ chế **60s Pinger Bot** chống Cold-Start.

---

## 1. Sơ Đồ Kiến Trúc Tổng Thể (Vector SVG & Draw.io)

- **File Vector SVG**: [docs/architecture/f-alumni-architecture-overview.svg](architecture/f-alumni-architecture-overview.svg)
- **File Draw.io (Chỉnh sửa trực tiếp trên diagrams.net)**: [docs/architecture/f-alumni-system-architecture.drawio](architecture/f-alumni-system-architecture.drawio)

![FPT Alumni Connect System Architecture](architecture/f-alumni-architecture-overview.svg)

---

## 2. Sơ Đồ Kiến Trúc Tổng Thể (Mermaid Flowchart)

```mermaid
flowchart TB
    subgraph Clients["1. Client Layer (Frontend)"]
        Web["Web Client (React 18 + Vite + TS)"]
        Mobile["Mobile App (Flutter Cross-Platform)"]
        Pinger["60s Render Pinger Bot\n(GET /ping & /health)"]
    end

    subgraph Edge["2. API Gateway Layer"]
        Gateway["YARP Reverse Proxy (API Gateway)\nRoute Rewrite • JWT Pass-through • CORS • SignalR Proxy"]
    end

    subgraph Services["3. Core Microservices Layer (.NET 10 — 3-Layer Pattern)"]
        UserSvc["User Service (BE1 - Chí Trung)\nREST API + gRPC Server + Kafka Producer\nUS-01..US-11"]
        ChatSvc["Chat Service (BE1 - Chí Trung)\nREST API + SignalR ChatHub + gRPC Client + Kafka Producer\nCH-01..CH-10"]
        ForumSvc["Forum Service (BE2 - Minh Triết)\nPosts • Comments • Reactions • ForumHub"]
        AISvc["AI Service (BE2 - Minh Triết)\nMentorship Matching • Career Q&A"]
        AdminSvc["Admin Service (Team Lead - Quang Tâm)\nDashboard • Moderation • Audit Logs"]
    end

    subgraph Async["4. Event Streaming & Background Worker"]
        Kafka[("Apache Kafka Broker\n• user.registered.v1\n• user.suspended.v1\n• chat.message_sent.v1")]
        Worker["Notification Background Worker\n(.NET 10 BackgroundService)\nWelcome OTP Email & Push Notification"]
    end

    subgraph Data["5. Isolated Database-per-Service Layer (Zero Cross-DB FK)"]
        UserDb[("FAlumni_User_Db")]
        ChatDb[("FAlumni_Chat_Db")]
        ForumDb[("FAlumni_Forum_Db")]
        AIDb[("FAlumni_AI_Db")]
        AdminDb[("FAlumni_Admin_Db")]
    end

    Web -->|HTTPS REST + WSS| Gateway
    Mobile -->|HTTPS REST + WSS| Gateway
    Pinger -.->|Wake-up every 60s| Gateway

    Gateway -->|/api/users/*| UserSvc
    Gateway -->|/api/chat/* & /hubs/chat| ChatSvc
    Gateway -->|/api/forum/*| ForumSvc
    Gateway -->|/api/ai/*| AISvc
    Gateway -->|/api/admin/*| AdminSvc

    ChatSvc ==>|gRPC HTTP/2 Protobuf\nGetUserSummary| UserSvc
    AdminSvc -.->|Internal REST\nSuspend/Unsuspend| UserSvc

    UserSvc -->|Publish EventEnvelope| Kafka
    ChatSvc -->|Publish EventEnvelope| Kafka
    Kafka -->|Consume| Worker

    UserSvc --> UserDb
    ChatSvc --> ChatDb
    ForumSvc --> ForumDb
    AISvc --> AIDb
    AdminSvc --> AdminDb
```

---

## 3. Sơ Đồ ERD Của `User Service` & `Chat Service` (Zero Cross-DB Foreign Keys)

```mermaid
erDiagram
    USERS ||--o{ REFRESH_TOKENS : "has"
    USERS ||--o{ EMAIL_VERIFICATION_OTPS : "verifies"
    USERS ||--o{ USER_BLOCKS : "blocks"

    USERS {
        Guid Id PK
        string Email UK
        string PasswordHash
        string FullName
        string AvatarUrl
        string Role "USER | ADMIN"
        string Status "PendingVerification | Active | Suspended"
        string Campus "FU-HCM | FU-HL | FU-DN | FU-CT | FU-QN"
        string Major
        int GraduationYear
        string CurrentCompany
        string JobTitle
        string GoogleCalendarUrl "US-11 External Calendar Link"
        bool ShowEmail "US-09 Privacy"
        bool ShowPhoneNumber "US-09 Privacy"
        bool ShowCompany "US-09 Privacy"
        bool ShowCalendarUrl "US-09 Privacy"
        DateTime CreatedAtUtc
    }

    REFRESH_TOKENS {
        Guid Id PK
        Guid UserId FK
        string TokenHash UK
        DateTime ExpiresAtUtc
        bool IsRevoked
    }

    EMAIL_VERIFICATION_OTPS {
        Guid Id PK
        Guid UserId FK
        string Email
        string Code "6-digit OTP"
        string Purpose "EmailVerification | PasswordReset"
        DateTime ExpiresAtUtc
        bool IsUsed
    }

    USER_BLOCKS {
        Guid Id PK
        Guid BlockerUserId FK
        Guid BlockedUserId "US-10 Blocked User"
        DateTime CreatedAtUtc
    }

    CONVERSATIONS ||--o{ CONVERSATION_PARTICIPANTS : "contains"
    CONVERSATIONS ||--o{ CHAT_MESSAGES : "has"

    CONVERSATIONS {
        Guid Id PK
        string Title
        bool IsGroup "1-1 or Group"
        Guid CreatedByUserId "Scalar Guid (No Cross-DB FK)"
        DateTime CreatedAtUtc
        DateTime LastMessageAtUtc
    }

    CONVERSATION_PARTICIPANTS {
        Guid Id PK
        Guid ConversationId FK
        Guid UserId "Scalar Guid -> Resolved via gRPC"
        string DisplayName
        string AvatarUrl
        bool IsMuted "CH-09 Mute Notifications"
        bool IsHidden "CH-09 Hide Conversation"
        DateTime JoinedAtUtc
        DateTime LastReadAtUtc
    }

    CHAT_MESSAGES {
        Guid Id PK
        Guid ConversationId FK
        Guid SenderId "Scalar Guid (No Cross-DB FK)"
        string Content
        string AttachmentUrl "CH-07 Image/Attachment"
        string AttachmentType "Image | File"
        string Status "Sent | Delivered | Read (CH-05)"
        DateTime CreatedAtUtc
        DateTime ReadAtUtc
    }
```

---

## 4. Luồng Tương Tác Đồng Bộ gRPC & Real-time SignalR (`Chat Service` $\leftrightarrow$ `User Service`)

```mermaid
sequenceDiagram
    autonumber
    actor UserA as Alumni A (Client)
    actor UserB as Alumni B (Client)
    participant GW as YARP API Gateway
    participant Chat as Chat Service (:5002)
    participant User as User Service gRPC (:5001)
    participant Kafka as Apache Kafka
    participant ChatDB as FAlumni_Chat_Db

    Note over UserA,User: Luồng 1: Tra cứu Hồ sơ Người tham gia qua gRPC (CH-04)
    UserA->>GW: GET /api/chat/conversations/{convId}/participants/{userId}
    GW->>Chat: GET /api/v1/chats/conversations/{convId}/participants/{userId}
    Chat->>User: gRPC GetUserSummaryRequest { user_id } (HTTP/2 Protobuf, Deadline 3s)
    User-->>Chat: GetUserSummaryResponse { userId, displayName, avatarUrl, status }
    Chat-->>GW: 200 OK (ConversationParticipantResponse)
    GW-->>UserA: 200 OK JSON

    Note over UserA,ChatDB: Luồng 2: Nhắn tin Real-time SignalR + Đính kèm ảnh + Đọc tin nhắn + Kafka Push (CH-02..CH-10)
    UserA->>Chat: SignalR SendTypingIndicator(convId, true) (CH-06)
    Chat-->>UserB: SignalR "UserTyping"
    UserA->>Chat: POST /api/v1/chats/conversations/{convId}/messages (CH-02, CH-07)
    Chat->>ChatDB: INSERT ChatMessage (Status="Sent") & UPDATE LastMessageAtUtc
    Chat-->>UserA: SignalR "ReceiveMessage"
    Chat-->>UserB: SignalR "ReceiveMessage"
    Chat-)Kafka: Publish "chat.message_sent.v1" (CH-10 Push Notification)
    UserB->>Chat: POST /api/v1/chats/conversations/{convId}/read (CH-05)
    Chat->>ChatDB: UPDATE ChatMessage SET Status="Read", ReadAtUtc=UtcNow
    Chat-->>UserA: SignalR "MessagesRead"
```
