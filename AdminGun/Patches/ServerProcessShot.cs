namespace AdminGun.Patches
{
    using AdminGun.Components;
    using CustomPlayerEffects;
    using Footprinting;
    using HarmonyLib;
    using InventorySystem;
    using InventorySystem.Items.Firearms;
    using InventorySystem.Items.Firearms.BasicMessages;
    using InventorySystem.Items.Firearms.Modules;
    using InventorySystem.Items.Pickups;
    using InventorySystem.Items.ThrowableProjectiles;
    using Mirror;
    using PlayerStatsSystem;
    using UnityEngine;

    [HarmonyPatch(typeof(StandardHitregBase), nameof(StandardHitregBase.ServerProcessShot))]
    public static class ServerProcessShot
    {
        public static bool Prefix(StandardHitregBase __instance, ShotMessage message)
        {
            bool freddy = Plugin.FreddySerials.Contains(message.ShooterWeaponSerial);
            if (Plugin.AdminGunSerials.Contains(message.ShooterWeaponSerial) || freddy)
            {
                var ray = new Ray(__instance.Hub.PlayerCameraReference.position,
                    __instance.Hub.PlayerCameraReference.forward);
                RaycastHit hit;
                bool didHit = false;
                if (Physics.Raycast(ray, out hit, float.MaxValue, StandardHitregBase.HitregMask))
                {
                    didHit = true;
                    if (!freddy && hit.collider.TryGetComponent<IDestructible>(out var destructible) && destructible.NetworkId != __instance.Hub.netId)
                    {
                        bool flag = ReferenceHub.TryGetHubNetID(destructible.NetworkId, out var referenceHub);
                        if (flag && referenceHub.characterClassManager.GodMode)
                            referenceHub.characterClassManager.GodMode = false;
                        float damage = -1f;
                        if (destructible.Damage(damage, new CustomReasonDamageHandler("Deleted", damage, "Deleted"), hit.point))
                        {
                            if (!flag || !referenceHub.playerEffectsController.GetEffect<Invisible>().IsEnabled)
                            {
                                Hitmarker.SendHitmarker(__instance.Conn, 1f);
                            }
                            __instance.ShowHitIndicator(destructible.NetworkId, damage, ray.origin);
                            // glass
                            destructible.Damage(float.MaxValue, new WarheadDamageHandler(), Vector3.zero);
                        }
                    }
                }

                if(__instance.Hub.inventory.UserInventory.Items.TryGetValue(message.ShooterWeaponSerial, out var itemBase) && itemBase is Firearm firearm)
                {
                    firearm.Status = new FirearmStatus(69, firearm.Status.Flags, firearm.Status.Attachments);
                    if (freddy)
                    {
                        if (didHit)
                        {
                            if (firearm.Status.Flags.HasFlagFast(FirearmStatusFlags.FlashlightEnabled) &&
                                Plugin.Instance.Config.EnableFreddy)
                            {
                                if (Plugin.MerValid)
                                {
                                    var forward = __instance.Hub.PlayerCameraReference.forward;
                                    ((MonoBehaviour)Plugin.SpawnMethod.Invoke(null, new object[]
                                    {
                                        Plugin.Instance.Config.MERSchematicName,
                                        __instance.Hub.PlayerCameraReference.position +
                                        (forward * 10f),
                                        __instance.Hub.PlayerCameraReference.rotation,
                                        new Vector3(Plugin.Instance.Config.SchematicScale, Plugin.Instance.Config.SchematicScale, Plugin.Instance.Config.SchematicScale),
                                        Plugin.FreddySchematic
                                    })).gameObject.AddComponent<FreddyComponent>().AddForce(forward);
                                }
                            }
                            else
                            {
                                var position = hit.point;
                                ThrownProjectile thrownProjectile =
                                    Object.Instantiate(Plugin.ServerHostGrenadeVisual.Projectile, position,
                                        Quaternion.identity);
                                ((ExplosionGrenade)thrownProjectile)._fuseTime = 0f;
                                PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(Plugin.ServerHostGrenadeVisual.ItemTypeId,
                                    position, Quaternion.identity, Plugin.ServerHostGrenadeVisual.Weight,
                                    Plugin.ServerHostGrenadeVisual.ItemSerial)
                                {
                                    Locked = true
                                };
                                PickupSyncInfo pickupSyncInfo2 = pickupSyncInfo;
                                thrownProjectile.NetworkInfo = pickupSyncInfo2;
                                thrownProjectile.PreviousOwner = new Footprint(__instance.Hub);
                                NetworkServer.Spawn(thrownProjectile.gameObject);
                                ItemPickupBase itemPickupBase = thrownProjectile;
                                pickupSyncInfo2 = default;
                                ItemPickupBase itemPickupBase2 = itemPickupBase;
                                pickupSyncInfo = default;
                                itemPickupBase2.InfoReceived(pickupSyncInfo, pickupSyncInfo2);
                                thrownProjectile.ServerActivate();
                            }
                        }
                    }
                    else
                    {
                        if (Plugin.Instance.Config.FlashLightEnabledExplosiveAmmo && didHit &&
                            firearm.Status.Flags.HasFlagFast(FirearmStatusFlags.FlashlightEnabled))
                        {
                            var position = hit.point;
                            ThrownProjectile thrownProjectile =
                                Object.Instantiate(Plugin.ServerHostGrenade.Projectile, position,
                                    Quaternion.identity);
                            ((ExplosionGrenade)thrownProjectile)._fuseTime = 0f;
                            PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(Plugin.ServerHostGrenade.ItemTypeId,
                                position, Quaternion.identity, Plugin.ServerHostGrenade.Weight,
                                Plugin.ServerHostGrenade.ItemSerial)
                            {
                                Locked = true
                            };
                            PickupSyncInfo pickupSyncInfo2 = pickupSyncInfo;
                            thrownProjectile.NetworkInfo = pickupSyncInfo2;
                            thrownProjectile.PreviousOwner = new Footprint(__instance.Hub);
                            NetworkServer.Spawn(thrownProjectile.gameObject);
                            ItemPickupBase itemPickupBase = thrownProjectile;
                            pickupSyncInfo2 = default;
                            ItemPickupBase itemPickupBase2 = itemPickupBase;
                            pickupSyncInfo = default;
                            itemPickupBase2.InfoReceived(pickupSyncInfo, pickupSyncInfo2);
                            thrownProjectile.ServerActivate();
                        }
                    }
                }

                return false;
            }

            return true;
        }
    }
}