using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;

    namespace BindTools
    {
        [ApiVersion(1, 23)]
        public class BindTools : TerrariaPlugin
        {
            public static BTPlayer[] BTPlayers = new BTPlayer[255];
            public override string Name
            {
                get { return "BindTools"; }
            }
                public override string Author
            {
                get { return "by Jewsus"; }
            }
                public override string Description
            {
                get { return "Enables command binding to tools. Rewrite of InanZen's AdminTools"; }
            }
                public override Version Version
            {
                get { return new Version("1.0"); }
            }
                public BindTools(Main game)
                : base(game)
            {
                Order = -1;
            }
                public class BTPlayer
            {
                public bool Online = false;
                public List<BindTool> BindTools = new List<BindTool>();
                public BTPlayer()
                {
                }
                    public void AddBindTool(BindTool NewBT)
                {
                    foreach (BindTool PT in BindTools)
                    {
                        if (PT.item == NewBT.item)
                        {
                            BindTools.Remove(PT);
                            break;
                        }
                    }
                    BindTools.Add(NewBT);
                }
                    public BindTool GetBindTool(Item item)
                {
                    foreach (BindTool bt in BindTools)
                    {
                        if (bt.item.netID == item.netID)
                        return bt;
                    }
                    return null;
                }
                    public void RemoveBindTool(Item item)
                {
                    for (int i = 0; i < BindTools.Count; i++)
                    {
                        if (BindTools[i].item.netID == item.netID)
                        {
                            BindTools.RemoveAt(i);
                            return;
                        }
                    }
                }
            }
                public override void Initialize()
            {
                ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
                GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
                Commands.ChatCommands.Add(new Command("bindtool", BindToolCMD, "bindtool", "bt"));
            }
                private static void BindToolCMD(CommandArgs args)
            {
                var player = BTPlayers[args.Player.Index];
                if (player == null)
                return;
                if (args.Parameters.Count > 0)
                {
                    if (args.Player.TPlayer.selectedItem > 9)
                    {
                        args.Player.SendMessage("Please select an item from your Quickbar:", Color.Red);
                        return;
                    }
                        byte flagmod = 0;
                        bool looping = false;
                        bool clear = false;
                        if (args.Parameters[0].StartsWith("-"))
                    {
                        flagmod = 1;
                        for (int i = 1; i < args.Parameters[0].Length; i++)
                        {
                            if (args.Parameters[0][i] == 'l')
                            looping = true;
                            else if (args.Parameters[0][i] == 'c')
                            clear = true;
                            else
                            {
                                args.Player.SendMessage("Invalid BindTool flag.", Color.LightSalmon);
                                args.Player.SendMessage("Valid flags are -l [looping] -c [clear]:", Color.BurlyWood);
                                return;
                            }
                        }
                    }
                    var item = args.Player.TPlayer.inventory[args.Player.TPlayer.selectedItem];
                    if (clear)
                    {
                        player.RemoveBindTool(item);
                        args.Player.SendMessage(string.Format("All commands have been removed from {0}", item.name), Color.BurlyWood);
                        return;
                    }
                     else if (args.Parameters.Count < 2)
                    {
                        args.Player.SendMessage("Missing commands", Color.LightSalmon);
                        return;
                    }
                    var cmdstring = string.Join(" ", args.Parameters.GetRange(flagmod, args.Parameters.Count - flagmod));
                    List<string> cmdlist = cmdstring.Split(';').ToList();
                    player.AddBindTool(new BindTool(item, cmdlist, looping));
                    StringBuilder builder = new StringBuilder(100);
                    builder.Append("Bound");
                    foreach (string cmd in cmdlist)
                    {
                        builder.AppendFormat(" '{0}'", cmd);
                    }
                    builder.AppendFormat(" to {0}", item.name);
                    args.Player.SendMessage(builder.ToString(), Color.BurlyWood);
                    return;
                }
                args.Player.SendMessage("BindTool usage:", Color.LightSalmon);
                args.Player.SendMessage("/bindtool [-l, -c] commands; separated; by semicolon", Color.BurlyWood);
                args.Player.SendMessage("This will bind those commands to the current item in hand.", Color.BurlyWood);
                args.Player.SendMessage("-l Will loop trough commands in order", Color.BurlyWood);
                args.Player.SendMessage("-c Will clear all commands from the item", Color.BurlyWood);
            }
                protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
                }
                base.Dispose(disposing);
            }
                private void OnJoin(JoinEventArgs args)
            {
                BTPlayers[args.Who] = new BTPlayer();
            }
                private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
            {
                TSPlayer tsplayer = TShock.Players[args.PlayerId];
                if (tsplayer == null)
                return;
                BTPlayer player = BTPlayers[args.PlayerId];
                if (player == null)
                return;
                if ((args.Control & 32) == 32)
                {
                    try
                    {
                        var BT = player.GetBindTool(Main.player[args.PlayerId].inventory[args.Item]);
                        if (BT != null)
                        {
                            BT.DoCommand(tsplayer);
                        }
                    }
                        catch (Exception ex)
                    {
                        TShock.Log.ConsoleError(ex.ToString());
                    }
                }
            }
        }
    }
