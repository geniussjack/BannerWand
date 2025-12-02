# BannerWand

A comprehensive cheat mod for **Mount & Blade II: Bannerlord** with fully configurable options through **MCM (Mod Configuration Menu)**.

**All 31 cheats fully functional** - including previously non-working cheats now implemented via Harmony patches!

> ⚠️ **Version Notice**: 
> - **BannerWand v1.0.8** - Supports **Bannerlord 1.3.x** (project BannerWand-1.3) - includes Stealth Invisibility cheat
> - **BannerWand v1.0.8** - Supports **Bannerlord 1.2.12** (project BannerWand-1.2.12)

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
- ✅ **Stealth Invisibility** - Player is completely invisible and undetectable in stealth missions *(1.3.x only)*

### Inventory Cheats (7 cheats)
- ✅ **Edit Gold** - Add/remove gold (supports NPC targeting)
- ✅ **Edit Influence** - Add/remove influence (supports clan targeting)
- ✅ **Unlimited Food** - Party food never decreases (supports NPC parties)
- ✅ **Trade Items No Decrease** - Items stay in inventory after trading *(NEW: Harmony)*
- ✅ **Max Carrying Capacity** - Carry unlimited weight
- ✅ **Unlimited Smithy Materials** - Infinite iron and charcoal
- ✅ **Unlock All Smithy Parts** - All crafting parts unlocked *(NEW: Harmony + Reflection)*

### Stats Cheats (8 cheats)
- ✅ **Edit Attribute Points** - Add/remove attribute points (supports NPC targeting)
- ✅ **Edit Focus Points** - Add/remove focus points (supports NPC targeting)
- ✅ **Unlimited Renown** - Renown increases constantly
- ✅ **Renown Multiplier** - Multiply all renown gains *(NEW: Harmony)*
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

