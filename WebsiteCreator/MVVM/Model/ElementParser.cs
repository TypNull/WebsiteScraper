using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Creator.MVVM.Model
{
    public static class ElementParser
    {
        public static ParseMessage ParseFloat(ParseRequest request)
        {
            request.Raw = true;
            ParseMessage found = Parse(request);
            if (float.TryParse(found.Message.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out float number))
                found.Message = $"Found {request.Tag} number: {number}";
            else if (found.IsSuccess)
                found.Message = $"No {request.Tag} number found!\nOutput\"{found.Message}\"";
            return found;
        }

        public static ParseMessage ParseLink(ParseRequest request, string? baseUrl)
        {
            request.Raw = true;
            ParseMessage found = Parse(request);
            if (!found.IsSuccess)
                return found;
            if (string.IsNullOrWhiteSpace(found.Message))
                found.Message = "Nothing found!";
            else if (found.Message.StartsWith('/'))
                found.Message = baseUrl + found.Message.Remove(0, 1);
            else if (found.Message.StartsWith("../"))
                found.Message = baseUrl + found.Message.Remove(0, 3);
            bool result = Uri.TryCreate(found.Message, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
            {
                found.Message = $"No {request.Tag} link found!\nOutput\"{found.Message}\"";
            }
            return found;
        }

        public static ParseMessage ParseDate(ParseRequest request)
        {
            string format = string.Empty;
            request.Raw = true;
            if (new Regex("[[]format[]]{(.*?)}") is Regex reg && reg.Match(request.Pattern) is Match match && match.Success)
            {
                format = match.Groups[1].Value.Trim();
                request.Pattern = reg.Replace(request.Pattern, string.Empty);
            }

            ParseMessage found = ElementParser.Parse(request);
            if (new Regex(@"\d+\s?(minutes)|(hours)|(day(s)?)\s?ago").IsMatch(found.Message))
                found.Message = $"Found date: \n\"{found.Message}\"";
            else if (DateTime.TryParseExact(found.Message, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                found.Message = $"Found date: \n\"{parsedDate.Date}\"";
            else if (DateTime.TryParse(found.Message, out parsedDate))
                found.Message = $"Found date: \n\"{parsedDate.Date}\"" + (string.IsNullOrEmpty(format) ? "" : $"\n But format is not right! {format}");
            else if (!found.Message.EndsWith("!") && found.Message != "[Not set]")
                found.Message = $"No date found \nOutput:\"{found.Message}\"";
            return found;
        }

        public static ParseMessage Parse(ParseRequest request)
        {
            ParseMessage message = new();
            _ = request?.Pattern ?? throw new ArgumentNullException(nameof(request.Pattern));
            if (request.Pattern.Trim() == ".")
            {
                message.Message = "[Not Set]";
                message.IsNotSet = true;
                return message;
            }
            if (request.Pattern.Trim() == "")
            {
                message.Message = "Please set pattern!";
                message.IsNotSet = true;
                return message;
            }
            if (new Regex("[[]regex[]]{(.*?)}") is Regex reg && reg.Match(request.Pattern) is Match match && match.Success)
            {
                request.Regex = match.Groups[1].Value.Trim();
                request.Pattern = reg.Replace(request.Pattern, string.Empty);
            }

            reg = new Regex("[[]attribute[]]{(.*?)}");
            match = reg.Match(request.Pattern);
            if (match.Success)
            {
                request.BaseAttribute = match.Groups[1].Value.Trim();
                request.Pattern = reg.Replace(request.Pattern, string.Empty);
            }
            message = CssSelector(request, message);
            if (request.Regex != null)
                message = RegexMatch(request, message);

            if (request.Seperator != ' ' && message.IsSuccess)
            {
                StringBuilder messageBuilder = new(request.Raw ? "" : $"Found {message.Messages.Length} {request.Tag}\n");
                foreach (string foundItem in message.Messages)
                    messageBuilder.Append(foundItem + request.Seperator + '\n');
                message.Message = messageBuilder.ToString().Trim();
            }
            message.Message ??= string.Empty;
            return message;
        }

        private static ParseMessage RegexMatch(ParseRequest request, ParseMessage message)
        {
            try
            {
                if (request.Seperator == ' ')
                {
                    MatchCollection matchCollection = new Regex(request.Regex!, RegexOptions.IgnoreCase | RegexOptions.Multiline).Matches(message.Message);
                    if (matchCollection.Count == 0)
                    {
                        message.Message = "Nothing found!";
                        return message;
                    }
                    string firstElement = matchCollection[0].Value;
                    message.Message = (!request.Raw && request.Regex != null) ? $"Found {matchCollection.Count} matches: First found {request.Tag}:\n\"{firstElement}\"" : firstElement;
                    message.IsSuccess = true;
                }
                else
                {
                    List<string> matches = new();
                    if (message.Messages?.Length > 0)
                        foreach (string item in message.Messages)
                            matches.AddRange(new Regex(request.Regex!, RegexOptions.IgnoreCase | RegexOptions.Multiline).Matches(item).Select(x => x.Value));

                    if (matches.Count == 0)
                    {
                        message.Message = "Nothing found!";
                        return message;
                    }
                    message.Messages = matches.ToArray();
                    message.IsSuccess = true;
                }
            }
            catch (Exception)
            {
                message.IsSuccess = false;
                message.IsNotInvalid = true;
                message.Message = $"The Regex pattern: \"{request.Regex}\" is not valid!";
            }
            return message;
        }

        private static ParseMessage CssSelector(ParseRequest request, ParseMessage parse)
        {
            try
            {
                if (request.Searchable == null)
                {
                    parse.Message = "Nothing found!";
                    return parse;
                }
                IHtmlCollection<IElement> elements = request.Searchable.QuerySelectorAll(request.Pattern);

                if (elements.Length == 0)
                {
                    parse.Message = "Nothing found!";
                    return parse;
                }

                if (request.Seperator == ' ')
                {
                    parse.FoundElement = elements[0];
                    string? firstElement = request.BaseAttribute == "raw-content" ? elements[0].TextContent.Trim() : request.BaseAttribute == "html" ? elements[0].Html() : elements[0].GetAttribute(request.BaseAttribute);
                    parse.Message = ((!request.Raw && request.Regex == null) ? $"Found {elements.Length} matches: First found {request.Tag}:\n\"{firstElement}\"" : firstElement) ?? "Nothing found!";
                    parse.IsSuccess = true;
                }
                else
                {
                    List<string> found = new();
                    foreach (IElement foundItem in elements)
                    {
                        if (request.BaseAttribute == "html")
                            found.Add(foundItem.Html().Trim());
                        else if (request.BaseAttribute == "raw-content")
                            found.Add(foundItem.TextContent.Trim());
                        else if (foundItem.GetAttribute(request.BaseAttribute) is string tag)
                            found.Add(tag.Trim());
                    }
                    parse.Messages = found.ToArray();
                    parse.IsSuccess = true;
                }
            }
            catch (Exception)
            {
                parse.IsSuccess = false;
                parse.IsNotInvalid = true;
                parse.Message = $"The Css Query: \"{request.Pattern}\" is not valid!";
            }
            return parse;
        }
    }
    public class ParseMessage
    {
        public bool IsNotSet { get; set; }
        public string Message { get; set; } = string.Empty;
        public string[] Messages { get; set; } = Array.Empty<string>();
        public bool IsNotInvalid { get; set; }
        public bool IsSuccess { get; set; }
        public IElement? FoundElement { get; set; }
    }

    public class ParseRequest
    {
        public string Tag { get; set; } = string.Empty;
        public string Pattern { get; set; }
        public bool Raw { get; set; }
        public string? Regex { get; set; } = null;
        public string BaseAttribute { get; set; } = "raw-content";
        public IElement? Searchable { get; set; }
        public char Seperator { get; set; } = ' ';
        public ParseRequest(string? pattern, IElement? searchable) { Pattern = pattern ?? string.Empty; Searchable = searchable; }
    }
}
