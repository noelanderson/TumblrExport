using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Tumblr.Client;

namespace TumblrExport
{
    class BaseProcessor
    {
        protected ILogger _logger;
        private ILoggerFactory _loggerFactory;
        protected HttpClient _httpClient = new HttpClient();

        protected void CreateLogger(string name, bool isVerbose)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.UseUtcTimestamp = true;
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    //options.TimestampFormat = "hh:mm:ss";
                });
                builder.AddFilter(null, isVerbose ? LogLevel.Trace : LogLevel.Information);
            });
            _logger = _loggerFactory.CreateLogger(name);
        }

        protected void FlushLogs()
        {
            _loggerFactory.Dispose();
        }

        /// <summary>Get posts from Tumblr API.</summary>
        /// <param name="blog">The blog to target.</param>
        /// <param name="published">if set to <c>true</c> [published].</param>
        /// <param name="drafts">if set to <c>true</c> [drafts].</param>
        /// <param name="queued">if set to <c>true</c> [queued].</param>
        /// <param name="forceUserAuth">if set to <c>true</c> [force user authentication].</param>
        /// <returns>JArray.</returns>
        public async Task<JArray> GetPostsFromTumblr(string blog, bool published, bool drafts, bool queued, bool forceUserAuth)
        {
            JArray posts = new JArray();
            using (TumblrClient tumblrClient = new TumblrClient(_httpClient, OauthSettings.ConsumerKey, OauthSettings.ConsumerSecret, _logger))
            {
                if (published)
                {
                    JArray publicPosts = await tumblrClient.GetPosts(blog, forceUserAuth);
                    posts.Merge(publicPosts);
                }
                if (drafts)
                {
                    JArray draftPosts = await tumblrClient.GetDrafts(blog);
                    posts.Merge(draftPosts);
                }
                if (queued)
                {
                    JArray queuedPosts = await tumblrClient.GetQueue(blog);
                    posts.Merge(queuedPosts);
                }
            }
            return posts;
        }

        /// <summary>Copy Media files.</summary>
        /// <param name="copyList">The copy list.</param>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="isTest">if set to <c>true</c> [is test].</param>
        /// <returns>System.Int32. Count of copy failures</returns>
        public async Task<int> CopyFiles(List<MediaToCopy> copyList, string targetDirectory, bool isTest)
        {
            int countCopyFailures = 0;
            foreach (var mediaFile in copyList)
            {
                FileInfo targetMediaFile = targetMediaFile = new FileInfo(Path.Combine(targetDirectory, mediaFile.To));
                _logger.LogInformation($"Copy {mediaFile.From}  --> {targetMediaFile.FullName}");
                if (!isTest)
                {
                    if (targetMediaFile.Exists)
                    {
                        _logger.LogInformation($"File exists: {targetMediaFile.FullName}");
                    }
                    else
                    {
                        try
                        {
                            var s = await _httpClient.GetStreamAsync(mediaFile.From);
                            using (var stream = targetMediaFile.Create())
                            {
                                await s.CopyToAsync(stream);
                                await stream.FlushAsync();
                            }
                        }
                        catch
                        {
                            _logger.LogError($"Copy failed {mediaFile.From}");
                            countCopyFailures++;
                        }
                    }
                }
            }
            return countCopyFailures;
        }

        /// <summary>Creates the markdown file.</summary>
        /// <param name="targetFile">The target file.</param>
        /// <param name="markDown">The mark down content.</param>
        /// <param name="isTest">if set to <c>true</c> [is test].</param>
        /// <returns>System.Int32. 1 if file creation or write failed, else 0</returns>
        public int CreateMarkDownFile(FileInfo targetFile, string markDown, bool isTest)
        {
            int fileFailue = 0;
            _logger.LogInformation($"Creating markdown file: {targetFile.FullName} ...");
            if (isTest)
            {
                _logger.LogTrace($"\n{markDown}");
            }
            else
            {
                try
                {
                    using (var stream = targetFile.CreateText())
                    {
                        stream.Write(markDown);
                    }
                }
                catch
                {
                    _logger.LogError($"Create output file failed: {targetFile.FullName}");
                    fileFailue = 1;
                }
            }
            return fileFailue;
        }
    }
}

