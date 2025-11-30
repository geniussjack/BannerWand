namespace BannerWandRetro.Behaviors
{
    /// <summary>
    /// [WIP] Campaign behavior for unlocking all smithing parts.
    /// Currently non-functional - automatic unlocking not possible in Bannerlord 1.2.12.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TODO: Need to find working method to unlock smithing parts programmatically.
    /// Console commands (campaign.unlock_all_crafting_pieces) exist but cannot be
    /// invoked reliably from mod code in current game version.
    /// </para>
    /// <para>
    /// DISABLED: Entire behavior commented out until solution is found.
    /// Users should use console commands manually:
    /// 1. Press Alt + ~ to open console
    /// 2. Type: config.cheat_mode 1
    /// 3. Type: campaign.unlock_all_crafting_pieces
    /// </para>
    /// </remarks>
    /*
    public class SmithingUnlockBehavior : CampaignBehaviorBase
    {
        private static CheatSettings Settings => CheatSettings.Instance!;
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        private bool _hasAttemptedUnlock;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No data to sync
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // TODO: Implement working unlock method
        }
    }
    */
}
