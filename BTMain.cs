using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;

namespace BindTools
{
	[ApiVersion(2, 1)]
	public class BindTools : TerrariaPlugin
	{
		public static BTPlayer[] BTPlayers = new BTPlayer[255];
		public override string Name
		{
			get { return "BindTools"; }
		}
		public override string Author
		{
			get { return "by Jewsus & Anzhelika"; }
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
			public TSPlayer TSPlayer;
			public List<BindTool> BindTools;

			public BTPlayer(int Index)
			{
				TSPlayer = TShock.Players[Index];
				BindTools = (TSPlayer.IsLoggedIn)
							? BTDatabase.BTGet(TSPlayer.User.ID)
							: new List<BindTool>();
			}

			public void AddBindTool(BindTool NewBT, bool Database)
			{
				foreach (BindTool PT in BindTools)
				{
					if ((PT.item == NewBT.item) && (PT.slot == NewBT.slot) && (PT.prefix == NewBT.prefix))
					{
						BindTools.Remove(PT);
						if (Database && TSPlayer.IsLoggedIn)
						{ BTDatabase.BTDelete(TSPlayer.User.ID, PT.item.netID, PT.slot, PT.prefix); }
						break;
					}
				}
				BindTools.Add(NewBT);
				if (Database && TSPlayer.IsLoggedIn)
				{ BTDatabase.BTAdd(TSPlayer.User.ID, NewBT); }
			}
			public BindTool GetBindTool(Item item, int Slot)
			{
				foreach (BindTool bt in BindTools)
				{
					if ((bt.item.netID == item.netID) && ((bt.slot == -1) || (bt.slot == Slot))
						&& ((bt.prefix == -1) || (bt.prefix == item.prefix))) { return bt; }
				}
				return null;
			}
			public void RemoveBindTool(Item item, int slot, int prefix)
			{
				if (slot == -1)
				{
					if (TSPlayer.IsLoggedIn)
					{ BTDatabase.BTDelete(TSPlayer.User.ID, item.netID); }
					BindTools = (from BindTool b in BindTools
								 where (b.item.netID != item.netID)
								 select b).ToList();
				}
				else if (prefix == -1)
				{
					if (TSPlayer.IsLoggedIn)
					{ BTDatabase.BTDelete(TSPlayer.User.ID, item.netID, slot); }
					BindTools = (from BindTool b in BindTools
								 where ((b.item.netID != item.netID)
									 && (b.slot != slot))
								 select b).ToList();
				}
				else
				{
					if (TSPlayer.IsLoggedIn)
					{ BTDatabase.BTDelete(TSPlayer.User.ID, item.netID, slot, prefix); }
					BindTools = (from BindTool b in BindTools
								 where ((b.item.netID != item.netID)
									 && (b.slot != slot)
									 && (b.prefix != prefix))
								 select b).ToList();
				}
			}
		}
		public override void Initialize()
		{
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			PlayerHooks.PlayerPostLogin += OnLogin;
			GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
			Commands.ChatCommands.Add(new Command("bindtool", BindToolCMD, "bindtool", "bt"));
			BTDatabase.DBConnect();
		}

