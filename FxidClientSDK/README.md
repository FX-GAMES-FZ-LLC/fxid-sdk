# FXID Client SDK

A lightweight C# SDK for interacting with the FXID Profile Service. This SDK enables seamless integration with user profiles, features, and authentication in your application.

## Features

- **Automatic Profile Management**: Handles profile updates and expiration
- **Connection Management**: Easily connect, disconnect, pause, and resume profile fetching
- **Protocol Buffer Integration**: Fully compatible with the FXID Profile Service's protobuf schema
- **Configurable Logging**: Optional logging for debugging and monitoring
- **Async Support**: Modern async/await pattern for non-blocking operations

## Installation

### NuGet Package (Recommended)

```bash
dotnet add package FxidClientSDK
```

### Manual Installation

1. Clone this repository
2. Add a reference to the project in your solution
3. Add required dependencies:
   - System.Net.Http
   - Google.Protobuf

## Quick Start

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

## Advanced Usage

### Pausing and Resuming Updates

You can temporarily pause background profile fetching:

```csharp
// Pause updates during intensive operations
sdk.PauseFetch();

// Do intensive work...

// Resume updates when ready
sdk.ResumeFetch();
```

### Manual Connection Management

```csharp
// Initialize but don't connect yet
sdk.Initialize();

// Connect when you're ready
sdk.Connect();

// Manually disconnect (the SDK will reconnect automatically when needed)
sdk.Disconnect();
```

### Handling Profile Updates

The SDK automatically manages profile refreshing based on the expiration timestamp. You can retrieve the latest profile at any time:

```csharp
// Get the current profile immediately (returns null if none available yet)
var currentProfile = await sdk.GetProfileResponseAsync(TimeSpan.Zero);

// Wait indefinitely for the next profile update
var nextProfile = await sdk.GetProfileResponseAsync();
```

## Configuration

### Logging

The SDK includes a simple logging mechanism that can be enabled or disabled through the constructor:

```csharp
// Enable logging
var sdk = new FxidClientSDK(enableLogging: true);

// Disable logging
var sdk = new FxidClientSDK(enableLogging: false);
```

### Connection String

The connection string should be passed as a command-line argument:

```bash
dotnet run --fxid "https://api.example.com/fxid/profile"
```

The SDK will automatically extract this parameter during initialization.

## Protocol Buffer Model

The SDK uses Protocol Buffers for efficient data serialization. The key message types are:

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

## Error Handling

The SDK includes robust error handling for network issues, protocol buffer parsing, and more. Most errors are logged when logging is enabled.

Common exceptions:

- `ArgumentException`: When connection string is not found
- `InvalidOperationException`: When methods are called before initialization
- `HttpRequestException`: When network requests fail
- `OperationCanceledException`: When operations are canceled

## Requirements

- .NET 6.0 or higher
- C# 9.0 or higher

## License

[Specify your license here]

## Contributing

[Your contribution guidelines]