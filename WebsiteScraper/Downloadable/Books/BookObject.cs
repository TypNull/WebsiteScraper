namespace WebsiteScraper.Downloadable.Books
{
    public class BookObject : DownloadableObject
    {
        public string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CoverPath { get => _coverPath; set => SetField(ref _coverPath, value); }
        private string _coverPath = string.Empty;
        public string[] Genres { get; set; } = Array.Empty<string>();

        public BookObject(string url, string title) : base(url) => Title = title;

        public override Task UpdateAsync(CancellationToken? token = default) => throw new NotImplementedException();
    }
}
