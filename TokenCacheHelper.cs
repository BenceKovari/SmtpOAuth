using Microsoft.Identity.Client;
using System.IO;

public static class TokenCacheHelper
{
    private static readonly string CacheFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "msal_cache.bin3");

    private static readonly object FileLock = new();

    public static void EnableSerialization(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(BeforeAccessNotification);
        tokenCache.SetAfterAccess(AfterAccessNotification);
    }

    private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        lock (FileLock)
        {
            if (File.Exists(CacheFilePath))
            {
                args.TokenCache.DeserializeMsalV3(File.ReadAllBytes(CacheFilePath));
            }
        }
    }

    private static void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        // if the access operation resulted in a cache update
        if (args.HasStateChanged)
        {
            lock (FileLock)
            {
                File.WriteAllBytes(CacheFilePath, args.TokenCache.SerializeMsalV3());
            }
        }
    }
}
