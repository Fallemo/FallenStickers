using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TL;

namespace FallenStickers
{
    public static class AppInfo
    {
        public const string Version = "2.0";
        public const string Copyright = "© 2026 Fallen";
        public const string RepoUrl = "https://github.com/Fallemo/FallenStickers";
        public static readonly string Dir = Environment.GetEnvironmentVariable("FALLENSTICKERS_DIR") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FallenStickers");
    }

    /// <summary>Texte RO/EN: L.T("ro", "en").</summary>
    public static class L
    {
        public static bool En;
        public static string T(string ro, string en) => En ? en : ro;

        public static void Apply(string setting)
        {
            En = setting switch
            {
                "en" => true,
                "ro" => false,
                _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "ro" && CultureInfo.CurrentCulture.TwoLetterISOLanguageName != "ro"
            };
        }
    }

    public class Config
    {
        public string ApiId { get; set; } = "";
        public string ApiHash { get; set; } = "";
        public string Phone { get; set; } = "";
        public string PackName { get; set; } = "";     // short name (t.me/addstickers/<PackName>)
        public string PackTitle { get; set; } = "";    // titlul afișat, dacă a fost ales din listă
        public string DefaultEmoji { get; set; } = "😀";
        public string Language { get; set; } = "auto"; // auto / ro / en
        public bool AutoUpload { get; set; } = false;
        public bool AutoClose { get; set; } = true;
        public TextStyle LastStyle { get; set; } = new TextStyle();

        static string FilePath => Path.Combine(AppInfo.Dir, "config.json");

        public bool HasAccount =>
            !string.IsNullOrWhiteSpace(ApiId) && !string.IsNullOrWhiteSpace(ApiHash) && !string.IsNullOrWhiteSpace(Phone);

        public bool IsComplete => HasAccount && !string.IsNullOrWhiteSpace(PackName);

        public string PackDisplay => string.IsNullOrWhiteSpace(PackTitle) || PackTitle == PackName
            ? PackName : $"{PackTitle} ({PackName})";

