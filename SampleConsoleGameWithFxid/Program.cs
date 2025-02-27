using FxidProfile;

class Program
{
    static int width = 10;
    static int height = 20;
    static int score = 0;
    static bool[,] grid;
    static List<int[]> currentPiece;
    static int currentX, currentY;
    static bool gameOver = false;
    private static FxidClientSDK.SDK.FxidClientSDK sdk;

    static void Main(string[] args)
    {
        sdk = new FxidClientSDK.SDK.FxidClientSDK(false);
        sdk.Initialize();

        while (true)
        {
            var profileResponse = sdk.GetProfileResponseAsync().Result;
            
            CheckOnServer(profileResponse.User.Token);

            if (profileResponse.Features.Maintenance is { MaintenanceMode: true })
            {
                ProcessMaintananceMessage(profileResponse);
                Thread.Sleep(1000);
                continue;
            }
            
            if (profileResponse.Features.Announce.Items.Count > 0 && profileResponse.Features.Announce.Items.Any(a => !a.Seen))
            {
                Console.WriteLine("Announcements:");
                foreach (var announcement in profileResponse.Features.Announce.Items.Where(a => !a.Seen))
                {
                    Console.WriteLine($"- {announcement.MarkdownText}");
                    if (!string.IsNullOrEmpty(announcement.Url.Address))
                    {
                        Console.WriteLine($"  More info: {announcement.Url.Address}");
                    }
                    Console.WriteLine();
                    announcement.Seen = true;
                    
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }

            Console.WriteLine($"Welcome to Tetris! {profileResponse.User.Login}");
            Console.WriteLine("1. Start Game");
            Console.WriteLine($"2. Shop (with {profileResponse.Features.Store.Products.Count} items)");
            Console.WriteLine("Q. Quit");
            Console.Write("Select an option: ");


            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    PlayGame();
                    break;
                case "Q":
                    return;
                case "2":
                    if (!string.IsNullOrEmpty(profileResponse.Features.Store.Url.Address))
                    {
                        OpenShopLink(profileResponse.Features.Store.Url.Address);
                    }
                    else
                    {
                        Console.WriteLine("Shop option is not available. Press any key to continue.");
                        Console.ReadKey();
                    }

                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue.");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private static void CheckOnServer(string userToken)
    {
        //throw new NotImplementedException();
    }

    private static void ProcessMaintananceMessage(ProfileResponse profileResponse)
    {
        Console.WriteLine("Maintenance Mode: ON");

        // Show time left
        if (profileResponse.Features.Maintenance.EndsAt > 0)
        {
            DateTimeOffset endsAt =
                DateTimeOffset.FromUnixTimeSeconds(profileResponse.Features.Maintenance.EndsAt);
            TimeSpan timeLeft = endsAt - DateTimeOffset.UtcNow;
            if (timeLeft > TimeSpan.Zero)
            {
                Console.WriteLine(
                    $"Maintenance ends in: {timeLeft.Days}d, {timeLeft.Hours}h, {timeLeft.Minutes}m");
            }

            {
                Console.WriteLine("Maintenance should end soon.");
            }
        }
        else
        {
            Console.WriteLine("Maintenance end time not specified.");
        }

        // Show links
        if (!string.IsNullOrEmpty(profileResponse.Features.Maintenance.InfoPage.Address))
        {
            Console.WriteLine(
                $"For more information, visit: {profileResponse.Features.Maintenance.InfoPage.Address}");
        }

        if (profileResponse.Features.Maintenance.IsBan)
        {
            Console.WriteLine(
                $"Sorry - you are banned from accessing this game. {profileResponse.Features.Maintenance.Reason}");
        }
    }

    static void OpenShopLink(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Console.WriteLine("Opening shop in your default browser. Press any key to continue.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open the shop link: {ex.Message}");
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }
    }

    static void PlayGame()
    {
        InitializeGame();

        while (!gameOver)
        {
            for (int i = 0; i < 40; i++)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    HandleInput(key);
                    Draw();

                    if (key == ConsoleKey.Q)
                    {
                        gameOver = true;
                        break;
                    }
                }

                Thread.Sleep(5);
            }

            Update();
            Draw();
        }

        Console.Clear();
        Console.WriteLine($"Game Over! Your score: {score}");
        Console.WriteLine("Press any key to return to the main menu.");
        Console.ReadKey();
    }

