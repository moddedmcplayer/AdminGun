namespace AdminGun.Patches
{
    using System;
    using System.Linq;
    using HarmonyLib;
    using PlayerStatsSystem;

    [HarmonyPatch(typeof(FriendlyFireHandler), nameof(FriendlyFireHandler.IsFriendlyFire))]
    public static class IsFriendlyFire
    {
        public static bool Prefix(FriendlyFireHandler __instance, ref bool __result, ReferenceHub damagedPlayer,
            DamageHandlerBase handler)
        {
            if (handler is WarheadDamageHandler warheadDamageHandler)
            {
                __result = false;
                return false;
            }

            if (handler is CustomReasonDamageHandler customReasonDamageHandler)
            {
                if (Math.Abs(customReasonDamageHandler.Damage - (-1f)) < 0.5f)
                {
                    __result = false;
                    return false;
                }
            }
            
            if (handler is ExplosionDamageHandler explosionDamageHandler)
            {
                if (handler is AttackerDamageHandler attackerDamageHandler)
                {
                    if (attackerDamageHandler.Attacker.Hub.inventory.UserInventory.Items.Keys.Any(x =>
                            Plugin.AdminGunSerials.Contains(x)))
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}