using Microsoft.Extensions.Configuration;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace TumblrExport
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Get any App settings
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", true, true);
            var config = builder.Build();

            // Are there Oauth config in the appsettings file?
            OauthSettings.ConsumerKey ??= config["key"] ?? null;
            OauthSettings.ConsumerSecret ??= config["secret"] ?? null;
            if (string.IsNullOrEmpty(OauthSettings.ConsumerKey) || string.IsNullOrEmpty(OauthSettings.ConsumerSecret))
            {
                Console.WriteLine("Need to supply Tumblr API Key (Consumer key & Secret) Get these at https://www.tumblr.com/oauth/apps");
                Console.WriteLine("Use a json formatted file named \"appsettings.json\"");
                Console.WriteLine("{\n\t\"key\": \"your-consumer-key\",\n\t\"secret\": \"your-secret-key\"\n}");
                return (1);
            }

            var hugo = new Command("hugo", "Convert blog posts to Hugo.")
            {
                new Argument<string>("blog", "Your blog."),
                new Argument<DirectoryInfo>("posts", "Output Directory for posts."),
                new Argument<DirectoryInfo>("media", "Output Directory for images, etc."),
                new Option<bool>(new[] { "--published", "-p" }, "Get public posts"),
                new Option<bool>(new[] { "--restricted", "-r" }, "Get private posts"),
                new Option<bool>(new[] { "--drafts", "-d" }, "Get draft posts"),
                new Option<bool>(new[] { "--queued", "-q" }, "Get queued posts"),
                new Option<bool>(new[] { "--noreblogs", "-n" }, "Only processes original posts, no reblogs"),
                new Option<bool>(new[] { "--authenticate", "-a" }, "Always use user authenication (required to get private or censored posts)"),
                new Option<DateTime>(new[] { "--since", "-s" }, "Only process posts newer than this date"),
                new Option<FileInfo>(new[] { "--jsonout", "-o" }, "Writes raw tumblr post json to a file"),
                new Option<FileInfo>(new[] { "--jsonin", "-i" }, "Reads raw tumblr post json from a file"),
                new Option<bool>(new[] { "--verbose", "-v" }, "Verbose mode"),
                new Option<bool>(new[] { "--test", "-t" }, "Test output")
            };

            var hugoPostBundle = new Command("hugopagebundle", "Convert blog posts to Hugo post bundles.")
            {
                new Argument<string>("blog", "Your blog."),
                new Argument<DirectoryInfo>("output", "Output Directory for posts, images, etc."),
                new Option<bool>(new[] { "--published", "-p" }, "Get public posts"),
                new Option<bool>(new[] { "--restricted", "-r" }, "Get private posts"),
                new Option<bool>(new[] { "--drafts", "-d" }, "Get draft posts"),
                new Option<bool>(new[] { "--queued", "-q" }, "Get queued posts"),
                new Option<bool>(new[] { "--noreblogs", "-n" }, "Only processes original posts, no reblogs"),
                new Option<bool>(new[] { "--authenticate", "-a" }, "Always use user authenication (required to get private or censored posts)"),
                new Option<DateTime>(new[] { "--since", "-s" }, "Only process posts newer than this date"),
                new Option<FileInfo>(new[] { "--jsonout", "-o" }, "Writes raw tumblr post json to a file"),
                new Option<FileInfo>(new[] { "--jsonin", "-i" }, "Reads raw tumblr post json from a file"),
                new Option<bool>(new[] { "--verbose", "-v" }, "Verbose mode"),
                new Option<bool>(new[] { "--test", "-t" }, "Test output")
            };

            var cmd = new RootCommand
            {
                hugo,
                hugoPostBundle
            };

            hugo.Handler = CommandHandler.Create<HugoOptions>(new HugoProcessor().Run);
            hugoPostBundle.Handler = CommandHandler.Create<HugoPageBundleOptions>(new HugoPageBundleProcessor().Run);

            return await cmd.InvokeAsync(args);
        }
    }
}
