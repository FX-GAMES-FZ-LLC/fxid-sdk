using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using FxidProfile;

namespace FxidClientSDK.SDK;

public class FxidClientSDK
{
    private string _connectionString;
    private Task _updateTask;
    private CancellationTokenSource _cancellationTokenSource;
    private HttpClient _httpClient;
    private ProfileResponse _latestProfileResponse;
    private TaskCompletionSource<ProfileResponse> _profileUpdateTcs;
    private bool _isPaused;
    private ILogger _logger;

    // Constructor
    public FxidClientSDK(bool enableLogging = true)
    {
        _httpClient = new HttpClient();
        _profileUpdateTcs = new TaskCompletionSource<ProfileResponse>();
        _logger = new ConsoleLogger(enableLogging);
    }

    // Add methods for SDK functionality
    public void Initialize()
    {
        // Retrieve connection string from process start arguments
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--fxid");

        if (index != -1 && index < args.Length - 1)
        {
            _connectionString = args[index + 1];
            _logger.Log($"Connection string initialized: {_connectionString}");
        }
        else
        {
            throw new ArgumentException("Connection string not found. Please provide it using the --fxid flag.");
        }

        Connect();

        // Additional initialization logic
    }

    private void Connect()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("Connection string is not initialized. Call Initialize() first.");
        }

        // Cancel any existing task
        _cancellationTokenSource?.Cancel();
        _updateTask?.Wait();

        // Dispose of the old CancellationTokenSource and create a new one
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        // Start a new task
        _updateTask = Task.Run(() => FetchUpdatesAsync(_cancellationTokenSource.Token));

        _logger.Log("Connected and started fetching updates.");
    }

    private async Task FetchUpdatesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_isPaused)
            {
                await Task.Delay(1000, cancellationToken); // Check every second if still paused
                continue;
            }

            try
            {
                {
                    _logger.Log($"Attempting to fetch update from: {_connectionString}");
                    using (var request = new HttpRequestMessage(HttpMethod.Get, _connectionString))
                    {
                        request.Headers.Accept.Add(
                            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-protobuf"));

                        _logger.Log("Sending HTTP request...");
                        using (var response = await _httpClient.SendAsync(request, cancellationToken))
                        {
                            //_logger.Log($"Received response with status code: {response.StatusCode}");

                            // Log all response headers
                            //_logger.Log("Response Headers:");
                            // foreach (var header in response.Headers)
                            // {
                            //     _logger.Log($"{header.Key}: {string.Join(", ", header.Value)}");
                            // }

                            // Check content type
                            var contentType = response.Content.Headers.ContentType?.MediaType;
                            //Console.WriteLine($"Content-Type: {contentType}");

                            // if (contentType != "application/x-protobuf")
                            // {
                            //     Console.WriteLine(
                            //         $"Warning: Expected content type 'application/x-protobuf', but received '{contentType}'");
                            // }

                            response.EnsureSuccessStatusCode();

                            byte[] protobufData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                            //Console.WriteLine($"Received data of length: {protobufData.Length}");

                            if (protobufData.Length == 0)
                            {
                                throw new InvalidOperationException("Received empty data");
                            }

                            // If content type is not protobuf, try to log the content as string
                            if (contentType != "application/x-protobuf")
                            {
                                string content = System.Text.Encoding.UTF8.GetString(protobufData);
                                //Console.WriteLine($"Received content: {content}");
                            }

                            //Console.WriteLine("Attempting to parse protobuf data...");
                            ProfileResponse profileResponse = ProfileResponse.Parser.ParseFrom(protobufData);
                            //Console.WriteLine($"Successfully parsed profile response: {profileResponse}");

                            // Update the latest profile response
                            _latestProfileResponse = profileResponse;

                            // Notify any waiting tasks
                            _profileUpdateTcs.TrySetResult(profileResponse);
                            _profileUpdateTcs = new TaskCompletionSource<ProfileResponse>();

                            // Calculate delay until next fetch
                            long delayUntilNextFetch = (long)(_latestProfileResponse.ExpirationTimestamp -
                                                              DateTimeOffset.UtcNow.ToUnixTimeSeconds()) * 1000;
                            delayUntilNextFetch =
                                Math.Max(delayUntilNextFetch, 10000); // wait at least 10 seconds before fetching again

                            // Update connection string for next fetch
                            if (!string.IsNullOrEmpty(_latestProfileResponse.RefreshUrl?.Address))
                            {
                                _connectionString = _latestProfileResponse.RefreshUrl.Address;
                                //  Console.WriteLine($"Updated connection string to: {_connectionString}");
                            }

                            // Console.WriteLine($"Waiting {delayUntilNextFetch}ms before next fetch");
                            await Task.Delay((int)delayUntilNextFetch, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Task was canceled, exiting the loop");
                break;
            }
            catch (HttpRequestException ex)
            {
                _logger.Log($"HTTP request error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Log($"Inner exception: {ex.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error fetching updates: {ex.GetType().Name} - {ex.Message}");
                _logger.Log($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    public void Disconnect()
    {
        _cancellationTokenSource?.Cancel();
        _updateTask?.Wait();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _updateTask = null;

        _logger.Log("Disconnected and stopped fetching updates.");
    }

    // Add more methods as needed for your SDK functionality

    public void Dispose()
    {
        Disconnect();
        _httpClient.Dispose();
    }

    public void PauseFetch()
    {
        if (!_isPaused)
        {
            _isPaused = true;
            _logger.Log("Background fetch paused.");
        }
    }

    public void ResumeFetch()
    {
        if (_isPaused)
        {
            _isPaused = false;
            _logger.Log("Background fetch resumed.");
        }
    }

    public async Task<ProfileResponse> GetProfileResponseAsync(TimeSpan? timeout = null)
    {
        if (_latestProfileResponse != null)
        {
            return _latestProfileResponse;
        }

        if (timeout.HasValue)
        {
            using var cts = new CancellationTokenSource(timeout.Value);
            try
            {
                return await Task.WhenAny(_profileUpdateTcs.Task, Task.Delay(-1, cts.Token))
                    .ContinueWith(t => t.Result == _profileUpdateTcs.Task ? _profileUpdateTcs.Task.Result : null);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
        else
        {
            return await _profileUpdateTcs.Task;
        }
    }
}