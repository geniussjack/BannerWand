#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using static BannerWandRetro.Utils.ModLogger;

namespace BannerWandRetro.Models
{
    /// <summary>
    /// Custom settlement garrison model that enables garrison recruitment multiplier.
    /// Extends <see cref="DefaultSettlementGarrisonModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all garrison calculations.
    /// </para>
        /// <para>
        /// Cheat features provided:
        /// - Garrison Recruitment Multiplier: LIMITED FUNCTIONALITY in Bannerlord 1.2.12
        ///   (GetMaximumDailyAutoRecruitmentCount and CalculateBaseGarrisonChange methods added in 1.3.x)
        /// </para>
    /// <para>
    /// Note: Garrison wages multiplier is handled via CustomPartyWageModel
    /// because wages are calculated in PartyWageModel, not in SettlementGarrisonModel.
    /// </para>
    /// </remarks>
    public class CustomSettlementGarrisonModel : DefaultSettlementGarrisonModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for recruitment multiplier description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject RecruitmentMultiplierText = new("BannerWand Garrison Recruitment Multiplier");

        // NOTE: GetMaximumDailyAutoRecruitmentCount and CalculateBaseGarrisonChange methods do not exist 
        // in DefaultSettlementGarrisonModel for Bannerlord 1.2.12
        // These methods were added in version 1.3.x, so we cannot override them in 1.2.12
        // The GarrisonRecruitmentMultiplier setting will have limited functionality in 1.2.12 version
        // Only CalculateMilitiaChange is available for militia recruitment in 1.2.12
    }
}

