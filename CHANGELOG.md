# Changelog

All notable changes to BannerWand mod will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.1] - 2024-XX-XX

### Added
- Comprehensive refactoring and code optimization
- C# 14.0 language version fully utilized across all projects
- All magic numbers extracted to Constants folder (including speed multipliers, health thresholds)
- Created `ICheatManager` and `IVersionReader` interfaces for improved modularity and testability
- Enhanced XML documentation throughout codebase with professional formatting (added `<see cref>` and `<paramref>` tags)
- Code style unification and formatting improvements
- Removed unused using directives
- Optimized complex code sections
- Eliminated code duplication (fixed duplicate logging in CheatExtensions)
- Fixed all compiler warnings and linter errors (~30+ warnings resolved)

### Changed
- Constants organization - Added new constants:
  - `InfiniteHealthBonusThresholdMultiplier` - For Infinite Health detection
  - `MaxSpeedMultiplier` - Maximum speed multiplier cap
  - `SpeedChangeDetectionThreshold` - For speed change logging
  - `MultiplierFactorBase` - Base value for multiplier calculations
- Interface-based design - Created `ICheatManager` and `IVersionReader` interfaces for dependency injection and testing
- Documentation - Improved XML documentation for all public APIs with cref references and paramref tags
- Code quality - Enhanced maintainability, readability, and consistency
- Performance optimizations - Verified CampaignDataCache usage, optimized conditional access patterns
- Null safety - Fixed all CS8602 warnings with proper null checks and conditional access operators

### Fixed
- All compiler warnings and linter errors
- Null reference exceptions with proper null checks
- Code style violations
- Performance issues in hot paths

---

## [1.0.9] - 2024-XX-XX

### Added
- Comprehensive refactoring and optimization
- C# 14.0 language version fully utilized across all projects
- All magic numbers extracted to Constants folder (including `SmithingMaterialReplenishThreshold`)
- Improved code readability with explicit typing (replaced `var` in documentation examples)
- Enhanced XML documentation throughout codebase with professional formatting
- Code style unification and formatting improvements
- Removed unused using directives (BarterSystem.Barterables)
- Optimized complex code sections (GetActiveCheatCount using ternary operators)
- Eliminated code duplication (centralized exception handling in CheatManager and CheatExtensions)
- Fixed all compiler warnings and linter errors

### Changed
- Exception handling - Created `LogException` helper methods to reduce code duplication:
  - `CheatManager.LogException()` - Centralized exception logging for CheatManager (replaced 10 duplicate blocks)
  - `CheatExtensions.LogException()` - Centralized exception logging for CheatExtensions (replaced 17 duplicate blocks)
  - `PlayerCheatBehavior.LogException()` - Centralized exception logging for PlayerCheatBehavior (replaced 4 duplicate blocks)
- Constants organization - Added `SmithingMaterialReplenishThreshold` constant to both versions
- Documentation - Improved XML documentation for all public APIs with cref references
- Code quality - Enhanced maintainability, readability, and consistency

### Fixed
- All compiler warnings and linter errors
- Code duplication issues
- Exception handling redundancy

---

## [1.0.8] - 2024-XX-XX

### Added
- C# 14.0 language version support
- Comprehensive XML documentation for all public/internal members
- Dynamic version reading from SubModule.xml via VersionReader utility (no hardcoded versions)
- Improved log file location to use CommonApplicationData folder (platform-independent)
- First-launch welcome message with dynamic version
- Code organization with proper regions, consistent naming conventions

### Changed
- Constants - All magic numbers and strings extracted to Constants folder (GameConstants, MessageConstants, LogConstants)
- Version management - Dynamic version reading from SubModule.xml
- Logging - Improved log file location to use CommonApplicationData folder
- Initialization - Fixed duplicate initialization messages, added first-launch welcome message with dynamic version
- Code structure - Improved code organization with proper regions, consistent naming conventions

### Fixed
- ModLogger: Fixed empty setter for LogFilePath property, fixed uninitialized state when file logging fails
- CustomPartySpeedModel: Fixed loss of speed modifiers when creating new ExplainedNumber
- CampaignDataCache: Fixed missing field declarations for cached properties
- PlayerCheatBehavior: Removed duplicate ApplyUnlimitedSmithyMaterials() call from OnDailyTick()
- Code quality - Removed hardcoded paths from .csproj files, standardized using directives order, fixed all compiler warnings and IDE suggestions
- IDE warnings - Resolved all IDE0060, IDE0270, IDE0300, and RCS1163 warnings
- Compiler warnings - Resolved all CS8632 nullable annotation warnings by adding `#nullable enable` throughout codebase
- Removed unnecessary suppressions - Cleaned up IDE0079 warnings in ModLogger.cs
- Improved null safety - Full nullable reference type support with proper annotations
- Code quality improvements - Explicit typing, comprehensive XML documentation, clean codebase
- Constants organization - All hardcoded values properly organized in Constants folder
- Interface-based design - Enhanced abstraction layer with comprehensive interfaces
- Version management - Dynamic version reading from SubModule.xml
- C# 14 support - Set LangVersion to 14.0 for modern C# features

