using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Yenetch.Crm;

namespace Yenetch.Data
{
    /// <summary>
    /// Image optimisation for uploads (blog, website content, team photos, logos). Every JPG or PNG is turned the right way
    /// up, stripped of camera data (which can include GPS location), limited to 2000px wide and re-compressed, and smaller
    /// copies are made at 480, 960 and 1600px wide. The admin's browser also sends WebP copies (about a third smaller than
    /// JPG), because .NET Framework cannot write WebP itself. Pages then let each visitor's browser pick the smallest file
    /// that looks sharp on their screen (srcset), and images below the fold load only when scrolled to (lazy loading).
    /// </summary>
    public static class Images
    {
        public static readonly int[] Widths = { 480, 960, 1600 };
        public const int MaxWidth = 2000;
        private const long JpegQuality = 82;

        /// <summary>Optimises a saved upload in place and writes its smaller copies. Never throws: an image that cannot be
        /// processed is kept exactly as uploaded.</summary>
        public static void Optimise(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") return;
            try
            {
                Image src;
                using (var fs = File.OpenRead(path)) src = Image.FromStream(fs, true, true);
                using (src)
                {
                    Orient(src);
                    var png = ext == ".png";
                    // PNG photos without transparency are much smaller as JPG, but the file name must stay the same: keep PNG.
                    var main = Resize(src, Math.Min(src.Width, MaxWidth));
                    var tmp = path + ".tmp";
                    Save(main, tmp, png);
                    if (main != src) main.Dispose();
                    // Keep the optimised file only if it is actually smaller (or the original was too wide).
                    if (new FileInfo(tmp).Length < new FileInfo(path).Length || src.Width > MaxWidth) { File.Delete(path); File.Move(tmp, path); }
                    else File.Delete(tmp);
                    foreach (var w in Widths.Where(w => w < src.Width))
                        using (var small = Resize(src, w)) Save(small, Variant(path, w, png ? ".png" : ".jpg"), png);
                }
            }
            catch (Exception ex) { Mailer.Log("image optimise " + Path.GetFileName(path), ex); }
        }

        /// <summary>Path of a smaller copy: photo.jpg at 960px → photo-960w.jpg (or .webp).</summary>
        public static string Variant(string path, int width, string ext)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-" + width + "w" + ext);
        }

        private static Image Resize(Image src, int width)
        {
            if (width >= src.Width) return src;
            var height = Math.Max(1, (int)Math.Round(src.Height * (width / (double)src.Width)));
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            bmp.SetResolution(72, 72);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var wrap = new ImageAttributes())
                {
                    wrap.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(src, new Rectangle(0, 0, width, height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, wrap);
                }
            }
            return bmp;
        }

        private static void Save(Image img, string path, bool png)
        {
            if (png) { img.Save(path, ImageFormat.Png); return; }
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var p = new EncoderParameters(1))
            {
                p.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JpegQuality);
                // Drawing on a fresh bitmap already dropped the camera data (EXIF, GPS); a plain copy does the same for originals.
                if (img.PropertyIdList.Length > 0) using (var clean = new Bitmap(img)) clean.Save(path, codec, p);
                else img.Save(path, codec, p);
            }
        }

        /// <summary>Phones store photos sideways with an "orientation" note; turn the pixels instead so every browser shows them upright.</summary>
        private static void Orient(Image img)
        {
            const int OrientationId = 0x0112;
            if (!img.PropertyIdList.Contains(OrientationId)) return;
            var o = img.GetPropertyItem(OrientationId).Value[0];
            RotateFlipType r;
            switch (o)
            {
                case 2: r = RotateFlipType.RotateNoneFlipX; break;
                case 3: r = RotateFlipType.Rotate180FlipNone; break;
                case 4: r = RotateFlipType.Rotate180FlipX; break;
                case 5: r = RotateFlipType.Rotate90FlipX; break;
                case 6: r = RotateFlipType.Rotate90FlipNone; break;
                case 7: r = RotateFlipType.Rotate270FlipX; break;
                case 8: r = RotateFlipType.Rotate270FlipNone; break;
                default: return;
            }
            img.RotateFlip(r);
            img.RemovePropertyItem(OrientationId);
        }

        // ---- Rendering --------------------------------------------------------------------------------------

        /// <summary>srcset for an uploaded image (/uploads/...): its WebP copies if the admin's browser made them, else its JPG/PNG copies.</summary>
        public static string SrcSet(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || url.Contains("..")) return "";
            var key = "srcset|" + url;
            var cached = HttpRuntime.Cache[key] as string;
            if (cached != null) return cached;
            var result = "";
            try
            {
                var path = Util.AppPath(url.TrimStart('/'));
                if (File.Exists(path))
                {
                    var ext = Path.GetExtension(path);
                    var webp = Widths.Where(w => File.Exists(Variant(path, w, ".webp"))).ToList();
                    var list = webp.Count > 0 ? webp.Select(w => VariantUrl(url, w, ".webp") + " " + w + "w").ToList()
                                              : Widths.Where(w => File.Exists(Variant(path, w, ext))).Select(w => VariantUrl(url, w, ext) + " " + w + "w").ToList();
                    var full = webp.Count > 0 && File.Exists(Path.ChangeExtension(path, ".webp")) ? Path.ChangeExtension(url, ".webp") : url;
                    if (list.Count > 0)
                    {
                        using (var img = Image.FromFile(path, false)) list.Add(full + " " + img.Width + "w");
                        result = string.Join(", ", list);
                    }
                }
            }
            catch { result = ""; }
            HttpRuntime.Cache.Insert(key, result, null, DateTime.UtcNow.AddHours(6), System.Web.Caching.Cache.NoSlidingExpiration);
            return result;
        }

        private static string VariantUrl(string url, int w, string ext)
        {
            var dot = url.LastIndexOf('.');
            return (dot > 0 ? url.Substring(0, dot) : url) + "-" + w + "w" + ext;
        }

        private static readonly Regex ImgTag = new Regex(@"<img\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SrcAttr = new Regex(@"\bsrc\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Adds lazy loading, async decoding and responsive sizes to the images inside article HTML. Safe to run twice.</summary>
        public static string OptimiseHtml(string html)
        {
            if (string.IsNullOrEmpty(html) || html.IndexOf("<img", StringComparison.OrdinalIgnoreCase) < 0) return html;
            return ImgTag.Replace(html, m =>
            {
                var tag = m.Value;
                if (tag.IndexOf(" loading=", StringComparison.OrdinalIgnoreCase) < 0) tag = tag.Insert(4, " loading=\"lazy\"");
                if (tag.IndexOf(" decoding=", StringComparison.OrdinalIgnoreCase) < 0) tag = tag.Insert(4, " decoding=\"async\"");
                if (tag.IndexOf(" srcset=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    var src = SrcAttr.Match(tag);
                    var set = src.Success ? SrcSet(HttpUtility.HtmlDecode(src.Groups[1].Value)) : "";
                    if (set.Length > 0) tag = tag.Insert(4, " srcset=\"" + HttpUtility.HtmlAttributeEncode(set) + "\" sizes=\"(max-width: 760px) 100vw, 760px\"");
                }
                return tag;
            });
        }
    }
}
