# BannerWand API Documentation

## Overview

This document provides a comprehensive API reference for BannerWand mod developers. It covers all public APIs, interfaces, and extension points.

## Table of Contents

1. [Core APIs](#core-apis)
2. [Interfaces](#interfaces)
3. [Utilities](#utilities)
4. [Constants](#constants)
5. [Settings](#settings)
6. [Extension Points](#extension-points)

---

## Core APIs

### CheatManager

**Namespace**: `BannerWand.Core`

**Type**: `static class`

**Purpose**: Central facade for cheat operations.

#### Methods

##### `Initialize(bool showMessage = true)`

Initializes the cheat manager and displays welcome message.

**Parameters**:
- `showMessage` (bool): Whether to show initialization message. Default: `true`.

**Example**:
```csharp
CheatManager.Initialize();
```

##### `GetActiveCheatCount()`

Gets the total number of currently active cheats.

**Returns**: `int` - Number of active cheats.

**Example**:
```csharp
int activeCount = CheatManager.GetActiveCheatCount();
```

##### `ApplyUnlimitedHealth(Hero hero)`

Applies unlimited health cheat to a hero.

**Parameters**:
- `hero` (Hero): The hero to apply the cheat to.

**Example**:
```csharp
CheatManager.ApplyUnlimitedHealth(Hero.MainHero);
```

##### `ApplyMaxMorale(MobileParty party)`

Applies maximum morale to a party.

**Parameters**:
- `party` (MobileParty): The party to apply the cheat to.

**Example**:
```csharp
CheatManager.ApplyMaxMorale(MobileParty.MainParty);
```

---

### HarmonyManager

**Namespace**: `BannerWand.Core`

**Type**: `static class`

**Purpose**: Manages Harmony patches.

#### Properties

##### `Instance`

Gets the Harmony instance.

**Type**: `HarmonyLib.Harmony?`

**Example**:
```csharp
if (HarmonyManager.Instance != null)
{
    // Harmony is initialized
}
```

#### Methods

##### `Initialize()`

Initializes Harmony and applies all patches.

**Returns**: `bool` - `true` if initialization succeeded, `false` otherwise.

**Example**:
```csharp
bool success = HarmonyManager.Initialize();
```

##### `Uninitialize()`

Removes all Harmony patches.

**Example**:
```csharp
HarmonyManager.Uninitialize();
```

---

## Interfaces

### IModLogger

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Abstraction for logging operations.

#### Methods

##### `LogInfo(string message)`

Logs an informational message.

**Parameters**:
- `message` (string): The message to log.

##### `LogWarning(string message)`

Logs a warning message.

**Parameters**:
- `message` (string): The warning message.

##### `LogError(string message)`

Logs an error message.

**Parameters**:
- `message` (string): The error message.

##### `LogException(Exception exception, string context)`

Logs an exception with context.

**Parameters**:
- `exception` (Exception): The exception to log.
- `context` (string): Context information.

**Implementation**: `ModLoggerWrapper` (wraps static `ModLogger`)

---

### ITargetFilter

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Abstraction for target filtering logic.

#### Methods

##### `ShouldApplyToPlayer(Hero hero)`

Determines if cheat should apply to player.

**Parameters**:
- `hero` (Hero): The hero to check.

**Returns**: `bool` - `true` if should apply, `false` otherwise.

##### `ShouldApplyToPlayerClan(Hero hero)`

Determines if cheat should apply to player clan members.

**Parameters**:
- `hero` (Hero): The hero to check.

**Returns**: `bool` - `true` if should apply, `false` otherwise.

**Implementation**: `TargetFilterWrapper` (wraps static `TargetFilter`)

---

### ICampaignDataCache

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Abstraction for campaign data caching.

#### Properties

##### `AllAliveHeroes`

Gets cached list of all alive heroes.

**Type**: `MBReadOnlyList<Hero>`

##### `AllClans`

Gets cached list of all clans.

**Type**: `MBReadOnlyList<Clan>`

#### Methods

##### `Refresh()`

Refreshes the cache.

**Implementation**: `CampaignDataCacheWrapper` (wraps static `CampaignDataCache`)

---

### IHealthCheatHandler

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Handles health-related cheats.

#### Methods

##### `ApplyUnlimitedHealth(Agent agent)`

Applies unlimited health to an agent.

**Parameters**:
- `agent` (Agent): The agent to apply the cheat to.

##### `ApplyUnlimitedHorseHealth(Agent agent)`

Applies unlimited health to agent's mount.

**Parameters**:
- `agent` (Agent): The agent whose mount to modify.

**Implementation**: `HealthCheatHandler`

---

### IAmmoCheatHandler

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Handles ammunition-related cheats.

#### Methods

##### `ApplyUnlimitedAmmo(Agent agent)`

Applies unlimited ammunition to an agent.

**Parameters**:
- `agent` (Agent): The agent to apply the cheat to.

**Implementation**: `AmmoCheatHandler`

---

### IShieldCheatHandler

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Handles shield-related cheats.

#### Methods

##### `ApplyUnlimitedShieldDurability(Agent agent)`

Applies unlimited shield durability to an agent.

**Parameters**:
- `agent` (Agent): The agent to apply the cheat to.

**Implementation**: `ShieldCheatHandler`

---

### IOneHitKillHandler

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Handles one-hit kill logic.

#### Methods

##### `ApplyOneHitKill(Agent attacker, Agent victim, in Blow blow)`

Applies one-hit kill damage.

**Parameters**:
- `attacker` (Agent): The attacking agent.
- `victim` (Agent): The victim agent.
- `blow` (Blow): The blow information.

**Implementation**: `OneHitKillHandler`

---

### INPCCheatHandler

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Handles NPC-specific cheat logic.

#### Methods

##### `ApplyNPCCheats(Hero hero)`

Applies NPC-specific cheats to a hero.

**Parameters**:
- `hero` (Hero): The hero to apply cheats to.

**Implementation**: `NPCCheatHandler`

---

### IHarmonyManager

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Abstraction for Harmony patch management.

#### Methods

##### `Initialize()`

Initializes Harmony patches.

**Returns**: `bool` - `true` if successful.

##### `Uninitialize()`

Removes all patches.

**Implementation**: `HarmonyManagerWrapper` (wraps static `HarmonyManager`)

---

### ICheatManager

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Abstraction for cheat management operations.

#### Methods

##### `Initialize(bool showMessage)`

Initializes the cheat manager.

**Parameters**:
- `showMessage` (bool): Whether to show message.

##### `GetActiveCheatCount()`

Gets active cheat count.

**Returns**: `int`

**Note**: Currently, `CheatManager` is static and doesn't implement this interface. This is reserved for future refactoring.

---

### ISettlementCheatHelper

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Helper for settlement-related cheat operations.

#### Methods

##### `ShouldApplyToSettlement(Settlement settlement)`

Determines if cheat should apply to settlement.

**Parameters**:
- `settlement` (Settlement): The settlement to check.

**Returns**: `bool`

**Implementation**: `SettlementCheatHelperWrapper` (wraps static `SettlementCheatHelper`)

---

### IVersionReader

**Namespace**: `BannerWand.Interfaces`

**Purpose**: Reads mod version information.

#### Methods

##### `ReadVersion()`

Reads the mod version from SubModule.xml.

**Returns**: `string` - The version string.

**Implementation**: `VersionReader` (static utility)

---

## Utilities

### ModLogger

**Namespace**: `BannerWand.Utils`

**Type**: `static class`

**Purpose**: Centralized logging system.

#### Properties

##### `LogFilePath`

Gets the log file path.

**Type**: `string`

#### Methods

##### `Info(string message)`

Logs an informational message.

**Parameters**:
- `message` (string): The message.

##### `Warning(string message)`

Logs a warning message.

**Parameters**:
- `message` (string): The warning.

##### `Error(string message)`

Logs an error message.

**Parameters**:
- `message` (string): The error.

##### `Exception(Exception exception, string context)`

Logs an exception.

**Parameters**:
- `exception` (Exception): The exception.
- `context` (string): Context information.

**Example**:
```csharp
ModLogger.Info("Cheat applied successfully");
ModLogger.Error("Failed to apply cheat");
ModLogger.Exception(ex, "In ApplyUnlimitedHealth");
```

---

### TargetFilter

**Namespace**: `BannerWand.Utils`

**Type**: `static class`

**Purpose**: Filters targets for cheat application.

#### Methods

##### `ShouldApplyToPlayer(Hero hero)`

Checks if cheat should apply to player.

**Parameters**:
- `hero` (Hero): The hero.

**Returns**: `bool`

##### `ShouldApplyToPlayerClan(Hero hero)`

Checks if cheat should apply to player clan.

**Parameters**:
- `hero` (Hero): The hero.

**Returns**: `bool`

**Example**:
```csharp
if (TargetFilter.ShouldApplyToPlayer(hero))
{
    // Apply cheat
}
```

---

### CampaignDataCache

**Namespace**: `BannerWand.Utils`

**Type**: `static class`

**Purpose**: Caches expensive game API calls.

#### Properties

##### `AllAliveHeroes`

Gets cached list of all alive heroes.

**Type**: `MBReadOnlyList<Hero>`

##### `AllClans`

Gets cached list of all clans.

**Type**: `MBReadOnlyList<Clan>`

#### Methods

##### `Refresh()`

Refreshes the cache.

**Example**:
```csharp
// Instead of: Hero.AllAliveHeroes (expensive)
var heroes = CampaignDataCache.AllAliveHeroes; // Cached
```

---

### DictionaryPool

**Namespace**: `BannerWand.Utils`

**Type**: `static class`

**Purpose**: Object pool for dictionaries to reduce allocations.

#### Methods

##### `Get<TKey, TValue>()`

Gets a dictionary from the pool.

**Returns**: `PooledDictionary<TKey, TValue>` - Disposable wrapper.

**Example**:
```csharp
using (var pooled = DictionaryPool.Get<int, bool>())
{
    var dict = pooled.Value;
    dict[1] = true;
    // Automatically returned to pool on dispose
}
```

---

### CheatExtensions

**Namespace**: `BannerWand.Utils`

**Type**: `static class`

**Purpose**: Extension methods for cheat operations.

#### Methods

##### `ApplyUnlimitedHealth(this Hero hero)`

Extension method to apply unlimited health to a hero.

**Parameters**:
- `hero` (Hero): The hero.

**Example**:
```csharp
Hero.MainHero.ApplyUnlimitedHealth();
```

---

### VersionReader

**Namespace**: `BannerWand.Utils`

**Type**: `static class`

**Purpose**: Reads mod version from SubModule.xml.

#### Methods

##### `ReadVersion()`

Reads the version string.

**Returns**: `string` - Version (e.g., "v1.1.1").

**Example**:
```csharp
string version = VersionReader.ReadVersion();
```

---

## Constants

### GameConstants

**Namespace**: `BannerWand.Constants`

**Type**: `static class`

**Purpose**: Game-related constants.

#### Constants

- `AbsoluteMaxGameSpeed` (float): Maximum game speed multiplier (16.0f)
- `MaxSpeedMultiplier` (float): Maximum speed multiplier (10.0f)
- `InfiniteHealthBonusThresholdMultiplier` (float): Health threshold for infinite health detection
- `MultiplierFactorBase` (float): Base value for multiplier calculations (1.0f)
- `TroopXPLogInterval` (int): Interval for logging troop XP (10)
- And more...

**Example**:
```csharp
if (speed > GameConstants.AbsoluteMaxGameSpeed)
{
    speed = GameConstants.AbsoluteMaxGameSpeed;
}
```

---

### MessageConstants

**Namespace**: `BannerWand.Constants`

**Type**: `static class`

**Purpose**: User-facing message strings.

#### Constants

- `InitializationMessage` (string): Welcome message
- `SettingsError` (string): Settings error message
- And more...

---

### LogConstants

**Namespace**: `BannerWand.Constants`

**Type**: `static class`

**Purpose**: Logging-related constants.

#### Constants

- `LogFileName` (string): Log file name
- `LogDirectory` (string): Log directory path
- And more...

---

## Settings

### CheatSettings

**Namespace**: `BannerWand.Settings`

**Type**: `class` (MCM settings)

**Purpose**: MCM configuration for all cheats.

#### Properties

All cheat toggles and multipliers are properties of this class.

**Example**:
```csharp
if (CheatSettings.Instance.UnlimitedHealth)
{
    // Apply unlimited health
}
```

---

### CheatTargetSettings

**Namespace**: `BannerWand.Settings`

**Type**: `class` (MCM settings)

**Purpose**: MCM configuration for target filtering.

#### Properties

All target filter toggles are properties of this class.

**Example**:
```csharp
if (CheatTargetSettings.Instance.ApplyToPlayer)
{
    // Apply to player
}
```

---

## Extension Points

### Creating a Custom Game Model

```csharp
namespace BannerWand.Models
{
    public class CustomYourModel : DefaultYourModel
    {
        public override ExplainedNumber GetYourValue(...)
        {
            ExplainedNumber result = base.GetYourValue(...);
            
            if (CheatSettings.Instance.YourCheatEnabled)
            {
                // Modify result
            }
            
            return result;
        }
    }
}
```

Register in `SubModule.OnBeforeInitialModuleScreenSetAsRoot()`:
```csharp
gameStarter.AddModel(new CustomYourModel());
```

---

### Creating a Custom Behavior

```csharp
namespace BannerWand.Behaviors
{
    public class CustomBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }
        
        private void OnHourlyTick()
        {
            // Your logic
        }
    }
}
```

Register in `SubModule.OnBeforeInitialModuleScreenSetAsRoot()`:
```csharp
gameStarter.AddBehavior(new CustomBehavior());
```

---

### Creating a Harmony Patch

```csharp
namespace BannerWand.Patches
{
    [HarmonyPatch(typeof(YourClass), "YourMethod")]
    public class YourPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            // Your logic
            __result = true; // Modify result
            return false; // Skip original
        }
    }
}
```

The patch will be automatically discovered and applied by `HarmonyManager`.

---

## Best Practices

1. **Use interfaces** for dependencies
2. **Cache expensive operations** using `CampaignDataCache`
3. **Use object pooling** for temporary collections
4. **Extract constants** to `Constants/` folder
5. **Log operations** using `ModLogger`
6. **Validate inputs** before processing
7. **Handle exceptions** gracefully
8. **Follow naming conventions**

---

## Conclusion

This API documentation covers all public APIs in BannerWand. For architecture details, see [ARCHITECTURE.md](ARCHITECTURE.md). For usage examples, see [EXAMPLES.md](EXAMPLES.md).