---

## [1.0.7] - 2024-XX-XX

### Added
- Dual version support - Single solution supporting both Bannerlord 1.3.x and 1.2.12
- Separate projects - BannerWand-1.3 for 1.3.x, BannerWand-1.2.12 for 1.2.12
- Namespace separation - BannerWandRetro namespace for 1.2.12 backend, BannerWand.dll output
- Unified build system - Release-1.3 and Release-1.2.12 configurations with automatic deployment

### Changed
- Project structure - Reorganized to support dual version builds
- Build configurations - Added Release-1.3 and Release-1.2.12 configurations

---

## [2.0.0] - 2024-XX-XX (1.3.x only)

### Added
- Bannerlord 1.3.x support - Full compatibility with the latest Bannerlord beta
- MCM v5 integration - Updated to work with MCM v5 and its dependencies
- BLSE requirement - Now requires BLSE (Bannerlord Software Extender)
- Stealth system awareness - Added compatibility notes for new stealth features
- All GameModels verified - All custom models tested for 1.3.x compatibility
- Harmony patches updated - All patches verified to work with 1.3.x API
- Documentation updated - Clear version requirements and installation instructions

### Changed
- Code refactoring - Comprehensive optimization and refactoring:
  - C# language version - Updated project to use latest C# version explicitly
  - Constants centralization - All hardcoded values moved to `Constants/GameConstants.cs`
  - Code cleanup - Removed unused using directives, improved formatting
  - Modern C# patterns - Utilized latest C# features and patterns
  - Architecture improvements - Enhanced modularity and maintainability

### Breaking Changes
- NOT compatible with Bannerlord 1.2.12 or earlier - Use BannerWand v1.0.9 for 1.2.12

---

## Feature List

### Player Cheats (10 cheats)
- ✅ Unlimited Health - Never die in combat
- ✅ Unlimited Horse Health - Your mount never dies
- ✅ Unlimited Shield Durability - Shields never break
- ✅ Unlimited Party Health - All party members invincible (supports NPC targeting)
- ✅ Ignore Melee Damage - Immune to melee attacks
- ✅ Max Morale - Party morale always at maximum
- ✅ Movement Speed Multiplier - Travel faster on campaign map
- ✅ Barter Always Accepted - All trade offers accepted
- ✅ Unlimited Smithy Stamina - Never run out of crafting stamina
- ✅ Max Character Relationship - Improve relationships with NPCs (supports NPC targeting)

### Inventory Cheats (7 cheats)
- ✅ Edit Gold - Add/remove gold (supports NPC targeting)
- ✅ Edit Influence - Add/remove influence (supports clan targeting)
- ✅ Unlimited Food - Party food never decreases (supports NPC parties)
- ✅ Trade Items No Decrease - Items stay in inventory after trading (Harmony)
- ✅ Max Carrying Capacity - Carry unlimited weight
- ✅ Unlimited Smithy Materials - Infinite iron and charcoal
- ✅ Unlock All Smithy Parts - All crafting parts unlocked (Harmony + Reflection)

### Stats Cheats (8 cheats)
- ✅ Edit Attribute Points - Add/remove attribute points (supports NPC targeting)
- ✅ Edit Focus Points - Add/remove focus points (supports NPC targeting)
- ✅ Unlimited Renown - Renown increases constantly
- ✅ Renown Multiplier - Multiply all renown gains (Harmony)
- ✅ Unlimited Skill XP - All skills level up quickly (supports NPC targeting)
- ✅ Skill XP Multiplier - Multiply skill XP gains
- ✅ Unlimited Troops XP - Troops level up instantly (supports NPC parties)
- ✅ Troops XP Multiplier - Multiply troop XP gains (supports NPC parties)

### Enemy Cheats (2 cheats)
- ✅ Slow AI Movement Speed - Enemy parties move slower
- ✅ One-Hit Kills - All enemies die in one hit

### Game Mechanics (6 cheats)
- ✅ Freeze Daytime - Stop time progression
- ✅ Persuasion Always Succeed - Critical success in all persuasion attempts
- ✅ Recruit Prisoners Anytime - Prisoners recruitable immediately
- ✅ One Day Settlements Construction - Buildings complete in 1 day
- ✅ Instant Siege Construction - Siege equipment builds instantly
- ✅ Game Speed Multiplier - Speed up campaign time (up to 16x)

---

## Version History Summary

- **v1.1.1** - Major code optimization & refactoring release
- **v1.0.9** - Code optimization and exception handling improvements
- **v1.0.8** - Bug fixes, constants organization, version management
- **v1.0.7** - Dual version support (1.3.x and 1.2.12)
- **v2.0.0** - Bannerlord 1.3.x support (breaking change from 1.2.12)

---

## Notes

- All versions are tested and verified to work with their respective game versions
- Breaking changes are clearly marked
- For detailed feature descriptions, see [README.md](README.md)
- For API documentation, see [Documentation/API.md](Documentation/API.md)
- For developer guide, see [Documentation/DEVELOPER_GUIDE.md](Documentation/DEVELOPER_GUIDE.md)

