using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace EmoteService.Redis;

public class RedisCache : IRedisCache
{

  private readonly IDistributedCache _cache;

  public RedisCache(IDistributedCache cache) { _cache = cache; }

  public async Task<T> GetCacheValueAsync<T>(string key)
  {
    var value = await _cache.GetStringAsync(key);
    return value == null ? default : JsonConvert.DeserializeObject<T>(value, new JsonSerializerSettings
    {
      TypeNameHandling = TypeNameHandling.Objects
    });
  }

  public async Task SetCacheValueAsync<T>(string key, T value,
                                          TimeSpan? expiry = null)
  {
    var serializedValue = JsonConvert.SerializeObject(value, new JsonSerializerSettings
    {
      TypeNameHandling = TypeNameHandling.Objects,
      TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
    });

    await _cache.SetStringAsync(
        key, serializedValue,
        new DistributedCacheEntryOptions
        {
          AbsoluteExpirationRelativeToNow =
                                               expiry
        });
  }

  public async Task<T> GetOrSetCacheValueAsync<T>(string key,
                                                  Func<Task<T>> getValueFunc,
                                                  TimeSpan? expiry = null)
  {
    var value = await _cache.GetStringAsync(key);
    if (value != null)
    {
      return JsonConvert.DeserializeObject<T>(value, new JsonSerializerSettings
      {
        TypeNameHandling = TypeNameHandling.Objects
      });
    }

    // Value not found in cache
    var newValue = await getValueFunc();
    await SetCacheValueAsync(key, newValue, expiry);
    return newValue;
  }
}
