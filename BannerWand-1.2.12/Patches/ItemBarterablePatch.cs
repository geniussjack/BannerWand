namespace BannerWandRetro.Patches
{
    /// <summary>
    /// [WIP] Harmony patch for preventing item loss during barter/trade.
    /// Currently non-functional - missing Postfix method to restore items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TODO: This patch only saves items in Prefix but never restores them.
    /// Need to implement Postfix method that:
    /// 1. Compares _playerItemsBackup with current inventory
    /// 2. Adds back any items that were removed
    /// 3. Returns dictionary to pool
    /// </para>
    /// <para>
    /// DISABLED: Entire class commented out until Postfix implementation is complete.
    /// </para>
    /// </remarks>
    /*
    [HarmonyPatch(typeof(ItemBarterable), nameof(ItemBarterable.Apply))]
    public static class ItemBarterablePatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        [ThreadStatic]
        private static Dictionary<ItemObject, int>? _playerItemsBackup;

        [HarmonyPrefix]
        public static void Prefix(ItemBarterable __instance)
        {
            try
            {
                if (Settings is null || TargetSettings is null)
                {
                    return;
                }

                if (!Settings.TradeItemsNoDecrease || !TargetSettings.ApplyToPlayer)
                {
                    return;
                }

                if (__instance.OriginalOwner != Hero.MainHero)
                {
                    return;
                }

                try
                {
                    _playerItemsBackup = ItemBackupPool.Rent();

                    MobileParty playerParty = MobileParty.MainParty;

                    if (playerParty is null || playerParty.ItemRoster is null)
                    {
                        ItemBackupPool.Return(_playerItemsBackup);
                        _playerItemsBackup = null;
                        return;
                    }

                    ItemRoster itemRoster = playerParty.ItemRoster;
                    int rosterCount = itemRoster.Count;

                    for (int i = 0; i < rosterCount; i++)
                    {
                        ItemRosterElement element = itemRoster.GetElementCopyAtIndex(i);
                        ItemObject item = element.EquipmentElement.Item;

                        if (item != null)
                        {
                            _playerItemsBackup[item] = element.Amount;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Error($"[ItemBarterablePatch] Error saving item backup: {ex.Message}");

                    if (_playerItemsBackup != null)
                    {
                        ItemBackupPool.Return(_playerItemsBackup);
                        _playerItemsBackup = null;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ItemBarterablePatch] Critical error in Prefix: {ex.Message}");
            }
        }
        
        // TODO: Implement Postfix method here to restore items
    }
    */
}
