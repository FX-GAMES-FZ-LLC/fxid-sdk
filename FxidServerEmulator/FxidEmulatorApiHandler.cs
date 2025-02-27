using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FxidProfile;
using Microsoft.IdentityModel.Tokens;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace fxid_server_emulator
{
    public class FxidEmulatorApiHandler
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static (byte[] responseString, int statusCode, string contentType) ValidateTokenAndGenerateResponse(
            string token, ServerOptions options, string path, bool binaryRequest, string product = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                return (Encoding.UTF8.GetBytes("Invalid or missing token"), 400, "text/plain");
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(options.JwtSecret);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                ClaimsPrincipal claimsPrincipal =
                    tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var userIdFromToken = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (path == "/launcher")
                {
                    var profileResponse = CreateRandomProfileResponseFromUserId(userIdFromToken, token, options.Port);

                    if (binaryRequest)
                    {
                        var binaryResponse = profileResponse.ToByteArray();
                        return (binaryResponse, 200, "application/x-protobuf");
                    }
                    else
                    {
                        var jsonSerializerSettings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Include,
                            DefaultValueHandling = DefaultValueHandling.Include
                        };
                        var jsonResponse = JsonConvert.SerializeObject(profileResponse, Formatting.Indented,
                            jsonSerializerSettings);
                        return (Encoding.UTF8.GetBytes(jsonResponse), 200, "application/json");
                    }
                }
                else if (path == "/buy")
                {
                    if (string.IsNullOrEmpty(product))
                    {
                        return (Encoding.UTF8.GetBytes("Product parameter is missing"), 400, "application/json");
                    }

                    var purchaseResult = DeliverProductToThirdPartyServer(userIdFromToken, product, options).Result;
                    var jsonResponse = JsonConvert.SerializeObject(purchaseResult);
                    return (Encoding.UTF8.GetBytes(jsonResponse), 200, "application/json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid token? {ex.Message}");
                return (Encoding.UTF8.GetBytes($"Invalid token: {ex.Message}"), 401, "text/plain");
            }

            return (Encoding.UTF8.GetBytes("unknown action"), 500, "text/plain");
        }


        private static async Task<JObject> DeliverProductToThirdPartyServer(string userId, string product,
            ServerOptions options)
        {
            /// @mv тут нужно добавить код имитрующий реальный пейлоад от прода

            var requestBody = new
            {
                userId = userId,
                product = product,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8,
                "application/json");

            try
            {
                var response = await httpClient.PostAsync(options.GameServerUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JObject.Parse(responseContent);
                }
                else
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["error"] =
                            $"Failed to deliver product to {options.GameServerUrl}. Status code: {response.StatusCode}",
                        ["details"] = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "Failed to deliver product",
                    ["details"] = ex.Message
                };
            }
        }

        private static ProfileResponse CreateRandomProfileResponseFromUserId(string userIdFromToken,
            string token, int port)
        {
            string userId = userIdFromToken ?? Guid.NewGuid().ToString();
            var random = new Random(userId.GetHashCode());


            var profileResponse = new ProfileResponse
            {
                User = new User
                {
                    Fxid = long.Parse(userId),
                    Login = $"user{random.Next(1000, 9999):D4}@example.com",
                    Token = token
                },
                Features = new Features
                {
                    Store = new StoreFeature
                    {
                        Url = new Url
                        {
                            Address = $"http://localhost:{port}/static/shop.html?token={token}",
                            PreferredBrowser = BrowserType.Internal
                        }
                    },
                    Announce = new AnnounceFeature
                    {
                        SetSeenUrl = new Url
                        {
                            Address = $"http://localhost:{port}/set-seen?token={token}",
                            PreferredBrowser = BrowserType.Internal
                        },
                        Items = { }
                    },
                    Tags = new TagsFeature
                    {
                        Tags = { }
                    },
                    Config = new RemoteConfigFeature
                    {
                        UpdateAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        AbTestGroup = "group_a",
                        Values =
                        {
                            new RemoteConfigValue { Key = "enable_new_feature", String = "true" },
                            new RemoteConfigValue { Key = "max_level", Int = 100 }, // max_level
                            new RemoteConfigValue { Key = "daily_bonus", Int = 50 }, // daily_bonus
                        }
                    }
                },
                ExpirationTimestamp = DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeSeconds(),
                ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                RefreshUrl = new Url
                {
                    Address = $"http://localhost:{port}/launcher?token={token}",
                    PreferredBrowser = BrowserType.Refresh
                }
            };
            // Populate the Products
            profileResponse.Features.Store.Products.Add("Premium Subscription", new Product
            {
                Currency = "USD",
                Price = "9.99",
                UsdPrice = "9.99",
                Url = new Url
                {
                    Address = $"http://localhost:{port}/static/buy.html?product=test&token={token}",
                    PreferredBrowser = BrowserType.Internal
                },
                Jpeg = "https://cdn.fxgam.es/static_assets/fxid/images/store/products/pass/icon.png"
            });

            profileResponse.Features.Store.Products.Add("Coins Pack 100", new Product
            {
                Currency = "USD",
                Price = "4.99",
                UsdPrice = "4.99",
                Url = new Url
                {
                    Address = $"http://localhost:{port}/static/buy.html?product=test&token={token}",
                    PreferredBrowser = BrowserType.Internal
                },
                Jpeg = "https://cdn.fxgam.es/static_assets/fxid/images/store/products/redmin/icon.png"
            });

            profileResponse.Features.Store.Products.Add("Character Skin Rare", new Product
            {
                Currency = "USD",
                Price = "2.99",
                UsdPrice = "2.99",
                Url = new Url
                {
                    Address = $"http://localhost:{port}/static/buy.html?product=test&token={token}",
                    PreferredBrowser = BrowserType.Internal
                },
                Jpeg = "https://cdn.fxgam.es/static_assets/fxid/images/store/products/pass/icon.png"
            });
            profileResponse.Features.Announce.Items.Add(new AnnounceItem
            {
                Title = "Daily Reward",
                MarkdownText = "Congratulations! *You've earned a daily reward!*",
                Url = new Url
                {
                    Address = $"http://localhost:{port}/static/daily.html?token={token}",
                    PreferredBrowser = BrowserType.Internal
                },
                Seen = false,
                Flash = true
            });
            return profileResponse;
        }
    }
}