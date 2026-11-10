# Sequence Diagram — Chat Service (`f-alumni-chat-service`)

> **Thành viên thực hiện**: Trương Chí Trung (`chstrg2208` — Backend 1)  
> **Service**: `f-alumni-chat-service` (.NET 10 + SignalR + gRPC Client + Kafka)  
> **File Draw.io (Trắng Đen Chuẩn UML)**: [`docs/architecture/f-alumni-chat-sequence-diagram.drawio`](architecture/f-alumni-chat-sequence-diagram.drawio)

---

## 1. Luồng 1: Tạo Cuộc trò chuyện & Đồng bộ Hồ sơ Người tham gia qua gRPC HTTP/2 (`CH-01`, `CH-04`)

```mermaid
sequenceDiagram
    autonumber
    actor UserA as Alumni A (Client)
    participant GW as YARP API Gateway (:8080)
    participant Ctrl as ChatsController (:5002)
    participant Svc as ChatAppService / ProfileLookupService
    participant Grpc as UserService gRPC (:5001)
    participant Repo as ChatRepository
    participant DB as FAlumni_Chat_Db

    Note over UserA,DB: Flow 1: CH-01 & CH-04 — Tạo Cuộc trò chuyện & Tra cứu Hồ sơ qua gRPC (HTTP/2 Protobuf)
    UserA->>GW: POST /api/chat/conversations { ParticipantUserIds, Title, IsGroup }
    GW->>Ctrl: POST /api/v1/chats/conversations
    Ctrl->>Svc: CreateOrGetConversationAsync(creatorUserId, request)
    Svc->>Grpc: gRPC GetUserSummaryRequest { user_id } (Deadline = 3s)
    Grpc-->>Svc: GetUserSummaryResponse { userId, displayName, avatarUrl, status }
    Svc->>Repo: CreateConversationAsync(Conversation + ConversationParticipants)
    Repo->>DB: INSERT Conversations & ConversationParticipants (Scalar UserId, Cached DisplayName/AvatarUrl)
    DB-->>Repo: Saved Conversation
    Svc-->>Ctrl: ConversationResponse
    Ctrl-->>UserA: 201 Created (ConversationResponse)
```

---

## 2. Luồng 2 & 3: Nhắn tin Real-time qua SignalR `ChatHub`, Typing Indicator, Kafka Push & Read Receipts (`CH-02..CH-10`)

```mermaid
sequenceDiagram
    autonumber
    actor UserA as Alumni A (Sender)
    participant Hub as ChatHub / ChatsController (:5002)
    participant Svc as ChatAppService
    participant Repo as ChatRepository & FAlumni_Chat_Db
    participant Kafka as Apache Kafka Broker
    actor UserB as Alumni B (Recipient)

    Note over UserA,UserB: Flow 2: CH-02, CH-06, CH-07, CH-10 — Nhắn tin Real-time SignalR & Phát sự kiện Kafka
    UserA->>Hub: WSS SignalR JoinConversation(conversationId)
    UserB->>Hub: WSS SignalR JoinConversation(conversationId)
    UserA->>Hub: SignalR SendTypingIndicator(conversationId, isTyping=true) (CH-06)
    Hub-->>UserB: SignalR "UserTyping" { conversationId, userId, isTyping=true }

    UserA->>Hub: POST /api/v1/chats/conversations/{id}/messages (hoặc SignalR SendMessage)
    Hub->>Svc: SendMessageAsync(conversationId, senderUserId, SendMessageRequest)
    Svc->>Repo: AddMessageAsync -> INSERT Messages (Status="Sent") & UPDATE LastMessageAtUtc
    Repo-->>Svc: Saved ChatMessage
    Svc-)Kafka: PublishMessageSentAsync("chat.message_sent.v1", { messageId, senderId, recipientIds })
    Hub-->>UserA: SignalR "ReceiveMessage" (ChatMessageResponse)
    Hub-->>UserB: SignalR "ReceiveMessage" (ChatMessageResponse)

    Note over UserA,UserB: Flow 3: CH-05 & CH-09 — Đánh dấu Đã đọc (MarkConversationRead) & Cài đặt Mute/Hide
    UserB->>Hub: POST /api/v1/chats/conversations/{id}/read (hoặc SignalR MarkConversationRead)
    Hub->>Svc: MarkConversationReadAsync(conversationId, readerUserId)
    Svc->>Repo: MarkMessagesAsReadAsync -> UPDATE Messages SET Status='Read', ReadAtUtc=UtcNow
    Hub-->>UserA: SignalR "MessagesRead" { conversationId, readerUserId, markedCount, readAtUtc }

    UserA->>Hub: PUT /api/v1/chats/conversations/{id}/settings { IsMuted, IsHidden } (CH-09)
    Hub->>Repo: UpdateParticipantAsync -> UPDATE ConversationParticipants SET IsMuted, IsHidden
    Repo-->>UserA: 200 OK (ParticipantDto)
```
