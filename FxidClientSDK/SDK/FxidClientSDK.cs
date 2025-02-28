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
    public void Initialize(string connectionString = null)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            // Existing method: Retrieve connection string from process start arguments
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--fxid");

            if (index != -1 && index < args.Length - 1)
            {
                _connectionString = args[index + 1];
            }
            else
            {
                throw new ArgumentException("Connection string not found. Please provide it using the --fxid flag or pass it directly to the Initialize method.");
            }
        }
        else
        {
            // New method: Use the provided connection string
            _connectionString = connectionString;
        }

        _logger.Log($"Connection string initialized: {_connectionString}");

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
    
    public DateTimeOffset? GetServerTime()
    {
        if (_latestProfileResponse != null && _latestProfileResponse.ServerTimestamp > 0)
        {
            // Get the profile timestamp as DateTimeOffset
            DateTimeOffset profileTimestamp = DateTimeOffset.FromUnixTimeSeconds(_latestProfileResponse.ServerTimestamp);
            
            TimeSpan elapsed = DateTimeOffset.UtcNow - profileTimestamp;
            
            // Add the elapsed time to the original server timestamp to get the current server time
            return profileTimestamp.Add(elapsed);
        }
        
        _logger.Log("Server time is not available yet. Wait for profile response to be fetched.");
        return null;
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
                           
                            var contentType = response.Content.Headers.ContentType?.MediaType;
                           
                            response.EnsureSuccessStatusCode();

                            byte[] protobufData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                           
                            if (protobufData.Length == 0)
                            {
                                throw new InvalidOperationException("Received empty data");
                            }

                            if (contentType != "application/x-protobuf")
                            {
                                string content = System.Text.Encoding.UTF8.GetString(protobufData);
                            }

                            ProfileResponse profileResponse = ProfileResponse.Parser.ParseFrom(protobufData);
                           
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
                            }

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