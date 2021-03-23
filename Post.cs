using LinqKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TumblrExport
{
    /// <summary>
    /// Tracks media file needed to support post
    /// </summary>
    public sealed class MediaToCopy
    {
        public string From { get; }

        public string To { get; }

        public MediaToCopy(string from, string to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// Tumblr Post
    /// </summary>
    public sealed class Post
    {
        private sealed class Blog
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }
        }
        private sealed class ReblogTrail
        {
            [JsonProperty("content")]
            public List<ContentBlock> Content { get; set; }

            [JsonProperty("blog")]
            public Blog Blog { get; set; }
        }

        [JsonProperty("original_type")]
        private string OriginalType { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("slug")]
        private string Slug { get; set; }

        [JsonProperty("date")]
        private string Date { get; set; }

        [JsonProperty("state")]
        private string State { get; set; }

        [JsonProperty("tags")]
        private List<string> Tags { get; set; }

        [JsonProperty("summary")]
        private string Summary { get; set; }

        [JsonProperty("content")]
        private List<ContentBlock> Content { get; set; }

        [JsonProperty("trail")]
        private List<ReblogTrail> Trail { get; set; }

        public List<MediaToCopy> CopyList => _copyList;

        private readonly Dictionary<string, string> _hugoFrontMatter = new Dictionary<string, string>();
        private string _markdown;
        private string _title;

        private readonly List<MediaToCopy> _copyList = new List<MediaToCopy>();

        public string PostName { get; set; }

        public void Process(string relativeMediaPath = null)
        {
            PostName = Id + (string.IsNullOrEmpty(Slug) ? null : $"---{Slug}");

            // Most Hugo themes work best if there's a title in the frontmatter
            // Tumblr doesn't always have a title for a post, so we try to create one from the post
            if (string.IsNullOrWhiteSpace(_title))
            {
                if (!string.IsNullOrWhiteSpace(Summary))
                {
                    _title = Summary.GetFirstTextPhrase();

                }
            }
            if (string.IsNullOrWhiteSpace(_title))
            {
                var m = (TextContent)Content.FirstOrDefault(n => n.GetType() == typeof(TextContent));
                if (m != null)
                {
                    _title = m.Text.GetFirstTextPhrase();
                }
            }

            if (string.IsNullOrWhiteSpace(_title))
            {
                _title = "...";
            }
            else
            {
                _title = _title.EscapeYAML();
            }


            // Process all the content blocks
            int count = 1;
            foreach (var content in Content)
            {
                _markdown += content.Process($"{Id}_{count++}", relativeMediaPath);
                _copyList.AddRange(content.CopyList);
            }

            // Process all the reblogged content blocks
            if (Trail.Count != 0)
            {
                foreach (var trail in Trail)
                {
                    foreach (var content in trail.Content)
                    {
                        _markdown += content.Process($"{Id}_{count++}", relativeMediaPath);
                        _copyList.AddRange(content.CopyList);
                    }
                    var rebloggedFrom = Trail.FirstOrDefault().Blog;
                    if (rebloggedFrom != null)
                    {
                        _markdown += $">Reblogged from [{rebloggedFrom.Name}]({rebloggedFrom.Url})";
                    }
                }
            }

            // Build the list of Hugo Frontmatter
            DateTime date = DateTime.Parse(Date, CultureInfo.InvariantCulture);
            _hugoFrontMatter.TryAdd("id", Id.ToString());
            _hugoFrontMatter.TryAdd("date", date.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            _hugoFrontMatter.TryAdd("categories", $"[\"{OriginalType}\"]");
            _hugoFrontMatter.TryAdd("draft", (State == "draft" || State == "private") ? "true" : "false");
            _hugoFrontMatter.TryAdd("title", $"\"{_title}\"");
            _hugoFrontMatter.TryAdd("reblog", Trail.Count != 0 ? "true" : "false");
            if (Tags.Count > 0)
            {
                var cleanTags = new List<string>();
                foreach (var t in Tags)
                {
                    string tag = $"\"{t}\"";
                    cleanTags.Add(tag);
                }
                _hugoFrontMatter.TryAdd("tags", $"[{string.Join(",", cleanTags)}]");
            }
        }

        /// <summary>
        /// Convert  all the frontmatter items to YAML
        /// Add markdown formatted body
        /// </summary>
        public string ToHugoMarkdown()
        {
            string markdown = $"---\n";
            foreach (var p in _hugoFrontMatter)
            {
                markdown += $"{p.Key}: {p.Value}\n";
            }
            markdown += $"---\n\n{_markdown}";

            return markdown;
        }

    }

    public static class PostsExtensions
    {
        // Get post content from a json file
        public static void ReadPostsFromFile(this JArray posts, FileInfo file)
        {
            if (file != null && file.Exists)
            {
                using (StreamReader stream = file.OpenText())
                {
                    JsonSerializer serializer = new JsonSerializer();
                    posts.Merge((JArray)serializer.Deserialize(stream, typeof(JArray)));
                }
            }
        }

        // Write posts content to a json file
        public static void WritePostsToFile(this JArray posts, FileInfo file)
        {
            if (file != null)
            {
                if (file.Exists)
                {
                    file.Delete();
                }
                using (var stream = file.CreateText())
                {
                    stream.WriteLine(posts.ToString());
                }
            }
        }

        // Filter post collection on criteria set
        public static JArray FilterPosts(this JArray posts, DateTime since, bool noReblogs, bool publishedPosts, bool privatePosts, bool draftPosts, bool queuedPosts)
        {
            var predicate = PredicateBuilder.New<JToken>();

            if (noReblogs)
            {
                predicate = predicate.And(p => p["content"].Value<JArray>().Count != 0 && p["trail"].Value<JArray>().Count == 0);
            }

            if (since != null)
            {
                predicate = predicate.And(p => p["date"].Value<DateTime>() >= since);
            }

            var postStatePredicate = PredicateBuilder.New<JToken>();
            if (draftPosts)
            {
                postStatePredicate = postStatePredicate.Or(p => p["state"].Value<string>() == "draft");
            }
            if (privatePosts)
            {
                postStatePredicate = postStatePredicate.Or(p => p["state"].Value<string>() == "private");
            }
            if (queuedPosts)
            {
                postStatePredicate = postStatePredicate.Or(p => p["state"].Value<string>() == "queued");
            }
            if (publishedPosts)
            {
                postStatePredicate = postStatePredicate.Or(p => p["state"].Value<string>() == "published");
            }
            predicate = predicate.And(postStatePredicate);

            var f = posts.Where(predicate);
            return new JArray(f);
        }
    }
}