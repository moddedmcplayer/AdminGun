namespace AdminGun
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Footprinting;
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
    using PluginAPI.Loader;
    using Scp914;
    using UnityEngine;

    public class Plugin
    {
        public static Plugin Instance { get; private set; }
        private Harmony _harmony;

        public static List<ushort> AdminGunSerials = new List<ushort>();
        public static List<ushort> FreddySerials = new List<ushort>();
        public static ThrowableItem HostGrenade = null;
        public static List<Footprint> AGFootPrint = new List<Footprint>();

        public static object FreddySchematic = null;
        public static bool MerValid = false;
        public static MethodInfo SpawnMethod = null;

        [PluginEntryPoint("AdminGun", "1.0.0", "Admin gun that ignores FF", "moddedmcplayer")]
        public void OnEnabled()
        {
            Instance = this;
            _harmony = new Harmony("moddedmcplayer.admingun");
            _harmony.PatchAll();

            EventManager.RegisterEvents(this);
        }

        private void CheckMer()
        {
            try
            {
                var exiledLoader = AssemblyLoader.InstalledPlugins.FirstOrDefault(x => x.PluginName == "Exiled Loader");
                _ = exiledLoader ?? throw new Exception("Exiled not found");
                var exiledLoaderAssembly =
                    AssemblyLoader.Plugins.FirstOrDefault(x => x.Value.Any(x => x.Value == exiledLoader));
                _ = exiledLoaderAssembly.Value ??
                    throw new Exception("Cannot get exiled assembly (this shouldnt happen)");
                var exiledLoaderType = exiledLoaderAssembly.Key.GetType("Exiled.Loader.Loader", true);
                var pluginList = exiledLoaderType.GetProperty("Plugins",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                _ = pluginList ?? throw new Exception("Cannot find plugin list (this shouldnt happen)");
                var plugins = (IEnumerable)pluginList.GetValue(null);
                _ = plugins ?? throw new Exception("Cannot get plugins (this shouldnt happen)");
                var merPlugin = plugins.Cast<object>().FirstOrDefault(x =>
                    x?.GetType().GetProperty("Name")?.GetValue(x).ToString() == "MapEditorReborn");
                _ = merPlugin ?? throw new Exception("MER not found or Exiled is disabled");
                var mapUtilsType = merPlugin.GetType().Assembly.GetType("MapEditorReborn.API.Features.MapUtils", true);
                var getSchematicDataByNameMethod = mapUtilsType.GetMethod("GetSchematicDataByName",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                _ = getSchematicDataByNameMethod ??
                    throw new Exception("Cannot find GetSchematicDataByName method (this shouldnt happen)");
                var schematicData = getSchematicDataByNameMethod.Invoke(null, new object[] { Config.MERSchematicName });
                SpawnMethod = merPlugin.GetType().Assembly.GetType("MapEditorReborn.API.Features.ObjectSpawner", true)
                    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).FirstOrDefault(x => x.Name == "SpawnSchematic" && x.GetParameters().First().ParameterType == typeof(string));
                _ = SpawnMethod ?? throw new Exception("Cannot find SpawnSchematic method (this shouldnt happen)");
                FreddySchematic = schematicData ??
                                  throw new Exception("Cannot find schematic : " + Config.MERSchematicName);
                // var merSingleton = merType.GetProperty("Singleton ", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                // _ = merSingleton ?? throw new Exception("Cannot find mer singleton (this shouldnt happen)");

            }
            catch (Exception e)
            {
                Log.Debug($"YOU CAN IGNORE THIS: MER integration unavailable: {e}");
            }
            finally
            {
                MerValid = FreddySchematic != null;
                if (MerValid)
                    Log.FormatText("MER integration enabled", "green");
            }
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
            if (Config.AllowDropping)
                return;
            List<ushort> toRemove = new List<ushort>();
            foreach (var item in player.Items)
            {
                if(AdminGunSerials.Contains(item.ItemSerial) || FreddySerials.Contains(item.ItemSerial))
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

        //[PluginEvent(ServerEventType.PlayerSearchedPickup)]
		public bool OnSearchedPickup(Player plr, ItemPickupBase pickup)
        {
            if (AdminGunSerials.Contains(pickup.Info.Serial))
            {
                bool hasPerm = plr.CheckPermission("admingun");
                if (Config.RestrictPickingUp && !hasPerm)
                {
                    pickup.Info.InUse = false;
                    return false;
                }
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
            else if (FreddySerials.Contains(pickup.Info.Serial))
            {
                bool hasPerm = plr.CheckPermission("freddygun");
                if (Config.RestrictPickingUp && !hasPerm)
                {
                    pickup.Info.InUse = false;
                    return false;
                }
                plr.ReceiveHint(
                    hasPerm
                        ? Config.AuthorizedObtainMessageFreddy
                        : Config.UnauthorizedObtainMessageFreddy, 6);
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
            if (AdminGunSerials.Contains(item.ItemSerial) || FreddySerials.Contains(item.ItemSerial))
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
            if (AdminGunSerials.Contains(item.Info.Serial) || FreddySerials.Contains(item.Info.Serial))
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
            if (!isToggled)
                return true;
            if (AdminGunSerials.Contains(item.ItemSerial))
            {
                if(ply.CheckPermission("admingun.explosive"))
                    return true;
                ply.ReceiveHint("Missing perm: admingun.explosive");
                return false;
            }
            if (FreddySerials.Contains(item.ItemSerial) && !MerValid)
            {
                ply.ReceiveHint("MER config invalid/not found");
                return false;
            }

            return true;
        }
        
        [PluginEvent(ServerEventType.WaitingForPlayers)]
        public void OnWaitingForPlayers()
        {
            CheckMer();
            AdminGunSerials.Clear();
            FreddySerials.Clear();
            HostGrenade = Server.Instance.AddItem(ItemType.GrenadeFlash) as ThrowableItem;
        }

        [PluginConfig]
        public Config Config;
    }
}