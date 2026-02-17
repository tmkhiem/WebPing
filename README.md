# WebPing

ASP.NET Core 8 WebAPI project that allows users to create topics and sends WebPush notifications to registered devices when HTTP requests are received on matching topic endpoints.

## Features

- User registration and authentication (Basic Auth)
- Topic management (create topics like "copyFilesCompleted")
- Push endpoint registration (register devices to receive notifications)
- Send notifications via HTTP requests (e.g., POST to `/send/copyFilesCompleted`)
- WebPush integration for push notifications

## Data Models

### User
- Username (primary key)
- Password (hashed with BCrypt)
- Topics (collection)
- Push Endpoints (collection)

### Topic
- Name (primary key)
- Username (foreign key to User)

### Push Endpoint
- Id (primary key)
- Name
- Endpoint (WebPush subscription endpoint)
- P256dh (public key for encryption)
- Auth (authentication secret)
- Username (foreign key to User)

## API Endpoints

### Authentication

#### Register a new user
```bash
POST /auth/register
Content-Type: application/json

{
  "username": "testuser",
  "password": "testpass123"
}
```

#### Login
```bash
POST /auth/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "testpass123"
}
```

### Topics (requires Basic Auth)

#### Create a topic
```bash
POST /topics
Authorization: Basic <base64(username:password)>
Content-Type: application/json

{
  "name": "copyFilesCompleted"
}
```

#### List topics
```bash
GET /topics
Authorization: Basic <base64(username:password)>
```

### Push Endpoints (requires Basic Auth)

#### Register a push endpoint
```bash
POST /push-endpoints
Authorization: Basic <base64(username:password)>
Content-Type: application/json

{
  "name": "MyDevice",
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "p256dh": "your-p256dh-key",
  "auth": "your-auth-key"
}
```

#### List push endpoints
```bash
GET /push-endpoints
Authorization: Basic <base64(username:password)>
```

### Send Notification (no authentication required)

#### Send notification to a topic
```bash
POST /send/{topicName}
Content-Type: application/json

{
  "title": "Files Copied",
  "body": "Your files have been copied successfully",
  "icon": "/icon.png",
  "data": "additional-data"
}
```

This endpoint finds all users subscribed to the topic and sends the notification to all their registered push endpoints.

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later

### Installation

1. Clone the repository
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

The API will be available at `http://localhost:5065` (or the port specified in `launchSettings.json`).

### Configuration

#### VAPID Keys (Optional)

For production use with real WebPush notifications, configure VAPID keys in `appsettings.json`:

```json
{
  "VapidKeys": {
    "Subject": "mailto:your-email@example.com",
    "PublicKey": "your-vapid-public-key",
    "PrivateKey": "your-vapid-private-key"
  }
}
```

To generate VAPID keys, you can use the [web-push](https://www.npmjs.com/package/web-push) npm package:
```bash
npm install -g web-push
web-push generate-vapid-keys
```

If VAPID keys are not configured, the application runs in demo mode and logs notifications to the console instead of actually sending them.

#### Database

The application uses SQLite by default. The database file (`webping.db`) is created automatically on first run. You can change the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=webping.db"
  }
}
```

## Usage Example

1. Register a user:
```bash
curl -X POST http://localhost:5065/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "alice", "password": "secret123"}'
```

2. Create a topic:
```bash
curl -X POST http://localhost:5065/topics \
  -H "Content-Type: application/json" \
  -H "Authorization: Basic $(echo -n 'alice:secret123' | base64)" \
  -d '{"name": "fileTransferComplete"}'
```

3. Register a push endpoint:
```bash
curl -X POST http://localhost:5065/push-endpoints \
  -H "Content-Type: application/json" \
  -H "Authorization: Basic $(echo -n 'alice:secret123' | base64)" \
  -d '{
    "name": "MyPhone",
    "endpoint": "https://fcm.googleapis.com/fcm/send/...",
    "p256dh": "your-p256dh-key",
    "auth": "your-auth-key"
  }'
```

4. Send a notification:
```bash
curl -X POST http://localhost:5065/send/fileTransferComplete \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Transfer Complete",
    "body": "Your file transfer has finished",
    "icon": "/transfer-icon.png"
  }'
```

## Development

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

## Technologies Used

- ASP.NET Core 8.0
- Entity Framework Core (with SQLite)
- WebPush library for push notifications
- BCrypt.Net for password hashing
- Swagger/OpenAPI for API documentation

## API Documentation

When running in development mode, Swagger UI is available at `http://localhost:5065/swagger`