**For Bannerlord 1.3.x:**
- **Mount & Blade II: Bannerlord** v1.3.x (beta)
- **[BLSE (Bannerlord Software Extender)](https://www.nexusmods.com/mountandblade2bannerlord/mods/1)** - **REQUIRED**
- **[Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)** v2.3.3+
- **[Bannerlord.ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018)** - **REQUIRED for MCM v5**
- **[Bannerlord.UIExtenderEx](https://www.nexusmods.com/mountandblade2bannerlord/mods/2102)** - **REQUIRED for MCM v5**
- **[Mod Configuration Menu v5 (MCM)](https://www.nexusmods.com/mountandblade2bannerlord/mods/612)** v5.10.1+

**For Bannerlord 1.2.12:**
- **Mount & Blade II: Bannerlord** v1.2.12
- **[Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)** v2.3.3+
- **[Bannerlord.MBOptionScreen](https://www.nexusmods.com/mountandblade2bannerlord/mods/612)** (MCM v5)

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

Open **Mod Options** in the main menu → **BannerWand**

All features can be enabled/disabled individually. Configure:
- **Player Cheats** - Health, morale, movement, etc.
- **Inventory Cheats** - Gold, food, trade, smithing
- **Stats Cheats** - XP, renown, attributes, skills
- **Enemy Cheats** - AI movement speed, one-hit kills
- **Game Cheats** - Time control, persuasion, construction
- **Target Settings** - Choose which NPCs receive cheats

---

## ⚠️ Stealth System Compatibility (v1.3.x)

Bannerlord 1.3.x introduces new **stealth and disguise systems**. Some cheats may interfere with stealth gameplay:

### Potentially Conflicting Cheats
- **Unlimited Health / Infinite Health** - May prevent stealth kills from working properly
- **One-Hit Kills** - May interfere with stealth attack mechanics
- **Unlimited Shield Durability** - Could affect stealth detection

### Recommendation
**Disable combat cheats when playing stealth missions** (prison escapes, infiltration quests) for the best experience. You can safely re-enable them after completing stealth sections.

---

## 🌍 Localization

Supported languages:
- **English** (EN) - Full support
- **Russian** (RU) - Full support

To add translations, see [README_TRANSLATION.md](README_TRANSLATION.md)

---

## 📊 Statistics

- **Total Cheats**: 32 (1.3.x) / 31 (1.2.12)
- **Working**: 32/31 (100%)
- **NPC Support**: 10 cheats
- **Harmony Patches**: 4 cheats (1.3.x) / 3 cheats (1.2.12)
- **Game Models**: 12 custom models
- **Behaviors**: 7 custom behaviors
- **Interfaces**: 6 abstraction interfaces
- **Code Quality**: Comprehensive XML documentation, explicit typing, optimized C# patterns
- **Stealth Invisibility cheat** (1.3.x only)

---

## 🔧 Technical Details

### Architecture
BannerWand follows modern C# best practices with clean architecture principles:
- **Separation of Concerns**: Organized into Behaviors, Models, Core, Utils, Settings, Patches
- **Dependency Injection Ready**: Key components abstracted through interfaces (IModLogger, ITargetFilter, ICampaignDataCache, IHarmonyManager)
- **Performance Optimized**: Implements caching, object pooling, and efficient algorithms
- **Fully Documented**: Comprehensive XML documentation for all public APIs

### Technology Stack
- **Language**: C# latest (compatible with .NET Framework 4.7.2)
- **Framework**: .NET Framework 4.7.2
- **Game API**: TaleWorlds Engine (Bannerlord v1.3.x / v1.2.12)
- **Configuration**: MCM v5 (Mod Configuration Menu)
- **Patching**: Harmony 2.3.6

### Code Quality Features
- **C# 14 Language Version**: All projects use `<LangVersion>14.0</LangVersion>` for modern C# features
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
  - `ICampaignDataCache` - Data caching abstraction
  - `IHarmonyManager` - Harmony patch management abstraction
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

---

## 🔗 Links

- **Nexus Mods**: [BannerWand on Nexus](https://www.nexusmods.com/mountandblade2bannerlord/mods/)
- **Documentation**: See [CHEAT_AUDIT.md](CHEAT_AUDIT.md) for detailed cheat information
- **Translation Guide**: See [README_TRANSLATION.md](README_TRANSLATION.md)

---

**Enjoy your enhanced Bannerlord experience! 🎉**

**Version v1.0.8** - Comprehensive Code Refactoring & Optimization Release!

### Changelog v1.0.8
- 🆕 **Stealth Invisibility Cheat** (1.3.x only) - Player is completely invisible and undetectable in stealth missions
- ✅ **Bug Fixes** - Fixed critical bugs:
  - ModLogger: Fixed empty setter for LogFilePath property, fixed uninitialized state when file logging fails
  - CustomPartySpeedModel: Fixed loss of speed modifiers when creating new ExplainedNumber
  - CampaignDataCache: Fixed missing field declarations for cached properties
  - PlayerCheatBehavior: Removed duplicate ApplyUnlimitedSmithyMaterials() call from OnDailyTick()
- ✅ **Code Quality** - Removed hardcoded paths from .csproj files, standardized using directives order, fixed all compiler warnings and IDE suggestions
- ✅ **Constants** - All magic numbers and strings extracted to Constants folder (GameConstants, MessageConstants, LogConstants)
- ✅ **Version Management** - Dynamic version reading from SubModule.xml via VersionReader utility (no hardcoded versions)
- ✅ **Logging** - Improved log file location to use CommonApplicationData folder (platform-independent)
- ✅ **Initialization** - Fixed duplicate initialization messages, added first-launch welcome message with dynamic version
- ✅ **C# 14 Support** - Set LangVersion to 14.0 in all projects for modern C# features
- ✅ **XML Documentation** - Comprehensive XML documentation added to all public/internal members
- ✅ **Code Structure** - Improved code organization with proper regions, consistent naming conventions
- ✅ **IDE Warnings Fixed** - Resolved all IDE0060, IDE0270, IDE0300, and RCS1163 warnings
- ✅ **Fixed All Compiler Warnings** - Resolved all CS8632 nullable annotation warnings by adding `#nullable enable` throughout codebase
- ✅ **Removed Unnecessary Suppressions** - Cleaned up IDE0079 warnings in ModLogger.cs
- ✅ **Improved Null Safety** - Full nullable reference type support with proper annotations
- ✅ **Code Quality Improvements** - Explicit typing, comprehensive XML documentation, clean codebase
- ✅ **Constants Organization** - All hardcoded values properly organized in Constants folder
- ✅ **Interface-Based Design** - Enhanced abstraction layer with comprehensive interfaces
- ✅ **Bug Fixes** - Fixed ModLogger initialization, CustomPartySpeedModel modifier preservation, CampaignDataCache field declarations
- ✅ **Version Management** - Dynamic version reading from SubModule.xml
- ✅ **C# 14 Support** - Set LangVersion to 14.0 for modern C# features
- 🎯 **Dual Version Support** - Single solution supporting both Bannerlord 1.3.x and 1.2.12
- 📦 **Separate Projects** - BannerWand-1.3 for 1.3.x, BannerWand-1.2.12 for 1.2.12
- 🔧 **Namespace Separation** - BannerWandRetro namespace for 1.2.12 backend, BannerWand.dll output
- ✅ **Unified Build System** - Release-1.3 and Release-1.2.12 configurations with automatic deployment

### Changelog v1.0.7
- 🎯 **Dual Version Support** - Single solution supporting both Bannerlord 1.3.x and 1.2.12
- 📦 **Separate Projects** - BannerWand-1.3 for 1.3.x, BannerWand-1.2.12 for 1.2.12
- 🔧 **Namespace Separation** - BannerWandRetro namespace for 1.2.12 backend, BannerWand.dll output
- ✅ **Unified Build System** - Release-1.3 and Release-1.2.12 configurations with automatic deployment

### Changelog v2.0.0 (1.3.x only)
- 🎮 **Bannerlord 1.3.x Support** - Full compatibility with the latest Bannerlord beta
- 📦 **MCM v5 Integration** - Updated to work with MCM v5 and its dependencies
- 🔧 **BLSE Requirement** - Now requires BLSE (Bannerlord Software Extender)
- ⚠️ **Stealth System Awareness** - Added compatibility notes for new stealth features
- ✅ **All GameModels Verified** - All custom models tested for 1.3.x compatibility
- ✅ **Harmony Patches Updated** - All patches verified to work with 1.3.x API
- 📝 **Documentation Updated** - Clear version requirements and installation instructions
- ❌ **Breaking Change**: NOT compatible with Bannerlord 1.2.12 or earlier
- 🔄 **Code Refactoring** - Comprehensive optimization and refactoring:
  - **C# Language Version** - Updated project to use latest C# version explicitly
  - **Constants Centralization** - All hardcoded values moved to `Constants/GameConstants.cs`
  - **Code Cleanup** - Removed unused using directives, improved formatting
  - **Modern C# Patterns** - Utilized latest C# features and patterns
  - **Architecture Improvements** - Enhanced modularity and maintainability
