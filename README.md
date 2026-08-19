# BannerWand

A comprehensive cheat mod for **Mount & Blade II: Bannerlord** with fully configurable options through **MCM (Mod Configuration Menu)**.

**All 32 cheats fully functional** - including previously non-working cheats now implemented via Harmony patches!

> **BannerWand v1.1.1** - Supports **Bannerlord v1.4.8**

---

## 🎮 Features

### Player Cheats (10 cheats)
- ✅ **Unlimited Health** - Never die in combat
- ✅ **Unlimited Horse Health** - Your mount never dies
- ✅ **Unlimited Shield Durability** - Shields never break
- ✅ **Unlimited Party Health** - All party members invincible (supports NPC targeting)
- ✅ **Ignore Melee Damage** - Immune to melee attacks
- ✅ **Max Morale** - Party morale always at maximum
- ✅ **Movement Speed Multiplier** - Travel faster on campaign map
- ✅ **Barter Always Accepted** - All trade offers accepted
- ✅ **Unlimited Smithy Stamina** - Never run out of crafting stamina
- ✅ **Max Character Relationship** - Improve relationships with NPCs (supports NPC targeting)

### Inventory Cheats (7 cheats)
- ✅ **Edit Gold** - Add/remove gold (supports NPC targeting)
- ✅ **Edit Influence** - Add/remove influence (supports clan targeting)
- ✅ **Unlimited Food** - Party food never decreases (supports NPC parties)
- ✅ **Trade Items No Decrease** - Items stay in inventory after trading *(Harmony)*
- ✅ **Max Carrying Capacity** - Carry unlimited weight
- ✅ **Unlimited Smithy Materials** - Infinite iron and charcoal
- ✅ **Unlock All Smithy Parts** - All crafting parts unlocked *(Harmony + Reflection)*

### Stats Cheats (8 cheats)
- ✅ **Edit Attribute Points** - Add/remove attribute points (supports NPC targeting)
- ✅ **Edit Focus Points** - Add/remove focus points (supports NPC targeting)
- ✅ **Unlimited Renown** - Renown increases constantly
- ✅ **Renown Multiplier** - Multiply all renown gains *(Harmony)*
- ✅ **Unlimited Skill XP** - All skills level up quickly (supports NPC targeting)
- ✅ **Skill XP Multiplier** - Multiply skill XP gains
- ✅ **Unlimited Troops XP** - Troops level up instantly (supports NPC parties)
- ✅ **Troops XP Multiplier** - Multiply troop XP gains (supports NPC parties)

### Enemy Cheats (2 cheats)
- ✅ **Slow AI Movement Speed** - Enemy parties move slower
- ✅ **One-Hit Kills** - All enemies die in one hit

### Game Mechanics (6 cheats)
- ✅ **Freeze Daytime** - Stop time progression
- ✅ **Persuasion Always Succeed** - Critical success in all persuasion attempts
- ✅ **Recruit Prisoners Anytime** - Prisoners recruitable immediately
- ✅ **One Day Settlements Construction** - Buildings complete in 1 day
- ✅ **Instant Siege Construction** - Siege equipment builds instantly
- ✅ **Game Speed Multiplier** - Speed up campaign time (up to 16x)

---

## 🎯 Advanced Features

### Target Filtering System
Configure which heroes/parties receive cheats:
- **Apply to Player** - Apply cheats to yourself
- **Apply to Player Clan Members** - Companions, family members
- **Apply to Kingdom Rulers** - Kings and queens
- **Apply to Clan Leaders in Kingdoms** - Vassal clan leaders
- **Apply to Clan Members in Kingdoms** - All lords and ladies
- **Apply to Independent Clan Leaders** - Non-kingdom clan leaders
- **Apply to Independent Clan Members** - Members of independent clans
- **Apply to Player Kingdom Vassals** - Your vassals only
- **Apply to Player Kingdom Vassal Members** - Your vassal clan members

### Harmony Integration
3 cheats use advanced Harmony patches:
- **Trade Items No Decrease** - Prevents item loss during trades
- **Unlock All Smithy Parts** - Uses Reflection to unlock hidden parts
- **Renown Multiplier** - Intercepts renown gain events

---

## 📦 Installation

### Requirements

