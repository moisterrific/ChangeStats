using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Terraria.ID;

namespace ChangeStats 
{

    [ApiVersion (2, 1)]
    public class ChangeStats : TerrariaPlugin 
    {
        Config config = new Config();

        public override string Name => "ChangeStats";
        public override string Author => "Johuan, maintained by moisterrific";
        public override string Description => "Changes the stats of players";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public ChangeStats(Main game) : base(game) { }

        public override void Initialize() 
        {
            string path = Path.Combine(TShock.SavePath, "changestats.json");
            config = Config.Read(path);
            if (!File.Exists(path))
                config.Write(path);

            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            PlayerHooks.PlayerPostLogin += OnPostLogin;
        }

        protected override void Dispose(bool disposing) 
        {
            if (disposing) 
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                PlayerHooks.PlayerPostLogin -= OnPostLogin;

                string path = Path.Combine(TShock.SavePath, "changestats.json");
                config.Write(path);
            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args) 
        {
            Commands.ChatCommands.Add(new Command("changestats.set", SetHP, "sethp", "shp") 
            { 
                HelpText = "Set your own HP" 
            });
            Commands.ChatCommands.Add(new Command("changestats.set", SetMP, "setmp", "smp") 
            { 
                HelpText = "Set your own MP"
            });
            Commands.ChatCommands.Add(new Command("changestats.add", AddHP, "addhp", "ahp") 
            { 
                HelpText = "Add your own HP" 
            });
            Commands.ChatCommands.Add(new Command("changestats.add", AddMP, "addmp", "amp") 
            { 
                HelpText = "Add your own MP" 
            });
            Commands.ChatCommands.Add(new Command("changestats.login", SetLoginHP, "loginhp", "lhp") 
            { 
                HelpText = "Set login HP"
            });
            Commands.ChatCommands.Add(new Command("changestats.give", GiveHP, "givehp", "ghp")
            { 
                HelpText = "Give another player HP"
            });
            Commands.ChatCommands.Add(new Command("changestats.give", GiveMP, "givemp", "gmp") 
            { 
                HelpText = "Give another player MP" 
            });
            Commands.ChatCommands.Add(new Command("changestats.resetcharacter", ResetCharacter, "resetstats", "rs")
            { 
                HelpText = "Reset stats (this is irreversible)" 
            });
        }

        private void OnPostLogin(PlayerPostLoginEventArgs e)
        {
            if (config.userLoginHP.ContainsKey(e.Player.Account.ID) && config.userLoginHP[e.Player.Account.ID] != -1) {
                SetStat(config.userLoginHP[e.Player.Account.ID], e.Player, "hp");
            }
        }

        private void SetLoginHP(CommandArgs args)
        {

            var cmd = TShock.Config.Settings.CommandSpecifier;

            int hp = 100;
            if (args.Parameters.Count == 0) 
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}loginhp <health>");
                return;
            }

