using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TumblrExport
{
    public class PosterImage
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class MediaDescription
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("has_original_dimensions")]
        public bool HasOriginalDimensions { get; set; }
    }

    /// <summary>
    /// Content Block Base Class
    /// </summary>
    [JsonConverter(typeof(ContentConverter))]
    abstract public class ContentBlock
    {
        [JsonProperty("type")]
        protected string Type { get; set; }

        public List<MediaToCopy> CopyList = new List<MediaToCopy>();

        public abstract string Process(string postId, string mediaUriOffset = null);
    }


    /// <summary>
    /// Text Content Block
    /// </summary>
    public sealed class TextContent : ContentBlock
    {
        public class InlineFormat
        {
            [JsonProperty("start")]
            public int Start { get; set; }

            [JsonProperty("end")]
            public int End { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; } // format type

            [JsonProperty("hex")]
            public string Hex { get; set; } // color code
        }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("subtype")]
        private string BlockFormat { get; set; }

        [JsonProperty("formatting")]
        private List<InlineFormat> Formatting { get; set; }

        public override string Process(string id, string mediaUriOffset)
        {
            return Text.ToMarkDown(false, BlockFormat, Formatting);
        }
    }

    /// <summary>
    /// Image Content Block
    /// </summary>
    public sealed class ImageContent : ContentBlock
    {
        [JsonProperty("media")]
        private List<MediaDescription> Media { get; set; }

        public override string Process(string id, string mediaUriOffset)
        {
            string markdown = "";
            // Find largest image, if we need multiple image versions for a responsive blog we'll create them in the Static Site Generator
            MediaDescription m = Media.FirstOrDefault(n => n.HasOriginalDimensions == true);
            if (m == null)
            {
                m = Media.OrderByDescending(n => n.Width).FirstOrDefault();
            }
            string filename = $"{id}.{m.Url.GetFileType()}";
            CopyList.Add(new MediaToCopy(m.Url, filename));

            markdown = $"{{{{<figure src=\"{(mediaUriOffset ?? "")}{filename}\" caption=\"\" >}}}}\n";
            return markdown;
        }
    }

    /// <summary>
    /// Link Content Block
    /// </summary>
    public sealed class LinkContent : ContentBlock
    {
        [JsonProperty("title")]
        private string Title { get; set; }

        [JsonProperty("url")]
        private string Url { get; set; }

        public override string Process(string id, string mediaUriOffset)
        {
            string title = string.IsNullOrEmpty(Title) ? Url : Title;
            title = title.Trim();
            string markdown = $"[{title}]({Url})\n";
            return markdown;
        }
    }

    /// <summary>
    /// Video Content Block
    /// </summary>
    public sealed class VideoContent : ContentBlock
    {
        private class SourceMetadata
        {
            [JsonProperty("id")]
            public string Id { get; set; }
        }
        private class SourceAttribution
        {
            [JsonProperty("display_text")]
            public string DisplayText { get; set; }
        }

        [JsonProperty("provider")]
        private string Provider { get; set; }

        [JsonProperty("metadata")]
        private SourceMetadata Metadata { get; set; }

        [JsonProperty("attribution")]
        private SourceAttribution Attribution { get; set; }

        [JsonProperty("url")]
        private string Url { get; set; }

        [JsonProperty("media")]
        private MediaDescription Media { get; set; }

        public override string Process(string id, string mediaUriOffset)
        {
            string markdown;
            switch (Provider)
            {
                case "youtube":
                    {
                        var title = Attribution.DisplayText.Replace("\"", "");
                        markdown = $"{{{{<youtube id=\"{Metadata.Id}\" title=\"{title}\" >}}}}\n";
                    }
                    break;
                case "vimeo":
                    {
                        var title = Attribution.DisplayText.Replace("\"", "");
                        markdown = $"{{{{<vimeo id=\"{Url.Split('/').Last()}\" title=\"{title}\" >}}}}\n";
                    }
                    break;

                case null: //tumblr video
                    {
                        // Copy over the video file
                        string url = Media.Url;
                        string filename = $"{id}.{url.GetFileType()}";
                        CopyList.Add(new MediaToCopy(url, filename));
                        markdown = $"{{{{<video src=\"{(mediaUriOffset ?? "")}{filename}\" type=\"{Media.Type}\" >}}}}\n";
                    }
                    break;
                default:
                    {
                        markdown = $"Video provider unknown- {Provider}\n";
                    }
                    break;
            }
            return markdown;
        }
    }

    /// <summary>
    /// Audio Content Block
    /// </summary>
    public sealed class AudioContent : ContentBlock
    {
        [JsonProperty("provider")]
        private string Provider { get; set; }

        [JsonProperty("embed_url")]
        private string EmbedUrl { get; set; }

        [JsonProperty("poster")]
        private List<PosterImage> Poster { get; set; }

        [JsonProperty("media")]
        private MediaDescription Media { get; set; }

        [JsonProperty("title")]
        private string Title { get; set; }

        [JsonProperty("artist")]
        private string Artist { get; set; }

        public override string Process(string id, string mediaUriOffset)
        {
            string markdown;
            switch (Provider)
            {
                case "spotify":
                    {
                        markdown = $"{{{{<embedded_audio src=\"{EmbedUrl}\" class=\"spotify_audio_player\" >}}}}\n";
                    }
                    break;
                case "soundcloud":
                    {
                        markdown = $"{{{{<embedded_audio src=\"{EmbedUrl}\" class=\"soundcloud_audio_player\" >}}}}\n";
                    }
                    break;
                case null:
                    {
                        // Copy over the poster image
                        string posterUrl = Poster.FirstOrDefault().Url;
                        string filename = $"{id}.{posterUrl.GetFileType()}";
                        CopyList.Add(new MediaToCopy(posterUrl, filename));
                        // Copy over the audio file
                        string url = Media.Url;
                        filename = $"{id}.{url.GetFileType()}";
                        CopyList.Add(new MediaToCopy(url, filename));

                        markdown = $"{{{{<audio src=\"{(mediaUriOffset ?? "")}{filename}\" type=\"{Media.Type}\" poster=\"{filename}\" caption=\"{Artist} - {Title}\">}}}}\n";
                    }
                    break;
                default:
                    {
                        markdown = $"Audio provider unknown- {Provider}";
                    }
                    break;
            }
            return markdown + "\n";
        }
    }

    /// <summary>
    /// Unsupported Content Block
    /// </summary>
    public class UnknownContent : ContentBlock
    {
        public override string Process(string id, string mediaUriOffset)
        {
            return $"Content type unknown- {Type}";
        }
    }

    /// <summary>
    /// Factory to create correct derived type for content Block
    /// </summary>
    public class ContentConverter : JsonCreationConverter<ContentBlock>
    {
        protected override ContentBlock Create(Type objectType, JObject jObject)
        {
            string type = (string)jObject.GetValue("type");

            return type switch
            {
                "video" => new VideoContent(),
                "text" => new TextContent(),
                "image" => new ImageContent(),
                "audio" => new AudioContent(),
                "link" => new LinkContent(),
                _ => new UnknownContent(),
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}