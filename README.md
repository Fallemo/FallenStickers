# FallenStickers

**Add pictures to your own Telegram sticker pack in one click — right-click → Send to → Telegram sticker.**

No more `/addsticker`, resizing to 512 px, exporting PNG, sending as a file, choosing an emoji and `/done` by hand. FallenStickers does the whole @Stickers routine for you, from your own Telegram account.

## Features

- **Send to / drag & drop** – one or many pictures at once
- **Automatic resize** to 512 px on the long side, **PNG under 512 KB**, EXIF rotation, transparency kept
- **Text on stickers** – any installed font, size, color, outline, bold, drag to position, live preview
- **Emoji per sticker**, with a quick emoji row
- **Pick your pack from a list** – your own sticker packs are read straight from Telegram
- **Romanian / English** interface
- Everything stays on your PC – no server, no third party, only Telegram itself

## Download

Get the latest zip from [Releases](https://github.com/Fallemo/FallenStickers/releases), unzip and run `FallenStickers.exe`.
Requires Windows 10/11 and the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).

## Getting started (one time only)

1. **api_id / api_hash** – go to [my.telegram.org/apps](https://my.telegram.org/apps), log in with your phone number, open *API development tools* and create an app (any title / short name, platform Desktop). Copy *App api_id* and *App api_hash* into FallenStickers. They are yours — don't share them.
2. **A sticker pack** (only if you don't have one) – in Telegram open [@Stickers](https://t.me/Stickers), send `/newpack`, give it a name, send a first 512 px PNG **as a file** and an emoji, then `/publish`, `/skip` for the icon and choose the short name.
3. **Phone + pack** – enter your phone number (with country code) and press **Choose…** next to *Pack*. The first time you will get a login code in Telegram (and your 2FA password is asked if you have one). Then pick your pack.
4. Press **Add to Send to menu**. Done — right-click pictures → *Send to* → *Telegram sticker*.

## How it works

FallenStickers is a small Telegram client (MTProto, via [WTelegramClient](https://github.com/wiz0u/WTelegramClient)) that logs in as **you** and chats with the official **@Stickers** bot exactly like you would: `/addsticker` → pack → file → emoji → `/done`. This is needed because packs created with @Stickers can only be edited by their owner (the Bot API cannot add to them).

## Privacy

- `api_id`, `api_hash`, phone number and the Telegram session (`session.dat`) are stored **only** in `%APPDATA%\FallenStickers` on your PC.
- `session.dat` gives access to your Telegram account — never share it. **Log out** in the app deletes it.
- The app connects only to Telegram's servers. The source code is right here.

## Build

```
cd _sursa
dotnet publish -c Release -o ..\app
```

## License

MIT © 2026 Fallen. Uses [WTelegramClient](https://github.com/wiz0u/WTelegramClient) (MIT).

---

🇷🇴 *Aplicația are și interfață în română. Instrucțiuni în română: [_sursa/CITESTE-MA.txt](_sursa/CITESTE-MA.txt).*
