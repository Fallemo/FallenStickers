FallenStickers v2.0  –  © 2026 Fallen
=====================================
https://github.com/Fallemo/FallenStickers

Add pictures to YOUR Telegram sticker pack in one click.
It does everything you would do by hand in @Stickers: /addsticker, pick the pack,
resize to 512 px on the long side, save as PNG under 512 KB, send as a file,
set the emoji, /done. You can also write text on the sticker (font, color, outline, position).

Requirements: Windows 10/11 with the .NET 8 Desktop Runtime (if the app does not start,
install it from https://dotnet.microsoft.com/download/dotnet/8.0 → ".NET Desktop Runtime").


GETTING STARTED (one time only) – also shown in the app under "Settings & getting started"
------------------------------------------------------------------------------------------
1. api_id and api_hash
   Go to https://my.telegram.org/apps, log in with your Telegram phone number,
   under "API development tools" create an app (any title and short name,
   e.g. "stickers", platform Desktop). Copy "App api_id" (a number) and
   "App api_hash" (a long string) into FallenStickers.
   They are yours – don't share them.

2. A sticker pack (only if you don't have one yet)
   In Telegram open @Stickers and send /newpack. Give it a name, send a first
   picture as a FILE (512 px PNG) and an emoji, then /publish, /skip for the icon
   and choose the short name (the pack link: t.me/addstickers/<name>).

3. Phone and pack
   Enter your phone number with country code (e.g. +44...) and press "Choose..." next to Pack.
   On the first login you get a code in Telegram (and your 2FA password is asked, if set).
   Then pick your pack from the list. (You can also type the short name or paste the link.)

4. Press "Add to Send to menu".


USAGE
-----
- Right-click one or more pictures → Send to → Telegram sticker
  (or drop pictures into the window / onto FallenStickers.exe).
- Pick an emoji for each picture (type it, Win + . opens the emoji keyboard, or click one
  in the row below – it is set on the selected picture and moves to the next one).
- Optionally write text on the picture (see below).
- Press "Upload to <pack>". The log shows what @Stickers answers at every step.

Options:
- "upload right away with the default emoji" – from Send to, uploads without waiting.
- "close the window by itself" – closes after success.

Formats: PNG, JPG, BMP, GIF (first frame), TIFF, WEBP/HEIC (if Windows has the codec).
Phone photos are rotated by EXIF. PNG transparency is kept.
The pack must be a static sticker pack (not animated / video).


TEXT ON STICKERS
----------------
Select a picture in the list; the large preview appears on the right.
- Type the text under the preview (Enter = new line; long lines wrap).
- Font: any font installed in Windows (default Impact). Optional bold.
- Size and Outline: sliders (outline 0 = none).
- Color / Outline color: click a swatch or type the code (#RRGGBB).
- Position: drag on the picture, or the Top / Middle / Bottom buttons.
- "Style to all" copies the style (not the text) to all pictures.
- The last used style is remembered.


LANGUAGE
--------
Română / English – top right corner. "Auto" follows your Windows settings.


DATA AND PRIVACY
----------------
Everything stays on your PC, in %APPDATA%\FallenStickers
(config.json = settings, session.dat = your Telegram session).
session.dat gives access to your Telegram account: never share it.
"Log out" deletes it. The app only talks to Telegram (the @Stickers bot).
The source code is public on GitHub.


CHANGELOG
---------
v2.0 (2026-11-13)
- Renamed to FallenStickers; works for anyone, no built-in defaults.
- Getting-started guide inside the app.
- "Choose..." lists your own sticker packs straight from Telegram.
- The Pack field also accepts a t.me/addstickers/... link.
- Romanian and English interface.

v1.1
- Text on stickers: font, size, color, outline, bold, position, live preview.

v1.0
- Send to / drag & drop, 512 px resize, PNG under 512 KB, emoji per picture,
  automatic upload through @Stickers.
