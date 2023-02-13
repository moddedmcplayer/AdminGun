namespace AdminGun.Patches
{
    using HarmonyLib;
    using InventorySystem.Items.Firearms;
    using InventorySystem.Items.Firearms.Attachments;
    using InventorySystem.Searching;
    using NWAPIPermissionSystem;
    using MEC;
    using PluginAPI.Core;

    [HarmonyPatch(typeof(ItemSearchCompletor), nameof(ItemSearchCompletor.ValidateAny))]
    public static class ValidateAny
    {
        public static bool Prefix(ItemSearchCompletor __instance, ref bool __result)
        {
            if (AdminGun.Plugin.AdminGunSerials.Contains(__instance.TargetPickup.Info.Serial) || AdminGun.Plugin.FreddySerials.Contains(__instance.TargetPickup.Info.Serial))
            {
                var plr = Player.Get(__instance.Hub);
                bool hasPerm = plr.CheckPermission("admingun");
                if (AdminGun.Plugin.Instance.Config.RestrictPickingUp && !hasPerm)
                {
                    __result = false;
                    return false;
                }

                ushort serial = __instance.TargetPickup.Info.Serial;
                Timing.CallDelayed(0.5f, () =>
                {
                    if (plr.ReferenceHub.inventory.UserInventory.Items.TryGetValue(serial, out var item))
                    {
                        if (item is Firearm firearm)
                        {
                            plr.ReceiveHint(
                                hasPerm
                                    ? AdminGun.Plugin.Instance.Config.AuthorizedObtainMessage
                                    : AdminGun.Plugin.Instance.Config.UnauthorizedObtainMessage, 6);
                            firearm.Status = new FirearmStatus(69,
                                FirearmStatusFlags.Chambered | FirearmStatusFlags.Cocked |
                                FirearmStatusFlags.MagazineInserted, firearm.GetCurrentAttachmentsCode());
                        }
                    }
                });
            }

            return true;
        }
    }
}