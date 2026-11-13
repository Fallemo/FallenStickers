using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FallenStickers
{
    public class StickerItem : INotifyPropertyChanged
    {
        public string Path { get; set; }
        public string Name => System.IO.Path.GetFileName(Path);
        public BitmapSource Base { get; set; }
        public TextStyle Style { get; set; }
        BitmapSource thumb;
        string info = "", status = "", emoji = "";
        public BitmapSource Thumb { get => thumb; set { thumb = value; On(); } }
        public string Info { get => info; set { info = value; On(); } }
        public string Status { get => status; set { status = value; On(); } }
        public string Emoji { get => emoji; set { emoji = value; On(); } }
        public bool Done { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        void On([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public void Rerender() => Thumb = Base == null ? null : ImagePrep.Compose(Base, Style);

        public void UpdateInfo(int pngBytes = 0)
        {
            if (Base == null) return;
            var s = $"{Base.PixelWidth}×{Base.PixelHeight} px";
            if (!string.IsNullOrWhiteSpace(Style?.Text)) s += L.T(", cu text", ", with text");
            if (pngBytes > 0) s += $", PNG {pngBytes / 1024.0:0} KB";
            Info = s;
        }
    }

    public partial class MainWindow : Window
    {
        static readonly string[] Exts = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".webp", ".heic", ".jfif" };
        static readonly string[] Quick = { "😀", "😂", "🤣", "😍", "😎", "🤔", "😢", "😡", "👍", "👎", "🙏", "🔥", "❤️", "💩", "🐱", "🎉", "😴", "🤡" };
        static readonly string[] FillColors = { "#FFFFFF", "#000000", "#FFE600", "#FF8A00", "#FF2D2D", "#FF4FB8", "#A45CFF", "#2F8BFF", "#22D3EE", "#2EDB5B" };
        static readonly string[] OutlineColors = { "#000000", "#FFFFFF", "#7A0000", "#003A8C", "#5A2D00", "#1E5E00" };
        static readonly string[] LangCodes = { "auto", "ro", "en" };

        readonly ObservableCollection<StickerItem> items = new();
        Config cfg = Config.Load();
        readonly bool fromSendTo;
        bool busy;
        bool loading = true;   // blochează handler-ele cât timp populăm controalele
        bool dragging;

        StickerItem Current => List.SelectedItem as StickerItem;

        public MainWindow(string[] args)
        {
            L.Apply(cfg.Language);
            InitializeComponent();
            Title = $"FallenStickers v{AppInfo.Version}";
            AboutRun.Text = $"v{AppInfo.Version}  {AppInfo.Copyright}";
            RepoLink.NavigateUri = new Uri(AppInfo.RepoUrl);
            List.ItemsSource = items;
            items.CollectionChanged += (s, e) => DropHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var q in Quick)
            {
                var b = new Button
                {
                    Content = q, FontFamily = new FontFamily("Segoe UI Emoji"), FontSize = 18,
                    Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 4, 4),
                    Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2C, 0x33))
                };
                b.Click += (s, e) => QuickEmoji_Click(q);
                QuickEmoji.Children.Add(b);
            }
            foreach (var c in FillColors) FillSwatches.Children.Add(Swatch(c, () => FillHex.Text = c));
            foreach (var c in OutlineColors) OutlineSwatches.Children.Add(Swatch(c, () => OutlineHex.Text = c));

            FontCombo.ItemsSource = Fonts.SystemFontFamilies
                .Select(f => f.Source).Where(s => !string.IsNullOrWhiteSpace(s) && !s.Contains("#"))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

            LangCombo.ItemsSource = new[] { "Auto", "Română", "English" };
            LangCombo.SelectedIndex = Math.Max(0, Array.IndexOf(LangCodes, cfg.Language));

            cfg.LastStyle ??= new TextStyle();
            LoadSettingsToUi();
            SettingsExp.IsExpanded = !cfg.IsComplete;
            ShowStyle(cfg.LastStyle);
            ApplyTexts();
            loading = false;

            Drop += (s, e) =>
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files) AddFiles(files);
            };
            Closing += (s, e) => { try { cfg.Save(); } catch { } };

            fromSendTo = args != null && args.Length > 0;
            Loaded += async (s, e) =>
            {
                if (!fromSendTo) return;
                AddFiles(args);
                if (cfg.IsComplete && cfg.AutoUpload && items.Any(i => i.Base != null))
                    await DoUpload();
            };
        }

        // ---------- limbă ----------

        void ApplyTexts()
        {
            LangLbl.Text = L.T("Limbă", "Language");
            SettingsExp.Header = L.T("Setări și primii pași", "Settings & getting started");

            GuideTitle.Text = L.T("Primii pași (o singură dată)", "Getting started (one time only)");
            Step1a.Text = L.T("1. Intră pe ", "1. Go to ");
            Step1b.Text = L.T(", loghează-te cu numărul tău de Telegram și la „API development tools” creează o aplicație " +
                              "(orice titlu și nume scurt, ex. „stickers”, platforma Desktop). Copiază aici „App api_id” și „App api_hash”.",
                              ", log in with your Telegram phone number and under \"API development tools\" create an app " +
                              "(any title and short name, e.g. \"stickers\", platform Desktop). Copy \"App api_id\" and \"App api_hash\" here.");
            Step2a.Text = L.T("2. Dacă nu ai încă un pack de stickere: în Telegram deschide ", "2. If you don't have a sticker pack yet: in Telegram open ");
            Step2b.Text = L.T(", trimite /⁠newpack, dă-i un nume, trimite o primă poză ca fișier (PNG 512 px) și un emoji, " +
                              "apoi /⁠publish, /⁠skip la iconiță și alege numele scurt (linkul pack-ului).",
                              ", send /⁠newpack, give it a name, send a first picture as a file (512 px PNG) and an emoji, " +
                              "then /⁠publish, /⁠skip for the icon and choose the short name (the pack's link).");
            Step3.Text = L.T("3. Scrie telefonul (cu prefix, ex. +40…) și apasă „Alege” lângă Pack. La prima logare primești un cod " +
                             "în Telegram (și ți se cere parola 2FA, dacă ai). Apoi alegi pack-ul din listă.",
                             "3. Enter your phone number (with country code, e.g. +44…) and press \"Choose\" next to Pack. On the first login " +
                             "you get a code in Telegram (and your 2FA password is asked, if you have one). Then pick your pack from the list.");
            Step4.Text = L.T("4. Apasă „Adaugă în meniul Send to”. De acum: click dreapta pe poze → Send to → Telegram sticker.",
                             "4. Press \"Add to Send to menu\". From now on: right-click pictures → Send to → Telegram sticker.");
            PrivacyNote.Text = L.T("Datele tale (api_id, api_hash, telefon, sesiunea Telegram) rămân doar pe calculatorul tău, în %APPDATA%\\FallenStickers. " +
                                   "Aplicația vorbește doar cu Telegram, cu botul oficial @Stickers.",
                                   "Your data (api_id, api_hash, phone, Telegram session) stays only on your PC, in %APPDATA%\\FallenStickers. " +
                                   "The app only talks to Telegram, to the official @Stickers bot.");

            PhoneLbl.Text = L.T("Telefon", "Phone");
            PickBtn.Content = L.T("Alege…", "Choose…");
            EmojiLbl.Text = L.T("Emoji implicit", "Default emoji");
            AutoUploadChk.Content = L.T("Din Send to: urcă imediat cu emoji-ul implicit (fără să mai întrebe)",
                                        "From Send to: upload right away with the default emoji (no questions)");
            AutoCloseChk.Content = L.T("Din Send to: închide fereastra singură dacă totul a mers",
                                       "From Send to: close the window by itself when everything worked");
            SaveBtn.Content = L.T("Salvează", "Save");
            SendToBtn.Content = L.T("Adaugă în meniul Send to", "Add to Send to menu");
            LogoutBtn.Content = L.T("Deconectează contul", "Log out");

            DropHint.Text = L.T("Trage pozele aici\nsau click dreapta pe ele\n→ Send to → Telegram sticker",
                                "Drop pictures here\nor right-click them\n→ Send to → Telegram sticker");
            BrowseBtn.Content = L.T("Adaugă poze…", "Add pictures…");
            foreach (Button b in QuickEmoji.Children)
                b.ToolTip = L.T("Pune emoji-ul pe poza selectată și trece la următoarea", "Set this emoji on the selected picture and go to the next one");
            UpdateUploadText();

            DragHint.Text = L.T("Trage cu mouse-ul pe poză ca să muți textul.", "Drag on the picture to move the text.");
            CapText.ToolTip = L.T("Textul de pe sticker (Enter = rând nou)", "Sticker text (Enter = new line)");
            SizeTxt.Text = L.T("Mărime", "Size");
            OutlineTxt.Text = L.T("Contur", "Outline");
            ColorTxt.Text = L.T("Culoare", "Color");
            OutColorTxt.Text = L.T("Culoare\ncontur", "Outline\ncolor");
            TopBtn.Content = L.T("Sus", "Top");
            MidBtn.Content = L.T("Mijloc", "Middle");
            BotBtn.Content = L.T("Jos", "Bottom");
            StyleAllBtn.Content = L.T("Stilul la toate", "Style to all");
            StyleAllBtn.ToolTip = L.T("Copiază fontul, mărimea, culorile și poziția (nu și textul) pe toate pozele",
                                      "Copy font, size, colors and position (not the text) to all pictures");
            ClearBtn.Content = L.T("Fără text", "No text");

            foreach (var it in items) it.UpdateInfo();
        }

        void Lang_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (loading || LangCombo.SelectedIndex < 0) return;
            cfg.Language = LangCodes[LangCombo.SelectedIndex];
            L.Apply(cfg.Language);
            ApplyTexts();
            try { cfg.Save(); } catch { }
        }

        Border Swatch(string hex, Action click)
        {
            var b = new Border
            {
                Width = 17, Height = 17, Margin = new Thickness(0, 0, 2, 0), CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(TextStyle.ParseColor(hex, Colors.White)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x6A, 0x6D, 0x76)), BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand, ToolTip = hex
            };
            b.MouseLeftButtonUp += (s, e) => click();
            return b;
        }

        void Log(string s)
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => Log(s)); return; }
            LogBox.AppendText($"{DateTime.Now:HH:mm:ss}  {s}\n");
            LogBox.ScrollToEnd();
        }

        // ---------- setări ----------

        void LoadSettingsToUi()
        {
            ApiIdBox.Text = cfg.ApiId; ApiHashBox.Text = cfg.ApiHash; PhoneBox.Text = cfg.Phone;
            PackBox.Text = cfg.PackName; EmojiBox.Text = cfg.DefaultEmoji;
            AutoUploadChk.IsChecked = cfg.AutoUpload; AutoCloseChk.IsChecked = cfg.AutoClose;
        }

        void SaveSettingsFromUi()
        {
            cfg.ApiId = ApiIdBox.Text.Trim(); cfg.ApiHash = ApiHashBox.Text.Trim();
            cfg.Phone = PhoneBox.Text.Replace(" ", "").Trim();
            cfg.PackName = PackBox.Text.Trim();
            if (cfg.PackName.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || cfg.PackName.StartsWith("t.me/", StringComparison.OrdinalIgnoreCase))
                cfg.PackName = cfg.PackName.TrimEnd('/').Split('/').Last();   // acceptă și linkul t.me/addstickers/xyz
            cfg.DefaultEmoji = string.IsNullOrWhiteSpace(EmojiBox.Text) ? "😀" : EmojiBox.Text.Trim();
            cfg.AutoUpload = AutoUploadChk.IsChecked == true; cfg.AutoClose = AutoCloseChk.IsChecked == true;
            cfg.Save();
            UpdateUploadText();
        }

        void PackBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (loading) return;
            // scris de mână: titlul memorat nu mai e valabil
            if (!string.Equals(PackBox.Text.Trim(), cfg.PackName, StringComparison.OrdinalIgnoreCase)) cfg.PackTitle = "";
        }

        void UpdateUploadText()
        {
            var name = string.IsNullOrWhiteSpace(cfg.PackTitle) ? cfg.PackName : cfg.PackTitle;
            UploadBtn.Content = string.IsNullOrWhiteSpace(name)
                ? L.T("Urcă în pack", "Upload to pack")
                : L.T($"Urcă în {name}", $"Upload to {name}");
        }

        // ---------- lista ----------

        void AddFiles(IEnumerable<string> paths)
        {
            var list = new List<string>();
            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                    list.AddRange(Directory.GetFiles(p).Where(f => Exts.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant())).OrderBy(f => f));
                else if (File.Exists(p)) list.Add(p);
            }
            StickerItem first = null;
            foreach (var p in list)
            {
                if (items.Any(i => string.Equals(i.Path, p, StringComparison.OrdinalIgnoreCase))) continue;
                var st = cfg.LastStyle.Clone(); st.Text = "";
                var it = new StickerItem { Path = p, Emoji = cfg.DefaultEmoji, Style = st };
                try
                {
                    it.Base = ImagePrep.LoadBase(p);
                    it.Thumb = it.Base;
                    it.UpdateInfo();
                }
                catch (Exception ex)
                {
                    it.Info = L.T("nu pot citi imaginea", "cannot read this image");
                    it.Status = "✖ " + ex.Message;
                }
                items.Add(it);
                first ??= it;
            }
            if (first != null) List.SelectedItem = first;
        }

        void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var it = Current;
            Editor.IsEnabled = it?.Base != null && !busy;
            Preview.Source = it?.Thumb;
            if (it?.Base == null) return;
            loading = true;
            ShowStyle(it.Style);
            loading = false;
        }

        void QuickEmoji_Click(string e)
        {
            var it = Current ?? items.FirstOrDefault();
            if (it == null) return;
            it.Emoji = e;
            if (List.SelectedIndex >= 0 && List.SelectedIndex < items.Count - 1) List.SelectedIndex++;
        }

        void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (busy) return;
            if ((sender as FrameworkElement)?.Tag is StickerItem it) items.Remove(it);
            if (items.Count == 0) { Preview.Source = null; Editor.IsEnabled = false; }
        }

        void Browse_Click(object sender, RoutedEventArgs e)
        {
            var d = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = L.T("Imagini", "Images") + "|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp;*.heic;*.jfif|" + L.T("Toate", "All") + "|*.*"
            };
            if (d.ShowDialog(this) == true) AddFiles(d.FileNames);
        }

        // ---------- editor text ----------

        void ShowStyle(TextStyle s)
        {
            CapText.Text = s.Text ?? "";
            var fonts = (List<string>)FontCombo.ItemsSource;
            FontCombo.SelectedItem = fonts.FirstOrDefault(f => f.Equals(s.Font, StringComparison.OrdinalIgnoreCase))
                ?? fonts.FirstOrDefault(f => f == "Impact") ?? fonts.FirstOrDefault(f => f == "Arial Black") ?? fonts.FirstOrDefault();
            BoldChk.IsChecked = s.Bold;
            SizeSlider.Value = s.Size;
            OutlineSlider.Value = s.Outline;
            FillHex.Text = s.Color;
            OutlineHex.Text = s.OutlineColor;
            SizeLbl.Text = $"{s.Size:0}";
            OutlineLbl.Text = $"{s.Outline:0}";
        }

        void ReadStyle(TextStyle s)
        {
            s.Text = CapText.Text;
            s.Font = FontCombo.SelectedItem as string ?? s.Font;
            s.Bold = BoldChk.IsChecked == true;
            s.Size = Math.Round(SizeSlider.Value);
            s.Outline = Math.Round(OutlineSlider.Value);
            s.Color = FillHex.Text.Trim();
            s.OutlineColor = OutlineHex.Text.Trim();
        }

        void Style_Changed(object sender, RoutedEventArgs e)
        {
            if (loading || SizeLbl == null) return;
            SizeLbl.Text = $"{SizeSlider.Value:0}";
            OutlineLbl.Text = $"{OutlineSlider.Value:0}";
            var it = Current;
            if (it?.Base == null) return;
            ReadStyle(it.Style);
            RememberStyle(it.Style);
            Redraw(it);
        }

        void RememberStyle(TextStyle s)
        {
            var keep = s.Clone(); keep.Text = "";
            cfg.LastStyle = keep;
        }

        void Redraw(StickerItem it)
        {
            it.Rerender();
            if (it == Current) Preview.Source = it.Thumb;
            it.UpdateInfo();
        }

        void SetPos(double x, double y)
        {
            var it = Current;
            if (it?.Base == null) return;
            it.Style.X = Math.Clamp(x, 0, 1);
            it.Style.Y = Math.Clamp(y, 0, 1);
            RememberStyle(it.Style);
            Redraw(it);
        }

        void DragTo(MouseEventArgs e)
        {
            if (Preview.ActualWidth <= 0 || Preview.ActualHeight <= 0) return;
            var p = e.GetPosition(Preview);
            SetPos(p.X / Preview.ActualWidth, p.Y / Preview.ActualHeight);
        }

        void Preview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Current?.Base == null) return;
            dragging = true;
            Preview.CaptureMouse();
            DragTo(e);
        }

        void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging) DragTo(e);
        }

        void Preview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
            Preview.ReleaseMouseCapture();
        }

        void PosTop_Click(object sender, RoutedEventArgs e) => SetPos(0.5, 0.15);
        void PosMid_Click(object sender, RoutedEventArgs e) => SetPos(0.5, 0.5);
        void PosBot_Click(object sender, RoutedEventArgs e) => SetPos(0.5, 0.85);

        void StyleAll_Click(object sender, RoutedEventArgs e)
        {
            var src = Current?.Style;
            if (src == null) return;
            foreach (var it in items.Where(i => i.Base != null && i != Current))
            {
                var text = it.Style.Text;
                it.Style = src.Clone();
                it.Style.Text = text;
                Redraw(it);
            }
            Log(L.T("Stilul a fost copiat pe toate pozele.", "Style copied to all pictures."));
        }

        void ClearText_Click(object sender, RoutedEventArgs e) => CapText.Text = "";

        // ---------- Telegram ----------

        string Ask(string question, bool password)
        {
            return Dispatcher.Invoke(() =>
            {
                var w = new Window
                {
                    Title = "FallenStickers", Width = 380, SizeToContent = SizeToContent.Height, Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
                    Background = Background, Foreground = Foreground
                };
                var sp = new StackPanel { Margin = new Thickness(14) };
                sp.Children.Add(new TextBlock { Text = question, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
                Control input = password ? new PasswordBox { Padding = new Thickness(4, 3, 4, 3) } : new TextBox();
                sp.Children.Add(input);
                var ok = new Button { Content = "OK", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
                ok.Click += (s, e) => w.DialogResult = true;
                sp.Children.Add(ok);
                w.Content = sp;
                w.Loaded += (s, e) => input.Focus();
                if (w.ShowDialog() != true) return null;
                return password ? ((PasswordBox)input).Password : ((TextBox)input).Text.Trim();
            });
        }

        bool CheckAccount()
        {
            SaveSettingsFromUi();
            if (cfg.HasAccount) return true;
            SettingsExp.IsExpanded = true;
            MessageBox.Show(this, L.T("Completează întâi api_id, api_hash și telefonul (vezi pașii din Setări).",
                                      "Fill in api_id, api_hash and your phone number first (see the steps in Settings)."), "FallenStickers");
            return false;
        }

        async void PickPack_Click(object sender, RoutedEventArgs e)
        {
            if (busy || !CheckAccount()) return;
            busy = true; PickBtn.IsEnabled = false; UploadBtn.IsEnabled = false;
            List<(string title, string shortName, int count)> packs = null;
            try
            {
                var c = cfg;
                packs = await Task.Run(async () =>
                {
                    using var up = new StickerUploader(c, Ask, Log);
                    await up.ConnectAsync();
                    return await up.ListMyPacksAsync();
                });
            }
            catch (Exception ex)
            {
                Log(L.T("EROARE: ", "ERROR: ") + ex.Message);
                MessageBox.Show(this, ex.Message, L.T("FallenStickers – eroare", "FallenStickers – error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { busy = false; PickBtn.IsEnabled = true; UploadBtn.IsEnabled = true; }
            if (packs == null) return;

            if (packs.Count == 0)
            {
                MessageBox.Show(this, L.T("Nu ai încă niciun pack de stickere. Creează unul în Telegram cu @Stickers → /newpack (pasul 2), apoi apasă din nou „Alege”.",
                                          "You don't have any sticker pack yet. Create one in Telegram with @Stickers → /newpack (step 2), then press \"Choose\" again."),
                                "FallenStickers");
                return;
            }
            Log(L.T($"Am găsit {packs.Count} pack-uri.", $"Found {packs.Count} packs."));

            var w = new Window
            {
                Title = L.T("Alege pack-ul", "Choose your pack"), Width = 420, Height = 420, Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Background, Foreground = Foreground
            };
            var dp = new DockPanel { Margin = new Thickness(12) };
            var ok = new Button { Content = "OK", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            DockPanel.SetDock(ok, Dock.Bottom);
            var lb = new ListBox
            {
                Background = new SolidColorBrush(Color.FromRgb(0x24, 0x26, 0x2C)), Foreground = Foreground, FontSize = 14,
                ItemsSource = packs.Select(p => $"{p.title}   ({p.shortName}, {p.count} {L.T("stickere", "stickers")})").ToList()
            };
            int sel = packs.FindIndex(p => p.shortName.Equals(cfg.PackName, StringComparison.OrdinalIgnoreCase));
            lb.SelectedIndex = sel >= 0 ? sel : 0;
            lb.MouseDoubleClick += (s, e2) => w.DialogResult = true;
            ok.Click += (s, e2) => w.DialogResult = true;
            dp.Children.Add(ok);
            dp.Children.Add(lb);
            w.Content = dp;
            if (w.ShowDialog() != true || lb.SelectedIndex < 0) return;

            var chosen = packs[lb.SelectedIndex];
            loading = true;
            PackBox.Text = chosen.shortName;
            loading = false;
            cfg.PackName = chosen.shortName;
            cfg.PackTitle = chosen.title;
            SaveSettingsFromUi();
            Log(L.T($"Pack ales: {cfg.PackDisplay}", $"Pack selected: {cfg.PackDisplay}"));
        }

        async Task DoUpload()
        {
            if (busy) return;
            if (!CheckAccount()) return;
            if (!cfg.IsComplete)
            {
                SettingsExp.IsExpanded = true;
                MessageBox.Show(this, L.T("Alege pack-ul (butonul „Alege” din Setări).", "Choose your pack (the \"Choose\" button in Settings)."), "FallenStickers");
                return;
            }
            var todo = items.Where(i => i.Base != null && !i.Done).ToList();
            if (todo.Count == 0) { Log(L.T("Nu am ce urca.", "Nothing to upload.")); return; }
            var noEmoji = todo.FirstOrDefault(i => string.IsNullOrWhiteSpace(i.Emoji));
            if (noEmoji != null) { MessageBox.Show(this, L.T($"Pune un emoji la {noEmoji.Name}.", $"Set an emoji for {noEmoji.Name}."), "FallenStickers"); return; }

            // PNG-urile finale (cu text) – pe firul interfeței, randarea WPF o cere
            var batch = new List<(byte[], string, string, Action<string>)>();
            foreach (var i in todo)
            {
                try
                {
                    var png = ImagePrep.Encode(ImagePrep.Compose(i.Base, i.Style));
                    i.UpdateInfo(png.Length);
                    var item = i;
                    batch.Add((png, i.Emoji.Trim(), i.Name, st => Dispatcher.Invoke(() =>
                    {
                        item.Status = st;
                        if (st.StartsWith("✔")) item.Done = true;
                    })));
                }
                catch (Exception ex) { i.Status = "✖ " + ex.Message; }
            }
            if (batch.Count == 0) return;

            busy = true; UploadBtn.IsEnabled = false; PickBtn.IsEnabled = false; Editor.IsEnabled = false;
            int ok = 0;
            try
            {
                var c = cfg;
                ok = await Task.Run(async () =>
                {
                    using var up = new StickerUploader(c, Ask, Log);
                    await up.ConnectAsync();
                    return await up.UploadAsync(batch);
                });
                Log(L.T($"Gata: {ok} din {batch.Count} adăugate în {cfg.PackDisplay}.",
                        $"Done: {ok} of {batch.Count} added to {cfg.PackDisplay}."));
            }
            catch (Exception ex)
            {
                Log(L.T("EROARE: ", "ERROR: ") + ex.Message);
                MessageBox.Show(this, ex.Message, L.T("FallenStickers – eroare", "FallenStickers – error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                busy = false; UploadBtn.IsEnabled = true; PickBtn.IsEnabled = true;
                Editor.IsEnabled = Current?.Base != null;
            }

            if (fromSendTo && cfg.AutoClose && ok == todo.Count)
            {
                await Task.Delay(1500);
                Close();
            }
        }

        async void Upload_Click(object sender, RoutedEventArgs e) => await DoUpload();

        void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsFromUi();
            Log(L.T("Setări salvate.", "Settings saved."));
        }

        void SendTo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
                var lnk = System.IO.Path.Combine(dir, "Telegram sticker.lnk");
                var exe = Environment.ProcessPath;
                dynamic sh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                dynamic sc = sh.CreateShortcut(lnk);
                sc.TargetPath = exe;
                sc.WorkingDirectory = System.IO.Path.GetDirectoryName(exe);
                sc.IconLocation = exe + ",0";
                sc.Description = "FallenStickers";
                sc.Save();
                Log(L.T("Adăugat în meniul Send to: click dreapta pe poze → Send to → Telegram sticker.",
                        "Added to the Send to menu: right-click pictures → Send to → Telegram sticker."));
                Log(L.T("(dacă muți FallenStickers.exe în alt folder, apasă din nou butonul)",
                        "(if you move FallenStickers.exe to another folder, press the button again)"));
            }
            catch (Exception ex) { Log(L.T("Nu am putut crea scurtătura: ", "Could not create the shortcut: ") + ex.Message); }
        }

        void Logout_Click(object sender, RoutedEventArgs e)
        {
            var f = System.IO.Path.Combine(AppInfo.Dir, "session.dat");
            if (File.Exists(f))
            {
                File.Delete(f);
                Log(L.T("Sesiunea Telegram a fost ștearsă; la următoarea conectare se cere iar codul.",
                        "Telegram session deleted; the login code will be asked again next time."));
            }
            else Log(L.T("Nu există sesiune salvată.", "No saved session."));
        }

        void Link_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
