using System;

namespace FxidClientSDK.SDK;

public interface ILogger
{
    void Log(string message);
}

public class ConsoleLogger : ILogger
{
    private readonly bool _isEnabled;

    public ConsoleLogger(bool isEnabled = true)
    {
        _isEnabled = isEnabled;
    }

    public void Log(string message)
    {
        if (_isEnabled)
        {
            Console.WriteLine(message);
        }
    }
}