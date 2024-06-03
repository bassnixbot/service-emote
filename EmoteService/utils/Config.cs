namespace EmoteService.Utils;

public static class Config
{
    public static Redis redis { get; set; } = new();
    public static WatchDog watchdog { get; set; } = new();
}

public class Redis
{
    public int defaultTimeout { get; set; } = 30;
    public int shortTimeout { get; set; } = 1;
    public int longTimeout { get; set; } = 60;
}

public class WatchDog
{
    public string username { get; set; } = "admin";
    public string password { get; set; } = "admin";
}