		private static void BindToolCMD(CommandArgs args)
		{
			var player = BTPlayers[args.Player.Index];
			if (player == null)
				return;
			if (args.Parameters.Count > 0)
			{
				if (args.Parameters[0].ToLower() == "help")
				{
					List<string> Help = new List<string>
					{
						"-l will loop trough commands in order",
						"-s will bind item only at certain slot",
						"-d will add bind to database, so it will be saved and can be used after rejoin",
						"-p will bind item only with certain prefix",
						"-c will clear all commands from the item at certain slot with certain prefix",
						"-ca will clear all commands from the item at certain slot",
						"-ce will clear all commands from the item",
						"You can combine flags: -spd = slot + prefix + database, -ld = looping + database"
					};
					int page = 1;
					if ((args.Parameters.Count > 1)
						&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
					{ return; }
					PaginationTools.SendPage(args.Player, page, Help,
							new PaginationTools.Settings
							{
								HeaderFormat = "Bindtools help ({0}/{1}):",
								FooterFormat = "Type {0}bindtool help {{0}} for more info.".SFormat(TShock.Config.CommandSpecifier)
							}
						);
					return;
				}
				else if (args.Parameters[0].ToLower() == "list")
				{
					int page = 1;
					if ((args.Parameters.Count > 1)
						&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
					{ return; }
					var Normal = BTPlayers[args.Player.Index].BindTools.Select
					(
						b => (string.Format("Item: [i:{0}]. Commands: {1}. Looping: {2}. Slot: {3}. Prefix: {4}. Database: {5}.",
							b.item.netID,
							string.Join("; ", b.commands),
							b.looping,
							((b.slot == -1) ? "Any" : (b.slot + 1).ToString()),
							((b.prefix == -1) ? 0 : b.prefix),
							b.database))
					).ToList();
					PaginationTools.SendPage(args.Player, page, Normal,
							new PaginationTools.Settings
							{
								HeaderFormat = "Current binds ({0}/{1}):",
								FooterFormat = "Type {0}bindtool list {{0}} for more info.".SFormat(TShock.Config.CommandSpecifier),
								NothingToDisplayString = "You do not have any binds."
							}
						);
					return;
				}

				if (args.Player.TPlayer.selectedItem > 9)
				{
					args.Player.SendMessage("Please select an item from your Quickbar:", Color.Red);
					return;
				}
				byte flagmod = 0;
				bool looping = false;
				bool slot = false;
				bool clear = false;
				bool cany = false;
				bool ceverything = false;
				bool prefix = false;
				bool database = false;
				if (args.Parameters[0].StartsWith("-"))
				{
					flagmod = 1;
					for (int i = 1; i < args.Parameters[0].Length; i++)
					{
						if ((args.Parameters[0][i] == 'l') || ((args.Parameters[0][i] == 'L')))
							looping = true;
						else if ((args.Parameters[0][i] == 's') || ((args.Parameters[0][i] == 'S')))
							slot = true;
						else if ((args.Parameters[0][i] == 'c') || ((args.Parameters[0][i] == 'C')))
							clear = true;
						else if ((args.Parameters[0][i] == 'a') || ((args.Parameters[0][i] == 'A')))
							cany = true;
						else if ((args.Parameters[0][i] == 'e') || ((args.Parameters[0][i] == 'E')))
							ceverything = true;
						else if ((args.Parameters[0][i] == 'p') || ((args.Parameters[0][i] == 'P')))
							prefix = true;
						else if ((args.Parameters[0][i] == 'd') || ((args.Parameters[0][i] == 'D')))
							database = true;
						else
						{
							args.Player.SendMessage("Invalid BindTool flag.", Color.LightSalmon);
							args.Player.SendMessage("Valid flags are l [looping], s [slot], p [prefix], d [database], c [clear], a [(clear) any], e [(clear) everything]", Color.BurlyWood);
							args.Player.SendMessage("You can combine flags: -spd = slot + prefix + database, -ca = clear any, -ce = clear everything", Color.BurlyWood);
							return;
						}
					}
				}
				var item = args.Player.TPlayer.inventory[args.Player.TPlayer.selectedItem];
				if (clear)
				{
					if (cany)
					{
						player.RemoveBindTool(item, args.Player.TPlayer.selectedItem, -1);
						args.Player.SendMessage(string.Format("All commands have been removed from {0} at {1} slot", item.Name, (args.Player.TPlayer.selectedItem + 1)), Color.BurlyWood);
						return;
					}
					else if (ceverything)
					{
						player.RemoveBindTool(item, -1, -1);
						args.Player.SendMessage(string.Format("All commands have been removed from {0}", item.Name), Color.BurlyWood);
						return;
					}
					player.RemoveBindTool(item, args.Player.TPlayer.selectedItem, item.prefix);
					args.Player.SendMessage(string.Format("All commands have been removed from {0} at {1} slot with {2} prefix", item.Name, (args.Player.TPlayer.selectedItem + 1), (Lang.prefix[item.prefix].Value)).Replace("with  prefix", "with no prefix"), Color.BurlyWood);
					return;
				}
				else if (args.Parameters.Count < 1)
				{
					args.Player.SendMessage("Missing commands", Color.LightSalmon);
					return;
				}
				var cmdstring = string.Join(" ", args.Parameters.GetRange(flagmod, args.Parameters.Count - flagmod));
				List<string> cmdlist = cmdstring.Split(';').ToList();
				player.AddBindTool(new BindTool(item, (slot ? args.Player.TPlayer.selectedItem : -1), cmdlist, looping, (prefix ? item.prefix : -1), database), database);
				StringBuilder builder = new StringBuilder();
				builder.Append("Bound");
				foreach (string cmd in cmdlist)
				{
					builder.AppendFormat(" '{0}'", cmd);
				}
				builder.AppendFormat(" to {0}{1}{2}", item.Name, (slot ? (" at " + args.Player.TPlayer.selectedItem + 1 + " slot") : ""), (prefix ? (" with " + Lang.prefix[item.prefix].Value + " prefix").Replace("with  prefix", "with no prefix") : ""));
				args.Player.SendMessage(builder.ToString(), Color.BurlyWood);
				return;
			}
			args.Player.SendMessage("BindTool usage:", Color.LightSalmon);
			args.Player.SendMessage("/bindtool [-flags] commands;separated;by semicolon", Color.BurlyWood);
			args.Player.SendMessage("This will bind those commands to the current item in hand.", Color.BurlyWood);
			args.Player.SendMessage("Type /bindtool help for flag info.", Color.BurlyWood);
			args.Player.SendMessage("Type /bindtool list for current bind list.", Color.BurlyWood);
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				PlayerHooks.PlayerPostLogin -= OnLogin;
				GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
			}
			base.Dispose(disposing);
		}

		private void OnGreet(GreetPlayerEventArgs args)
		{ BTPlayers[args.Who] = new BTPlayer(args.Who); }
		private void OnLogin(PlayerPostLoginEventArgs args)
		{ BTPlayers[args.Player.Index] = new BTPlayer(args.Player.Index); }
		private void OnLeave(LeaveEventArgs args)
		{ BTPlayers[args.Who] = null; }

		private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
		{
			TSPlayer tsplayer = TShock.Players[args.PlayerId];
			if (tsplayer == null) return;
			BTPlayer player = BTPlayers[args.PlayerId];
			if (player == null) return;
			if ((args.Control & 32) == 32)
			{
				try
				{
					Item Selected = Main.player[args.PlayerId].inventory[args.Item];
					var BT = player.GetBindTool(Selected, args.Item);
					if ((BT != null) && ((BT.slot == -1) || (BT.slot == player.TSPlayer.TPlayer.selectedItem)) && ((BT.prefix == -1) || (BT.prefix == Selected.prefix)))
					{ BT.DoCommand(tsplayer); }
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError(ex.ToString());
				}
			}
		}
	}
}