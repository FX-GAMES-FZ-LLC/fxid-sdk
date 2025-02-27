# FXID Server Emulator

A lightweight server for emulating FXID backend services during local development and testing.

## Overview

This tool provides a local emulation of the FXID backend services, allowing developers to test game client integration without requiring access to production servers. It creates a mock authentication and user profile system that mirrors the behavior of the production environment.

## Features

- **JWT-based Authentication**: Generates and validates JWT tokens for user sessions
- **User Profile Emulation**: Creates randomized user profiles based on user ID
- **Store Integration**: Emulates the store interface with mock products
- **Announcements System**: Provides mock announcements with markdown support
- **Remote Config**: Simulates remote configuration features with test values
- **Local HTTP Server**: Runs on your local machine for quick testing
- **Client Binary Integration**: Can automatically launch your game client with the appropriate parameters

## Prerequisites

- .NET SDK (6.0 or later)
- Your game client binary (optional, for full integration testing)

## Installation

1. Clone this repository
2. Build the solution:
   ```
   dotnet build
   ```
3. Run the server:
   ```
   dotnet run
   ```

## Usage

### Basic Usage

```
dotnet run
```

This will start the server with default settings (port 5001, user ID 100).

### Command Line Options

```
dotnet run -- [options]
```

Available options:

- `--port <port>`: Set the server port (default: 5001)
- `--user-id <id>`: Set the user ID (default: 100)
- `--jwt-secret <secret>`: Set a custom JWT secret key
- `--client-binary <path>`: Path to the game client binary
- `--client-args <args>`: Additional arguments to pass to the client binary
- `--game-server-url <url>`: URL of your game server for product delivery (default: http://localhost:8080)

### Example Commands

Start server on port 5002:
```
dotnet run -- --port 5002
```

Start server and automatically launch the game client:
```
dotnet run -- --client-binary "C:\Games\MyGame\client.exe" --client-args "--fullscreen"
```

Use a custom user ID:
```
dotnet run -- --user-id 12345
```

### Accessing the Server

Once running, your client can connect to the server using:
```
http://localhost:<port>/launcher?token=<jwt_token>
```

The server will output the full URL with a valid token when it starts.

## API Endpoints

- **/launcher?token=<jwt_token>**: Returns the user profile
- **/buy?token=<jwt_token>&product=<product_id>**: Simulates a product purchase
- **/static/...**: Serves static files from the static folder

## Protocol Buffer (protobuf) Support

The server supports both JSON and Protocol Buffer responses. Set the `Accept` header to `application/x-protobuf` to receive binary protobuf responses.

## Development

### Adding Static Content

Place HTML, CSS, JS and other static files in the `/static` folder to serve them via the `/static/` endpoint.

### Customizing the Profile Response

Modify the `CreateRandomProfileResponseFromUserId` method in the `FxidEmulatorApiHandler.cs` file to customize the profile data returned to clients.

### Security Note

This server uses a fixed JWT secret by default. This is intentional for local testing purposes only. Do not use this code in a production environment.

## Troubleshooting

- **JWT Validation Issues**: Ensure your client is using the same JWT secret as the server
- **Connection Refused**: Verify that the port is not in use by another application
- **Missing Static Files**: Make sure your static files are in the correct location

## License

[Include your license here]