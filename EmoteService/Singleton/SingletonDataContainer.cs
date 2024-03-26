using EmoteService.Models;

namespace EmoteService.Singleton;

public class SingletonDataContainer : ISingletonInterface
{
    private List<Error> errorlist = new();

    public SingletonDataContainer()
    {
        string jsonData = File.ReadAllText("data/errorlist.json");
        var tempData = System.Text.Json.JsonSerializer.Deserialize<List<Error>>(jsonData);

        if (tempData != null)
            errorlist = tempData;
    }

    public List<Error> GetErrors()
    {
        return errorlist;
    }

    private static Lazy<SingletonDataContainer> instance = new Lazy<SingletonDataContainer>(
        () => new SingletonDataContainer()
    );

    public static SingletonDataContainer Instance => instance.Value;
}
