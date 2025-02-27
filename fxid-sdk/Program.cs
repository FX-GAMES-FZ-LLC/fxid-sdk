using FxidProfile;
using System.Text.Json;
using Google.Protobuf;

Console.WriteLine("Hello, World!");

// Example of using the generated ProfileResponse class
var response = new ProfileResponse
{
    User = new User
    {
        Fxid = 123456789,
        Email = "user@example.com",
        Token = "sample_token_123"
    },
    Features = new Features
    {
       
        Store = new StoreFeature
        {
            Url = new Url { Address = "https://store.example.com", PreferredBrowser = BrowserType.Internal },
            Products = 
            {
                ["product1"] = new Product
                {
                    Currency = "USD",
                    Price = "9.99",
                    UsdPrice = "9.99",
                    Url = new Url { Address = "https://store.example.com/product1", PreferredBrowser = BrowserType.Internal },
                    Jpeg = "https://store.example.com/product1.jpg"
                }
            }
        },
        Announce = new AnnounceFeature
        {
            Items = 
            {
                new AnnounceItem
                {
                    Title = "New Feature Announcement",
                    MarkdownText = "We're excited to announce our latest feature...",
                    Url = new Url { Address = "https://blog.example.com/new-feature", PreferredBrowser = BrowserType.External },
                    Seen = false,
                    Flash = true
                }
            },
            SetSeenUrl = new Url { Address = "https://api.example.com/mark-seen", PreferredBrowser = BrowserType.Internal }
        },
        Tags = new TagsFeature
        {
            Tags = 
            {
                ["level"] = new Tag { DisplayName = "Level", Value = "10", Counter = 1 }
            }
        }
    },
    ExpirationTimestamp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
    ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    RefreshUrl = new Url { Address = "https://api.example.com/refresh", PreferredBrowser = BrowserType.Internal }
};

// Convert the response to JSON and format it
string jsonString = JsonSerializer.Serialize(response, new JsonSerializerOptions
{
    WriteIndented = true
});

Console.WriteLine("Formatted JSON response:");
Console.WriteLine(jsonString);

// Serialize to protobuf and write to file
byte[] protobufData = response.ToByteArray();
File.WriteAllBytes("example_protobuf.binary", protobufData);

Console.WriteLine("Protobuf data written to example_protobuf.binary");

