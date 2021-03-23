using System;
using System.IO;

namespace TumblrExport
{
    public class CommonOptions
    {
        public string Blog { get; set; }
        public bool Published { get; set; }
        public bool Restricted { get; set; }
        public bool Drafts { get; set; }
        public bool Queued { get; set; }
        public bool Authenticate { get; set; }
        public DateTime Since { get; set; }
        public FileInfo JsonOut { get; set; }
        public FileInfo JsonIn { get; set; }
        public FileInfo Keys { get; set; }
        public bool NoReblogs { get; set; }
        public bool Verbose { get; set; }
        public bool Test { get; set; }
    }

    public class HugoOptions : CommonOptions
    {
        public DirectoryInfo Posts { get; set; }
        public DirectoryInfo Media { get; set; }
    }

    public class HugoPageBundleOptions : CommonOptions
    {
        public DirectoryInfo Output { get; set; }
    }
}
