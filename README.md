# BannerWand

![C#](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logo=csharp)
![.NET Framework](https://img.shields.io/badge/.NET_Framework-4.7.2-512BD4?style=for-the-badge&logo=dotnet)
![Harmony](https://img.shields.io/badge/Harmony-2.3.6-red?style=for-the-badge)
![MCM](https://img.shields.io/badge/MCM-v5-orange?style=for-the-badge)
![Bannerlord](https://img.shields.io/badge/Bannerlord-1.4.8-2E5A87?style=for-the-badge)

**BannerWand** is a cheat mod for **Mount & Blade II: Bannerlord**, configured entirely through **Mod Configuration Menu (MCM)**. Every cheat can be toggled or tuned in-game, without editing files or restarting.

---

## Features

- **Player** - health, ammo, movement speed, morale, relationships, aging, smithing, ships, perks
- **NPC** - the same combat and stat cheats, targetable at allied heroes or regular troops
- **Inventory** - gold, influence, food, carrying capacity, smithing materials, a rebindable add-gold hotkey
- **Stats** - attribute/focus points, renown, skill and troop XP
- **Enemies** - one-hit kills
- **Settlements** - garrison, militia, food, prosperity, loyalty, security, construction speed
- **Game** - persuasion, siege construction, game speed
- **Targeting** - apply any cheat to the player, their clan, kingdom rulers/vassals/nobles, or minor clans, in any combination

Most cheats replace the game's own balance formulas through its native Model system rather than patching code directly, so they stay stable across game updates. A handful of Harmony patches cover cases the Model system doesn't reach.

---

## Installation

Requires [BLSE](https://www.nexusmods.com/mountandblade2bannerlord/mods/1), [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006), [ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018), [UIExtenderEx](https://www.nexusmods.com/mountandblade2bannerlord/mods/2102), and [MCM v5](https://www.nexusmods.com/mountandblade2bannerlord/mods/612).

1. Install BLSE and the dependencies above.
2. Extract the `BannerWand` folder into `Mount & Blade II Bannerlord/Modules/`.
3. Launch the game through the BLSE launcher, with BannerWand loading after Harmony, ButterLib, UIExtenderEx, and MCM.

## Configuration

Open **Mod Options** from the main menu and select **BannerWand**. Every cheat has its own toggle and hint text explaining exactly what it does and any conditions it needs.

## Logging

BannerWand writes a daily log to `Documents/Mount and Blade II Bannerlord/Configs/ModLogs/`. Logs older than 14 days are cleaned up automatically.

## Localization

Fully translated into English, Russian, Ukrainian, German, French, Italian, Spanish, Polish, Portuguese (Brazil), Romanian, Turkish, Belarusian, Japanese, Korean, and Chinese (Simplified and Traditional).

---

## Links

- [Source code](https://github.com/geniussjack/BannerWand)
- [Report an issue](https://github.com/geniussjack/BannerWand/issues)