- **Mount & Blade II: Bannerlord** v1.4.8
- **[BLSE (Bannerlord Software Extender)](https://www.nexusmods.com/mountandblade2bannerlord/mods/1)** - **REQUIRED**
- **[Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)** v2.3.3+
- **[Bannerlord.ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018)** - **REQUIRED for MCM v5**
- **[Bannerlord.UIExtenderEx](https://www.nexusmods.com/mountandblade2bannerlord/mods/2102)** - **REQUIRED for MCM v5**
- **[Mod Configuration Menu v5 (MCM)](https://www.nexusmods.com/mountandblade2bannerlord/mods/612)** v5.10.1+

### Steps
1. Download and install **BLSE** first
2. Download and install all other required dependencies
3. Extract the `BannerWand` folder to:
```
Mount & Blade II Bannerlord/Modules/
```
4. Launch the game **through BLSE launcher**
5. **Load order (important):**
   - Bannerlord.Harmony
   - Bannerlord.ButterLib
   - Bannerlord.UIExtenderEx
   - Mod Configuration Menu v5
   - **BannerWand**

---

## ⚙️ Configuration

### Accessing Settings

1. Launch the game and go to **Main Menu**
2. Click **Mod Options** (or press `F1` if using MCM hotkey)
3. Select **BannerWand** from the mod list
4. Configure your desired cheats

### Settings Categories

All features can be enabled/disabled individually:

- **Player Cheats** - Health, morale, movement, etc.
  - Toggle each cheat on/off
  - Adjust multipliers (e.g., Movement Speed Multiplier: 1.0x to 10.0x)
  - Configure relationship boost amounts

- **Inventory Cheats** - Gold, food, trade, smithing
  - Set gold/influence amounts to add/remove
  - Enable unlimited food for parties
  - Configure smithing material thresholds

- **Stats Cheats** - XP, renown, attributes, skills
  - Set XP multipliers (1.0x to 100.0x)
  - Configure renown gain multipliers
  - Adjust attribute/focus point amounts

- **Enemy Cheats** - AI movement speed, one-hit kills
  - Slow down enemy party movement
  - Enable one-hit kills for all enemies

- **Game Cheats** - Time control, persuasion, construction
  - Freeze/pause campaign time
  - Speed up campaign time (up to 16x)
  - Enable instant construction

- **Target Settings** - Choose which NPCs receive cheats
  - Apply cheats to player only
  - Apply to player clan members
  - Apply to kingdom rulers
  - Apply to specific clans/kingdoms
  - Fine-tune targeting for maximum control

### Tips

- **Save Before Enabling**: Some cheats (like unlimited gold) can significantly alter gameplay. Save your game before enabling major cheats.
- **Start Small**: Begin with low multiplier values and gradually increase if needed.
- **Disable When Not Needed**: Disable cheats when not actively using them to avoid unintended side effects.
- **Check Compatibility**: Some cheats may conflict with other mods. See [Known Issues](#-known-issues--compatibility) section.

---

## ⚠️ Known Issues & Compatibility

### Stealth System Compatibility

Bannerlord's **stealth and disguise systems** may be affected by some cheats:

**Potentially Conflicting Cheats:**
- **Unlimited Health / Infinite Health** - May prevent stealth kills from working properly
- **One-Hit Kills** - May interfere with stealth attack mechanics
- **Unlimited Shield Durability** - Could affect stealth detection

**Recommendation:** Disable combat cheats when playing stealth missions (prison escapes, infiltration quests) for the best experience. You can safely re-enable them after completing stealth sections.

### Mod Compatibility

**Generally Compatible:**
- Most gameplay mods (diplomacy, economy, troop mods)
- Visual mods (graphics, UI)
- Content mods (new items, troops, factions)

**Potentially Incompatible:**
- Other cheat mods (may conflict with similar features)
- Mods that heavily modify game models (may override BannerWand's custom models)
- Mods that patch the same methods via Harmony (load order matters)

**Load Order Recommendation:**
1. Core dependencies (Harmony, ButterLib, UIExtenderEx)
2. MCM v5
3. BannerWand (should load after most mods)
4. Other gameplay mods

### Performance Considerations

- **Large Multipliers**: Very high multipliers (e.g., 100x XP) may cause performance issues in some scenarios
- **NPC Targeting**: Applying cheats to many NPCs simultaneously may impact performance
- **Campaign Speed**: Maximum game speed (16x) may cause lag on slower systems

### Troubleshooting

**Mod Not Loading:**
- Verify all dependencies are installed and enabled
- Check load order (see Installation section)
- Check the log files (see [Logging](#-logging) below)

**Cheats Not Working:**
- Verify the cheat is enabled in MCM
- Check target settings (some cheats require specific target filters)
- Ensure you're in the correct game state (e.g., some cheats only work in campaign, not in battles)
- Check log files for error messages

**Game Crashes:**
- Disable all cheats and re-enable one by one to identify the problematic cheat
- Check for mod conflicts (disable other mods temporarily)
- Verify game version compatibility
- Report the issue with log files attached

---

## 📝 Logging

BannerWand writes a daily log file to:
```
Documents/Mount and Blade II Bannerlord/Configs/ModLogs/BannerWand_yyyyMMdd.log
```
Log files older than 14 days are cleaned up automatically.

---

## 🌍 Localization

Supported languages:
- **English** (EN) - Full support
- **Russian** (RU) - Full support

---

## 📊 Statistics

- **Game Models**: 20 custom `DefaultXxxModel` overrides
- **Behaviors**: 6 campaign behaviors + 5 cheat handlers
- **Harmony Patches**: 13 patches
- **Interfaces**: 17 abstraction interfaces
- **Code Quality**:
  - C# 14.0 language version with modern features
  - Comprehensive XML documentation with cref references
  - Explicit typing (no `var` usage)
  - All magic numbers extracted to Constants folder
  - Optimized performance-critical code paths
  - Thread-safe implementations
  - Nullable reference types enabled throughout

---

## 🔧 Technical Details

### Architecture
BannerWand follows modern C# best practices with clean architecture principles:
- **Separation of Concerns**: Organized into Behaviors, Models, Core, Utils, Settings, Patches
- **Dependency Injection Ready**: Key components abstracted through interfaces (IModLogger, ITargetFilter, ICampaignDataCache, IHarmonyManager)
- **Performance Optimized**: Implements caching, object pooling, and efficient algorithms
- **Fully Documented**: Comprehensive XML documentation for all public APIs

### Technology Stack
- **Language**: C# 14.0 (compatible with .NET Framework 4.7.2)
- **Framework**: .NET Framework 4.7.2
- **Game API**: TaleWorlds Engine (Bannerlord v1.4.8)
- **Configuration**: MCM v5 (Mod Configuration Menu)
- **Patching**: Harmony 2.3.6

### Code Quality Features
- **C# 14 Language Version**: Project uses `<LangVersion>14.0</LangVersion>` for modern C# features
- **Explicit Typing**: All types explicitly declared for maximum readability (no `var` usage where readability is improved)
- **Pattern Matching**: Modern C# pattern matching for null checks and type checks
- **Constants Organization**: All magic numbers and hardcoded values extracted to `Constants/` folder
  - `GameConstants.cs` - Game-related constants (XP, renown, morale, speed thresholds, etc.)
  - `LogConstants.cs` - Logging-related constants (log levels, file names, formats, directory paths)
  - `MessageConstants.cs` - User-facing message strings (initialization messages, cheat descriptions, etc.)
- **Dynamic Version Management**: Version read from `SubModule.xml` via `VersionReader` utility (no hardcoded versions)
- **Nullable Reference Types**: Full `#nullable enable` support throughout codebase for better null safety
- **Performance Logging**: Built-in performance tracking and monitoring with scoped measurements
- **Null Safety**: Comprehensive null checking throughout codebase using modern C# patterns
- **Interface-Based Design**: Key components abstracted through interfaces for testability
  - `IModLogger` - Logging abstraction
  - `ILogPathResolver` - Log file path resolution abstraction
  - `ILogWriter` - Log file writing abstraction
  - `ITargetFilter` - Target filtering abstraction
  - `ISettlementCheatHelper` - Settlement targeting abstraction
  - `ICampaignDataCache` - Data caching abstraction
  - `IHarmonyManager` - Harmony patch management abstraction
  - `ICheatManager` - Cheat management operations abstraction
  - `IVersionReader` - Version reading abstraction
- **Clean Architecture**: Separation of concerns with Behaviors, Models, Core, Utils, Settings, Patches, Constants, Interfaces
- **Comprehensive XML Documentation**: All public and internal APIs fully documented with XML comments (`summary`, `param`, `returns`, `remarks`)
- **Code Standards**: No compiler warnings, clean codebase following C# best practices, consistent naming conventions
- **Code Organization**: Proper use of regions (Fields, Properties, Methods, Constants) for better readability

### For Developers
If you want to extend or modify BannerWand:
1. All major components have interfaces for easy mocking/testing
2. Use wrapper classes (e.g., ModLoggerWrapper) for dependency injection scenarios
3. See `/Interfaces` folder for abstraction contracts
4. Constants are centralized in `/Constants` folder
5. Full XML documentation available via IntelliSense

---

## 🙏 Credits

- **Inspiration**: WeMod (Wand) cheats
- **Framework**: MCMv5 for configuration interface
- **Patching**: Harmony 2.3.6 for advanced features
- **Community**: Bannerlord modding community for tools and support

---

## 📜 License

This mod is provided as-is for personal use. Feel free to modify for personal use, but please credit the original author if sharing modified versions.

## 🔗 Links

- **GitHub Repository**: [BannerWand Source Code](https://github.com/geniussjack/BannerWand)
- **Issue Tracker**: Report bugs and request features on GitHub Issues

---

**Enjoy your enhanced Bannerlord experience! 🎉**