        public static Config Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath)) ?? new Config();
            }
            catch { }
            return new Config();
        }

        public void Save()
        {
            Directory.CreateDirectory(AppInfo.Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    /// <summary>Stilul textului scris pe sticker (coordonate în spațiul de 512 px).</summary>
    public class TextStyle
    {
        public string Text { get; set; } = "";
        public string Font { get; set; } = "Impact";
        public double Size { get; set; } = 64;
        public string Color { get; set; } = "#FFFFFF";
        public string OutlineColor { get; set; } = "#000000";
        public double Outline { get; set; } = 6;
        public bool Bold { get; set; } = false;
        public double X { get; set; } = 0.5;   // centrul textului, 0..1
        public double Y { get; set; } = 0.85;

        public TextStyle Clone() => (TextStyle)MemberwiseClone();

        public static System.Windows.Media.Color ParseColor(string s, System.Windows.Media.Color fallback)
        {
            try { return (System.Windows.Media.Color)ColorConverter.ConvertFromString(s); } catch { return fallback; }
        }
    }

    /// <summary>Redimensionează la 512 px pe latura mare, scrie textul și scoate PNG sub 512 KB.</summary>
    public static class ImagePrep
    {
        public const int Side = 512;
        public const int MaxBytes = 512 * 1024;

        public static BitmapSource LoadBase(string path)
        {
            BitmapSource src;
            BitmapMetadata meta = null;
            using (var fs = File.OpenRead(path))
            {
                var dec = BitmapDecoder.Create(fs, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                var frame = dec.Frames[0];
                try { meta = frame.Metadata as BitmapMetadata; } catch { }
                src = frame;
            }
            src = ApplyExifOrientation(src, meta);

            double sw = src.PixelWidth, sh = src.PixelHeight;
            double scale = Side / Math.Max(sw, sh);
            int w = Math.Max(1, (int)Math.Round(sw * scale));
            int h = Math.Max(1, (int)Math.Round(sh * scale));
            if (sw >= sh) w = Side; else h = Side;
            return Render(w, h, dc => dc.DrawImage(src, new Rect(0, 0, w, h)));
        }

        public static BitmapSource Compose(BitmapSource bg, TextStyle t)
        {
            if (t == null || string.IsNullOrWhiteSpace(t.Text)) return bg;
            int w = bg.PixelWidth, h = bg.PixelHeight;
            return Render(w, h, dc =>
            {
                dc.DrawImage(bg, new Rect(0, 0, w, h));
                var tf = new Typeface(new FontFamily(t.Font), FontStyles.Normal,
                    t.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
                double maxW = w * 0.96;
                var ft = new FormattedText(t.Text.Replace("\r\n", "\n"), CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight, tf, Math.Max(4, t.Size), Brushes.Black, 1.0)
                { MaxTextWidth = maxW, TextAlignment = TextAlignment.Center };
                // ținem textul în interiorul pozei (cât încape), oriunde l-ar trage utilizatorul
                double pad = t.Outline + 2;
                double halfW = ft.Width / 2 + pad, halfH = ft.Height / 2 + pad;
                double cx = halfW * 2 < w ? Math.Clamp(t.X * w, halfW, w - halfW) : w / 2.0;
                double cy = halfH * 2 < h ? Math.Clamp(t.Y * h, halfH, h - halfH) : h / 2.0;
                double x = cx - maxW / 2, y = cy - ft.Height / 2;
                var geo = ft.BuildGeometry(new Point(x, y));
                if (t.Outline > 0)
                {
                    var pen = new Pen(new SolidColorBrush(TextStyle.ParseColor(t.OutlineColor, Colors.Black)), t.Outline * 2)
                    { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                    dc.DrawGeometry(null, pen, geo);
                }
                dc.DrawGeometry(new SolidColorBrush(TextStyle.ParseColor(t.Color, Colors.White)), null, geo);
            });
        }

        static BitmapSource Render(int w, int h, Action<DrawingContext> draw)
        {
            var dv = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);
            using (var dc = dv.RenderOpen()) draw(dc);
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            var bgra = new FormatConvertedBitmap(rtb, PixelFormats.Bgra32, null, 0);
            bgra.Freeze();
            return bgra;
        }

        public static byte[] Encode(BitmapSource bgra)
        {
            int w = bgra.PixelWidth, h = bgra.PixelHeight;
            byte[] png = EncodePng(bgra);
            if (png.Length <= MaxBytes) return png;

            // Prea mare: reducem treptat numărul de nuanțe (compresează mult mai bine).
            int stride = w * 4;
            var pixels = new byte[stride * h];
            bgra.CopyPixels(pixels, stride, 0);
            foreach (int step in new[] { 4, 8, 12, 16, 24, 32, 48, 64 })
            {
                var p = (byte[])pixels.Clone();
                for (int i = 0; i < p.Length; i += 4)
                {
                    for (int c = 0; c < 3; c++)
                        p[i + c] = (byte)Math.Min(255, (p[i + c] / step) * step + step / 2);
                }
                var bs = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, p, stride);
                png = EncodePng(bs);
                if (png.Length <= MaxBytes) return png;
            }
            throw new Exception(L.T($"nu am reușit să o aduc sub 512 KB ({png.Length / 1024} KB)",
                                    $"could not get it under 512 KB ({png.Length / 1024} KB)"));
        }

        static byte[] EncodePng(BitmapSource bs)
        {
            var enc = new PngBitmapEncoder { Interlace = PngInterlaceOption.Off };
            enc.Frames.Add(BitmapFrame.Create(bs));
            using var ms = new MemoryStream();
            enc.Save(ms);
            return ms.ToArray();
        }

        static BitmapSource ApplyExifOrientation(BitmapSource src, BitmapMetadata meta)
        {
            int o = 1;
            try
            {
                if (meta != null && meta.ContainsQuery("System.Photo.Orientation"))
                    o = Convert.ToInt32(meta.GetQuery("System.Photo.Orientation"));
            }
            catch { }
            if (o <= 1 || o > 8) return src;
            var tg = new TransformGroup();
            switch (o)
            {
                case 2: tg.Children.Add(new ScaleTransform(-1, 1)); break;
                case 3: tg.Children.Add(new RotateTransform(180)); break;
                case 4: tg.Children.Add(new ScaleTransform(1, -1)); break;
                case 5: tg.Children.Add(new RotateTransform(90)); tg.Children.Add(new ScaleTransform(-1, 1)); break;
                case 6: tg.Children.Add(new RotateTransform(90)); break;
                case 7: tg.Children.Add(new RotateTransform(270)); tg.Children.Add(new ScaleTransform(-1, 1)); break;
                case 8: tg.Children.Add(new RotateTransform(270)); break;
            }
            var tb = new TransformedBitmap(src, tg);
            tb.Freeze();
            return tb;
        }
    }

    /// <summary>Discută automat cu @Stickers din contul utilizatorului.</summary>
    public sealed class StickerUploader : IDisposable
    {
        readonly Config cfg;
        readonly Func<string, bool, string> ask;   // (întrebare, eParolă) -> răspuns
        readonly Action<string> log;
        WTelegram.Client client;
        InputPeer bot;

        public StickerUploader(Config cfg, Func<string, bool, string> ask, Action<string> log)
        {
            this.cfg = cfg; this.ask = ask; this.log = log;
            WTelegram.Helpers.Log = (lvl, s) => { };
        }

        string TgConfig(string what) => what switch
        {
            "api_id" => cfg.ApiId.Trim(),
            "api_hash" => cfg.ApiHash.Trim(),
            "phone_number" => cfg.Phone.Trim(),
            "verification_code" => ask(L.T("Codul primit în Telegram (doar la prima logare):",
                                           "The code you received in Telegram (first login only):"), false),
            "password" => ask(L.T("Parola de verificare în doi pași (2FA):", "Your two-step verification (2FA) password:"), true),
            "session_pathname" => Path.Combine(AppInfo.Dir, "session.dat"),
            _ => null
        };

        public async Task ConnectAsync()
        {
            Directory.CreateDirectory(AppInfo.Dir);
            log(L.T("Mă conectez la Telegram...", "Connecting to Telegram..."));
            if (!int.TryParse(cfg.ApiId.Trim(), out _))
                throw new Exception(L.T("api_id trebuie să fie un număr (îl iei de pe my.telegram.org/apps)",
                                        "api_id must be a number (get it from my.telegram.org/apps)"));
            client = new WTelegram.Client(TgConfig);
            var me = await client.LoginUserIfNeeded();
            log(L.T("Conectat ca ", "Logged in as ") + $"{me.first_name} {me.last_name}".TrimEnd());
            var r = await client.Contacts_ResolveUsername("Stickers");
            bot = r.User ?? throw new Exception(L.T("nu găsesc botul @Stickers", "cannot find the @Stickers bot"));
        }

        /// <summary>Pack-urile de stickere create de utilizator (titlu, short name, număr de stickere).</summary>
        public async Task<List<(string title, string shortName, int count)>> ListMyPacksAsync()
        {
            var res = new List<(string, string, int)>();
            long offset = 0;
            for (int page = 0; page < 10; page++)
            {
                var r = await client.Messages_GetMyStickers(offset, 100);
                var sets = r.sets.Select(s => s.Set).Where(s => s != null).ToList();
                foreach (var s in sets)
                    if (!s.flags.HasFlag(StickerSet.Flags.emojis) && !s.flags.HasFlag(StickerSet.Flags.masks))
                        res.Add((s.title, s.short_name, s.count));
                if (sets.Count < 100) break;
                offset = sets.Last().id;
            }
            return res;
        }

        class Reply { public string Text = ""; public Message Last; }

        async Task<Reply> SendAndWait(string text, InputMedia media = null)
        {
            var sent = await client.SendMessageAsync(bot, text, media);
            return await WaitReply(sent.id, 60);
        }

        async Task Cancel()
        {
            try { var sent = await client.SendMessageAsync(bot, "/cancel"); await WaitReply(sent.id, 12); }
            catch { }
        }

        async Task<Reply> WaitReply(int afterId, int polls)
        {
            List<Message> got = null;
            for (int i = 0; i < polls; i++)
            {
                await Task.Delay(500);
                var h = await client.Messages_GetHistory(bot, limit: 10);
                var msgs = h.Messages.OfType<Message>()
                    .Where(m => m.id > afterId && !m.flags.HasFlag(Message.Flags.out_))
                    .OrderBy(m => m.id).ToList();
                if (msgs.Count == 0) continue;
                // botul trimite uneori 2 mesaje la rând – mai așteptăm puțin
                if (got != null && got.Count == msgs.Count) break;
                got = msgs;
            }
            if (got == null) throw new Exception(L.T($"@Stickers nu a răspuns în {polls / 2} secunde",
                                                     $"@Stickers did not answer within {polls / 2} seconds"));
            var rep = new Reply { Text = string.Join("\n", got.Select(m => m.message)), Last = got.Last() };
            log("  @Stickers: " + Shorten(rep.Text));
            return rep;
        }

        static string Shorten(string s)
        {
            s = (s ?? "").Replace("\n", " ").Trim();
            return s.Length > 160 ? s.Substring(0, 160) + "…" : s;
        }

        static List<string> Buttons(Message m) =>
            (m?.reply_markup as ReplyKeyboardMarkup)?.rows?
                .SelectMany(r => r.buttons).OfType<KeyboardButton>().Select(b => b.text).ToList()
            ?? new List<string>();

        static bool Has(Reply r, params string[] words) =>
            words.Any(w => r.Text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>Urcă toate pozele; întoarce câte au reușit.</summary>
        public async Task<int> UploadAsync(IList<(byte[] png, string emoji, string name, Action<string> status)> items)
        {
            int ok = 0;
            // pornim curat, în caz că a rămas o comandă neterminată
            await Cancel();

            log(L.T("Trimit /addsticker...", "Sending /addsticker..."));
            var r = await SendAndWait("/addsticker");
            var btns = Buttons(r.Last);
            // butoanele pot arăta short name-ul sau titlul pack-ului
            var names = new[] { cfg.PackName, cfg.PackTitle }.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList();
            string pack = names.Select(n => btns.FirstOrDefault(b => b.Equals(n, StringComparison.OrdinalIgnoreCase))).FirstOrDefault(b => b != null)
                       ?? names.Select(n => btns.FirstOrDefault(b => b.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)).FirstOrDefault(b => b != null);
            if (pack == null)
            {
                await Cancel();
                throw new Exception(btns.Count == 0
                    ? L.T("@Stickers nu a afișat lista de pack-uri (ai creat deja un pack cu /newpack?)",
                          "@Stickers did not show any packs (did you create one with /newpack?)")
                    : L.T($"nu găsesc pack-ul „{cfg.PackName}”. Pack-urile tale: ",
                          $"pack \"{cfg.PackName}\" not found. Your packs: ") + string.Join(", ", btns));
            }
            log(L.T($"Aleg pack-ul „{pack}”...", $"Selecting pack \"{pack}\"..."));
            r = await SendAndWait(pack);
            if (Has(r, "invalid", "sorry"))
            {
                await Cancel();
                throw new Exception(L.T("@Stickers a refuzat pack-ul: ", "@Stickers rejected the pack: ") + Shorten(r.Text));
            }

            foreach (var it in items)
            {
                try
                {
                    it.status(L.T("Se urcă...", "Uploading..."));
                    log(L.T("Trimit ", "Sending ") + $"{it.name} ({it.png.Length / 1024} KB)...");
                    using var ms = new MemoryStream(it.png);
                    var up = await client.UploadFileAsync(ms, "sticker.png");
                    var doc = new InputMediaUploadedDocument
                    {
                        file = up,
                        mime_type = "image/png",
                        attributes = new DocumentAttribute[] { new DocumentAttributeFilename { file_name = "sticker.png" } },
                        flags = InputMediaUploadedDocument.Flags.force_file
                    };
                    r = await SendAndWait("", doc);
                    if (!Has(r, "emoji"))
                        throw new Exception(L.T("fișier respins: ", "file rejected: ") + Shorten(r.Text));

                    r = await SendAndWait(it.emoji);
                    if (!Has(r, "congratulations", "added", "stickers in the set"))
                        throw new Exception(L.T("emoji respins: ", "emoji rejected: ") + Shorten(r.Text));

                    ok++;
                    it.status(L.T("✔ Adăugat", "✔ Added"));
                }
                catch (Exception ex)
                {
                    it.status("✖ " + ex.Message);
                    log(L.T("  EROARE: ", "  ERROR: ") + ex.Message);
                    // ne întoarcem în pack pentru următoarea poză
                    await Cancel();
                    r = await SendAndWait("/addsticker");
                    r = await SendAndWait(pack);
                }
            }

            log(L.T("Trimit /done...", "Sending /done..."));
            await SendAndWait("/done");
            return ok;
        }

        public void Dispose() => client?.Dispose();
    }
}
