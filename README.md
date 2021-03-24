# TumblrExport

Commandline tool for exporting Tunblr blogs to Markdown usable by a Static Site Generator.
Currently [Hugo](https://gohugo.io/) is supported

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
  -a, --authenticate         Always use user authenication (required to get private or censored posts)
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
  -a, --authenticate         Always use user authenication (required to get private or censored posts)
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