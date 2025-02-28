namespace fxid_server_emulator;

public class IntegrationStatusStorage
{
    // class FeatureStatus with log for successful and failed status events 
    public class FeatureStatusLog
    {
        public string Title { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime LogTime { get; set; }
        public string Message { get; set; }

        public FeatureStatusLog(string title, bool isSuccessful, string message)
        {
            Title = title;
            IsSuccessful = isSuccessful;
            LogTime = DateTime.Now;
            Message = message;
            Console.WriteLine($"FeatureStatusLog recorded {title}: {isSuccessful} - {message}");
        }
    }

    public class FeatureStatus
    {
        public bool IsTestSuccessful { get; set; }
        public int CountSuccess { get; set; }
        public int CountFailed { get; set; }

        public List<FeatureStatusLog> Logs { get; set; }

        public FeatureStatus()
        {
            Logs = new List<FeatureStatusLog>();
        }

        public void LogStatus(string title, bool isSuccessful, string message)
        {
            while (Logs.Count >= 20)
            {
                if (Logs[0].IsSuccessful) CountSuccess -= 1;
                else CountFailed -= 1;
                Logs.RemoveAt(0);
            }

            Logs.Add(new FeatureStatusLog(title, isSuccessful, message));
            if (isSuccessful)
                CountSuccess += 1;
            else
                CountFailed += 1;
            
            IsTestSuccessful = CountSuccess > 0 && CountFailed == 0;
        }
    }
    
    public Dictionary<Feature, FeatureStatus> featureStatuses = new Dictionary<Feature, FeatureStatus>();

    public void SetFeatureStatus(Feature featureId,string title, bool isTestSuccessful, string? message)
    {
        if (!featureStatuses.ContainsKey(featureId))
            featureStatuses[featureId] = new FeatureStatus();
        featureStatuses[featureId].LogStatus(title, isTestSuccessful, message);
    }
}