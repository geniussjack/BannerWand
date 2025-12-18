# BannerWand Architecture Documentation

## Overview

BannerWand is a comprehensive cheat mod for Mount & Blade II: Bannerlord, built with modern C# best practices and clean architecture principles. This document provides a detailed overview of the mod's architecture, design patterns, and component organization.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [Design Patterns](#design-patterns)
5. [Data Flow](#data-flow)
6. [Extension Points](#extension-points)

---

## Architecture Overview

BannerWand follows a **layered architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│              (MCM Settings UI - CheatSettings)            │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                    Application Layer                     │
│  (CheatManager, Behaviors, Handlers, Game Models)       │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                    Infrastructure Layer                  │
│  (HarmonyManager, ModLogger, Utils, Patches)            │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                    Domain Layer                         │
│         (Interfaces, Constants, Settings)               │
└─────────────────────────────────────────────────────────┘
```

### Key Principles

1. **Separation of Concerns**: Each component has a single, well-defined responsibility
2. **Dependency Inversion**: High-level modules depend on abstractions (interfaces), not concrete implementations
3. **Open/Closed Principle**: Open for extension, closed for modification
4. **Interface Segregation**: Small, focused interfaces rather than large, monolithic ones
5. **Single Responsibility**: Each class has one reason to change

---

## Project Structure

### Directory Organization

```
BannerWand-1.3/
├── Behaviors/          # Campaign and Mission behaviors
│   ├── Handlers/       # Cheat handler implementations
│   ├── AutoBuildingQueueBehavior.cs
│   ├── CombatCheatBehavior.cs
│   ├── FoodCheatBehavior.cs
│   ├── NPCCheatBehavior.cs
│   ├── PlayerCheatBehavior.cs
│   └── SkillXPCheatBehavior.cs
│
├── Constants/          # Centralized constants
│   ├── GameConstants.cs
│   ├── LogConstants.cs
│   └── MessageConstants.cs
│
├── Core/              # Core functionality
│   ├── Harmony/       # Harmony patch management
│   ├── CheatManager.cs
│   ├── HarmonyManager.cs
│   └── SubModule.cs
│
├── Interfaces/        # Abstraction contracts
│   ├── IAmmoCheatHandler.cs
│   ├── ICampaignDataCache.cs
│   ├── ICheatManager.cs
│   ├── IHarmonyManager.cs
│   ├── IHealthCheatHandler.cs
│   ├── ILogPathResolver.cs
│   ├── ILogWriter.cs
│   ├── IModLogger.cs
│   ├── INPCCheatHandler.cs
│   ├── IOneHitKillHandler.cs
│   ├── IPatchApplier.cs
│   ├── IPatchLogger.cs
│   ├── IPatchValidator.cs
│   ├── ISettlementCheatHelper.cs
│   ├── IShieldCheatHandler.cs
│   ├── ITargetFilter.cs
│   └── IVersionReader.cs
│
├── Models/            # Custom game models
│   ├── CustomBarterModel.cs
│   ├── CustomBuildingConstructionModel.cs
│   ├── CustomClanTierModel.cs
│   ├── CustomCombatXpModel.cs
│   ├── CustomGenericXpModel.cs
│   ├── CustomMobilePartyFoodConsumptionModel.cs
│   ├── CustomPartyMoraleModel.cs
│   ├── CustomPartySpeedModel.cs
│   ├── CustomPartyTrainingModel.cs
│   ├── CustomPartyWageModel.cs
│   ├── CustomPersuasionModel.cs
│   ├── CustomSettlementFoodModel.cs
│   ├── CustomSettlementGarrisonModel.cs
│   ├── CustomSettlementLoyaltyModel.cs
│   ├── CustomSettlementMilitiaModel.cs
│   ├── CustomSettlementProsperityModel.cs
│   ├── CustomSettlementSecurityModel.cs
│   ├── CustomSiegeEventModel.cs
│   └── CustomSmithingModel.cs
│
├── Patches/           # Harmony patches
│   ├── AgingPatch.cs
│   ├── AmmoConsumptionPatch.cs
│   ├── AutoBuildingQueuePatch.cs
│   ├── BarterableValuePatch.cs
│   ├── GameSpeedPatch.cs
│   ├── GarrisonWagesPatch.cs
│   ├── InventoryCapacityPatch.cs
│   ├── ItemBarterablePatch.cs
│   ├── ItemRosterTradePatch.cs
│   ├── MobilePartySpeedPatch.cs
│   ├── NavalSpeedPatch.cs
│   ├── RelationshipBoostPatch.cs
│   └── RenownMultiplierPatch.cs
│
├── Settings/          # MCM configuration
│   ├── CheatSettings.cs
│   └── CheatTargetSettings.cs
│
└── Utils/             # Utility classes
    ├── CampaignDataCache.cs
    ├── CampaignDataCacheWrapper.cs
    ├── CheatExtensions.cs
    ├── DictionaryPool.cs
    ├── LogPathResolver.cs
    ├── LogWriter.cs
    ├── ModLogger.cs
    ├── ModLoggerWrapper.cs
    ├── SettlementCheatHelper.cs
    ├── SettlementCheatHelperWrapper.cs
    ├── TargetFilter.cs
    ├── TargetFilterWrapper.cs
    └── VersionReader.cs
```

---

## Core Components

### 1. SubModule (Entry Point)

**Location**: `Core/SubModule.cs`

**Responsibility**: Main entry point for the mod, manages lifecycle and initialization.

**Key Methods**:
- `OnSubModuleLoad()` - Initializes logging system
- `OnBeforeInitialModuleScreenSetAsRoot()` - Registers game models and behaviors
- `OnMissionBehaviorInitialize()` - Registers mission behaviors

**Dependencies**:
- `ModLogger` - For logging
- `CheatManager` - For cheat initialization
- `HarmonyManager` - For Harmony patches

### 2. CheatManager (Facade)

**Location**: `Core/CheatManager.cs`

**Responsibility**: Central facade for all cheat operations, provides utility methods and validation.

**Key Features**:
- Static utility class (no state)
- Validates settings before applying cheats
- Provides performance tracking
- Centralized exception handling

**Usage Pattern**:
```csharp
CheatManager.Initialize();
CheatManager.ApplyUnlimitedHealth(hero);
```

### 3. HarmonyManager (Patch Coordinator)

**Location**: `Core/HarmonyManager.cs`

**Responsibility**: Manages all Harmony patches, handles initialization and cleanup.

**Key Features**:
- Automatic patch discovery via `[HarmonyPatch]` attributes
- Manual patch registration via `[HarmonyTargetMethod]`
- Patch validation and logging
- Cleanup on mod unload

**Architecture**:
- Uses `IPatchApplier` for patch application
- Uses `IPatchValidator` for validation
- Uses `IPatchLogger` for logging

### 4. Game Models

**Location**: `Models/*.cs`

**Responsibility**: Extend Bannerlord's game models to modify game behavior.

**Pattern**: All models extend base game models (e.g., `DefaultBarterModel`, `DefaultPersuasionModel`).

**Key Models**:
- `CustomBarterModel` - Trade and barter modifications
- `CustomPartySpeedModel` - Party movement speed
- `CustomGenericXpModel` - Skill XP multipliers
- `CustomCombatXpModel` - Combat XP multipliers
- `CustomPartyTrainingModel` - Troop training XP
- `CustomSmithingModel` - Smithing modifications
- And 13 more...

### 5. Behaviors

**Location**: `Behaviors/*.cs`

**Responsibility**: Implement campaign and mission behaviors for features not covered by game models.

**Types**:
- **Campaign Behaviors** (`CampaignBehaviorBase`):
  - `PlayerCheatBehavior` - Player-specific cheats
  - `NPCCheatBehavior` - NPC-specific cheats
  - `FoodCheatBehavior` - Food consumption cheats
  - `SkillXPCheatBehavior` - Skill XP cheats
  - `AutoBuildingQueueBehavior` - Building construction cheats

- **Mission Behaviors** (`MissionLogic`):
  - `CombatCheatBehavior` - Combat-related cheats (health, ammo, shields)

**Dependency Injection**: Behaviors accept interfaces in constructors for testability.

### 6. Handlers

**Location**: `Behaviors/Handlers/*.cs`

**Responsibility**: Encapsulate specific cheat logic, following the Handler pattern.

**Handlers**:
- `HealthCheatHandler` - Health-related cheats
- `AmmoCheatHandler` - Ammunition cheats
- `ShieldCheatHandler` - Shield durability cheats
- `OneHitKillHandler` - One-hit kill logic
- `NPCCheatHandler` - NPC-specific cheat logic

**Pattern**: Each handler implements an interface and is injected into behaviors.

---

## Design Patterns

### 1. Facade Pattern

**Implementation**: `CheatManager`

**Purpose**: Provides a simplified interface to complex subsystems (settings, validation, logging).

**Benefits**:
- Reduces coupling between components
- Simplifies API for consumers
- Centralizes common operations

### 2. Strategy Pattern

**Implementation**: Handler interfaces (`IHealthCheatHandler`, `IAmmoCheatHandler`, etc.)

**Purpose**: Encapsulates cheat algorithms and makes them interchangeable.

**Benefits**:
- Easy to add new cheat types
- Testable in isolation
- Follows Open/Closed Principle

### 3. Dependency Injection

**Implementation**: Interfaces + Wrapper classes

**Purpose**: Enables testability and loose coupling.

**Example**:
```csharp
public class CombatCheatBehavior : MissionLogic
{
    private readonly IHealthCheatHandler _healthCheatHandler;
    
    public CombatCheatBehavior(IHealthCheatHandler healthCheatHandler)
    {
        _healthCheatHandler = healthCheatHandler ?? throw new ArgumentNullException(nameof(healthCheatHandler));
    }
}
```

**Wrapper Classes**: `ModLoggerWrapper`, `TargetFilterWrapper`, `CampaignDataCacheWrapper` - Bridge between static utilities and interfaces.

### 4. Object Pooling

**Implementation**: `DictionaryPool`

**Purpose**: Reduces allocations in hot paths (frequent dictionary creation).

**Usage**:
```csharp
using (var pooledDict = DictionaryPool.Get())
{
    var dict = pooledDict.Value;
    // Use dictionary
    // Automatically returned to pool on dispose
}
```

### 5. Caching Pattern

**Implementation**: `CampaignDataCache`

**Purpose**: Caches expensive game API calls (e.g., `Hero.AllAliveHeroes`, `Clan.All`).

**Benefits**:
- Reduces API calls
- Improves performance
- Thread-safe implementation

### 6. Template Method Pattern

**Implementation**: Game Models (e.g., `CustomBarterModel`)

**Purpose**: Extends base game model behavior while preserving structure.

**Example**:
```csharp
public override ExplainedNumber GetBarterOfferChangeForHero(...)
{
    ExplainedNumber result = base.GetBarterOfferChangeForHero(...);
    // Modify result based on settings
    return result;
}
```

---

## Data Flow

### Initialization Flow

```
Game Start
    ↓
SubModule.OnSubModuleLoad()
    ↓
ModLogger.Initialize()
    ↓
SubModule.OnBeforeInitialModuleScreenSetAsRoot()
    ↓
HarmonyManager.Initialize()
    ├── Discover patches
    ├── Apply patches
    └── Validate patches
    ↓
CheatManager.Initialize()
    ├── Validate settings
    └── Show welcome message
    ↓
Register Game Models
    ├── CustomBarterModel
    ├── CustomPartySpeedModel
    └── ... (19 models)
    ↓
Register Campaign Behaviors
    ├── PlayerCheatBehavior
    ├── NPCCheatBehavior
    └── ... (5 behaviors)
    ↓
Ready
```

### Cheat Application Flow

```
User Enables Cheat in MCM
    ↓
CheatSettings.Property Changed
    ↓
Behavior.OnDailyTick() / OnHourlyTick()
    ↓
CheatManager.ValidateSettings()
    ↓
Handler.ApplyCheat()
    ├── Check target filter
    ├── Apply cheat logic
    └── Log operation
    ↓
Game State Modified
```

### Harmony Patch Flow

```
HarmonyManager.Initialize()
    ↓
Discover [HarmonyPatch] Attributes
    ↓
For Each Patch:
    ├── PatchValidator.Validate()
    ├── PatchApplier.Apply()
    └── PatchLogger.Log()
    ↓
Patches Active
    ↓
Game Method Called
    ↓
Harmony Intercepts
    ↓
Patch Logic Executes
    ↓
Original Method (Optionally)
```

---

## Extension Points

### Adding a New Cheat

1. **If using Game Model API**:
   - Create new model in `Models/` extending appropriate base model
   - Register in `SubModule.OnBeforeInitialModuleScreenSetAsRoot()`
   - Add settings to `CheatSettings.cs`

2. **If using Behavior**:
   - Add logic to appropriate behavior (or create new)
   - Add settings to `CheatSettings.cs`
   - Update `CheatManager` if needed

3. **If using Harmony Patch**:
   - Create patch class in `Patches/`
   - Add `[HarmonyPatch]` attribute
   - Add settings to `CheatSettings.cs`

### Adding a New Handler

1. Create interface in `Interfaces/` (e.g., `IYourCheatHandler`)
2. Create implementation in `Behaviors/Handlers/`
3. Inject into behavior constructor
4. Use in behavior logic

### Adding a New Utility

1. Create utility class in `Utils/`
2. If needed for DI, create interface in `Interfaces/`
3. Create wrapper class if needed
4. Use throughout codebase

---

## Technology Stack

- **Language**: C# 14.0
- **Framework**: .NET Framework 4.7.2
- **Game API**: TaleWorlds Engine (Bannerlord 1.3.x)
- **Configuration**: MCM v5 (Mod Configuration Menu)
- **Patching**: Harmony 2.3.6
- **Logging**: Custom `ModLogger` with file output

---

## Best Practices

1. **Always use interfaces** for dependencies
2. **Extract constants** to `Constants/` folder
3. **Add XML documentation** for all public APIs
4. **Use nullable reference types** (`#nullable enable`)
5. **Validate inputs** before processing
6. **Log operations** for debugging
7. **Handle exceptions** gracefully
8. **Cache expensive operations** using `CampaignDataCache`
9. **Use object pooling** for frequent allocations
10. **Follow naming conventions** (PascalCase for classes, camelCase for fields)

---

## Performance Considerations

1. **Caching**: Use `CampaignDataCache` instead of direct API calls
2. **Early Exits**: Return early if conditions aren't met
3. **Object Pooling**: Use `DictionaryPool` for temporary dictionaries
4. **Lazy Initialization**: Initialize expensive resources on demand
5. **Batch Operations**: Process multiple items in single pass when possible

---

## Testing Strategy

While BannerWand doesn't include unit tests (due to game API dependencies), the architecture supports testing:

1. **Interface-Based Design**: All dependencies are abstracted
2. **Wrapper Classes**: Bridge static utilities to interfaces
3. **Dependency Injection**: Behaviors accept interfaces in constructors
4. **Pure Functions**: Many utility methods are pure and testable

**Mocking Example**:
```csharp
var mockLogger = new Mock<IModLogger>();
var mockTargetFilter = new Mock<ITargetFilter>();
var handler = new HealthCheatHandler(mockLogger.Object, mockTargetFilter.Object);
```

---

## Conclusion

BannerWand's architecture emphasizes:
- **Modularity**: Clear separation of concerns
- **Extensibility**: Easy to add new features
- **Maintainability**: Clean, documented code
- **Performance**: Optimized hot paths
- **Testability**: Interface-based design

This architecture ensures the mod remains maintainable, performant, and easy to extend as Bannerlord evolves.

