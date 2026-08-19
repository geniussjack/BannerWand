#nullable enable
using BannerWand.Settings;
using BannerWand.Utils;
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
using TaleWorlds.Localization;

namespace BannerWand.Models
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
    /// Target support:
    /// - Applies to player party if ApplyToPlayer enabled
    /// - Applies to player clan members' parties if ApplyToPlayerClanMembers enabled
    /// - Applies to other NPC parties based on target settings
    /// </para>
    /// <para>
    /// Combat XP system in Bannerlord:
    /// - XP is calculated per hit during battles
    /// - This model's GetXpFromHit is called whenever a troop hits an enemy
    /// - Multiplier scales the XP gain (1.0 = normal, 2.0 = double, etc.)
    /// </para>
    /// </remarks>
    public class CustomCombatXpModel : CombatXpModel
    {
        #region Constants (from DefaultCombatXpModel)

        /// <summary>
        /// The default XP multiplier (no boost).
        /// </summary>
        private const float DefaultMultiplier = 1.0f;

        /// <summary>
        /// Power offset added to attacker/defender power to prevent zero XP.
        /// Matches DefaultCombatXpModel behavior.
        /// </summary>
        private const float PowerOffset = 0.5f;

        /// <summary>
        /// Base XP calculation coefficient. Matches DefaultCombatXpModel.
        /// </summary>
        private const float BaseXpCoefficient = 0.4f;

        /// <summary>
        /// Maximum shot difficulty value for XP calculation.
        /// Matches DefaultCombatXpModel.
        /// </summary>
        private const float MaxShotDifficulty = 14.4f;

        /// <summary>
        /// Shot difficulty range for lerp calculation (MaxShotDifficulty - 1.0f).
        /// Matches DefaultCombatXpModel.
        /// </summary>
        private const float ShotDifficultyRange = 13.4f;

        #endregion


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
        /// </summary>
        /// <param name="attackerTroop">The troop that dealt the hit.</param>
        /// <param name="captain">The captain/commander of the attacking unit (can be null).</param>
        /// <param name="attackedTroop">The troop that was hit.</param>
        /// <param name="attackerParty">The party that the attacker belongs to.</param>
        /// <param name="damage">The damage dealt.</param>
        /// <param name="isFatal">Whether the hit was fatal.</param>
        /// <param name="missionType">The type of mission (battle, tournament, etc.).</param>
        /// <returns>An ExplainedNumber containing the XP amount with multiplier applied.</returns>
        /// <remarks>
        /// <para>
        /// This method:
        /// 1. Gets base XP from default model
        /// 2. Checks if the attacker's party should receive multiplier
        /// 3. Applies Troops XP Multiplier if enabled
        /// 4. Returns the multiplied XP
        /// </para>
        /// </remarks>
        public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
        {
            try
            {
                // Get base XP from default model
                ExplainedNumber baseXp = GetBaseXpFromHit(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);

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
                // Handle both mobile parties and settlement parties (garrisons)
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

                if (shouldApplyMultiplier)
                {
                    baseXp.AddFactor(Settings.TroopsXPMultiplier - DefaultMultiplier, new TextObject("{=BW_TroopsXPMultiplier}Troops XP Multiplier"));
                    return baseXp;
                }

                return baseXp;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in GetXpFromHit: {ex.Message}", ex);
                // Return base XP on error - use manual calculation to avoid potential recursion
                try
                {
                    return CalculateBaseXpManually(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);
                }
                catch
                {
                    // If manual calculation also fails, return zero XP to prevent infinite recursion
                    return new ExplainedNumber(0f, false, null);
                }
            }
        }

        /// <summary>
        /// Gets the base XP from hit using the same calculation as DefaultCombatXpModel.
        /// </summary>
        private ExplainedNumber GetBaseXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
        {
            // Always calculate manually to avoid recursion issues
            return CalculateBaseXpManually(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);
        }

        /// <summary>
        /// Calculates base XP manually using the same logic as DefaultCombatXpModel.
        /// This is a fallback if we can't access the default model.
        /// </summary>
        private ExplainedNumber CalculateBaseXpManually(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
        {
            try
            {
                int maxHitPoints = attackedTroop.MaxHitPoints();
                float leaderModifier = 0f;
                BattleSideEnum side = BattleSideEnum.Attacker;
                MapEvent.PowerCalculationContext context = MapEvent.PowerCalculationContext.PlainBattle;

                if (attackerParty?.MapEvent != null)
                {
                    // Use reflection to access internal LeaderSimulationModifier field
                    FieldInfo? leaderModifierField = attackerParty.MapEventSide.GetType().GetField("LeaderSimulationModifier",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (leaderModifierField != null)
                    {
                        leaderModifier = (float)(leaderModifierField.GetValue(attackerParty.MapEventSide) ?? 0f);
                    }
                    side = attackerParty.Side;

                    // Use reflection to access internal SimulationContext property
                    PropertyInfo? simulationContextProperty = attackerParty.MapEvent.GetType().GetProperty("SimulationContext",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    if (simulationContextProperty != null)
                    {
                        context = (MapEvent.PowerCalculationContext)(simulationContextProperty.GetValue(attackerParty.MapEvent) ?? MapEvent.PowerCalculationContext.PlainBattle);
                    }
                }

                float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(attackedTroop, side.GetOppositeSide(), context, leaderModifier);
                float attackerPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(attackerTroop, side, context, leaderModifier) + PowerOffset;
                float defenderPower = troopPower + PowerOffset;

                int totalDamage = MathF.Min(damage, maxHitPoints) + (isFatal ? maxHitPoints : 0);
                float baseXp = BaseXpCoefficient * attackerPower * defenderPower * totalDamage;
                baseXp *= GetXpfMultiplierForMissionType(missionType);

                ExplainedNumber result = new(baseXp, false, null);

                // Apply perk bonuses (same as DefaultCombatXpModel)
                if (attackerParty != null)
                {
                    GetBattleXpBonusFromPerks(attackerParty, ref result, attackerTroop);
                }

                // Apply captain perk bonus (Inspiring Leader)
                bool isAtSea = attackerParty?.IsMobile != true || attackerParty.MobileParty.IsCurrentlyAtSea;
                if (captain?.IsHero == true && !isAtSea && captain.GetPerkValue(DefaultPerks.Leadership.InspiringLeader))
                {
                    result.AddFactor(DefaultPerks.Leadership.InspiringLeader.SecondaryBonus, DefaultPerks.Leadership.InspiringLeader.Name);
                }

                return result;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomCombatXpModel] Error in CalculateBaseXpManually: {ex.Message}");
                return new ExplainedNumber(0f, false, null);
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
                if (shotDifficulty > MaxShotDifficulty)
                {
                    shotDifficulty = MaxShotDifficulty;
                }
                return MBMath.Lerp(0f, 2f, (shotDifficulty - 1f) / ShotDifficultyRange, 1E-05f);
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
                    if (!troop.IsRanged)
                    {
                        if (!party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.OneHanded.Trainer, true))
                        {
                            xpToGain.AddFactor(DefaultPerks.OneHanded.Trainer.SecondaryBonus, DefaultPerks.OneHanded.Trainer.Name);
                        }
                        PerkHelper.AddPerkBonusForParty(DefaultPerks.TwoHanded.BaptisedInBlood, party.MobileParty, false, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
                    }
                    if (troop.HasThrowingWeapon() && party.MobileParty.HasPerk(DefaultPerks.Throwing.Resourceful, true))
                    {
                        xpToGain.AddFactor(DefaultPerks.Throwing.Resourceful.SecondaryBonus, DefaultPerks.Throwing.Resourceful.Name);
                    }
                    if (troop.IsInfantry)
                    {
                        PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.CorpsACorps, party.MobileParty, true, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
                    }
                    PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.LeadByExample, party.MobileParty, true, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
                    if (troop.IsRanged)
                    {
                        PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.MountedCrossbowman, party.MobileParty, false, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
                        PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.BullsEye, party.MobileParty, true, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
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
