using System.Collections.Concurrent;

namespace TraderBot;

// TODO: Move to Platform.Collections (later the proposal to .NET should be added based on this method in order to make it thread-safe)
public static class ConcurrentDictionaryExtensions
{
    public static bool TryUpdateOrRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue, TValue> updateValueFactory, Func<TKey, TValue, bool> removeCondition)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            value = updateValueFactory(key, value);
            if (removeCondition(key, value))
            {
                return dictionary.TryRemove(key, out value);
            }
            else
            {
                dictionary[key] = value;
                return true;
            }
        }
        return false;
    }
}