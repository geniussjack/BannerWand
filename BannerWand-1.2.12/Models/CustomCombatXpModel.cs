#nullable enable
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using Helpers;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerWandRetro.Models
{
    /// <summary>
    /// Custom combat XP model that multiplies troop XP gains from battles.
    /// Extends <see cref="CombatXpModel"/> to add Troops XP Multiplier functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all combat XP calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Troops XP Multiplier: Multiplies XP gained from combat hits by the configured multiplier
    /// - Applies to player party and targeted NPC parties based on target settings
    /// </para>
    /// <para>
    /// NOTE: Version 1.2.12 uses a different API - GetXpFromHit uses 'out int' parameter instead of returning ExplainedNumber.
    /// </para>
    /// </remarks>
    public class CustomCombatXpModel : CombatXpModel
    {
        /// <summary>
        /// The default XP multiplier (no boost).
        /// </summary>
        private const float DefaultMultiplier = 1.0f;

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Gets the skill associated with a weapon for XP calculations.
        /// Uses the same logic as DefaultCombatXpModel.
        /// </summary>
        public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit)
        {
            try
            {
                return isSiegeEngineHit ? DefaultSkills.Engineering : weapon != null ? weapon.RelevantSkill : DefaultSkills.Athletics;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in GetSkillForWeapon: {ex.Message}");
                return DefaultSkills.Athletics;
            }
        }

        /// <summary>
        /// Gets the XP gained from a combat hit, with Troops XP Multiplier applied.
        /// Version 1.2.12 API: Uses 'out int' parameter instead of returning ExplainedNumber.
        /// </summary>
        public override void GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType, out int xpAmount)
        {
            try
            {
                // Get base XP from default model
                int baseXp = GetBaseXpFromHit(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);

                // Early exit if settings not available or multiplier not enabled
                if (Settings == null || TargetSettings == null)
                {
                    xpAmount = baseXp;
                    return;
                }

                // Check if multiplier is enabled
                if (Settings.TroopsXPMultiplier <= DefaultMultiplier)
                {
                    xpAmount = baseXp;
                    return;
                }

                // Check if this party should receive the multiplier
                bool shouldApplyMultiplier = false;

                if (attackerParty != null)
                {
                    // Check mobile parties
                    if (attackerParty.IsMobile && attackerParty.MobileParty != null)
                    {
                        // Check player party
                        if (attackerParty.MobileParty == MobileParty.MainParty && TargetSettings.ApplyToPlayer)
                        {
                            shouldApplyMultiplier = true;
                        }
                        // Check player clan members' parties
                        else if (TargetSettings.ApplyToPlayerClanMembers && attackerParty.MobileParty.LeaderHero?.Clan == Clan.PlayerClan)
                        {
                            shouldApplyMultiplier = true;
                        }
                        // Check other NPC parties
                        else if (TargetFilter.ShouldApplyCheatToParty(attackerParty.MobileParty))
                        {
                            shouldApplyMultiplier = true;
                        }
                    }
                    // Check settlement parties (garrisons, militias)
                    else if (attackerParty.IsSettlement)
                    {
                        Settlement? settlement = attackerParty.Settlement;
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

                xpAmount = shouldApplyMultiplier ? MathF.Round(baseXp * Settings.TroopsXPMultiplier) : baseXp;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in GetXpFromHit: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // Return base XP on error
                xpAmount = GetBaseXpFromHit(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);
            }
        }

        /// <summary>
        /// Gets the base XP from hit using the default model's calculation.
        /// </summary>
        private int GetBaseXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
        {
            try
            {
                // Try to get default model
                CombatXpModel? defaultModel = Campaign.Current?.Models?.CombatXpModel;
                if (defaultModel != null && defaultModel.GetType() != typeof(CustomCombatXpModel))
                {
                    defaultModel.GetXpFromHit(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType, out int xp);
                    return xp;
                }

                // Fallback: Calculate manually using the same logic as DefaultCombatXpModel
                return CalculateBaseXpManually(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in GetBaseXpFromHit: {ex.Message}");
                return CalculateBaseXpManually(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);
            }
        }

        /// <summary>
        /// Calculates base XP manually using the same logic as DefaultCombatXpModel.
        /// </summary>
        private int CalculateBaseXpManually(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
        {
            try
            {
                int maxHitPoints = attackedTroop.MaxHitPoints();
                MilitaryPowerModel militaryPowerModel = Campaign.Current.Models.MilitaryPowerModel;
                float defaultTroopPower = militaryPowerModel.GetDefaultTroopPower(attackedTroop);
                float defaultTroopPower2 = militaryPowerModel.GetDefaultTroopPower(attackerTroop);
                float leaderModifier = 0f;
                float contextModifier = 0f;

                if (attackerParty?.MapEvent != null)
                {
                    // Use reflection to access internal SimulationContext field
                    FieldInfo? simulationContextField = attackerParty.MapEvent.GetType().GetField("SimulationContext",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    MapEvent.PowerCalculationContext context = MapEvent.PowerCalculationContext.PlainBattle;
                    if (simulationContextField != null)
                    {
                        context = (MapEvent.PowerCalculationContext)(simulationContextField.GetValue(attackerParty.MapEvent) ?? MapEvent.PowerCalculationContext.PlainBattle);
                    }

                    contextModifier = militaryPowerModel.GetContextModifier(attackedTroop, attackerParty.Side, context);

                    // Use reflection to access internal LeaderSimulationModifier field
                    FieldInfo? leaderModifierField = attackerParty.MapEventSide.GetType().GetField("LeaderSimulationModifier",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (leaderModifierField != null)
                    {
                        leaderModifier = (float)(leaderModifierField.GetValue(attackerParty.MapEventSide) ?? 0f);
                    }
                }

                float troopPower = militaryPowerModel.GetTroopPower(defaultTroopPower, leaderModifier, contextModifier);
                float troopPower2 = militaryPowerModel.GetTroopPower(defaultTroopPower2, leaderModifier, contextModifier);
                float baseXp = 0.4f * (troopPower2 + 0.5f) * (troopPower + 0.5f) * (MathF.Min(damage, maxHitPoints) + (isFatal ? maxHitPoints : 0));
                baseXp *= GetXpfMultiplierForMissionType(missionType);

                ExplainedNumber result = new(baseXp, false, null);

                // Apply perk bonuses (same as DefaultCombatXpModel)
                if (attackerParty != null)
                {
                    GetBattleXpBonusFromPerks(attackerParty, ref result, attackerTroop);
                }

                // Apply captain perk bonus (Inspiring Leader)
                if (captain?.IsHero == true && captain.GetPerkValue(DefaultPerks.Leadership.InspiringLeader))
                {
                    result.AddFactor(DefaultPerks.Leadership.InspiringLeader.SecondaryBonus, DefaultPerks.Leadership.InspiringLeader.Name);
                }

                return MathF.Round(result.ResultNumber);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in CalculateBaseXpManually: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the XP multiplier for mission type (same as DefaultCombatXpModel).
        /// </summary>
        private static float GetXpfMultiplierForMissionType(MissionTypeEnum missionType)
        {
            return missionType switch
            {
                MissionTypeEnum.NoXp => 0f,
                MissionTypeEnum.PracticeFight => 0.0625f,
                MissionTypeEnum.Tournament => 0.33f,
                MissionTypeEnum.SimulationBattle => 0.9f,
                MissionTypeEnum.Battle => 1f,
                _ => 1f
            };
        }

        /// <summary>
        /// Gets the XP multiplier from shot difficulty.
        /// Uses the same logic as DefaultCombatXpModel.
        /// </summary>
        public override float GetXpMultiplierFromShotDifficulty(float shotDifficulty)
        {
            try
            {
                if (shotDifficulty > 14.4f)
                {
                    shotDifficulty = 14.4f;
                }
                return MBMath.Lerp(0f, 2f, (shotDifficulty - 1f) / 13.4f, 1E-05f);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in GetXpMultiplierFromShotDifficulty: {ex.Message}");
                return 1.0f;
            }
        }

        /// <summary>
        /// Gets the captain radius for XP calculations.
        /// Uses the same value as DefaultCombatXpModel.
        /// </summary>
        public override float CaptainRadius => 10f;

        /// <summary>
        /// Applies battle XP bonuses from perks (same logic as DefaultCombatXpModel.GetBattleXpBonusFromPerks).
        /// </summary>
        private static void GetBattleXpBonusFromPerks(PartyBase party, ref ExplainedNumber xpToGain, CharacterObject troop)
        {
            try
            {
                if (party.IsMobile && party.MobileParty.LeaderHero != null)
                {
                    if (!troop.IsRanged && party.MobileParty.HasPerk(DefaultPerks.OneHanded.Trainer, true))
                    {
                        xpToGain.AddFactor(DefaultPerks.OneHanded.Trainer.SecondaryBonus, DefaultPerks.OneHanded.Trainer.Name);
                    }
                    if (troop.HasThrowingWeapon() && party.MobileParty.HasPerk(DefaultPerks.Throwing.Resourceful, true))
                    {
                        xpToGain.AddFactor(DefaultPerks.Throwing.Resourceful.SecondaryBonus, DefaultPerks.Throwing.Resourceful.Name);
                    }
                    if (troop.IsInfantry)
                    {
                        if (party.MobileParty.HasPerk(DefaultPerks.OneHanded.CorpsACorps, false))
                        {
                            xpToGain.AddFactor(DefaultPerks.OneHanded.CorpsACorps.PrimaryBonus, DefaultPerks.OneHanded.CorpsACorps.Name);
                        }
                        if (party.MobileParty.HasPerk(DefaultPerks.TwoHanded.BaptisedInBlood, true))
                        {
                            xpToGain.AddFactor(DefaultPerks.TwoHanded.BaptisedInBlood.SecondaryBonus, DefaultPerks.TwoHanded.BaptisedInBlood.Name);
                        }
                    }
                    if (party.MobileParty.HasPerk(DefaultPerks.OneHanded.LeadByExample, false))
                    {
                        xpToGain.AddFactor(DefaultPerks.OneHanded.LeadByExample.PrimaryBonus, DefaultPerks.OneHanded.LeadByExample.Name);
                    }
                    if (troop.IsRanged)
                    {
                        if (party.MobileParty.HasPerk(DefaultPerks.Crossbow.MountedCrossbowman, true))
                        {
                            xpToGain.AddFactor(DefaultPerks.Crossbow.MountedCrossbowman.SecondaryBonus, DefaultPerks.Crossbow.MountedCrossbowman.Name);
                        }
                        if (party.MobileParty.HasPerk(DefaultPerks.Bow.BullsEye, false))
                        {
                            xpToGain.AddFactor(DefaultPerks.Bow.BullsEye.PrimaryBonus, DefaultPerks.Bow.BullsEye.Name);
                        }
                    }
                    if (troop.Culture.IsBandit && party.MobileParty.HasPerk(DefaultPerks.Roguery.NoRestForTheWicked, false))
                    {
                        xpToGain.AddFactor(DefaultPerks.Roguery.NoRestForTheWicked.PrimaryBonus, DefaultPerks.Roguery.NoRestForTheWicked.Name);
                    }
                }
                if (party.IsMobile && party.MobileParty.IsGarrison)
                {
                    Settlement? currentSettlement = party.MobileParty.CurrentSettlement;
                    if (currentSettlement?.Town?.Governor != null)
                    {
                        PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.ProjectileDeflection, party.MobileParty.CurrentSettlement.Town, ref xpToGain);
                        if (troop.IsMounted)
                        {
                            PerkHelper.AddPerkBonusForTown(DefaultPerks.Polearm.Guards, party.MobileParty.CurrentSettlement.Town, ref xpToGain);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in GetBattleXpBonusFromPerks: {ex.Message}");
            }
        }
    }
}
