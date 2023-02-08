namespace AdminGun.Patches
{
    using HarmonyLib;
    using InventorySystem.Items;
    using InventorySystem.Items.Pickups;

    //[HarmonyPatch(typeof(ItemBase), nameof(ItemBase.ServerDropItem))]
    public static class ServerDropItem
    {
        public static bool Prefix(ItemBase __instance, ref ItemPickupBase __result)
        {
            if (!Plugin.Instance.Config.AllowDropping && Plugin.AdminGunSerials.Contains(__instance.ItemSerial))
            {
                __result = null;
                return false;
            }

            return true;
        }
    }
}