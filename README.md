# TumblrExport

Commandline tool for exporting Tumblr blogs to Markdown usable by a Static Site Generator.
Currently [Hugo](https://gohugo.io/) is supported

## Usage

```
Commands:
  hugo <blog> <posts> <media>       Convert blog posts to Hugo.
  hugopagebundle <blog> <output>    Convert blog posts to Hugo post bundles.
  mediaonly <blog> <media>          Copy original media (images etc) from blog
```


```
hugo:
  Convert blog posts to Hugo.

Usage:
  TumblrExport hugo [options] <blog> <posts> <media>

Arguments:
  <blog>     Your blog.
  <posts>    Output Directory for posts.
  <media>    Output Directory for images, etc.

Options:
  -p, --published            Get public posts
  -r, --restricted           Get private posts
  -d, --drafts               Get draft posts
  -q, --queued               Get queued posts
  -n, --noreblogs            Only processes original posts, no reblogs
  -a, --authenticate         Always use user authentication (required to get private or censored posts)
  -s, --since <since>        Only process posts newer than this date
  -o, --jsonout <jsonout>    Writes raw tumblr post json to a file
  -i, --jsonin <jsonin>      Reads raw tumblr post json from a file
  -v, --verbose              Verbose mode
  -t, --test                 Test output
  -?, -h, --help             Show help and usage information
```


```
hugopagebundle:
  Convert blog posts to Hugo post bundles.

Usage:
  TumblrExport hugopagebundle [options] <blog> <output>

Arguments:
  <blog>      Your blog.
  <output>    Output Directory for posts, images, etc.

Options:
  -p, --published            Get public posts
  -r, --restricted           Get private posts
  -d, --drafts               Get draft posts
  -q, --queued               Get queued posts
  -n, --noreblogs            Only processes original posts, no reblogs
  -a, --authenticate         Always use user authentication (required to get private or censored posts)
  -s, --since <since>        Only process posts newer than this date
  -o, --jsonout <jsonout>    Writes raw tumblr post json to a file
  -i, --jsonin <jsonin>      Reads raw tumblr post json from a file
  -v, --verbose              Verbose mode
  -t, --test                 Test output
  -?, -h, --help             Show help and usage information
```

   
```
mediaonly:
  Copy original media (images etc) from blog

Usage:
  TumblrExport mediaonly [options] <blog> <media>

Arguments:
  <blog>     Your blog.
  <media>    Output Directory for images, etc.

Options:
  -s, --since <since>    Only process posts newer than this date
  -t, --test             Test output
  -?, -h, --help         Show help and usage information
```

## Example Usage
To backup your Tumblr named "myBlog"
Which contains three posts (100, 101 & 103)
- 100 is just text
- 200 is text and one image
- 300 has four images
```
TumblrExport hugo myBlog c:\hugo\myblog\content\posts c:\hugo\myblog\content\images -p -d -r -q -a
```
This will create markdown pages for each Tumblr post and copy over any images, and create the following files:

```
└── content
    ├── posts
    |   ├── 100.md 
    |   ├── 200.md 
    |   └── 300.md         
    └── images
        ├── 200_1.jpg
        ├── 300_1.jpg
        ├── 300_2.jpg
        ├── 300_3.jpg
        └── 300_4.jpg 
```
If you wanted this as page bundles:
```
TumblrExport hugopagebundle myBlog c:\hugo\myblog\content\posts -p -d -r -q -a
```
This will create markdown pages for each Tumblr post and copy over any images, and create the following files:

```
└── content
    └── posts
        ├── 100
        |   └── index.md
        ├── 200
        |   ├── index.md
        |   └── 200_1.jpg
        └── 300
            ├── index.md      
            ├── 300_1.jpg
            ├── 300_2.jpg
            ├── 300_3.jpg
            └── 300_4.jpg 
```

## Tumblr Content Blocks
The Tumblr API returns content (lines of text, images,etc) as content blocks.  Tumblr export applies the following format rules.

### Text Blocks
Where possible Tumblr formatting will be mapped to markdown.

Tumblr formats that don't map to markdown will be converted to a Hugo [shortcode](https://gohugo.io/templates/shortcode-templates/)

- **quirky** - Tumblr Official clients display this with a large cursive font.
- **quote** - Intended for short quotations, official Tumblr clients display this with a large serif font.
- **chat** - Intended to mimic the behavior of the Chat Post type, official Tumblr clients display this with a monospace font.
*Note that these are Block level formats and apply to a complete line of text*


| Tumblr Format  | In Markdown |
| ------------- | ------------- |
| quirky | hugo shortcode {{%quirky%}} |
| quote  | hugo shortcode {{%quote%}}  |
| chat  | hugo shortcode {{%chat%}}  |

Tumblr also supports some inline formating, again where these map to markdown the markdown eqivalent will be used (e.g. italic will map to \*)
| Tumblr Format  | In Markdown |
| ------------- | ------------- |
| small | hugo shortcode {{%small%}} |
| color  | hugo shortcode {{%color hexcode%}}  |

The html files to support these shortcodes are in the project **[HugoShortcuts](https://github.com/noelanderson/TumblrExport/tree/master/HugoShortcuts)** folder and should be copied to the **\\layouts\\shortcodes** folder in Hugo

### Image Blocks
Images will be converted to the Hugo [Figure](https://gohugo.io/content-management/shortcodes/#figure) shortcut

### Video Blocks
| video source  | In Markdown |
| ------------- | ------------- |
| youtube | hugo [youtube](https://gohugo.io/content-management/shortcodes/#youtube) shortcode  |
| vimeo  | hugo [vimeo](https://gohugo.io/content-management/shortcodes/#vimeo) shortcode  |
| tumblr  | hugo {{%video%}} shortcode,  this supports the html5 video tag |
The html file to support the {{video}} shortcode is the project **[HugoShortcuts](https://github.com/noelanderson/TumblrExport/tree/master/HugoShortcuts)** folder and should be copied to the **\\layouts\\shortcodes** folder in Hugo

### Audio Blocks
| video source  | In Markdown |
| ------------- | ------------- |
| spotify | hugo {{embedded_audio}} shortcode, this supports an iframe containing the providers audio player  |
| soundcloud  | hugo {{embedded_audio}} shortcode, this supports an iframe containing the providers audio player  |
| tumblr  | hugo {{%audio%}} shortcode, this supports the html5 audio tag |
The html files to support these shortcodes are in the project **[HugoShortcuts](https://github.com/noelanderson/TumblrExport/tree/master/HugoShortcuts)** folder and should be copied to the **\\layouts\\shortcodes** folder in Hugo
