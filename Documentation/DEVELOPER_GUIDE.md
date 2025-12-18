# BannerWand Developer Guide

## Overview

This guide provides step-by-step instructions for developers who want to extend, modify, or contribute to BannerWand.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Development Environment Setup](#development-environment-setup)
3. [Project Structure](#project-structure)
4. [Adding a New Cheat](#adding-a-new-cheat)
5. [Adding a New Game Model](#adding-a-new-game-model)
6. [Adding a New Behavior](#adding-a-new-behavior)
7. [Adding a New Harmony Patch](#adding-a-new-harmony-patch)
8. [Adding a New Handler](#adding-a-new-handler)
9. [Testing Your Changes](#testing-your-changes)
10. [Code Style Guidelines](#code-style-guidelines)
11. [Submitting Changes](#submitting-changes)

---

## Getting Started

### Prerequisites

- **Visual Studio 2019+** or **JetBrains Rider**
- **.NET Framework 4.7.2 SDK**
- **Mount & Blade II: Bannerlord** (1.3.x for BannerWand-1.3 project)
- **Bannerlord DLLs** - Game DLLs for reference
- **Harmony 2.3.6+** - For patching
- **MCM v5** - For configuration UI

### Required Game DLLs

You'll need access to Bannerlord's DLLs. These are typically located at:
```
Mount & Blade II Bannerlord/bin/Win64_Shipping_Client/
```

Key DLLs:
- `TaleWorlds.*.dll` - Core game libraries
- `SandBox*.dll` - Campaign system
- `StoryMode*.dll` - Story mode

---

## Development Environment Setup

### 1. Clone the Repository

```bash
git clone https://github.com/geniussjack/BannerlordCheats.git
cd BannerlordCheats
```

### 2. Open the Solution

Open `BannerWand.slnx` in Visual Studio or Rider.

### 3. Configure Project References

The project references game DLLs via HintPath. Update paths in `.csproj` if needed:

```xml
<Reference Include="TaleWorlds.CampaignSystem">
  <HintPath>D:\SteamLibrary\steamapps\common\Mount &amp; Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll</HintPath>
  <Private>False</Private>
</Reference>
```

### 4. Build Configuration

- **Debug**: For development and testing
- **Release-1.3**: For Bannerlord 1.3.x release build
- **Release-1.2.12**: For Bannerlord 1.2.12 release build

### 5. Output Directory

Build output goes to:
```
bin/Release-1.3/Win64_Shipping_Client/
```

Copy this to your game's Modules folder for testing.

---

## Project Structure

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture overview.

**Key Directories**:
- `Behaviors/` - Campaign and mission behaviors
- `Models/` - Custom game models
- `Patches/` - Harmony patches
- `Core/` - Core functionality
- `Utils/` - Utility classes
- `Interfaces/` - Abstraction contracts
- `Constants/` - Centralized constants
- `Settings/` - MCM configuration

---

## Adding a New Cheat

### Step 1: Determine Implementation Method

Choose the appropriate method:

1. **Game Model** - If the game has a model for this feature (recommended)
2. **Behavior** - If it requires campaign/mission behavior
3. **Harmony Patch** - If the game API doesn't support it directly

### Step 2: Add Settings

Add toggles and configuration to `CheatSettings.cs`:

```csharp
[SettingPropertyBool("Your Cheat Name", RequireRestart = false)]
[SettingPropertyGroup("Player")]
public bool YourCheatEnabled { get; set; } = false;

[SettingPropertyFloatingInteger("Your Multiplier", 1, 100, RequireRestart = false)]
[SettingPropertyGroup("Player")]
public float YourMultiplier { get; set; } = 1.0f;
```

### Step 3: Implement the Cheat

See sections below for implementation details based on chosen method.

### Step 4: Add Constants

Extract magic numbers to `Constants/GameConstants.cs`:

```csharp
/// <summary>
/// Maximum value for your multiplier.
/// </summary>
public const float MaxYourMultiplier = 100.0f;
```

### Step 5: Add Documentation

Add XML documentation:

```csharp
/// <summary>
/// Applies your cheat to the specified hero.
/// </summary>
/// <param name="hero">The hero to apply the cheat to.</param>
/// <remarks>
/// This method modifies the hero's properties based on current settings.
/// </remarks>
public static void ApplyYourCheat(Hero hero)
{
    // Implementation
}
```

### Step 6: Test

Test in-game and verify:
- Cheat works as expected
- Settings are saved/loaded correctly
- No performance issues
- No conflicts with other cheats

---

## Adding a New Game Model

### Step 1: Identify Base Model

Find the base model to extend (e.g., `DefaultBarterModel`, `DefaultPersuasionModel`).

### Step 2: Create Model Class

Create file in `Models/`:

```csharp
#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom model for your feature.
    /// </summary>
    public class CustomYourModel : DefaultYourModel
    {
        /// <summary>
        /// Gets the value for your feature.
        /// </summary>
        /// <param name="hero">The hero.</param>
        /// <returns>The calculated value.</returns>
        public override ExplainedNumber GetYourValue(Hero hero)
        {
            ExplainedNumber result = base.GetYourValue(hero);

            if (CheatSettings.Instance?.YourCheatEnabled == true)
            {
                float multiplier = CheatSettings.Instance.YourMultiplier - GameConstants.MultiplierFactorBase;
                result.AddFactor(multiplier, new TextObject("{=BW_YourCheat}Your Cheat Multiplier"));
            }

            return result;
        }
    }
}
```

### Step 3: Register Model

Add to `SubModule.OnBeforeInitialModuleScreenSetAsRoot()`:

```csharp
gameStarter.AddModel(new CustomYourModel());
```

### Step 4: Add Localization

Add strings to `ModuleData/Languages/*/strings.xml`:

```xml
<string id="BW_YourCheat" text="Your Cheat Multiplier" />
```

---

## Adding a New Behavior

### Step 1: Create Behavior Class

Create file in `Behaviors/`:

```csharp
#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;

// Project namespaces
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Behaviors
{
    /// <summary>
    /// Behavior for your cheat feature.
    /// </summary>
    public class YourCheatBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// Registers campaign events.
        /// </summary>
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        /// <summary>
        /// Saves behavior data.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            // Save state if needed
        }

        /// <summary>
        /// Handles hourly tick event.
        /// </summary>
        private void OnHourlyTick()
        {
            try
            {
                if (CheatSettings.Instance?.YourCheatEnabled != true)
                {
                    return;
                }

                // Your logic here
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourCheatBehavior.OnHourlyTick");
            }
        }
    }
}
```

### Step 2: Register Behavior

Add to `SubModule.OnBeforeInitialModuleScreenSetAsRoot()`:

```csharp
gameStarter.AddBehavior(new YourCheatBehavior());
```

---

## Adding a New Harmony Patch

### Step 1: Create Patch Class

Create file in `Patches/`:

```csharp
#nullable enable
// System namespaces
using System;

// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch for your feature.
    /// </summary>
    [HarmonyPatch(typeof(YourClass), "YourMethod")]
    public class YourPatch
    {
        /// <summary>
        /// Prefix patch that runs before the original method.
        /// </summary>
        /// <param name="__result">The return value (if modified).</param>
        /// <returns>False to skip original, true to continue.</returns>
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            try
            {
                if (CheatSettings.Instance?.YourCheatEnabled != true)
                {
                    return true; // Continue to original
                }

                // Your logic
                __result = true; // Modify result
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourPatch.Prefix");
                return true; // Fall back to original on error
            }
        }

        /// <summary>
        /// Postfix patch that runs after the original method.
        /// </summary>
        /// <param name="__result">The return value from original method.</param>
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            try
            {
                if (CheatSettings.Instance?.YourCheatEnabled != true)
                {
                    return;
                }

                // Modify result
                __result *= (int)CheatSettings.Instance.YourMultiplier;
            }
            catch (Exception ex)
            {
                ModLogger.Exception(ex, "In YourPatch.Postfix");
            }
        }
    }
}
```

### Step 2: Automatic Discovery

The patch will be automatically discovered and applied by `HarmonyManager` via the `[HarmonyPatch]` attribute.

### Step 3: Manual Registration (if needed)

If you need manual registration, use `[HarmonyTargetMethod]`:

```csharp
[HarmonyTargetMethod]
public static MethodBase TargetMethod()
{
    return AccessTools.Method(typeof(YourClass), "YourMethod");
}
```

---

## Adding a New Handler

### Step 1: Create Interface

Create file in `Interfaces/`:

```csharp
#nullable enable
namespace BannerWand.Interfaces
{
    /// <summary>
    /// Handler for your cheat feature.
    /// </summary>
    public interface IYourCheatHandler
    {
        /// <summary>
        /// Applies your cheat.
        /// </summary>
        /// <param name="hero">The hero to apply to.</param>
        void ApplyYourCheat(Hero hero);
    }
}
```

### Step 2: Create Implementation

Create file in `Behaviors/Handlers/`:

```csharp
#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;

// Project namespaces
using BannerWand.Interfaces;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handler implementation for your cheat.
    /// </summary>
    public class YourCheatHandler : IYourCheatHandler
    {
        private readonly IModLogger _logger;
        private readonly ITargetFilter _targetFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="YourCheatHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="targetFilter">The target filter instance.</param>
        public YourCheatHandler(IModLogger logger, ITargetFilter targetFilter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _targetFilter = targetFilter ?? throw new ArgumentNullException(nameof(targetFilter));
        }

        /// <inheritdoc/>
        public void ApplyYourCheat(Hero hero)
        {
            if (hero == null)
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

### Step 3: Inject into Behavior

Update behavior constructor:

```csharp
public class YourBehavior : CampaignBehaviorBase
{
    private readonly IYourCheatHandler _handler;

    public YourBehavior(IYourCheatHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    private void OnHourlyTick()
    {
        _handler.ApplyYourCheat(Hero.MainHero);
    }
}
```

### Step 4: Register with DI

In `SubModule.OnBeforeInitialModuleScreenSetAsRoot()`:

```csharp
var logger = new ModLoggerWrapper();
var targetFilter = new TargetFilterWrapper();
var handler = new YourCheatHandler(logger, targetFilter);
var behavior = new YourBehavior(handler);
gameStarter.AddBehavior(behavior);
```

---

## Testing Your Changes

### 1. Build the Project

Build in **Debug** or **Release-1.3** configuration.

### 2. Deploy to Game

Copy build output to:
```
Mount & Blade II Bannerlord/Modules/BannerWand/
```

### 3. Test in Game

1. Launch game via BLSE
2. Load a campaign
3. Open MCM settings
4. Enable your cheat
5. Verify it works
6. Check log file: `C:\ProgramData\Mount and Blade II Bannerlord\logs\BannerWand.log`

### 4. Test Edge Cases

- Disable/enable cheat multiple times
- Change settings while cheat is active
- Test with different game states
- Test with other mods enabled

### 5. Performance Testing

- Monitor FPS
- Check for memory leaks
- Verify no lag spikes

---

## Code Style Guidelines

### 1. Using Directives Order

```csharp
#nullable enable
// System namespaces
using System;
using System.Collections.Generic;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using HarmonyLib;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
```

### 2. XML Documentation

Always document public APIs:

```csharp
/// <summary>
/// Applies your cheat to a hero.
/// </summary>
/// <param name="hero">The hero to apply the cheat to.</param>
/// <returns>True if successful, false otherwise.</returns>
/// <remarks>
/// This method modifies the hero's properties based on current settings.
/// </remarks>
/// <exception cref="ArgumentNullException">Thrown when hero is null.</exception>
public static bool ApplyYourCheat(Hero hero)
{
    // Implementation
}
```

### 3. Null Safety

Always use nullable reference types:

```csharp
#nullable enable

// Use null-conditional operators
if (hero?.IsAlive == true)
{
    // ...
}

// Use null-coalescing
var name = hero?.Name ?? "Unknown";
```

### 4. Exception Handling

Always wrap in try-catch:

```csharp
try
{
    // Your code
}
catch (Exception ex)
{
    ModLogger.Exception(ex, "Context information");
}
```

### 5. Constants

Extract magic numbers:

```csharp
// Bad
if (speed > 16.0f) { }

// Good
if (speed > GameConstants.AbsoluteMaxGameSpeed) { }
```

### 6. Regions

Use regions for organization:

```csharp
#region Fields
private int _field;
#endregion

#region Properties
public int Property { get; set; }
#endregion

#region Methods
public void Method() { }
#endregion
```

---

## Submitting Changes

### 1. Create a Branch

```bash
git checkout -b feature/your-feature-name
```

### 2. Commit Changes

```bash
git add .
git commit -m "Add: Your feature description"
```

### 3. Push to Remote

```bash
git push origin feature/your-feature-name
```

### 4. Create Pull Request

- Provide clear description
- List changes made
- Include testing notes
- Reference related issues

### 5. Code Review

Address review comments and update PR.

---

## Common Pitfalls

### 1. Forgetting to Register

Always register models/behaviors in `SubModule`.

### 2. Not Handling Nulls

Always check for null before using objects.

### 3. Missing Constants

Don't use magic numbers - extract to constants.

### 4. Missing Documentation

Always document public APIs.

### 5. Not Testing

Always test in-game before submitting.

---

## Resources

- [ARCHITECTURE.md](ARCHITECTURE.md) - Architecture overview
- [API.md](API.md) - API reference
- [EXAMPLES.md](EXAMPLES.md) - Code examples
- [Bannerlord Modding Documentation](https://docs.bannerlordmodding.com/)
- [Harmony Documentation](https://harmony.pardeike.net/)

---

## Conclusion

This guide covers the essentials of extending BannerWand. For more details, refer to the architecture and API documentation.

Happy modding! 🎮

