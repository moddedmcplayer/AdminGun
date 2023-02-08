namespace AdminGun.Commands
{
    using System;
    using System.Collections.Generic;
    using CommandSystem;
    using InventorySystem;
    using InventorySystem.Items;
    using InventorySystem.Items.Firearms;
    using InventorySystem.Items.Firearms.Attachments;
    using NWAPIPermissionSystem;
    using PluginAPI.Core;
    using RemoteAdmin;
    using Plugin = AdminGun.Plugin;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AdminGunCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if(sender is PlayerCommandSender plySender)
            {
                if (!plySender.CheckPermission("admingun"))
                {
                    response = "Missing permission: admingun";
                    return false;
                }
                
                if(arguments.Count == 0)
                {
                    response = "Usage: admingun <player> | admingun remove <player>";
                    return false;
                }

                if (arguments.At(0) != "remove")
                {
                    Player ply;
                    ply = arguments.At(0).ToLower() == "me" ? Player.Get(sender) : Player.Get(arguments.At(0));
                    ply ??= Player.GetByName(arguments.At(0));
                    if(ply == null && int.TryParse(arguments.At(0), out int id))
                        ply = Player.Get(id);
                    if(ply == null && uint.TryParse(arguments.At(0), out uint netid))
                        ply = Player.Get(netid);
                    if (ply == null)
                    {
                        response = $"Player not found: {arguments.At(0)}";
                        return false;
                    }

                    if (ply.Items.Count == 8)
                    {
                        response = "Make some space in the inventory first.";
                        return false;
                    }

                    var item = ply.AddItem(ItemType.GunCOM18);
                    if (item is Firearm firearm)
                    {
                        var flags = FirearmStatusFlags.MagazineInserted | FirearmStatusFlags.Chambered |
                                    FirearmStatusFlags.Cocked;
                        firearm.ApplyAttachmentsCode(402, true);
                        firearm.Status = new FirearmStatus(69, flags, firearm.GetCurrentAttachmentsCode());
                    }

                    ply.ReceiveHint(
                        ply.CheckPermission("admingun")
                            ? Plugin.Instance.Config.AuthorizedObtainMessage
                            : Plugin.Instance.Config.UnauthorizedObtainMessage, 6);
                    Plugin.AdminGunSerials.Add(item.ItemSerial);
                    response = $"Gave {ply.Nickname} the item.";
                    return true;
                }
                else
                {
                    if(arguments.Count == 1)
                    {
                        response = "Usage: admingun remove <player>";
                        return false;
                    }

                    Player ply = null;
                    bool handled = false;
                    int removed = 0;
                    List<ItemBase> toRemove = new List<ItemBase>();
                    if (arguments.At(1).ToLower() == "unauthorized")
                    {
                        handled = true;
                        foreach (var hub in ReferenceHub.AllHubs)
                        {
                            if(Player.Get(hub).CheckPermission("admingun"))
                                continue;
                            foreach (var kvp in hub.inventory.UserInventory.Items)
                            {
                                if(Plugin.AdminGunSerials.Contains(kvp.Key))
                                {
                                    toRemove.Add(kvp.Value);
                                }
                            }
                        }
                    }
                    else if (arguments.At(1).ToLower() == "all")
                    {
                        handled = true;
                        foreach (var hub in ReferenceHub.AllHubs)
                        {
                            foreach (var kvp in hub.inventory.UserInventory.Items)
                            {
                                if(Plugin.AdminGunSerials.Contains(kvp.Key))
                                {
                                    toRemove.Add(kvp.Value);
                                }
                            }
                        }

                        foreach (var firearmPickup in UnityEngine.Object.FindObjectsOfType<FirearmPickup>())
                        {
                            if(Plugin.AdminGunSerials.Contains(firearmPickup.Info.Serial))
                            {
                                removed++;
                                firearmPickup.DestroySelf();
                            }
                        }
                    }
                    else if (arguments.At(1).ToLower() == "pickup")
                    {
                        handled = true;
                        foreach (var firearmPickup in UnityEngine.Object.FindObjectsOfType<FirearmPickup>())
                        {
                            if(Plugin.AdminGunSerials.Contains(firearmPickup.Info.Serial))
                            {
                                removed++;
                                firearmPickup.DestroySelf();
                            }
                        }
                    }
                    else if (arguments.At(1).ToLower() == "me")
                    {
                        ply = Player.Get(sender);
                    }
                    else
                    {
                        ply = Player.Get(arguments.At(1));
                        ply ??= Player.GetByName(arguments.At(1));
                        if (ply == null && int.TryParse(arguments.At(1), out int id))
                            ply = Player.Get(id);
                        if (ply == null && uint.TryParse(arguments.At(1), out uint netid))
                            ply = Player.Get(netid);
                    }

                    if (ply == null && !handled)
                    {
                        response = $"Player not found: {arguments.At(1)}";
                        return false;
                    }

                    if(!handled)
                    {
                        foreach (var item in ply.Items)
                        {
                            if (Plugin.AdminGunSerials.Contains(item.ItemSerial))
                            {
                                toRemove.Add(item);
                            }
                        }
                    }

                    foreach (var ibase in toRemove)
                    {
                        Plugin.AdminGunSerials.Remove(ibase.ItemSerial);
                        var plyOwner = ibase.Owner;
                        if (plyOwner != null) plyOwner.inventory.ServerRemoveItem(ibase.ItemSerial, null);
                        removed++;
                    }

                    response = $"Removed {removed} instance(s).";
                    return true;
                }
            }
            
            response = "You are not a player";
            return false;
        }

        public string Command { get; } = "admingun";
        public string[] Aliases { get; } = new string[] { "ag" };
        public string Description { get; } = "Funny admin gun.";
    }
}