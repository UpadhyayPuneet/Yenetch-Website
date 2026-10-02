using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Yenetch.Crm;
using Yenetch.Models;

namespace Yenetch.Web.Admin
{
    /// <summary>
    /// Image upload for the blog editor (POST, field "file"). Admins and Managers only, same-site requests only.
    /// Saves JPG, PNG, WebP or GIF files up to 8 MB to /uploads/blog/yyyy-MM/ (or /uploads/content/ with ?to=content) and returns {"url": "..."}.
    /// The file type is checked from its first bytes, not only its name.
    /// </summary>
    public class UploadHandler : IHttpHandler
    {
        private const int MaxBytes = 8 * 1024 * 1024;

        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            ctx.Response.ContentType = "application/json";
            var me = Auth.Current;
            if (me == null || !me.CanUseMarketing) { Fail(ctx, 403, "Sign in as an Admin or Manager to upload images."); return; }
            if (ctx.Request.HttpMethod != "POST" || ctx.Request.Headers["X-Requested-With"] != "fetch" || !SameSite(ctx.Request)) { Fail(ctx, 400, "Bad request."); return; }

            var file = ctx.Request.Files["file"];
            if (file == null || file.ContentLength == 0) { Fail(ctx, 400, "Choose an image to upload."); return; }
            if (file.ContentLength > MaxBytes) { Fail(ctx, 400, "Images can be up to 8 MB."); return; }

            var head = new byte[12];
            file.InputStream.Read(head, 0, head.Length);
            file.InputStream.Position = 0;
            var ext = Sniff(head);
            if (ext == null) { Fail(ctx, 400, "Upload a JPG, PNG, WebP or GIF image."); return; }

            var name = BlogPost.Slugify(Path.GetFileNameWithoutExtension(file.FileName ?? ""));
            if (name.Length > 50) name = name.Substring(0, 50).TrimEnd('-');
            if (name.Length == 0) name = "image";
            var folder = "uploads/" + (ctx.Request.QueryString["to"] == "content" ? "content" : "blog") + "/" + Util.TodayIst.ToString("yyyy-MM");
            var fileName = name + "-" + Util.NewId().Substring(0, 6) + ext;
            Directory.CreateDirectory(Util.AppPath(folder));
            var path = Path.Combine(Util.AppPath(folder), fileName);
            file.SaveAs(path);

            // Smaller, faster copies (see Data/Images.cs): resized JPG/PNG made here, WebP made by the admin's browser.
            Yenetch.Data.Images.Optimise(path);
            if (ext == ".jpg" || ext == ".png")
            {
                foreach (var w in Yenetch.Data.Images.Widths)
                    SaveWebp(ctx.Request.Files["webp" + w], Yenetch.Data.Images.Variant(path, w, ".webp"));
                SaveWebp(ctx.Request.Files["webpFull"], Path.ChangeExtension(path, ".webp"));
            }

            ctx.Response.Write(new JavaScriptSerializer().Serialize(new { url = "/" + folder + "/" + fileName }));
        }

        /// <summary>Saves a WebP copy sent by the browser, only if it really is a WebP file of a sensible size.</summary>
        private static void SaveWebp(HttpPostedFile f, string target)
        {
            if (f == null || f.ContentLength < 12 || f.ContentLength > MaxBytes) return;
            var head = new byte[12];
            f.InputStream.Read(head, 0, head.Length);
            f.InputStream.Position = 0;
            if (Sniff(head) != ".webp") return;
            f.SaveAs(target);
        }

        private static string Sniff(byte[] b)
        {
            if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
            if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
            if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) return ".gif";
            if (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return ".webp";
            return null;
        }

        private static bool SameSite(HttpRequest req)
        {
            var origin = req.Headers["Origin"];
            if (string.IsNullOrEmpty(origin)) return req.UrlReferrer != null && req.UrlReferrer.Host == req.Url.Host;
            Uri u;
            return Uri.TryCreate(origin, UriKind.Absolute, out u) && u.Host == req.Url.Host;
        }

        private static void Fail(HttpContext ctx, int status, string message)
        {
            ctx.Response.StatusCode = status;
            ctx.Response.TrySkipIisCustomErrors = true;
            ctx.Response.Write(new JavaScriptSerializer().Serialize(new { error = message }));
        }
    }
}
