namespace EmoteService.Redis;

public interface IRedisCache {
  public Task<T> GetCacheValueAsync<T>(string key);
  public Task SetCacheValueAsync<T>(string key, T value,
                                    TimeSpan? expiry = null);
  public Task<T> GetOrSetCacheValueAsync<T>(string key,
                                            Func<Task<T>> getValueFunc,
                                            TimeSpan? expiry = null);
}
