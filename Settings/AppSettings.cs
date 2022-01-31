namespace KeyGenerationService.Settings
{
    public class AppSettings
    {
        public string DefaultConnection { get; set; }
        public string RedisConnection { get; set; }
        public string CacheKey { get; set; }
        public int KeysToGenerateOnEmptyCache { get; set; }
        public int MaxKeysInCache { get; set; }
    }
}