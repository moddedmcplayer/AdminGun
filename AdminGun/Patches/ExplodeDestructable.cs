namespace AdminGun.Patches
{
    using Footprinting;
    using HarmonyLib;
    using InventorySystem.Items.Firearms.BasicMessages;
    using InventorySystem.Items.MicroHID;
    using InventorySystem.Items.ThrowableProjectiles;
    using PlayerRoles;
    using PlayerStatsSystem;
    using UnityEngine;

    [HarmonyPatch(typeof(ExplosionGrenade), nameof(ExplosionGrenade.ExplodeDestructible))]
    public static class ExplodeDestructable
    {
        public static bool Prefix(ExplosionGrenade __instance, ref bool __result, IDestructible dest, Footprint attacker, Vector3 pos, ExplosionGrenade setts)
        {
            if (setts.Info.Serial != Plugin.ServerHostGrenade.ItemSerial)
                return true;
            if (Physics.Linecast(dest.CenterOfMass, pos, MicroHIDItem.WallMask))
            {
                __result = false;
            }
            Vector3 a = dest.CenterOfMass - pos;
            float magnitude = a.magnitude;
            float num = setts._playerDamageOverDistance.Evaluate(magnitude);
            ReferenceHub referenceHub;
            bool flag = ReferenceHub.TryGetHubNetID(dest.NetworkId, out referenceHub);
            if (attacker.Hub == referenceHub)
            {
                __result = true;
                return false;
            }
            if (flag && referenceHub.GetRoleId().GetTeam() == Team.SCPs)
            {
                num *= setts._scpDamageMultiplier;
                HumeShieldStat humeShieldStat;
                if (referenceHub.playerStats.TryGetModule(out humeShieldStat))
                {
                    if (num * setts._humeShieldMultipler < humeShieldStat.CurValue)
                    {
                        num *= setts._humeShieldMultipler;
                    }
                    else
                    {
                        num += humeShieldStat.CurValue / setts._humeShieldMultipler;
                    }
                }
            }

            if (flag && referenceHub.playerStats.TryGetModule<HealthStat>(out var mod))
            {
                if (mod.CurValue > 100f)
                {
                    num += (mod.CurValue - 100f * 0.3f);
                }

                if (referenceHub.characterClassManager.GodMode)
                {
                    if (mod.CurValue - num > 0f)
                    {
                        mod.CurValue -= num;
                    }
                    else
                    {
                        referenceHub.characterClassManager.GodMode = false;
                    }
                }
            }
            Vector3 force = (1f - magnitude / setts._maxRadius) * (a / magnitude) * setts._rigidbodyBaseForce + Vector3.up * setts._rigidbodyLiftForce;
            if (num > 0f && dest.Damage(num, new ExplosionDamageHandler(attacker, force, num, 50)
                {
                    ForceFullFriendlyFire = true 
                }, dest.CenterOfMass) && flag)
            {
                //float num2 = setts._effectDurationOverDistance.Evaluate(magnitude);
                bool flag2 = attacker.Hub == referenceHub;
                // if (num2 > 0f && flag2)
                // {
                //     float minimalDuration = setts._minimalDuration;
                //     ExplosionGrenade.TriggerEffect<Burned>(referenceHub, num2 * setts._burnedDuration, minimalDuration);
                //     ExplosionGrenade.TriggerEffect<Deafened>(referenceHub, num2 * setts._deafenedDuration, minimalDuration);
                //     ExplosionGrenade.TriggerEffect<Concussed>(referenceHub, num2 * setts._concussedDuration, minimalDuration);
                // }
                if (!flag2 && attacker.Hub != null)
                {
                    Hitmarker.SendHitmarker(attacker.Hub, 1f);
                }
                referenceHub.inventory.connectionToClient.Send(new GunHitMessage(false, num, pos), 0);
            }
            __result = true;

            return false;
        }
    }
}