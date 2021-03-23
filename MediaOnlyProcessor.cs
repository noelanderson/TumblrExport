using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TumblrExport
{
    class MediaOnlyProcessor : BaseProcessor
    {
        /// <summary>Process Blog Posts into Hugo format.</summary>
        /// <param name="options">options.</param>
        public async Task Run(MediaOptions options)
        {
            int fileFailures = 0;
            int countCopyFailures = 0;
            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();

            CreateLogger("mediaonly", true);

            JArray posts = await GetPostsFromTumblr(options.Blog, true, true, true, true);

            stopWatch.Stop();
            var readTime = stopWatch.Elapsed;
            stopWatch.Restart();

            // Filter posts; Tumblr API doesn't let us filter the query, so we have to read all posts & then filter 
            int initialCount = posts.Count;
            posts = posts.FilterPosts(options.Since, true, true, true, true, true);

            // Process posts
            DirectoryInfo targetMediaDirectory = options.Media;
            targetMediaDirectory?.Create();
            foreach (JToken jsonPost in posts)
            {
                Post post = jsonPost.ToObject<Post>();
                post.Process();
                // Copy any Media files referencd by this post - targetMediaDirectory/
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
