using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TumblrExport
{
    class HugoProcessor : BaseProcessor
    {
        /// <summary>Process Blog Posts into Hugo format.</summary>
        /// <param name="options">options.</param>
        public async Task Run(HugoOptions options)
        {
            int fileFailures = 0;
            int countCopyFailures = 0;
            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();

            // If test mode set verbose to true;
            options.Verbose = options.Test || options.Verbose;

            CreateLogger("Hugo", options.Verbose);

            JArray posts = new JArray();
            if (options.JsonIn != null)
            {
                posts.ReadPostsFromFile(options.JsonIn);
            }
            else // Get posts from Tumblr API
            {
                posts = await GetPostsFromTumblr(options.Blog, options.Published, options.Drafts, options.Queued, options.Authenticate);
            }

            posts.WritePostsToFile(options.JsonOut);

            stopWatch.Stop();
            var readTime = stopWatch.Elapsed;
            stopWatch.Restart();

            // Filter posts; Tumblr API doesn't let us filter the query, so we have to read all posts & then filter 
            int initialCount = posts.Count;
            posts = posts.FilterPosts(options.Since, options.NoReblogs, options.Published, options.Restricted, options.Drafts, options.Queued);

            // Process posts
            DirectoryInfo targetMarkdownDirectory = options.Posts;
            DirectoryInfo targetMediaDirectory = options.Media;
            string relativeMediaPath = targetMarkdownDirectory.GetRelativePathTo(targetMediaDirectory);

            if (!options.Test)
            {
                targetMarkdownDirectory?.Create();
                targetMediaDirectory?.Create();
            }

            foreach (JToken jsonPost in posts)
            {
                Post post = jsonPost.ToObject<Post>();
                post.Process(relativeMediaPath);

                // Create the Markdown File - targetMarkDownDir/postname.md
                FileInfo targetMarkdownFile = new FileInfo(Path.Combine(targetMarkdownDirectory.FullName, $"{post.PostName}.md"));
                fileFailures += CreateMarkDownFile(targetMarkdownFile, post.ToHugoMarkdown(), options.Test);

                // Copy any Media files needed for this post - targetMediaDirectory/
                countCopyFailures += await CopyFiles(post.CopyList, targetMediaDirectory.FullName, options.Test);
            }

            stopWatch.Stop();
            _logger.LogInformation($"Read Time      : {readTime}");
            _logger.LogInformation($"Processing Time: {stopWatch.Elapsed}");
            if (fileFailures != 0)
            {
                _logger.LogError($"File Errors:     {fileFailures}");
            }
            if (countCopyFailures != 0)
            {
                _logger.LogError($"Copy Errors:     {countCopyFailures}");
            }
            if (initialCount != posts.Count())
            {
                _logger.LogInformation($"Filtered:        {initialCount} posts down to {posts.Count()}");
            }
            _logger.LogInformation($"Published Posts: {posts.Where(p => p["state"].Value<string>() == "published").Count()}");
            _logger.LogInformation($"Private Posts:   {posts.Where(p => p["state"].Value<string>() == "private").Count()}");
            _logger.LogInformation($"Draft Posts:     {posts.Where(p => p["state"].Value<string>() == "draft").Count()}");
            _logger.LogInformation($"Queued Posts:    {posts.Where(p => p["state"].Value<string>() == "queued").Count()}");
            _logger.LogInformation($"Total:           {posts.Count}");

            FlushLogs();
        }
    }
}
