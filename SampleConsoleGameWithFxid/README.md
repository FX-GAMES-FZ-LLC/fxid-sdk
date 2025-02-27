# FXID SDK Integration

This README provides comprehensive documentation for integrating and using the FXID Client SDK in your application.

## Overview

The FXID SDK enables applications to manage user profiles, authentication, feature access, and other services through a simple client-side integration. The SDK handles communication with the FXID Profile Service and provides access to various features including login management, store access, announcements, user tags, statistics, remote configuration, maintenance information, and update management.

## Quick Start

### Installation

Add the FXID Client SDK to your project:

```csharp
// Via NuGet Package Manager
Install-Package FxidClientSDK

// Or via .NET CLI
dotnet add package FxidClientSDK
```

### Basic Usage

Here's a simple example of initializing the SDK and retrieving a user profile:

```csharp
using FxidClientSDK.SDK;

// Initialize the SDK
// The boolean parameter indicates debug mode (true for development, false for production)
var sdk = new FxidClientSDK(false);
sdk.Initialize();

// Get the user profile
var profileResponse = await sdk.GetProfileResponseAsync();

// You can now use the profile data
string userToken = profileResponse.User.Token;
CheckOnServer(userToken);
DisplayAnnouncements(profileResponse.Features.Announce.Items);
```

## SDK Features

The FXID SDK provides access to several features through the ProfileResponse object:

### User Information

Access basic user information:

```csharp
var user = profileResponse.User;
long fxid = user.Fxid;
string login = user.Login;
string token = user.Token;
```

### Features

The SDK provides access to various feature sets:

#### Login Feature

```csharp
var loginFeature = profileResponse.Features.Login;
string loginUrl = loginFeature.Url.Address;
var connectedProviders = loginFeature.Connected;
```

#### Store Feature

```csharp
var storeFeature = profileResponse.Features.Store;
string storeUrl = storeFeature.Url.Address;
var products = storeFeature.Products;

// Example: Access specific product
if (products.TryGetValue("productId", out var product))
{
    string price = product.Price;
    string currency = product.Currency;
}
```

#### Announcements

```csharp
var announceFeature = profileResponse.Features.Announce;
var announcements = announceFeature.Items;

foreach (var announcement in announcements)
{
    DisplayAnnouncement(
        announcement.Title,
        announcement.MarkdownText,
        announcement.Url.Address,
        announcement.Flash
    );
    
    // Mark as seen (if needed)
    if (!announcement.Seen)
    {
        // Call the set_seen_url
        await MarkAnnouncementAsSeen(announceFeature.SetSeenUrl.Address);
    }
}
```

#### User Tags

```csharp
var tagsFeature = profileResponse.Features.Tags;
var tags = tagsFeature.Tags;

// Access specific tag
if (tags.TryGetValue("tagName", out var tag))
{
    string displayName = tag.DisplayName;
    string value = tag.Value;
    long counter = tag.Counter;
}
```

#### Statistics

```csharp
var statsFeature = profileResponse.Features.Stats;
string bulkApiUrl = statsFeature.BulkApi.Address;
string getApiUrl = statsFeature.GetApi.Address;
string sentryDomain = statsFeature.SentryDomain;

// Example usage
await SendStatistics(bulkApiUrl, eventData);
```

#### Remote Configuration

```csharp
var configFeature = profileResponse.Features.Config;
long lastUpdateTimestamp = configFeature.UpdateAt;
string abTestGroup = configFeature.AbTestGroup;

foreach (var configValue in configFeature.Values)
{
    string key = configValue.Key;
    
    // Depending on type, access the appropriate property
    if (!string.IsNullOrEmpty(configValue.String))
    {
        ApplyStringConfig(key, configValue.String);
    }
    else if (!string.IsNullOrEmpty(configValue.Json))
    {
        ApplyJsonConfig(key, configValue.Json);
    }
    else
    {
        ApplyIntConfig(key, configValue.Int);
    }
}
```

#### Maintenance Information

```csharp
var maintenanceFeature = profileResponse.Features.Maintenance;
if (maintenanceFeature.MaintenanceMode)
{
    DisplayMaintenanceInfo(
        startTime: maintenanceFeature.StartAt,
        endTime: maintenanceFeature.EndsAt,
        reason: maintenanceFeature.Reason,
        infoUrl: maintenanceFeature.InfoPage.Address,
        isBan: maintenanceFeature.IsBan
    );
}
```

#### Update Management

```csharp
var updateFeature = profileResponse.Features.Update;
long currentVersion = GetCurrentAppVersion();

if (currentVersion < updateFeature.VersionId)
{
    DisplayUpdatePrompt(
        newVersion: updateFeature.VersionName,
        whatsNewUrl: updateFeature.WhatsNew.Address,
        updateUrl: updateFeature.UpdateLink.Address
    );
}
```

## Browser Handling

The SDK defines several browser types for opening URLs:

- `INTERNAL`: Opens URLs in an internal browser within your application
- `EXTERNAL`: Opens URLs in the system's default browser
- `STEAM`: Opens URLs in the Steam browser
- `REFRESH`: Refreshes the profile on successful completion

Example usage:

```csharp
// Check the preferred browser for a URL
var refreshUrl = profileResponse.RefreshUrl;
BrowserType browserType = refreshUrl.PreferredBrowser;

switch (browserType)
{
    case BrowserType.INTERNAL:
        OpenInternalBrowser(refreshUrl.Address);
        break;
    case BrowserType.EXTERNAL:
        OpenExternalBrowser(refreshUrl.Address);
        break;
    case BrowserType.STEAM:
        OpenSteamBrowser(refreshUrl.Address);
        break;
    case BrowserType.REFRESH:
        await RefreshProfile();
        break;
}
```

## Profile Refresh

To refresh the profile data:

```csharp
// Option 1: Using the refresh URL
var refreshUrl = profileResponse.RefreshUrl;
if (refreshUrl != null)
{
    await OpenUrlBasedOnType(refreshUrl);
}

// Option 2: Force a refresh directly
profileResponse = await sdk.GetProfileResponseAsync(forceRefresh: true);
```

## Error Handling

Implement proper error handling for network-related issues:

```csharp
try
{
    var profileResponse = await sdk.GetProfileResponseAsync();
    // Process the response
}
catch (FxidException ex)
{
    // Handle SDK-specific exceptions
    LogError("FXID SDK error: " + ex.Message);
}
catch (Exception ex)
{
    // Handle other exceptions
    LogError("Error while communicating with FXID service: " + ex.Message);
}
```

## Advanced Usage

### Custom Implementation

You can implement custom handling for various SDK events:

```csharp
// Create a custom handler
public class MyFxidHandler : IFxidHandler
{
    public void OnProfileUpdated(ProfileResponse profile)
    {
        // Custom profile update handling
    }
    
    public void OnMaintenanceDetected(MaintenanceFeature maintenance)
    {
        // Custom maintenance handling
    }
    
    public void OnUpdateRequired(UpdateFeature update)
    {
        // Custom update handling
    }
}

// Register the handler
sdk.SetHandler(new MyFxidHandler());
```

## Troubleshooting

Common issues and solutions:

1. **Authentication Failures**: Ensure the game access token is valid and not expired.

2. **Connection Issues**: Check network connectivity and firewall settings.

3. **Profile Data Not Updating**: Verify that the profile expiration timestamp hasn't passed, and force a refresh if necessary.

4. **Browser Integration Problems**: Implement proper URL handling for each browser type.

## API Reference

For detailed information about the API, refer to the Proto definition file or the SDK documentation.

## License

[License information]

## Contact

For support or questions, contact [support contact information].