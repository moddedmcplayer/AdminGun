namespace AdminGun.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    public class AdminGunAllCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if(sender is PlayerCommandSender plySender)
            {
                if (!plySender.CheckPermission("admingunall"))
                {
                    response = "Missing permission: admingunall";
                    return false;
                }

                bool clear = arguments.Count > 0 && arguments.Any(x => x.ToLower() == "clear");
                bool explo = arguments.Count > 0 && arguments.Any(x => x.ToLower() == "explosive");
                if (explo && !plySender.CheckPermission("admingunall.explosive"))
                {
                    response = "Missing permission: admingunall.explosive";
                    return false;
                }
                int i = 0;
                foreach (var ply in Player.GetPlayers())
                {
                    if(clear)
                        ply.ClearInventory();
                    var item = ply.AddItem(ItemType.GunCOM18);
                    if (item is Firearm firearm)
                    {
                        var flags = FirearmStatusFlags.MagazineInserted | FirearmStatusFlags.Chambered |
                                    FirearmStatusFlags.Cocked;
                        if(explo)
                            flags |= FirearmStatusFlags.FlashlightEnabled;
                        firearm.ApplyAttachmentsCode(402, true);
                        firearm.Status = new FirearmStatus(69, flags, firearm.GetCurrentAttachmentsCode());
                    }

                    ply.ReceiveHint(
                        ply.CheckPermission("admingun")
                            ? Plugin.Instance.Config.AuthorizedObtainMessage
                            : Plugin.Instance.Config.UnauthorizedObtainMessage, 6);
                    Plugin.AdminGunSerials.Add(item.ItemSerial);
                    i++;
                }
                response = $"Gave {i} people the item.";
                return true;
            }
            
            response = "You are not a player";
            return false;
        }

        public string Command { get; } = "admingunall";
        public string[] Aliases { get; } = new string[] { "agall" };
        public string Description { get; } = "Funny admin gun.";
    }
}