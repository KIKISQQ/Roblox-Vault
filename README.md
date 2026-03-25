# RobloxVault

A modern, cleaner alternative to the classic Roblox Account Manager. Store, launch, and manage multiple Roblox accounts from a single interface, with a slicker UI and more settings than the original.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Framework](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-GPL--3.0-green)

---

## Screenshots

![Main Window](RobloxVaultScreenshots/MainWindow.png)
![Launch Dialog](RobloxVaultScreenshots/LaunchAccount.png)
![Settings](RobloxVaultScreenshots/Settings.png)
![Settings2](RobloxVaultScreenshots/Settings2.png)

---

## Features

- **Multi-account management** — store unlimited accounts with encrypted cookies
- **One-click launch** — launch any account into any game by Place ID
- **Game search** — search for games by name directly from the launch dialog
- **Join player** — join someones server by their Roblox username if they heve joins enabled
- **Multi-Roblox** — run multiple Roblox instances simultaneously
- **FPS cap** — set a custom FPS cap per-session
- **Anti-AFK** — automatically sends input to all running Roblox windows every 2 minutes, no admin required
- **Robux balance** — optionally display each account's Robux balance
- **Sections** — organise accounts into custom groups
- **Rogue Lineage section** — automatically created on first launch; accounts inside it get an extra edit panel where you can track owned artifacts (Mysterious Artifact, Phoenix Down, White King Amulet, Lannis Amulet) shown as coloured dots and badges on the card
- **Accent colours** — customise the UI colour with presets or a custom hex code
- **Pinning** — pin frequently used accounts to the top

---

## Requirements

- Windows 10 or later
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (x64)
- Roblox installed

---

## Installation

1. Go to the [Releases](../../releases) page
2. Download the latest `RobloxVault.zip`
3. Extract and run `RobloxVault.exe`

---

## Usage

### Adding an account
1. Click **Add Account**
2. Paste your `.ROBLOSECURITY` cookie
3. Optionally give the account a display name
4. Click **Add** — the cookie is verified and saved with an encrypted cookie

### Launching an account
1. Click **▶ Launch** on any account card
2. Enter a Place ID or search for a game by name
3. Click **Launch**

### Joining a player
1. Click **▶ Launch** on the account you want to use
2. Type the target player's Roblox username in the **Join Player** field
3. Click **Join Player**

### Rogue Lineage section
The Rogue Lineage section is created automatically when you first open the app. Accounts added to it get an extra edit panel (✏) where you can set a character note and toggle which artifacts the account owns. These show up as coloured dots and labelled badges directly on the account card.

### Anti-AFK
1. Open **Settings**
2. Enable **Anti-AFK**
3. Optionally enable **Minimize after each anti-AFK input**
4. Save — it runs in the background automatically

### Multi-Roblox
Enable in Settings. Works by holding the Roblox singleton mutex so multiple instances can run at once. Roblox may patch this at any time — use at your own risk.

---

## Cookie Security

Cookies are encrypted at rest using Windows DPAPI before being saved to disk, meaning they are tied to your Windows user account and cannot be decrypted on another machine. They are never logged or transmitted anywhere other than directly to Roblox's own endpoints.

---

## Acknowledgements

RobloxVault was heavily inspired by [Roblox Account Manager](https://github.com/ic3w0lf22/Roblox-Account-Manager) by ic3w0lf22 — a lot of the core concepts, launch methods, and approaches used in this project were referenced from it. Go check it out if you haven't.

ic3w0lf22/Roblox-Account-Manager is licensed under the GNU General Public License v3.0.

---

## License

This project is licensed under the **GNU General Public License v3.0**.  
See [LICENSE](LICENSE) for the full text.

> This software is not affiliated with, endorsed by, or connected to Roblox Corporation in any way.
