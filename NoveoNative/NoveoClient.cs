using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoveoNative
{
    // --- Data Models ---
    public class ServerMessage
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("user")] public User? User { get; set; }
        [JsonPropertyName("users")] public List<User>? Users { get; set; }
        [JsonPropertyName("chats")] public List<Chat>? Chats { get; set; }
        [JsonPropertyName("chat")] public Chat? Chat { get; set; }
        [JsonPropertyName("channel")] public Chat? Channel { get; set; }

        [JsonPropertyName("messageId")] public string? MessageId { get; set; }
        [JsonPropertyName("chatId")] public string? ChatId { get; set; }
        [JsonPropertyName("senderId")] public string? SenderId { get; set; }
        [JsonPropertyName("content")] public object? Content { get; set; }
        [JsonPropertyName("newContent")] public object? NewContent { get; set; }
        [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
        [JsonPropertyName("editedAt")] public long? EditedAt { get; set; }
        [JsonPropertyName("token")] public string? Token { get; set; }
        [JsonPropertyName("replyToId")] public string? ReplyToId { get; set; }
        [JsonPropertyName("message")] public string? ErrorMessage { get; set; }
        [JsonPropertyName("publicChatId")] public string? PublicChatId { get; set; }

        // Pinned
        [JsonPropertyName("pinnedMessage")] public object? PinnedMessage { get; set; }

        // Presence info often comes in the user list or separate event
        [JsonPropertyName("userId")] public string? UserId { get; set; }
        [JsonPropertyName("online")] public object? OnlineData { get; set; }
        [JsonPropertyName("members")] public List<string>? Members { get; set; }
    }

    public class User
    {
        [JsonPropertyName("userId")] public string UserId { get; set; } = "";
        [JsonPropertyName("username")] public string Username { get; set; } = "Unknown";
        [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
    }

    public class Chat
    {
        [JsonPropertyName("chatId")] public string ChatId { get; set; } = "";
        [JsonPropertyName("chatName")] public string? ChatName { get; set; }
        [JsonPropertyName("chatType")] public string? ChatType { get; set; }
        [JsonPropertyName("messages")] public List<ServerMessage>? Messages { get; set; }
        [JsonPropertyName("members")] public List<string>? Members { get; set; }
        [JsonPropertyName("avatarUrl")] public string? AvatarUrl { get; set; }
        [JsonPropertyName("ownerId")] public string? OwnerId { get; set; }
        [JsonPropertyName("handle")] public string? Handle { get; set; }
        [JsonPropertyName("pinnedMessage")] public object? PinnedMessage { get; set; }

        public long LastMessageTimestamp { get; set; }
    }

    public class FileAttachment
    {
        [JsonPropertyName("url")] public string Url { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
    }

    public class ParsedContent
    {
        public string Text { get; set; } = "";
        public bool IsTheme { get; set; }
        public string ThemeName { get; set; } = "";
        public bool IsFile { get; set; }
        public bool IsImage { get; set; }
        public bool IsVideo { get; set; }
        public bool IsAudio { get; set; }
        public string FileName { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public long FileSize { get; set; }
        public bool IsForwarded { get; set; }
        public string ForwardedFrom { get; set; } = "";
    }

    public class NoveoClient
    {
        private ClientWebSocket _ws = new ClientWebSocket();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public const string DOMAIN = "api.pcpapc172.ir:8443";
        private readonly string _serverUrl = $"wss://{DOMAIN}/ws";
        private readonly string _uploadBaseUrl = $"https://{DOMAIN}/upload";

        public Dictionary<string, User> Users = new Dictionary<string, User>();
        public List<Chat> AllChats { get; private set; } = new List<Chat>();

        public string CurrentUserId { get; private set; } = "";
        public string CurrentUsername { get; private set; } = "";
        public string CurrentUserAvatar { get; private set; } = "";
        public string PublicChatId { get; private set; } = ""; // FIX ERROR
        private string _authToken = "";

        // Events
        public event Action<string>? OnLog;
        public event Action? OnLoginSuccess;
        public event Action? OnLoginFailed;
        public event Action? OnChatListUpdated;
        public event Action<ServerMessage?>? OnMessageReceived;
        public event Action<string, string>? OnMessageDeleted;
        public event Action<string, object?>? OnMessageUpdated;
        public event Action<double>? OnUploadProgress;

        // MISSING EVENTS FIXED
        public event Action<Chat>? OnNewChat;
        public event Action<Chat>? OnChannelInfo;
        public event Action<string, bool>? OnPresenceUpdate;

        // --- AUTH ---
        public async Task ConnectAndLogin(string username, string password)
        {
            await ConnectAndSend(JsonSerializer.Serialize(new { type = "login_with_password", username, password }));
        }
        public async Task ConnectAndRegister(string username, string password)
        {
            await ConnectAndSend(JsonSerializer.Serialize(new { type = "register", username, password }));
        }
        public async Task Reconnect(string userId, string token)
        {
            await ConnectAndSend(JsonSerializer.Serialize(new { type = "reconnect", userId, token }));
        }

        private async Task ConnectAndSend(string jsonPayload)
        {
            try
            {
                if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.Connecting) { try { _cts.Cancel(); await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnect", CancellationToken.None); } catch { } }
                _ws = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                await _ws.ConnectAsync(new Uri(_serverUrl), _cts.Token);
                _ = ReceiveLoop();
                await SendRaw(jsonPayload);
            }
            catch (Exception ex) { OnLog?.Invoke($"Connection Failed: {ex.Message}"); OnLoginFailed?.Invoke(); }
        }

        // --- MESSAGING & API ---
        // --- UPDATED: Accepts recipientId for new DMs ---
        public async Task SendMessage(string chatId, string text, object? fileObj = null, string? replyToId = null, object? forwardedInfo = null, string? recipientId = null)
        {
            var contentData = new Dictionary<string, object?>();
            contentData["text"] = text ?? "";
            contentData["file"] = fileObj;
            contentData["theme"] = null;
            if (forwardedInfo != null) contentData["forwardedInfo"] = forwardedInfo;

            // Construct message payload
            var msg = new Dictionary<string, object>
            {
                { "type", "message" },
                { "chatId", chatId },
                { "content", contentData }
            };

            if (replyToId != null) msg["replyToId"] = replyToId;

            // Crucial fix for new DMs: Pass recipientId if we have it
            if (recipientId != null) msg["recipientId"] = recipientId;

            await SendRaw(JsonSerializer.Serialize(msg));
        }
        public async Task DeleteMessage(string chatId, string messageId) { await SendRaw(JsonSerializer.Serialize(new { type = "delete_message", chatId, messageId })); }
        public async Task UpdateUsername(string newName) { await SendRaw(JsonSerializer.Serialize(new { type = "update_username", username = newName })); CurrentUsername = newName; }
        public async Task GetChannelByHandle(string handle) { await SendRaw(JsonSerializer.Serialize(new { type = "get_channel_by_handle", handle })); }

        // FIX MISSING METHODS
        public async Task PinMessage(string chatId, string messageId) { await SendRaw(JsonSerializer.Serialize(new { type = "pin_message", chatId, messageId })); }
        public async Task UnpinMessage(string chatId) { await SendRaw(JsonSerializer.Serialize(new { type = "unpin_message", chatId })); }
        public async Task JoinChannel(string chatId) { await SendRaw(JsonSerializer.Serialize(new { type = "join_channel", chatId })); }

        // FIX MISSING CreateChannel
        public async Task CreateChannel(string name, string handle, FileResult? avatar = null)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(name), "name");
                content.Add(new StringContent(handle), "handle");

                if (avatar != null)
                {
                    using var stream = await avatar.OpenReadAsync();
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(avatar.ContentType);
                    content.Add(streamContent, "avatar", avatar.FileName);
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-User-ID", CurrentUserId);
                client.DefaultRequestHeaders.Add("X-Auth-Token", _authToken);

                var response = await client.PostAsync($"https://{DOMAIN}/create_channel", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    OnLog?.Invoke($"Channel creation failed: {errorText}");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Channel creation error: {ex.Message}");
            }
        }

        public async Task<FileAttachment?> UploadFile(FileResult file, string endpoint = "file")
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = await file.OpenReadAsync();
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.FileName);
                OnUploadProgress?.Invoke(0.1);
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-User-ID", CurrentUserId);
                client.DefaultRequestHeaders.Add("X-Auth-Token", _authToken);
                var response = await client.PostAsync($"{_uploadBaseUrl}/{endpoint}", content);
                OnUploadProgress?.Invoke(1.0);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.GetProperty("success").GetBoolean())
                    {
                        if (endpoint == "avatar")
                        {
                            string url = doc.RootElement.GetProperty("url").GetString() ?? "";
                            CurrentUserAvatar = GetFullUrl(url);
                            return new FileAttachment { Url = url, Type = "image/png", Name = "avatar" };
                        }
                        else
                        {
                            var fileEl = doc.RootElement.GetProperty("file");
                            return JsonSerializer.Deserialize<FileAttachment>(fileEl.GetRawText());
                        }
                    }
                }
            }
            catch (Exception ex) { OnLog?.Invoke("Upload Failed: " + ex.Message); }
            return null;
        }

        private async Task SendRaw(string json)
        {
            if (_ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            try
            {
                while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                            if (result.MessageType == WebSocketMessageType.Close) { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); OnLoginFailed?.Invoke(); return; }
                            ms.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage);
                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            string message = await reader.ReadToEndAsync();
                            _ = Task.Run(() => HandleServerMessage(message));
                        }
                    }
                }
            }
            catch { if (!_cts.IsCancellationRequested) OnLoginFailed?.Invoke(); }
        }

        private void HandleServerMessage(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<ServerMessage>(json);
                if (data == null || data.Type == null) return;
                switch (data.Type)
                {
                    case "login_success":
                        if (data.User != null)
                        {
                            CurrentUserId = data.User.UserId;
                            CurrentUsername = data.User.Username;
                            CurrentUserAvatar = GetFullUrl(data.User.AvatarUrl);
                            _authToken = data.Token ?? "";
                            PublicChatId = data.PublicChatId ?? "";
                            SettingsManager.SaveSession(CurrentUserId, data.Token ?? "", data.User.Username);
                        }
                        OnLoginSuccess?.Invoke();
                        break;
                    case "auth_failed":
                    case "error":
                        if (data.ErrorMessage?.ToLower().Contains("auth") == true) OnLoginFailed?.Invoke();
                        break;
                    case "user_list_update":
                        if (data.Users != null)
                        {
                            // FIX: Parse online status
                            List<string> onlineIds = new List<string>();
                            try
                            {
                                if (json.Contains("\"online\""))
                                {
                                    var doc = JsonDocument.Parse(json);
                                    if (doc.RootElement.TryGetProperty("online", out var onlineProp) && onlineProp.ValueKind == JsonValueKind.Array)
                                        onlineIds = JsonSerializer.Deserialize<List<string>>(onlineProp.GetRawText()) ?? new List<string>();
                                }
                            }
                            catch { }

                            foreach (var u in data.Users)
                            {
                                u.IsOnline = onlineIds.Contains(u.UserId);
                                Users[u.UserId] = u;
                            }
                            OnChatListUpdated?.Invoke();
                        }
                        break;
                    case "chat_history":
                        if (data.Chats != null)
                        {
                            AllChats = data.Chats;
                            foreach (var c in AllChats)
                            {
                                if (c.Messages != null && c.Messages.Count > 0) c.LastMessageTimestamp = c.Messages.Last().Timestamp;
                            }
                            OnChatListUpdated?.Invoke();
                        }
                        break;
                    case "message":
                        var chat = AllChats.FirstOrDefault(c => c.ChatId == data.ChatId);
                        if (chat != null)
                        {
                            chat.Messages ??= new List<ServerMessage>();
                            if (!chat.Messages.Any(m => m.MessageId == data.MessageId))
                            {
                                chat.Messages.Add(data);
                                chat.LastMessageTimestamp = data.Timestamp;
                            }
                        }
                        OnMessageReceived?.Invoke(data); OnChatListUpdated?.Invoke();
                        break;
                    case "new_chat_info":
                        if (data.Chat != null)
                        {
                            if (!AllChats.Any(c => c.ChatId == data.Chat.ChatId)) AllChats.Add(data.Chat);
                            OnNewChat?.Invoke(data.Chat); OnChatListUpdated?.Invoke();
                        }
                        break;
                    case "channel_info":
                        if (data.Channel != null)
                        {
                            if (!AllChats.Any(c => c.ChatId == data.Channel.ChatId)) AllChats.Add(data.Channel);
                            OnChannelInfo?.Invoke(data.Channel); OnChatListUpdated?.Invoke();
                        }
                        break;
                    case "message_deleted":
                        var dChat = AllChats.FirstOrDefault(c => c.ChatId == data.ChatId);
                        if (dChat != null && dChat.Messages != null) dChat.Messages.RemoveAll(m => m.MessageId == data.MessageId);
                        OnMessageDeleted?.Invoke(data.MessageId ?? "", data.ChatId ?? "");
                        break;
                    case "message_updated":
                        OnMessageUpdated?.Invoke(data.MessageId ?? "", data.NewContent);
                        break;
                    case "presence_update":
                        // Simple presence update check
                        bool isOnline = json.Contains("\"online\":true");
                        if (!string.IsNullOrEmpty(data.UserId))
                        {
                            if (Users.ContainsKey(data.UserId)) Users[data.UserId].IsOnline = isOnline;
                            OnPresenceUpdate?.Invoke(data.UserId, isOnline);
                        }
                        break;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("JSON Error: " + ex.Message); }
        }

        public string GetFullUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return "";
            if (relativePath.StartsWith("http")) return relativePath;
            return $"https://{DOMAIN}{relativePath}";
        }

        public ParsedContent ParseMessageContent(object? contentObj)
        {
            var result = new ParsedContent();
            if (contentObj == null) return result;
            try
            {
                JsonElement root;
                if (contentObj is JsonElement element) root = element;
                else
                {
                    string s = contentObj.ToString() ?? "";
                    if (s.Trim().StartsWith("{")) { try { root = JsonDocument.Parse(s).RootElement; } catch { result.Text = s; return result; } }
                    else { result.Text = s; return result; }
                }
                if (root.ValueKind == JsonValueKind.String)
                {
                    string inner = root.GetString() ?? "";
                    if (inner.Trim().StartsWith("{")) return ParseMessageContent(inner);
                    result.Text = inner;
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("text", out var textProp)) result.Text = textProp.ValueKind == JsonValueKind.String ? (textProp.GetString() ?? "") : textProp.ToString();
                    if (root.TryGetProperty("file", out var fileProp) && fileProp.ValueKind != JsonValueKind.Null)
                    {
                        result.IsFile = true;
                        JsonElement fileRoot = fileProp;
                        if (fileProp.ValueKind == JsonValueKind.String) try { fileRoot = JsonDocument.Parse(fileProp.GetString()).RootElement; } catch { }
                        if (fileRoot.TryGetProperty("name", out var fName)) result.FileName = fName.GetString() ?? "File";
                        if (fileRoot.TryGetProperty("url", out var fUrl)) result.FileUrl = GetFullUrl(fUrl.GetString());
                        if (fileRoot.TryGetProperty("size", out var fSize)) result.FileSize = fSize.TryGetInt64(out long size) ? size : 0;
                        if (fileRoot.TryGetProperty("type", out var fType))
                        {
                            string type = (fType.GetString() ?? "").ToLower();
                            if (type.StartsWith("image")) result.IsImage = true; if (type.StartsWith("video")) result.IsVideo = true; if (type.StartsWith("audio")) result.IsAudio = true;
                        }
                    }
                    if (root.TryGetProperty("theme", out var themeProp) && themeProp.ValueKind != JsonValueKind.Null)
                    {
                        result.IsTheme = true; if (themeProp.TryGetProperty("name", out var tName)) result.ThemeName = tName.GetString() ?? "Unknown Theme";
                    }
                    if (root.TryGetProperty("forwardedInfo", out var fwdProp) && fwdProp.ValueKind == JsonValueKind.Object)
                    {
                        result.IsForwarded = true;
                        if (fwdProp.TryGetProperty("from", out var fromProp)) result.ForwardedFrom = fromProp.GetString() ?? "Unknown";
                    }
                }
            }
            catch { result.Text = "Error parsing content"; }
            return result;
        }

        public string GetUserName(string? userId) => string.IsNullOrEmpty(userId) ? "Unknown" : (userId == CurrentUserId ? "You" : (Users.ContainsKey(userId) ? Users[userId].Username : "User"));
        public string GetUserAvatar(string? userId) => (userId != null && Users.ContainsKey(userId)) ? GetFullUrl(Users[userId].AvatarUrl) : "";
        public bool IsUserOnline(string? userId) => userId != null && Users.ContainsKey(userId) && Users[userId].IsOnline;
    }
}