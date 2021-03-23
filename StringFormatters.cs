using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TumblrExport
{
    public static class StringExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetFirstTextPhrase(this string input)
        {
            string result = null;
            if (!string.IsNullOrEmpty(input) && input.Length > 1)
            {
                input = input.Trim(new Char[] { '\"', '\'' });
                string[] separators = { ",", ".", "!", "?", ";", ":", "\t", "<", "[", "\n", "\r", "\"", "“" };
                string[] fragments = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if (fragments.Count() != 0 && !string.IsNullOrEmpty(fragments[0]))
                {
                    result = fragments[0];
                }
            }
            return result;
        }

        public static string GetFileType(this string input)
        {
            string result = null;
            if (input != null)
            {
                result = input.Split('.').Last();
            }
            return result;
        }

        public static string EscapeMarkdown(this string input)
        {
            return Regex.Replace(input, @"[\\=`*_\[\]{}<>()#+-.!|]", "\\$0");
        }
        public static string EscapeYAML(this string input)
        {
            return Regex.Replace(input, @"[\\]", "\\$0");
        }

        // heading1 - Intended for Post headings.
        // heading2 - Intended for section subheadings.
        // quirky - Tumblr Official clients display this with a large cursive font.
        // quote - Intended for short quotations, official Tumblr clients display this with a large serif font.
        // indented - Intended for longer quotations or photo captions, official Tumblr clients indent this text block.
        // chat - Intended to mimic the behavior of the Chat Post type, official Tumblr clients display this with a monospace font.
        // ordered-list-item - Intended to be an ordered list item prefixed by a number
        // unordered-list-item - Intended to be an unordered list item prefixed with a bullet

        public static string ToMarkDown(this string input, bool strict, string blockFormat, List<TextContent.InlineFormat> inlineFormats)
        {
            // Use dictionary to map position in string to a list of opening & closing tags
            Dictionary<int, List<string>> markdownTagMap = new Dictionary<int, List<string>>();
            string output = "  \n"; // Markdown line break

            if (!string.IsNullOrEmpty(input))
            {
                // Add markdown tags for block level format
                if (blockFormat != null)
                {
                    // Add an opening tag at pos 0 (start of string)
                    markdownTagMap.TryAdd(0, new List<string>());
                    markdownTagMap[0].Add(blockFormat switch
                    {
                        "heading1" => "# ",
                        "heading2" => "## ",
                        "indented" => $"> ",
                        "ordered-list-item" => $"1. ",
                        "unordered-list-item" => $"- ",
                        "quirky" => strict ? "" : "{{%quirky%}}",
                        "quote" => strict ? "" : "{{%quote%}}",
                        "chat" => strict ? "" : "{{%chat%}}",
                        _ => ""
                    });

                    // Add a closing tag at end of string.
                    markdownTagMap.TryAdd(input.Length, new List<string>());
                    markdownTagMap[input.Length].Add(blockFormat switch
                    {
                        "quirky" => strict ? "" : "{{%/quirky%}}",
                        "quote" => strict ? "" : "{{%/quote%}}",
                        "chat" => strict ? "" : "{{%/chat%}}",
                        _ => ""
                    });
                }

                // Add markdown tags for all inline formats
                if (inlineFormats != null)
                {
                    foreach (var f in inlineFormats)
                    {
                        // Opening tag at inlineFormat start position. Add to end of tag list
                        markdownTagMap.TryAdd(f.Start, new List<string>());
                        markdownTagMap[f.Start].Add(f.Type switch
                        {
                            "bold" => "**",
                            "italic" => "*",
                            "strikethrough" => "~~",
                            "small" => strict ? "" : "{{%small%}}",
                            "color" => strict ? "" : $"{{{{%color \"{f.Hex}\"%}}}}",
                            _ => ""
                        });

                        // Closing tag at inlineFormat end position. Insert at beginning of tag list
                        markdownTagMap.TryAdd(f.End, new List<string>());
                        markdownTagMap[f.End].Insert(0, f.Type switch
                        {
                            "bold" => "**",
                            "italic" => "*",
                            "strikethrough" => "~~",
                            "small" => strict ? "" : "{{%/small%}}",
                            "color" => strict ? "" : "{{%/color%}}",
                            _ => ""
                        });
                    }
                }

                // Process input string
                string markdownEscapeCharacters = "\\`*_{}[]<>()#+-.!|";
                for (int pos = 0; pos < input.Length; pos++)
                {
                    // Add markdown nodes for characters that need markdown escaping. Add to end of tag list
                    if (markdownEscapeCharacters.IndexOf(input[pos]) != -1)
                    {
                        markdownTagMap.TryAdd(pos, new List<string>());
                        markdownTagMap[pos].Add("\\");
                    }

                    // Add any markdown tags and the input string character into the output string
                    if (markdownTagMap.ContainsKey(pos))
                    {
                        output += string.Join(null, markdownTagMap[pos]);
                    }
                    output += input[pos];
                }
                output += markdownTagMap.ContainsKey(input.Length) ? string.Join(null, markdownTagMap[input.Length]) : "";
                output += "\n\n";
            }
            return output;
        }
    }

    public static class DirectoryInfoExtensions
    {
        public static string GetRelativePathTo(this DirectoryInfo from, DirectoryInfo to)
        {
            Func<DirectoryInfo, string> getPath = fsi =>
            {
                var d = fsi as DirectoryInfo;
                return d == null ? fsi.FullName : d.FullName.TrimEnd('\\') + "\\";
            };

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return $"../{relativePath}";
        }
    }
}
