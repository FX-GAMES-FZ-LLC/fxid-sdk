using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
            string token, ServerOptions options, string path, bool binaryRequest, string product, HttpListenerRequest request)
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
                        var jsonResponse = JsonConvert.SerializeObject(
                            JsonConvert.DeserializeObject(ConvertProfileResponseToJson(profileResponse)),
                            Formatting.Indented
                        );
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
                else if (path == "/update")
                {
                    if (request.HttpMethod != "POST")
                    {
                        return (Encoding.UTF8.GetBytes("Method Not Allowed"), 405, "text/plain");
                    }

                    string requestBody;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = reader.ReadToEnd();
                    }

                    if (string.IsNullOrEmpty(requestBody))
                    {
                        return (Encoding.UTF8.GetBytes("Empty request body"), 400, "application/json");
                    }

                    try
                    {
                       

                        // Update the ProfileResponse with the new data
                        var profileResponse =  ConvertJsonToProfileResponse(requestBody);

                        // Save the updated ProfileResponse back to the file
                        SaveProfileResponseToFile(profileResponse);
                        

                        // Process the update (you can customize this part based on your needs)
                        var response = new JObject
                        {
                            ["success"] = true,
                            ["message"] = "Profile updated successfully",
                            ["updatedData"] = ConvertProfileResponseToJson(profileResponse)
                        };

                        var jsonResponse = JsonConvert.SerializeObject(
                            JsonConvert.DeserializeObject(ConvertProfileResponseToJson(profileResponse)),
                            Formatting.Indented
                        );
                        return (Encoding.UTF8.GetBytes(jsonResponse), 200, "application/json");
                    }
                    catch (JsonException ex)
                    {
                        return (Encoding.UTF8.GetBytes($"Invalid JSON: {ex.Message}"), 400, "application/json");
                    }
                }
                
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid token? {ex.Message} {ex.StackTrace}");
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

            var profileResponse = LoadProfileResponseFromFile();
            if (profileResponse == null)
            {
                profileResponse = CreateDefaultProfileResponse(userId, token, port);
            }
            else
            {
                // Update dynamic fields
                profileResponse.User.Fxid = long.Parse(userId);
                profileResponse.User.Login = $"user{random.Next(1000, 9999):D4}@example.com";
                profileResponse.User.Token = token;
                profileResponse.ExpirationTimestamp = DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeSeconds();
                profileResponse.ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                profileResponse.RefreshUrl = new Url
                {
                    Address = $"http://localhost:{port}/launcher?token={token}",
                    PreferredBrowser = BrowserType.Refresh
                };

                // Update URLs with correct port and token
                UpdateUrls(profileResponse, port, token);
            }

            return profileResponse;
        }

        private static ProfileResponse LoadProfileResponseFromFile()
        {
            string filePath = "profile_response.pb";
            try
            {
                if (File.Exists(filePath))
                {
                    using (var input = File.OpenRead(filePath))
                    {
                        return ProfileResponse.Parser.ParseFrom(input);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading ProfileResponse from file: {ex.Message}");
            }
            return null;
        }

        private static void UpdateUrls(ProfileResponse profileResponse, int port, string token)
        {
            if (profileResponse == null)
            {
                Console.WriteLine("ProfileResponse is null in UpdateUrls method.");
                return;
            }
        
            // Update Store URL
            if (profileResponse.Features?.Store?.Url != null)
            {
                profileResponse.Features.Store.Url.Address =
                    profileResponse.Features.Store.Url.Address
                        = $"http://localhost:{port}/static/shop.html?token={token}";
            }
        
            // Update Product URLs
            if (profileResponse.Features?.Store?.Products != null)
            {
                foreach (var product in profileResponse.Features.Store.Products.Values)
                {
                    if (product?.Url != null)
                    {
                        product.Url.Address = product.Url.Address
                            = $"http://localhost:{port}/static/buy.html?product={product.Sku}&token={token}";
                    }
                }
            }
        
            // Update Announce SetSeenUrl
            if (profileResponse.Features?.Announce?.SetSeenUrl != null)
            {
                profileResponse.Features.Announce.SetSeenUrl.Address = 
                    profileResponse.Features.Announce.SetSeenUrl.Address
                        = $"http://localhost:{port}/set_announce_seen?token={token}";
            }
        
            // Update Announce Item URLs
            if (profileResponse.Features?.Announce?.Items != null)
            {
                foreach (var item in profileResponse.Features.Announce.Items)
                {
                    if (item?.Url != null)
                    {
                        item.Url.Address = item.Url.Address
                            .Replace("{port}", port.ToString())
                            .Replace("{token}", token);
                    }
                }
            }
        }

        private static ProfileResponse CreateDefaultProfileResponse(string userId, string token, int port)
        {
            // Create default ProfileResponse similar to the previous implementation
            var profileResponse = new ProfileResponse
            {
                User = new User
                {
                    Fxid = long.Parse(userId),
                    Login = $"user{new Random(userId.GetHashCode()).Next(1000, 9999):D4}@example.com",
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
                        },
                        Products = 
                        {
                            ["Premium Subscription"] = new Product
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
                            },
                            // ... other products
                        }
                    },
                    // ... other features (Announce, Tags, Config)
                },
                ExpirationTimestamp = DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeSeconds(),
                ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                RefreshUrl = new Url
                {
                    Address = $"http://localhost:{port}/launcher?token={token}",
                    PreferredBrowser = BrowserType.Refresh
                }
            };

            // Save the default ProfileResponse to file
            SaveProfileResponseToFile(profileResponse);

            return profileResponse;
        }

        private static void SaveProfileResponseToFile(ProfileResponse profileResponse)
        {
            string filePath = "profile_response.pb";
            try
            {
                using (var output = File.Create(filePath))
                {
                    profileResponse.WriteTo(output);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ProfileResponse to file: {ex.Message}");
            }
        }
        private static ProfileResponse ConvertJsonToProfileResponse(string jsonString)
        {
            var jsonParser = new Google.Protobuf.JsonParser(Google.Protobuf.JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            return jsonParser.Parse<ProfileResponse>(jsonString);
        }

        private static string ConvertProfileResponseToJson(ProfileResponse profileResponse)
        {
            var jsonFormatter = new Google.Protobuf.JsonFormatter(Google.Protobuf.JsonFormatter.Settings.Default);
            return jsonFormatter.Format(profileResponse);
        }
    }
    
}