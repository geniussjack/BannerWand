#nullable enable
// System namespaces
using System;

// Third-party namespaces
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party training model that multiplies troop XP gains from battles.
    /// Extends <see cref="PartyTrainingModel"/> to add Troops XP Multiplier functionality for simulation battles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all party training calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Troops XP Multiplier: Multiplies XP gained from battles (including simulation battles) by the configured multiplier
    /// - Applies to player party and targeted NPC parties based on target settings
    /// </para>
    /// <para>
    /// This model is crucial for simulation battles (auto-battles), as it intercepts XP
    /// in <see cref="CalculateXpGainFromBattles"/>, which is called during <see cref="MapEventParty.CommitXpGain"/>.
    /// </para>
    /// </remarks>
    public class CustomPartyTrainingModel : PartyTrainingModel
    {
        /// <summary>
        /// The default XP multiplier (no boost).
        /// </summary>
        private const float DefaultMultiplier = GameConstants.MultiplierFactorBase;


        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Calculates XP gain from battles with Troops XP Multiplier applied.
        /// This method is called during <see cref="MapEventParty.CommitXpGain"/> for all battle types,
        /// including simulation battles (auto-battles).
        /// </summary>
        /// <param name="troopRosterElement">The troop roster element containing accumulated XP from battle hits.</param>
        /// <param name="party">The party that the troop belongs to.</param>
        /// <returns>An ExplainedNumber containing the XP amount with multiplier applied.</returns>
        public override ExplainedNumber CalculateXpGainFromBattles(FlattenedTroopRosterElement troopRosterElement, PartyBase party)
        {
            try
            {
                // Get base XP calculation from default model
                ExplainedNumber baseXp = GetBaseXpGainFromBattles(troopRosterElement, party);

                // Early exit if settings not available or multiplier not enabled
                if (Settings == null || TargetSettings == null)
                {
                    return baseXp;
                }

                // Check if multiplier is enabled
                if (Settings.TroopsXPMultiplier <= DefaultMultiplier)
                {
                    return baseXp;
                }

                // Check if this party should receive the multiplier
                bool shouldApplyMultiplier = false;

                if (party != null)
                {
                    // Check mobile parties
                    if (party.IsMobile && party.MobileParty != null)
                    {
                        // Check player party
                        if (party.MobileParty == MobileParty.MainParty && TargetSettings.ApplyToPlayer)
                        {
                            shouldApplyMultiplier = true;
                        }
                        // Check player clan members' parties
                        else if (TargetSettings.ApplyToPlayerClanMembers && party.MobileParty.LeaderHero?.Clan == Clan.PlayerClan)
                        {
                            shouldApplyMultiplier = true;
                        }
                        // Check other NPC parties
                        else if (TargetFilter.ShouldApplyCheatToParty(party.MobileParty))
                        {
                            shouldApplyMultiplier = true;
                        }
                    }
                    // Check settlement parties (garrisons, militias)
                    else if (party.IsSettlement)
                    {
                        Settlement? settlement = party.Settlement;
                        if (settlement != null)
                        {
                            // Check if settlement belongs to player clan
                            if (TargetSettings.ApplyToPlayerClanMembers && settlement.OwnerClan == Clan.PlayerClan)
                            {
                                shouldApplyMultiplier = true;
                            }
                            // Check if settlement owner should receive cheats
                            else if (settlement.OwnerClan?.Leader != null && TargetFilter.ShouldApplyCheat(settlement.OwnerClan.Leader))
                            {
                                shouldApplyMultiplier = true;
                            }
                        }
                    }
                }

                if (shouldApplyMultiplier)
                {
                    baseXp.AddFactor(Settings.TroopsXPMultiplier - DefaultMultiplier, new TextObject("{=BW_TroopsXPMultiplier}Troops XP Multiplier"));
                    return baseXp;
                }

                return baseXp;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyTrainingModel] Error in CalculateXpGainFromBattles: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // Return base XP on error
                return GetBaseXpGainFromBattles(troopRosterElement, party);
            }
        }

        /// <summary>
        /// Gets the base XP gain from battles using the default model's calculation.
        /// </summary>
        private ExplainedNumber GetBaseXpGainFromBattles(FlattenedTroopRosterElement troopRosterElement, PartyBase party)
        {
            try
            {
                // Try to get default model
                PartyTrainingModel? defaultModel = Campaign.Current?.Models?.PartyTrainingModel;
                if (defaultModel != null && defaultModel.GetType() != typeof(CustomPartyTrainingModel))
                {
                    return defaultModel.CalculateXpGainFromBattles(troopRosterElement, party);
                }

                // Fallback: Use same logic as DefaultPartyTrainingModel
                ExplainedNumber result = new(troopRosterElement.XpGained, false, null);
                if (((party.MapEvent?.IsPlayerSimulation ?? false) || !(party.MapEvent?.IsPlayerMapEvent ?? false)) && party.IsMobile && !party.MobileParty.IsCurrentlyAtSea)
                {
                    PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.TrustedCommander, party.MobileParty, false, ref result, false);
                }
                return result;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyTrainingModel] Error in GetBaseXpGainFromBattles: {ex.Message}");
                return new ExplainedNumber(troopRosterElement.XpGained, false, null);
            }
        }

        /// <summary>
        /// Generates shared XP for troops.
        /// Delegates to the default model implementation.
        /// </summary>
        public override int GenerateSharedXp(CharacterObject troop, int xp, MobileParty mobileParty)
        {
            try
            {
                // Delegate to default model
                PartyTrainingModel? defaultModel = Campaign.Current?.Models?.PartyTrainingModel;
                if (defaultModel != null && defaultModel.GetType() != typeof(CustomPartyTrainingModel))
                {
                    return defaultModel.GenerateSharedXp(troop, xp, mobileParty);
                }

                // Fallback implementation (same as DefaultPartyTrainingModel)
                ExplainedNumber explainedNumber = new(xp, false, null);
                PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LeaderOfMasses, mobileParty, false, ref explainedNumber, mobileParty.IsCurrentlyAtSea);
                if (troop.IsRegular)
                {
                    if (troop.IsMounted && !mobileParty.IsCurrentlyAtSea)
                    {
                        PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LeadByExample, mobileParty, false, ref explainedNumber, false);
                    }
                    if (troop.IsRanged)
                    {
                        PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.MakeADifference, mobileParty, false, ref explainedNumber, mobileParty.IsCurrentlyAtSea);
                    }
                }
                return (int)(explainedNumber.ResultNumber - xp);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyTrainingModel] Error in GenerateSharedXp: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the XP reward for a character.
        /// Delegates to the default model implementation.
        /// </summary>
        public override int GetXpReward(CharacterObject character)
        {
            try
            {
                // Delegate to default model
                PartyTrainingModel? defaultModel = Campaign.Current?.Models?.PartyTrainingModel;
                if (defaultModel != null && defaultModel.GetType() != typeof(CustomPartyTrainingModel))
                {
                    return defaultModel.GetXpReward(character);
                }

                // Fallback implementation (same as DefaultPartyTrainingModel)
                int num = character.Level + 6;
                return num * num / 3;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyTrainingModel] Error in GetXpReward: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the effective daily experience for a troop.
        /// Delegates to the default model implementation.
        /// </summary>
        public override ExplainedNumber GetEffectiveDailyExperience(MobileParty mobileParty, TroopRosterElement troop)
        {
            try
            {
                // Delegate to default model
                PartyTrainingModel? defaultModel = Campaign.Current?.Models?.PartyTrainingModel;
                if (defaultModel != null && defaultModel.GetType() != typeof(CustomPartyTrainingModel))
                {
                    return defaultModel.GetEffectiveDailyExperience(mobileParty, troop);
                }

                // Fallback: Return zero (shouldn't happen, but safe fallback)
                // Note: This can happen frequently during normal gameplay when accessing troops
                // that don't have a default model reference, so we silently return zero instead of logging
                return new ExplainedNumber(0f, false, null);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyTrainingModel] Error in GetEffectiveDailyExperience: {ex.Message}");
                return new ExplainedNumber(0f, false, null);
            }
        }
    }
}
