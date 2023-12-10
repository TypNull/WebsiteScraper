using System.Collections;
using System.Text.Json;

namespace WebsiteScraper.WebsiteUtilities
{
    public partial class Website
    {
        public Hashtable Map { get; init; } = new();

        public T? GetValue<T>(object key)
        {
            if (Map?.ContainsKey(key) == true)
                return ((JsonElement)Map[key]!).Deserialize<T>();
            return default;
        }

        public void SetValue<T>(object key, T value)
        {
            if (Map?.ContainsKey(key) == true)
                Map[key] = value;
            Map?.Add(key, value);
        }
    }
}