            if (Int32.TryParse(args.Parameters[0], out hp))
            {
                config.userLoginHP[args.Player.Account.ID] = hp;

                SetStat(hp, args.Player, "hp");

                string path = Path.Combine(TShock.SavePath, "changestats.json");
                config.Write(path);
                args.Player.SendSuccessMessage($"Your login HP has been set to {hp}.");
            } 
            else
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}loginhp <health>");
            }
        }

        private void GiveHP(CommandArgs args) 
        {
            int hp = 100;
            Int32.TryParse(args.Parameters[1], out hp);
            var targetPlayer = FindPlayer(args.Player, args.Parameters[0]);
            SetStat(hp, targetPlayer, "hp");

            //args.Player.SendSuccessMessage("You set " + (targetPlayer == null ? "no one" : targetPlayer.Name) + "'s hp to " + hp);
            args.Player.SendSuccessMessage($"You set {(targetPlayer == null ? "no one" : targetPlayer.Name)}'s HP to {hp}");
        }

        private void GiveMP(CommandArgs args)
        {
            int mp = 0;
            Int32.TryParse(args.Parameters[1], out mp);
            var targetPlayer = FindPlayer(args.Player, args.Parameters[0]);
            SetStat(mp, targetPlayer, "mp");

            args.Player.SendSuccessMessage($"You set {(targetPlayer == null ? "no one" : targetPlayer.Name)}'s mana to {mp}");
        }

        private void SetHP(CommandArgs args) 
        {

            var cmd = TShock.Config.Settings.CommandSpecifier;

            int hp = 100;
            if (args.Parameters.Count == 0) 
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}sethp <health>");
                return;
            }

            if (Int32.TryParse(args.Parameters[0], out hp)) 
            {
                if (!args.Player.HasPermission("changestats.give") && !args.Player.HasPermission("changestats.max")) 
                {
                    hp = Range(hp, 100, 500);
                }
                SetStat(hp, args.Player, "hp");
                args.Player.SendSuccessMessage($"Your HP has been set to {hp}");
            }
            else
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}sethp <health>");
            }
        }

        private void SetMP(CommandArgs args) 
        {
            var cmd = TShock.Config.Settings.CommandSpecifier;
            int mana = 0;

            if (args.Parameters.Count == 0) 
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}setmp <mana>");
                return;
            }

            if (Int32.TryParse(args.Parameters[0], out mana)) 
            {
                if (!args.Player.HasPermission("changestats.give") && !args.Player.HasPermission("changestats.max")) 
                {
                    mana = Range(mana, 0, 200);
                }

                SetStat(mana, args.Player, "mp");

                args.Player.SendSuccessMessage($"Your mana has been set to {mana}");
            } 
            else 
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}setmp <mana>");
            }
        }

        private void AddMP(CommandArgs args)
        {
            var cmd = TShock.Config.Settings.CommandSpecifier;

            if (args.Parameters.Count == 0) 
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}addmp <mana>");
                return;
            }

            if (Int32.TryParse(args.Parameters[0], out int mp)) 
            {
                mp += args.TPlayer.statManaMax;

                SetStat(mp, args.Player, "mp");
                args.Player.SendSuccessMessage($"Your mana has been set to {mp}");
            } 
            else
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}addmp <mana>");
            }
        }

        private void AddHP(CommandArgs args) 
        {
            var cmd = TShock.Config.Settings.CommandSpecifier;

            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}addhp <health>");
                return;
            }

            if (Int32.TryParse(args.Parameters[0], out int hp))
            {
                hp += args.TPlayer.statLifeMax;

                SetStat(hp, args.Player, "hp");
                args.Player.SendSuccessMessage($"Your HP has been set to {hp}");
            } 
            else 
            {
                args.Player.SendErrorMessage($"Incorrect Syntax. Type {cmd}addhp <health>");
            }
        }

        public static void SetStat(int amt, TSPlayer player, string stat) 
        {
            if (player == null) return;

            bool isSSC = Main.ServerSideCharacter;

            if (!isSSC) 
            {
                Main.ServerSideCharacter = true;
                NetMessage.SendData(7, player.Index, -1, null, 0, 0.0f, 0.0f, 0.0f, 0, 0, 0);
                player.IgnoreSSCPackets = true;
            }

            switch (stat)
            {
                case "hp":
                    player.TPlayer.statLifeMax = amt;
                    NetMessage.SendData(16, player.Index, -1, null, player.Index, 0.0f, 0.0f, 0.0f, 0, 0, 0);
                    break;

                case "mp":
                    player.TPlayer.statManaMax = amt;
                    NetMessage.SendData(42, player.Index, -1, null, player.Index, 0.0f, 0.0f, 0.0f, 0, 0, 0);
                    break;

            }

            if (!isSSC) 
            {
                Main.ServerSideCharacter = false;
                NetMessage.SendData(7, player.Index, -1, null, 0, 0.0f, 0.0f, 0.0f, 0, 0, 0);
                player.IgnoreSSCPackets = false;
            }
        }

        public TSPlayer FindPlayer(TSPlayer requestingPlayer, string targetPlayer)
        {
            List<TSPlayer> players = TSPlayer.FindByNameOrID(targetPlayer);

            if (players.Count < 1)
            {
                requestingPlayer.SendErrorMessage($"Could not find player {targetPlayer}");
                return null;
            } 
            else if (players.Count > 1)
            {
                requestingPlayer.SendMultipleMatchError(players.Select(p => p.Name));
                return null;
            } 
            else 
            {
                return players[0];
            }
        }

        private void ResetCharacter(CommandArgs args) 
        {
            TSPlayer player = new TSPlayer(args.Player.Index);

            if (args.Parameters.Count == 0 || args.Parameters[0] != "yes") 
            {
                player.SendErrorMessage("Are you sure you want to reset? You'll lose everything! Type \"/resetstats yes\" to confirm");
                return;
            }

            player.SetBuff(BuffID.Webbed, 30, true);

            bool isSSC = Main.ServerSideCharacter;

            if (!isSSC) 
            {
                Main.ServerSideCharacter = true;
                NetMessage.SendData(7, player.Index, -1, null, 0, 0.0f, 0.0f, 0.0f, 0, 0, 0);
                player.IgnoreSSCPackets = true;
            }

            for (int index1 = 0; index1 < NetItem.MaxInventory; ++index1) 
            {
                if (index1 < NetItem.InventorySlots) 
                {
                    (player.TPlayer.inventory[index1]).netDefaults(0);
                } 
                else if (index1 < NetItem.InventorySlots + NetItem.ArmorSlots)
                {
                    int index2 = index1 - NetItem.InventorySlots;
                    (player.TPlayer.armor[index2]).netDefaults(0);
                } 
                else if (index1 < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots) 
                {
                    int index2 = index1 - (NetItem.InventorySlots + NetItem.ArmorSlots);
                    (player.TPlayer.dye[index2]).netDefaults(0);
                } 
                else if (index1 < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots) 
                {
                    int index2 = index1 - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                    (player.TPlayer.miscEquips[index2]).netDefaults(0);
                } 
                else if (index1 < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots)
                {
                    int index2 = index1 - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots);
                    (player.TPlayer.miscDyes[index2]).netDefaults(0);
                }
            }
            int index3 = 0;
            for (int index1 = 0; index1 < NetItem.InventorySlots; ++index1) 
            {
                NetMessage.SendData(5, -1, -1, null, player.Index, index3, 0, 0, 0, 0, 0);
                ++index3;
            }
            for (int index1 = 0; index1 < NetItem.ArmorSlots; ++index1) 
            {
                NetMessage.SendData(5, -1, -1, null, player.Index, index3, 0, 0, 0, 0, 0);
                ++index3;
            }
            for (int index1 = 0; index1 < NetItem.DyeSlots; ++index1) 
            {
                NetMessage.SendData(5, -1, -1, null, player.Index, index3, 0, 0, 0, 0, 0);
                ++index3;
            }
            for (int index1 = 0; index1 < NetItem.MiscEquipSlots; ++index1) 
            {
                NetMessage.SendData(5, -1, -1, null, player.Index, index3, 0, 0, 0, 0, 0);
                ++index3;
            }
            for (int index1 = 0; index1 < NetItem.MiscDyeSlots; ++index1) 
            {
                NetMessage.SendData(5, -1, -1, null, player.Index, index3, 0, 0, 0, 0, 0);
                ++index3;
            }

            SetStat(100, player, "hp");
            SetStat(0, player, "mp");

            NetMessage.SendData(5, player.Index, -1, new NetworkText((Main.player[player.Index]).trashItem.Name, NetworkText.Mode.Formattable), player.Index, 179f, ((Main.player[player.Index]).trashItem).prefix, 0.0f, 0, 0, 0);

            if (!isSSC) 
            {
                Main.ServerSideCharacter = false;
                NetMessage.SendData(7, player.Index, -1, null, 0, 0.0f, 0.0f, 0.0f, 0, 0, 0);
                player.IgnoreSSCPackets = false;
            }
            
            player.SendMessage("Your character's stats have been reset!", Color.Green);
        }

        public int Range(int num, int min, int max) {
            if (num > max)
                return max;
            else if (num < min)
                return min;
            else
                return num;
        }
    }
}
