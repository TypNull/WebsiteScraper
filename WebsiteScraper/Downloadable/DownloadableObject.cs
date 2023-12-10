using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WebsiteScraper.WebsiteUtilities;

namespace WebsiteScraper.Downloadable
{
    public enum Status
    {
        Ongoing = 1,
        Completed = 2,
        Onhold = 3,
        None = 0
    }

    public abstract class DownloadableObject : IDownloadable<DownloadableObject>
    {
        public string Url { get; init; }

        protected DownloadableObject(string url) => Url = url;

        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// Notifies the Property Changed Event to update the GUI 
        /// </summary>
        /// <param name="name">Name of the Property</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        /// <summary>
        /// Sets the Field value of an Property with Field and update the GUI
        /// </summary>
        /// <typeparam name="T">Class of Property and Field</typeparam>
        /// <param name="field">Reference to the Field</param>
        /// <param name="value">Value to set</param>
        /// <param name="propertyName">Name of the Property</param>
        /// <returns>An boolean that retruns false if Field and Property are equal</returns>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public static Status StringToStatus(string found)
        {
            if (found == null || found == string.Empty)
                return Status.None;
            found = found.ToLower().Trim();
            if (new Regex("(on.?going)").IsMatch(found))
                return Status.Ongoing;
            else if (new Regex("(completed)").IsMatch(found))
                return Status.Completed;
            else if (new Regex("(on.?hold)").IsMatch(found))
                return Status.Onhold;
            return Status.None;
        }

        public static DownloadableObject Create(string url, Website website) => throw new NotImplementedException();


        public static string RemoveHTML(string searchableText) => Regex.Replace(searchableText, "<.*?>", " ");

        /// <summary>
        /// Converts a String to a DateTime Object
        /// </summary>
        /// <param name="value">input string</param>
        /// <param name="format">format of DateTime in string</param>
        /// <returns>DateTime Object</returns>
        public static DateTime StringToDateTime(string value, string? format)
        {
            TryStringToDateTime(value, format, out DateTime result);
            return result;
        }

        /// <summary>
        /// Converts a String to a DateTime Object
        /// </summary>
        /// <param name="value">input string</param>
        /// <param name="format">format of DateTime in string</param>
        /// <returns>bool that indicates sucess</returns>
        public static bool TryStringToDateTime(string value, string? format, out DateTime dateTime)
        {
            dateTime = default;
            if (value == null || value == string.Empty)
                return false;
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return true;
            }
            else if (DateTime.TryParse(value, out dateTime))
            {
                return true;
            }
            else if (new Regex(@"\d+\s?((min(ute)?(s)?)|(hour(s)?)|(day(s)?))\s?ago").IsMatch(value))
            {
                dateTime = default;
                Match matchM = new Regex(@"(\d+)\s?(min(ute)?(s)?)\s?ago").Match(value);
                Match matchH = new Regex(@"(\d+)\s?(hour(s)?)\s?ago").Match(value);
                Match matchD = new Regex(@"(\d+)\s?(day(s)?)\s?ago").Match(value);
                try
                {
                    if (matchM.Value != string.Empty)
                        dateTime = DateTime.Now.AddMinutes(-int.Parse(matchM.Groups[1].Value));
                    else if (matchD.Value != string.Empty)
                        dateTime = DateTime.Now.AddDays(-int.Parse(matchD.Groups[1].Value));
                    else if (matchH.Value != string.Empty)
                        dateTime = DateTime.Now.AddHours(-int.Parse(matchH.Groups[1].Value));
                    else
                        return false;
                    return true;
                }
                catch (Exception)
                {
                    dateTime = default;
                    return false;
                }
            }
            dateTime = default;
            return false;
        }

        public abstract Task UpdateAsync(CancellationToken? token = default);

    }
}
