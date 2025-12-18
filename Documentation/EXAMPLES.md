# BannerWand Usage Examples

## Overview

This document provides practical code examples for common tasks when working with BannerWand.

## Table of Contents

1. [Basic Cheat Application](#basic-cheat-application)
2. [Game Model Examples](#game-model-examples)
3. [Behavior Examples](#behavior-examples)
4. [Harmony Patch Examples](#harmony-patch-examples)
5. [Handler Examples](#handler-examples)
6. [Utility Examples](#utility-examples)
7. [Settings Examples](#settings-examples)

---

## Basic Cheat Application

### Applying Unlimited Health

```csharp
using BannerWand.Core;
using TaleWorlds.CampaignSystem;

// Apply to main hero
CheatManager.ApplyUnlimitedHealth(Hero.MainHero);

// Apply to specific hero
Hero targetHero = Hero.AllAliveHeroes.FirstOrDefault(h => h.Name == "YourHero");
if (targetHero != null)
{
    CheatManager.ApplyUnlimitedHealth(targetHero);
}
```

### Applying Max Morale

```csharp
using BannerWand.Core;
using TaleWorlds.CampaignSystem.Party;

// Apply to main party
CheatManager.ApplyMaxMorale(MobileParty.MainParty);

// Apply to all parties
foreach (MobileParty party in MobileParty.All)
{
    if (party != null && party.IsActive)
    {
        CheatManager.ApplyMaxMorale(party);
    }
}
```

### Using Extension Methods

```csharp
using BannerWand.Utils;
using TaleWorlds.CampaignSystem;

// Extension method for unlimited health
Hero.MainHero.ApplyUnlimitedHealth();

// Extension method for other cheats
Hero.MainHero.ApplyMaxRelationship();
```

---

## Game Model Examples

### Creating a Custom XP Model

```csharp
#nullable enable
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using BannerWand.Constants;
using BannerWand.Settings;

namespace BannerWand.Models
{
    public class CustomYourXpModel : DefaultGenericXpModel
    {
        public override float GetXpMultiplier(Hero hero)
        {
            float baseMultiplier = base.GetXpMultiplier(hero);

            if (CheatSettings.Instance?.YourXpMultiplierEnabled == true)
            {
                float multiplier = CheatSettings.Instance.YourXpMultiplier - GameConstants.MultiplierFactorBase;
                return baseMultiplier * (1.0f + multiplier);
            }

            return baseMultiplier;
        }
    }
}
```

### Creating a Custom Speed Model

```csharp
#nullable enable
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using BannerWand.Constants;
using BannerWand.Settings;

namespace BannerWand.Models
{
    public class CustomYourSpeedModel : DefaultPartySpeedCalculatingModel
    {
        public override ExplainedNumber CalculateFinalSpeed(
            MobileParty party,
            ExplainedNumber baseSpeed,
            bool ignorePartySize = false)
        {
            ExplainedNumber result = base.CalculateFinalSpeed(party, baseSpeed, ignorePartySize);

            if (CheatSettings.Instance?.YourSpeedMultiplierEnabled == true)
            {
                float multiplier = CheatSettings.Instance.YourSpeedMultiplier;
                
                // Cap at maximum
                if (multiplier > GameConstants.AbsoluteMaxGameSpeed)
                {
                    multiplier = GameConstants.AbsoluteMaxGameSpeed;
                }

                result.AddFactor(multiplier - GameConstants.MultiplierFactorBase, 
                    new TextObject("{=BW_YourSpeed}Your Speed Multiplier"));
            }

            return result;
        }
    }
}
```

---

## Behavior Examples

### Creating a Campaign Behavior

```csharp
#nullable enable
using System;
using TaleWorlds.CampaignSystem;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Behaviors
{
    public class YourCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Save state if needed
        }

        private void OnHourlyTick()
        {
            try
            {
                if (CheatSettings.Instance?.YourCheatEnabled != true)
                {
                    return;
                }

                // Apply cheat to all heroes
                foreach (Hero hero in CampaignDataCache.AllAliveHeroes)
                {
                    if (hero != null && hero.IsAlive)
                    {
                        ApplyYourCheat(hero);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourCampaignBehavior.OnHourlyTick");
            }
        }

        private void OnDailyTick()
        {
            // Daily logic here
        }

        private void ApplyYourCheat(Hero hero)
        {
            // Your cheat logic
        }
    }
}
```

### Creating a Mission Behavior

```csharp
#nullable enable
using System;
using TaleWorlds.MountAndBlade;
using BannerWand.Behaviors.Handlers;
using BannerWand.Interfaces;

namespace BannerWand.Behaviors
{
    public class YourMissionBehavior : MissionLogic
    {
        private readonly IYourCheatHandler _handler;

        public YourMissionBehavior(IYourCheatHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override void OnAgentHit(
            Agent affectedAgent,
            Agent affectorAgent,
            in MissionWeapon attackerWeapon,
            in Blow blow,
            in AttackCollisionData attackCollisionData)
        {
            try
            {
                if (affectedAgent?.Character != null)
                {
                    _handler.ApplyYourCheat(affectedAgent);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourMissionBehavior.OnAgentHit");
            }

            base.OnAgentHit(affectedAgent, affectorAgent, attackerWeapon, blow, attackCollisionData);
        }
    }
}
```

---

## Harmony Patch Examples

### Prefix Patch (Skip Original)

```csharp
#nullable enable
using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    [HarmonyPatch(typeof(Hero), "AddGold")]
    public class YourPrefixPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hero hero, int amount)
        {
            try
            {
                if (CheatSettings.Instance?.YourCheatEnabled == true)
                {
                    // Modify amount
                    amount *= 10;
                    
                    // Apply modified amount
                    hero.Gold += amount;
                    
                    // Skip original method
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourPrefixPatch.Prefix");
            }

            // Continue to original method
            return true;
        }
    }
}
```

### Postfix Patch (Modify Result)

```csharp
#nullable enable
using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    [HarmonyPatch(typeof(MobileParty), "Speed")]
    public class YourPostfixPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result, MobileParty __instance)
        {
            try
            {
                if (CheatSettings.Instance?.YourSpeedMultiplierEnabled == true)
                {
                    float multiplier = CheatSettings.Instance.YourSpeedMultiplier;
                    __result *= multiplier;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourPostfixPatch.Postfix");
            }
        }
    }
}
```

### Transpiler Patch (Modify IL Code)

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    [HarmonyPatch(typeof(Hero), "GetSkillValue")]
    public class YourTranspilerPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codes = new List<CodeInstruction>(instructions);

                // Find the instruction to modify
                for (int i = 0; i < codes.Count; i++)
                {
                    // Modify IL code here
                }

                return codes;
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourTranspilerPatch.Transpiler");
                return instructions; // Return original on error
            }
        }
    }
}
```

---

## Handler Examples

### Creating a Handler with Dependencies

```csharp
#nullable enable
using System;
using TaleWorlds.CampaignSystem;
using BannerWand.Interfaces;
using BannerWand.Settings;

namespace BannerWand.Behaviors.Handlers
{
    public class YourCheatHandler : IYourCheatHandler
    {
        private readonly IModLogger _logger;
        private readonly ITargetFilter _targetFilter;
        private readonly ICampaignDataCache _cache;

        public YourCheatHandler(
            IModLogger logger,
            ITargetFilter targetFilter,
            ICampaignDataCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _targetFilter = targetFilter ?? throw new ArgumentNullException(nameof(targetFilter));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public void ApplyYourCheat(Hero hero)
        {
            if (hero == null || !hero.IsAlive)
            {
                return;
            }

            if (!_targetFilter.ShouldApplyToPlayer(hero))
            {
                return;
            }

            if (CheatSettings.Instance?.YourCheatEnabled != true)
            {
                return;
            }

            try
            {
                // Your logic here
                _logger.LogInfo($"Applied your cheat to {hero.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"In YourCheatHandler.ApplyYourCheat for {hero.Name}");
            }
        }
    }
}
```

---

## Utility Examples

### Using CampaignDataCache

```csharp
using BannerWand.Utils;
using TaleWorlds.CampaignSystem;

// Instead of expensive direct call
// var heroes = Hero.AllAliveHeroes; // Expensive!

// Use cached version
var heroes = CampaignDataCache.AllAliveHeroes; // Cached

foreach (Hero hero in heroes)
{
    if (hero != null && hero.IsAlive)
    {
        // Process hero
    }
}

// Refresh cache when needed
CampaignDataCache.Refresh();
```

### Using DictionaryPool

```csharp
using BannerWand.Utils;

// Instead of creating new dictionary
// var dict = new Dictionary<int, bool>(); // Allocation!

// Use object pool
using (var pooled = DictionaryPool.Get<int, bool>())
{
    var dict = pooled.Value;
    dict[1] = true;
    dict[2] = false;
    
    // Dictionary automatically returned to pool on dispose
}
```

### Using ModLogger

```csharp
using BannerWand.Utils;

// Log information
ModLogger.Info("Cheat applied successfully");

// Log warning
ModLogger.Warning("Cheat partially applied");

// Log error
ModLogger.Error("Failed to apply cheat");

// Log exception
try
{
    // Your code
}
catch (Exception ex)
{
    ModLogger.Exception(ex, "In YourMethod");
}
```

### Using TargetFilter

```csharp
using BannerWand.Utils;
using TaleWorlds.CampaignSystem;

foreach (Hero hero in CampaignDataCache.AllAliveHeroes)
{
    if (TargetFilter.ShouldApplyToPlayer(hero))
    {
        // Apply to player
    }

    if (TargetFilter.ShouldApplyToPlayerClan(hero))
    {
        // Apply to player clan
    }
}
```

### Using VersionReader

```csharp
using BannerWand.Utils;

string version = VersionReader.ReadVersion();
ModLogger.Info($"BannerWand version: {version}");
```

---

## Settings Examples

### Accessing Settings

```csharp
using BannerWand.Settings;

// Check if cheat is enabled
if (CheatSettings.Instance?.UnlimitedHealth == true)
{
    // Apply unlimited health
}

// Get multiplier value
float speedMultiplier = CheatSettings.Instance?.MovementSpeedMultiplier ?? 1.0f;

// Check target settings
if (CheatTargetSettings.Instance?.ApplyToPlayer == true)
{
    // Apply to player
}
```

### Validating Settings

```csharp
using BannerWand.Settings;
using BannerWand.Constants;

float multiplier = CheatSettings.Instance?.YourMultiplier ?? 1.0f;

// Validate range
if (multiplier < GameConstants.MultiplierFactorBase)
{
    multiplier = GameConstants.MultiplierFactorBase;
}

if (multiplier > GameConstants.MaxSpeedMultiplier)
{
    multiplier = GameConstants.MaxSpeedMultiplier;
}
```

---

## Complete Example: Adding a New Cheat

### Step 1: Add Settings

```csharp
// In CheatSettings.cs
[SettingPropertyBool("Unlimited Stamina", RequireRestart = false)]
[SettingPropertyGroup("Player")]
public bool UnlimitedStaminaEnabled { get; set; } = false;
```

### Step 2: Add Constant

```csharp
// In GameConstants.cs
/// <summary>
/// Maximum stamina value.
/// </summary>
public const float MaxStamina = 100.0f;
```

### Step 3: Create Handler

```csharp
// In Behaviors/Handlers/StaminaCheatHandler.cs
public class StaminaCheatHandler : IStaminaCheatHandler
{
    private readonly IModLogger _logger;

    public StaminaCheatHandler(IModLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ApplyUnlimitedStamina(Agent agent)
    {
        if (agent == null || !agent.IsHuman)
        {
            return;
        }

        if (CheatSettings.Instance?.UnlimitedStaminaEnabled != true)
        {
            return;
        }

        try
        {
            agent.SetMaximumSpeedLimit(GameConstants.MaxStamina, false);
            _logger.LogInfo($"Applied unlimited stamina to {agent.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, $"In StaminaCheatHandler.ApplyUnlimitedStamina");
        }
    }
}
```

### Step 4: Use in Behavior

```csharp
// In Behaviors/CombatCheatBehavior.cs
public class CombatCheatBehavior : MissionLogic
{
    private readonly IStaminaCheatHandler _staminaHandler;

    public CombatCheatBehavior(IStaminaCheatHandler staminaHandler)
    {
        _staminaHandler = staminaHandler ?? throw new ArgumentNullException(nameof(staminaHandler));
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (Mission?.PlayerTeam?.PlayerAgent != null)
        {
            _staminaHandler.ApplyUnlimitedStamina(Mission.PlayerTeam.PlayerAgent);
        }
    }
}
```

---

## Best Practices

1. **Always check for null** before using objects
2. **Use try-catch** for error handling
3. **Log operations** for debugging
4. **Use constants** instead of magic numbers
5. **Cache expensive operations** using `CampaignDataCache`
6. **Use object pooling** for frequent allocations
7. **Validate settings** before applying cheats
8. **Document your code** with XML comments

---

## Conclusion

These examples demonstrate common patterns when working with BannerWand. For more details, see:
- [ARCHITECTURE.md](ARCHITECTURE.md) - Architecture overview
- [API.md](API.md) - API reference
- [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) - Developer guide

