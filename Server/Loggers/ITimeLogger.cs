namespace Server.Loggers
{
    public interface ITimeLogger
    {
        double CommonTime { get; }
        double ReadingTime { get; set; }
        double SolvingTime { get; set; }

        string GetLog();
    }
}