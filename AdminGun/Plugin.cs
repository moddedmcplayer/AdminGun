namespace AdminGun
{
    using System.Collections.Generic;
    using HarmonyLib;
    using InventorySystem;
    using InventorySystem.Items;
    using InventorySystem.Items.Firearms;
    using InventorySystem.Items.Firearms.Attachments;
    using InventorySystem.Items.Pickups;
    using InventorySystem.Items.ThrowableProjectiles;
    using MEC;
    using NWAPIPermissionSystem;
    using PlayerStatsSystem;
    using PluginAPI.Core;
    using PluginAPI.Core.Attributes;
    using PluginAPI.Enums;
    using PluginAPI.Events;
    using Scp914;
    using UnityEngine;

    public class Plugin
    {
        public static Plugin Instance { get; private set; }
        private Harmony _harmony;
        
        public static List<ushort> AdminGunSerials = new List<ushort>();
        public static ThrowableItem ServerHostGrenade;
        
        [PluginEntryPoint("AdminGun", "1.0.0", "Admin gun that ignores FF", "moddedmcplayer")]
        public void OnEnabled()
        {
            Instance = this;
            _harmony = new Harmony("moddedmcplayer.admingun");
            _harmony.PatchAll();
            
            EventManager.RegisterEvents(this);
        }

        [PluginUnload]
        public void OnDisabled()
        {
            Instance = null;
            _harmony.UnpatchAll(_harmony.Id);
            _harmony = null;
            
            EventManager.UnregisterEvents(this);
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        public void OnPlayerDying(Player player, Player attacker, DamageHandlerBase damageHandler)
        {
            List<ushort> toRemove = new List<ushort>();
            foreach (var item in player.Items)
            {
                if(AdminGunSerials.Contains(item.ItemSerial))
                    toRemove.Add(item.ItemSerial);
            }

            foreach (var toRemoveSerial in toRemove)
            {
                player.ReferenceHub.inventory.ServerRemoveItem(toRemoveSerial, null);
            }
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        bool OnPlayerDroppedItem(Player plr, ItemBase item)
        {
            if (AdminGunSerials.Contains(item.ItemSerial) && !Config.AllowDropping)
            {
                return false;
            }

            return true;
        }

        [PluginEvent(ServerEventType.PlayerSearchedPickup)]
		public bool OnSearchedPickup(Player plr, ItemPickupBase pickup)
        {
            if (AdminGunSerials.Contains(pickup.Info.Serial))
            {
                bool hasPerm = plr.CheckPermission("admingun");
                if (Config.RestrictPickingUp && !hasPerm)
                    return false;
                plr.ReceiveHint(
                    hasPerm
                        ? Config.AuthorizedObtainMessage
                        : Config.UnauthorizedObtainMessage, 6);
                ushort serial = pickup.Info.Serial;
                Timing.CallDelayed(0.5f, () =>
                {
                    if (plr.ReferenceHub.inventory.UserInventory.Items.TryGetValue(serial, out var item))
                    {
                        if (item is Firearm firearm)
                        {
                            firearm.Status = new FirearmStatus(69,
                                FirearmStatusFlags.Chambered | FirearmStatusFlags.Cocked |
                                FirearmStatusFlags.MagazineInserted, firearm.GetCurrentAttachmentsCode());
                        }
                    }
                });
            }
            
            return true;
        }
        
        [PluginEvent(ServerEventType.Scp914UpgradeInventory)]
        public bool OnScp914UpgradeInventory(Player player, ItemBase item, Scp914KnobSetting knobSetting)
        {
            if (AdminGunSerials.Contains(item.ItemSerial))
            {
                player.ReferenceHub.inventory.ServerRemoveItem(item.ItemSerial, null);
                AdminGunSerials.Remove(item.ItemSerial);
                return false;
            }

            return true;
        }

        [PluginEvent(ServerEventType.Scp914UpgradePickup)]
        public bool OnScp914UpgradePickup(ItemPickupBase item, Vector3 outputPosition, Scp914KnobSetting knobSetting)
        {
            if (AdminGunSerials.Contains(item.Info.Serial))
            {
                item.DestroySelf();
                AdminGunSerials.Remove(item.Info.Serial);
                return false;
            }

            return true;
        }

        [PluginEvent(ServerEventType.PlayerToggleFlashlight)]
        public bool ToggleFlashlight(Player ply, ItemBase item, bool isToggled)
        {
            if (!Config.FlashLightEnabledExplosiveAmmo)
                return true;
            if (isToggled)
                return true;
            if (AdminGunSerials.Contains(item.ItemSerial))
            {
                if(ply.CheckPermission("admingun.explosive"))
                    return true;
                ply.ReceiveHint("Missing perm: admingun.explosive");
                return false;
            }

            return true;
        }
        
        [PluginEvent(ServerEventType.WaitingForPlayers)]
        public void OnWaitingForPlayers()
        {
            AdminGunSerials.Clear();
            ServerHostGrenade = (ThrowableItem)ReferenceHub._hostHub.inventory.ServerAddItem(ItemType.GrenadeHE);
        }

        [PluginConfig]
        public Config Config;
    }
}