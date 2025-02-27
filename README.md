# FXID Development Suite

A comprehensive toolkit for developers working with the FXID Profile Service ecosystem, including both client integration and local testing capabilities.

## Components

This suite includes two main components:

1. **FXID Client SDK**: A lightweight C# SDK for seamless integration with FXID Profile Service
2. **FXID Server Emulator**: A local server that emulates FXID backend services for development and testing

## Features

### Client SDK Features
- **Automatic Profile Management**: Handles profile updates and expiration
- **Connection Management**: Easily connect, disconnect, pause, and resume profile fetching
- **Protocol Buffer Integration**: Fully compatible with the FXID Profile Service's protobuf schema
- **Configurable Logging**: Optional logging for debugging and monitoring
- **Async Support**: Modern async/await pattern for non-blocking operations

### Server Emulator Features
- **JWT-based Authentication**: Generates and validates JWT tokens for user sessions
- **User Profile Emulation**: Creates randomized user profiles based on user ID
- **Store Integration**: Emulates the store interface with mock products
- **Announcements System**: Provides mock announcements with markdown support
- **Remote Config**: Simulates remote configuration features with test values
- **Local HTTP Server**: Runs on your local machine for quick testing
- **Client Binary Integration**: Can automatically launch your game client with the appropriate parameters

## Installation

### Client SDK

#### NuGet Package (Recommended)
```bash
dotnet add package FxidClientSDK
```

#### Manual Installation
1. Clone the client SDK repository
2. Add a reference to the project in your solution
3. Add required dependencies:
   - System.Net.Http
   - Google.Protobuf

### Server Emulator

1. Clone the server emulator repository
2. Build the solution:
   ```
   dotnet build
   ```
3. Run the server:
   ```
   dotnet run
   ```

## Quick Start

### Using the Client SDK

```csharp
using System;
using System.Threading.Tasks;
using FxidClientSDK.SDK;

// Example usage
public class Program
{
    static async Task Main(string[] args)
    {
        // Create the SDK instance
        using var sdk = new FxidClientSDK(enableLogging: true);

        try
        {
            // Initialize with the connection string from command line args
            // Make sure to pass --fxid [connectionString] when running your application
            sdk.Initialize();

            // Get profile response with timeout
            var profile = await sdk.GetProfileResponseAsync(TimeSpan.FromSeconds(10));

            if (profile != null)
            {
                Console.WriteLine($"User ID: {profile.User.Fxid}");
                Console.WriteLine($"User Login: {profile.User.Login}");

                // Access various features
                if (profile.Features.Store != null)
                {
                    Console.WriteLine($"Store URL: {profile.Features.Store.Url?.Address}");
                }

                // Check if maintenance mode is active
                if (profile.Features.Maintenance?.MaintenanceMode == true)
                {
                    Console.WriteLine($"Maintenance active until: {DateTimeOffset.FromUnixTimeSeconds(profile.Features.Maintenance.EndsAt).ToString()}");
                }
            }
            else
            {
                Console.WriteLine("Failed to get profile response within timeout");
            }

            // Keep the app running for background updates
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

### Using the Server Emulator

#### Basic Usage
```
dotnet run
```

This will start the server with default settings (port 5001, user ID 100).

#### Command Line Options
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

## Integration Testing

For a complete development setup, you can run both components together:

1. Start the Server Emulator:
   ```
   dotnet run -- --port 5001
   ```

2. Configure your client application to use the local emulator by setting the connection string:
   ```
   --fxid "http://localhost:5001/launcher?token=<jwt_token>"
   ```
   Note: The server emulator will output a valid URL with token when it starts.

3. Run your client application with the SDK initialized.

This setup allows you to test your client integration with the FXID Profile Service locally, without requiring access to production servers.

## Protocol Buffer (protobuf) Support

Both the Client SDK and Server Emulator support Protocol Buffers for efficient data serialization. The key message types are:

- `ProfileResponse`: Contains user information, features, and expiration details
- `User`: Basic user identification and authentication
- `Features`: Collection of available features and their configurations
  - `LoginFeature`: Login-related URLs and connected providers
  - `StoreFeature`: Store access and product information
  - `AnnounceFeature`: Announcements and notifications
  - `TagsFeature`: User tags and attributes
  - `StatFeature`: Statistics and analytics endpoints
  - `RemoteConfigFeature`: Remote configuration values
  - `MaintenanceFeature`: Information about maintenance schedules
  - `UpdateFeature`: Application update information

## Advanced Usage

### Client SDK: Pausing and Resuming Updates

```csharp
// Pause updates during intensive operations
sdk.PauseFetch();

// Do intensive work...

// Resume updates when ready
sdk.ResumeFetch();
```

### Server Emulator: Customizing the Profile Response

Modify the `CreateRandomProfileResponseFromUserId` method in the `FxidEmulatorApiHandler.cs` file to customize the profile data returned to clients.

## Troubleshooting

### Client SDK Issues
- **Connection String Not Found**: Ensure you're passing the `--fxid` parameter to your application
- **Profile Retrieval Timeout**: Check network connectivity and server availability
- **InvalidOperationException**: Make sure to call `Initialize()` before accessing other methods

### Server Emulator Issues
- **JWT Validation Issues**: Ensure your client is using the same JWT secret as the server
- **Connection Refused**: Verify that the port is not in use by another application
- **Missing Static Files**: Make sure your static files are in the correct location

## Requirements

- .NET 6.0 or higher
- C# 9.0 or higher

## Security Note

The Server Emulator uses a fixed JWT secret by default. This is intentional for local testing purposes only. Do not use this code in a production environment.
