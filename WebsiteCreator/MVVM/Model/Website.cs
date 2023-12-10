using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using WebsiteCreator.MVVM.ViewModel;

namespace WebsiteCreator.MVVM.Model
{
    internal class Website
    {
        public Dictionary<string, Dictionary<string, string>>? InputDictionary { get; set; }
        public string? Name { get; set; }
        public string? Suffix { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? HexColor { get; set; }
        public string? SearchString { get; set; }
        public string? DirectSearchString { get; set; }

        public char? Seperator { get; set; }
        public string[]? StatusSearch { get; set; }
        public string[]? TypeSearch { get; set; }
        public string[]? AuthorSearch { get; set; }
        public byte[]? Image { get; set; }
    }

    internal class NewWebsite
    {
        public string? Name { get; set; }
        public string? Suffix { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Hexcolor { get; set; }
        public string? SearchPattern { get; set; }
        public string? Seperator { get; set; }

        public EnableAbleTag[]? EnableTags { get; set; }
        public DisableAbleTag[]? DisableTags { get; set; }
        public TextTag[]? TextTags { get; set; }
        public RadioTag[]? RadioTags { get; set; }
        public Hashtable Map { get; set; } = new Hashtable();
        public byte[]? Logo { get; set; }

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
