#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using static BannerWandRetro.Utils.ModLogger;

namespace BannerWandRetro.Models
{
    /// <summary>
    /// Custom party wage model that applies garrison wages multiplier.
    /// Extends <see cref="DefaultPartyWageModel"/> to modify garrison wages based on settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all wage calculations.
    /// </para>
        /// <para>
        /// Cheat features provided:
        /// - Garrison Wages Multiplier: NOT AVAILABLE via model override in Bannerlord 1.2.12
        ///   (GetTotalWage method is not virtual/overrideable in 1.2.12, made virtual in 1.3.x)
        /// </para>
        /// <para>
        /// NOTE: This model class exists for consistency but does not override any methods in 1.2.12.
        /// For 1.2.12, garrison wages multiplier should be implemented via Harmony patch instead.
        /// See GarrisonWagesPatch.cs for the patch implementation (if available).
        /// </para>
    /// </remarks>
    public class CustomPartyWageModel : DefaultPartyWageModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for wages multiplier description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject WagesMultiplierText = new TextObject("BannerWand Garrison Wages Multiplier");

        /// <summary>
        /// Calculates the total wage for a mobile party.
        /// Applies garrison wages multiplier if the party is a garrison.
        /// </summary>
        /// <param name="mobileParty">The mobile party to calculate wages for. Cannot be null.</param>
        /// <param name="troopRoster">The troop roster to calculate wages for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the total wage value.
        /// For garrison parties, the wage is modified according to the configured multiplier.
        /// </returns>
        // NOTE: GetTotalWage method is not virtual/overrideable in DefaultPartyWageModel for Bannerlord 1.2.12
        // This method was made virtual in version 1.3.x, so we cannot override it in 1.2.12
        // For 1.2.12, garrison wages multiplier should be implemented via Harmony patch instead
        // See GarrisonWagesPatch.cs for the patch implementation
        // This model class is kept for consistency but does not override any methods in 1.2.12
    }
}

