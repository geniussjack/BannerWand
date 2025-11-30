# BannerWand Translation Guide

Thank you for your interest in translating BannerWand mod to your language!

## Overview

BannerWand now supports full localization through Mount & Blade II: Bannerlord's standard localization system. This means all mod settings can be translated to any language the game supports.

## How to Translate

### Step 1: Create Language Folder and Files

1. Navigate to `ModuleData/Languages/`
2. Create a new folder for your language using the language code (see [Supported Languages](#supported-languages))
3. You need to create **TWO files** in your language folder:
   - `language_data.xml` - Language metadata
   - `strings.xml` - Translation strings

Example structure:
```
ModuleData/
└── Languages/
    ├── EN/
    │   ├── language_data.xml  (English metadata)
    │   └── strings.xml        (English strings)
    ├── RU/
    │   ├── language_data.xml  (Russian metadata)
    │   └── strings.xml        (Russian strings)
    └── FR/
        ├── language_data.xml  (French metadata)
        └── strings.xml        (French strings)
```

### Step 2: Create language_data.xml

Copy the `language_data.xml` from `ModuleData/Languages/EN/` and modify it for your language:

**Example for French:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
              xsi:noNamespaceSchemaLocation="https://raw.githubusercontent.com/BUTR/Bannerlord.XmlSchemas/master/ModuleLanguageData.xsd"

              id="Français"
              name="Français"
              subtitle_extension="fr"
              supported_iso="fr,fra,fr-FR,fr-BE,fr-CA,fr-CH,fr-LU"
              under_development="false">
  <LanguageFile xml_path="FR/strings.xml" />
</LanguageData>
```

**Key fields to change:**
- `id` - Language name (use native name from table below)
- `name` - Same as id
- `subtitle_extension` - Language code (lowercase, 2-letter)
- `supported_iso` - Comma-separated list of ISO codes for your language
- `xml_path` - Must be `[LANGUAGE_CODE]/strings.xml`

### Step 3: Create strings.xml

Copy the `strings.xml` from `ModuleData/Languages/EN/` and update the language tag:

```xml
<tag language="English" />
```

Change `"English"` to your language name (same as `id` in language_data.xml).

Example for French:
```xml
<tag language="Français" />
```

### Step 4: Translate the Strings

In your `strings.xml` file, translate only the `text=""` attribute values. **DO NOT** change:
- The `id=""` values
- Any XML structure or tags
- Anything outside of the text attribute

**Example:**

Before (English):
```xml
<string id="BW_Player_UnlimitedHealth" text="Unlimited Health" />
<string id="BW_Player_UnlimitedHealth_Hint" text="Player takes no damage." />
```

After (Russian):
```xml
<string id="BW_Player_UnlimitedHealth" text="Бесконечное здоровье" />
<string id="BW_Player_UnlimitedHealth_Hint" text="Игрок не получает урона." />
```

### Step 5: Test Your Translation

1. Copy your translated files to your Bannerlord modules folder:
   ```
   Mount & Blade II Bannerlord/
   └── Modules/
       └── BannerWand/
           └── ModuleData/
               └── Languages/
                   └── [YourLanguageCode]/
                       ├── language_data.xml
                       └── strings.xml
   ```

2. Launch Mount & Blade II: Bannerlord
3. Change your game language to your translated language in the game settings
4. Start or load a campaign
5. Open **Mod Options** → **BannerWand**
6. Verify that all settings appear in your language

**Checking logs:**
- If translation doesn't work, check the mod log at:
  `C:\ProgramData\Mount and Blade II Bannerlord\logs\BannerWand.log`
- Look for "Localization Check" section for diagnostic information

### Step 6: Share Your Translation

Once your translation is complete and tested, you can share it by:

1. **Creating a Pull Request** on the [BannerWand GitHub repository](https://github.com/geniussjack/BannerlordCheats)
2. **Uploading to Nexus Mods** comments section
3. **Contacting the mod author** directly with your translation file

## Supported Languages

Here are the language codes and names supported by Bannerlord:

| Code | Native Name | Subtitle Ext | Example ISO Codes |
|------|-------------|--------------|-------------------|
| EN | English | en-GB | en-GB,en-US,en,eng |
| RU | Русский | ru | ru,rus,ru-RU |
| FR | Français | fr | fr,fra,fr-FR,fr-BE,fr-CA |
| DE | Deutsch | de | de,deu,de-DE,de-AT,de-CH |
| ES | Español | es | es,spa,es-ES,es-MX |
| TR | Türkçe | tr | tr,tur,tr-TR |
| CNs | 简体中文 | zh-CN | zh-CN,zh-Hans,zh |
| CNt | 繁體中文 | zh-TW | zh-TW,zh-Hant,zh-HK |
| JP | 日本語 | ja | ja,jpn,ja-JP |
| KO | 한국어 | ko | ko,kor,ko-KR |
| PL | Polski | pl | pl,pol,pl-PL |
| IT | Italiano | it | it,ita,it-IT |
| PT | Português | pt | pt,por,pt-PT |
| BR | Português do Brasil | pt-BR | pt-BR |

**Note:** Use the "Native Name" for `id` and `name` in language_data.xml, and "Subtitle Ext" for `subtitle_extension`.

## Translation Strings Reference

The mod contains the following string categories:

### Category Names
- `BW_Category_Player` - Player category
- `BW_Category_Inventory` - Inventory category
- `BW_Category_Stats` - Stats category
- `BW_Category_Enemies` - Enemies category
- `BW_Category_Game` - Game category
- `BW_Category_CheatTargets` - Cheat Targets category

### Settings Strings
Each setting has two strings:
- **Setting name**: e.g., `BW_Player_UnlimitedHealth`
- **Hint text**: e.g., `BW_Player_UnlimitedHealth_Hint`

Total: **74 strings** to translate

## Translation Guidelines

1. **Keep text concise**: MCM UI has limited space. Try to keep translations similar in length to English.

2. **Use appropriate terminology**: Use gaming terminology common in your language's gaming community.

3. **Be consistent**: Use the same translation for repeated terms (e.g., always translate "Player" the same way).

4. **Test thoroughly**: Make sure all settings display correctly in-game.

5. **Preserve formatting**: Keep any special characters, punctuation, or numbers as they appear in English.

6. **Don't translate technical terms unnecessarily**: Terms like "XP", "HP", or game-specific mechanics might be better left in English if commonly understood.

## Troubleshooting

**Translation not showing up in-game?**
- Check that your language folder name matches exactly (e.g., `FR` for French, not `fr` or `French`)
- Verify both `language_data.xml` and `strings.xml` are present
- Check that `<tag language="...">` in strings.xml matches `id` in language_data.xml
- Verify `xml_path` in language_data.xml points to correct file (e.g., `FR/strings.xml`)
- Ensure both XML files have no syntax errors (missing quotes, brackets, etc.)
- Check the mod log at `C:\ProgramData\Mount and Blade II Bannerlord\logs\BannerWand.log`
- Restart the game completely after adding translations

**Some strings still in English?**
- Make sure you translated ALL strings in the strings.xml file
- Check that `id=""` values were not modified
- Verify the `<tag language="...">` matches your game's language setting exactly

**Game crashes after adding translation?**
- Your XML files likely have syntax errors
- Validate your XML using an online XML validator
- Double-check all opening/closing tags match
- Ensure both files use UTF-8 encoding

**Mod log shows "Localization NOT working"?**
- This means Bannerlord couldn't load your translation files
- Double-check file names: must be exactly `language_data.xml` and `strings.xml`
- Verify folder structure matches the example above
- Check that both files are in the correct folder

## Credits

Translations are credited in the mod description. Your name/username will be added when your translation is included in the official release.

## Questions?

If you have questions about translating BannerWand, feel free to:
- Open an issue on [GitHub](https://github.com/geniussjack/BannerlordCheats/issues)
- Comment on the [Nexus Mods page](https://www.nexusmods.com/mountandblade2bannerlord/mods/XXXX)
- Contact the mod author directly

Thank you for helping make BannerWand accessible to players worldwide! 🌍