    static void InitializeGame()
    {
        grid = new bool[height, width];
        score = 0;
        gameOver = false;
        SpawnNewPiece();
    }

    static void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.LeftArrow:
                if (CanMove(-1, 0)) currentX--;
                break;
            case ConsoleKey.RightArrow:
                if (CanMove(1, 0)) currentX++;
                break;
            case ConsoleKey.DownArrow:
                if (CanMove(0, 1)) currentY++;
                break;
            case ConsoleKey.Spacebar:
                RotatePiece();
                break;
        }
    }

    static void Update()
    {
        if (CanMove(0, 1))
        {
            currentY++;
        }
        else
        {
            PlacePiece();
            ClearLines();
            SpawnNewPiece();
            if (!CanMove(0, 0))
            {
                gameOver = true;
            }
        }
    }

    static void Draw()
    {
        Console.Clear();
        Console.WriteLine($"Score: {score}");
        Console.WriteLine("Q: Quit");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[y, x] || currentPiece.Any(p => p[0] + currentX == x && p[1] + currentY == y))
                {
                    Console.Write("[]");
                }
                else
                {
                    Console.Write("  ");
                }
            }

            Console.WriteLine();
        }
    }

    static void SpawnNewPiece()
    {
        currentPiece = GetRandomPiece();
        currentX = width / 2 - 1;
        currentY = 0;
    }

    static Random random = new Random();
    
    static List<int[]> GetRandomPiece()
    {
        var pieces = new List<List<int[]>>
        {
            new List<int[]> { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 } }, // Square
            new List<int[]> { new[] { 0, 0 }, new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 } }, // I
            new List<int[]> { new[] { 0, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { 2, 1 } }, // L
            new List<int[]> { new[] { 0, 1 }, new[] { 1, 1 }, new[] { 2, 1 }, new[] { 2, 0 } }, // J
            new List<int[]> { new[] { 0, 1 }, new[] { 1, 1 }, new[] { 1, 0 }, new[] { 2, 0 } }, // S
            new List<int[]> { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 2, 1 } }, // Z
            new List<int[]> { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 2, 1 } } // T
        };
    
        return pieces[random.Next(pieces.Count)];
    }

    static bool CanMove(int dx, int dy)
    {
        return currentPiece.All(p =>
        {
            int newX = p[0] + currentX + dx;
            int newY = p[1] + currentY + dy;
            return newX >= 0 && newX < width && newY < height && (newY < 0 || !grid[newY, newX]);
        });
    }

    static void PlacePiece()
    {
        foreach (var p in currentPiece)
        {
            grid[p[1] + currentY, p[0] + currentX] = true;
        }
    }

    static void ClearLines()
    {
        for (int y = height - 1; y >= 0; y--)
        {
            if (Enumerable.Range(0, width).All(x => grid[y, x]))
            {
                for (int y2 = y; y2 > 0; y2--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        grid[y2, x] = grid[y2 - 1, x];
                    }
                }

                score += 100;
                y++;
            }
        }
    }

    static void RotatePiece()
    {
        var rotated = currentPiece.Select(p => new[] { p[1], -p[0] }).ToList();
        if (rotated.All(p =>
                p[0] + currentX >= 0 && p[0] + currentX < width && p[1] + currentY < height &&
                (p[1] + currentY < 0 || !grid[p[1] + currentY, p[0] + currentX])))
        {
            currentPiece = rotated;
        }
    }
}