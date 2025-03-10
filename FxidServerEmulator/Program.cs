﻿using System.Net;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Diagnostics;

namespace fxid_server_emulator
{
    public class ServerOptions
    {
        public int Port { get; set; } = 5001;
        
        public IntegrationStatusStorage status { get; set; } = new IntegrationStatusStorage();
        public string ClientBinaryPath { get; set; } = "dotnet"; // Replace with your actual client binary path
        public string ClientBinaryArgs { get; set; } = "run --project SampleConsoleGameWithFxid/SampleConsoleGameWithFxid.csproj";
        public string UserToken { get; set; }
        public string GameServerUrl { get; set; } = "http://localhost:8080";
        private string _jwtSecret = GenerateDefaultJwtSecret();
        public int UserId { get; set; } = 100;

        public string JwtSecret
        {
            get => _jwtSecret;
            set => _jwtSecret = EnsureMinimumKeyLength(value);
        }

        private static string GenerateDefaultJwtSecret()
        {
            // Return a fixed, Base64 encoded secret
            return Convert.ToBase64String(Encoding.UTF8.GetBytes("ThisIsAFixedSecretForTestingPurposesOnly"));
        }

        private static string EnsureMinimumKeyLength(string secret)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            if (keyBytes.Length >= 32)
                return secret;

            byte[] newKey = new byte[32];
            Array.Copy(keyBytes, newKey, Math.Min(keyBytes.Length, 32));
            return Convert.ToBase64String(newKey);
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var options = ParseCommandLineArgs(args);
            Console.WriteLine($"JWT SECRET: {options.JwtSecret}");

            if (options.UserId == 0)
            {
                Console.Write("Enter user_id (integer): ");
                if (!int.TryParse(Console.ReadLine(), out int userId))
                {
                    Console.WriteLine("Invalid user_id. Please enter an integer.");
                    return;
                }

                options.UserId = userId;
            }

            string jwtToken = GenerateJwtToken(options.UserId, options.JwtSecret);
            string url = $"http://localhost:{options.Port}/";

            // // Start the client binary
            // if (!string.IsNullOrEmpty(options.ClientBinaryPath))
            // {
            //     string launcherUrl = $"{url}launcher?token={jwtToken}";
            //     string processArgs = $"{options.ClientBinaryArgs} --fxid {launcherUrl}";
            //     ProcessStartInfo startInfo = new ProcessStartInfo
            //     {
            //         FileName = options.ClientBinaryPath,
            //         Arguments = processArgs,
            //         UseShellExecute = false,
            //         RedirectStandardOutput = true,
            //         RedirectStandardError = true
            //     };
            //     Process process = new Process { StartInfo = startInfo };
            //     
            //     // Set up event handlers for output and error streams
            //     process.OutputDataReceived += (sender, e) => {
            //         if (!string.IsNullOrEmpty(e.Data))
            //         {
            //             Console.WriteLine($"Client binary: {e.Data}");
            //             options.status.SetFeatureStatus(Feature.ClientBinary, e.Data,true, null);
            //         }
            //     };
            //     
            //     process.ErrorDataReceived += (sender, e) => {
            //         if (!string.IsNullOrEmpty(e.Data))
            //         {
            //             Console.WriteLine($"Client error: {e.Data}");
            //             options.status.SetFeatureStatus(Feature.ClientBinary, e.Data,false, null);
            //         }
            //     };
            //     
            //     process.Start();
            //     
            //     // Begin asynchronous reading of the output and error streams
            //     process.BeginOutputReadLine();
            //     process.BeginErrorReadLine();
            //     
            //     Console.WriteLine($"Client binary started. {options.ClientBinaryPath} {processArgs}");
            // }
            // else
            {
                Console.WriteLine(
                    $"Start client binary using the provided URL line: mygame.exe --fxid {url}launcher?token={jwtToken}");
            }
            options.UserToken = jwtToken;

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine($"Listening on {url}static/index.html");

                while (true)
                {
                   
                    HttpListenerContext context = await listener.GetContextAsync();
                    // Process the request asynchronously
                    _ = Task.Run(() => ProcessRequestAsync(context, options));
                }
            }
        }

        private static async Task ProcessRequestAsync(HttpListenerContext context, ServerOptions options)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string? token = null;
                
                // Try to get token from query string
                token = request.QueryString["token"];

                // If token is not in query string, try to get it from Authorization header
                if (string.IsNullOrEmpty(token))
                {
                    string? authHeader = request.Headers["Authorization"];
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }

                string? product = request.QueryString["product"];
                string localPath = request.Url?.LocalPath ?? string.Empty;
                string acceptHeader = request.Headers["Accept"] ?? "Not specified";
                bool isBinary = acceptHeader.Contains("application/x-protobuf");

                if (localPath.StartsWith("/static/"))
                {
                    if (localPath.StartsWith("/static/shop.html"))
                    {
                        options.status.SetFeatureStatus(Feature.ShopBuy, "Shop page accessed", true, localPath);
                    }
                    else if (localPath.StartsWith("/static/daily.html"))
                    {
                        options.status.SetFeatureStatus(Feature.DailyOffer, "DailyOffer page accessed", true, localPath);
                    }
                    else if (localPath.StartsWith("/static/buy.html"))
                    {
                        options.status.SetFeatureStatus(Feature.ShopBuy, "Buy page accessed", true, localPath);
                    }
                    await ServeStaticFile(context, localPath);
                }
                else
                {
                    var (buffer, statusCode, contentTypeResponse) =
                        FxidEmulatorApiHandler.ValidateTokenAndGenerateResponse(
                            token,
                            options,
                            localPath, isBinary
                            , product,request);

                    response.StatusCode = statusCode;
                    response.ContentType = contentTypeResponse;
                    //byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] buffer = Encoding.UTF8.GetBytes("Internal Server Error");
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        static string GenerateJwtToken(int userId, string secret)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: "FxidServerEmulator",
                audience: "fxid-client",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        static ServerOptions ParseCommandLineArgs(string[] args)
        {
            var options = new ServerOptions();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--port":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int port))
                            options.Port = port;
                        break;
                    case "--client-binary":
                        if (i + 1 < args.Length)
                            options.ClientBinaryPath = args[++i];
                        break;
                    case "--client-args":
                        if (i + 1 < args.Length)
                            options.ClientBinaryArgs = args[++i];
                        break;
                    case "--jwt-secret":
                        if (i + 1 < args.Length)
                            options.JwtSecret = args[++i];
                        break;
                    case "--game-server-url":
                        if (i + 1 < args.Length)
                            options.GameServerUrl = args[++i];
                        break;
                    case "--user-id":
                        if (i + 1 < args.Length)
                            options.UserId = Convert.ToInt32(args[++i]);
                        break;
                }
            }

            return options;
        }

        private static async Task ServeStaticFile(HttpListenerContext context, string localPath)
        {
            string staticFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static");
            string filePath = Path.Combine(staticFolderPath, localPath.Substring("/static/".Length));
            Console.WriteLine($"Serving static file: {filePath}");

            if (File.Exists(filePath))
            {
                string contentType = GetContentType(Path.GetExtension(filePath));
                context.Response.ContentType = contentType;
                context.Response.StatusCode = (int)HttpStatusCode.OK;

                using (FileStream fs = File.OpenRead(filePath))
                {
                    context.Response.ContentLength64 = fs.Length;
                    await fs.CopyToAsync(context.Response.OutputStream);
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                byte[] buffer = Encoding.UTF8.GetBytes($"File not found {filePath}");
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private static string GetContentType(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                default:
                    return "application/octet-stream";
            }
        }
    }
}