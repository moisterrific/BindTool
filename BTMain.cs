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
		public override string Name { get { return "BindTools"; } }
		public override string Author { get { return "by Jewsus & Anzhelika"; } }
		public override string Description { get { return "Enables command binding to tools. Rewrite of InanZen's AdminTools"; } }
		public override Version Version { get { return new Version("1.2"); } }
		public BindTools(Main game) : base(game) { }

		public override void Initialize()
		{
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			PlayerHooks.PlayerPostLogin += OnLogin;
			GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
			Commands.ChatCommands.Add(new Command("bindtool", BindToolCMD, "bindtool", "bt")
			{
				AllowServer = false,
				HelpText = string.Format("Use '{0}bindtool help'.", TShock.Config.CommandSpecifier)
			});
			Commands.ChatCommands.Add(new Command("bindwait", BindWaitCMD, "bindwait", "bw")
			{
				AllowServer = false,
				HelpText = string.Format("Use '{0}bindwait help'.", TShock.Config.CommandSpecifier)
			});
			BTDatabase.DBConnect();
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

		private static void BindToolCMD(CommandArgs args)
		{
			var player = BTPlayers[args.Player.Index];
			if ((player == null) || (player.TSPlayer == null)) return;

			if (args.Parameters.Count == 0)
			{
				args.Player.SendMessage("BindTool usage:", Color.LightSalmon);
				args.Player.SendMessage(string.Format("{0}bindtool [-flags] commands; separated; by semicolon", TShock.Config.CommandSpecifier), Color.BurlyWood);
				args.Player.SendMessage("This will bind those commands to the current item in hand.", Color.BurlyWood);
				args.Player.SendMessage(string.Format("Type {0}bindtool help for flag info.", TShock.Config.CommandSpecifier), Color.BurlyWood);
				args.Player.SendMessage(string.Format("Type {0}bindtool list for current bind list.", TShock.Config.CommandSpecifier), Color.BurlyWood);
				return;
			}

			if (args.Parameters[0].ToLower() == "help")
			{
				List<string> Help = new List<string>
				{
					"-l will loop trough commands in order",
					"[c/aaaa00:-s will bind item only at certain slot]",
					"-p will bind item only with certain prefix",
					"[c/aaaa00:-d will add bind to database, so it will be saved and can be used after rejoin]",
					"You can combine flags: -spd = slot + prefix + database",
					string.Format("[c/aaaa00:-w instead of execution will add command to queue, so you could add parameters later (write {0}bindtool help awaiting for more info)]", TShock.Config.CommandSpecifier),
					"-c will clear all commands from the item at certain slot with certain prefix",
					"[c/aaaa00:-csp = clear any bind on item; -cs = clear binds on item with certain prefix, but any slot; -cp = clear binds on item with certain slot, but any prefix]"
				};
				int page = 1;
				if ((args.Parameters.Count > 1) && (!int.TryParse(args.Parameters[1], out page)))
				{
					if (args.Parameters[1].ToLower() == "awaiting")
					{
						page = 1;
						Help = new List<string>
						{
							"Text format uses {Num} syntax. Parameters start from 0.",
							string.Format("[c/aaaa00:For example ' {0}bt {0}region allow ", TShock.Config.CommandSpecifier) + "\"{0}\" " + string.Format("\"Region Name\" ' will allow you to use {0}bw Player1, {0}bw \"Player 2\", etc.]", TShock.Config.CommandSpecifier),
							"You need to fill all {Num} fields used in your bind command."
						};
					}
					else { args.Player.SendErrorMessage("\"{0}\" is not a valid page number.", args.Parameters[1]); return; }
				}
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
					b => (string.Format("Item: [i:{0}]. Commands: {1}. Awaiting: {2}. Looping: {3}. Slot: {4}. Prefix: {5}. Database: {6}.",
						b.item,
						string.Join("; ", b.commands),
						b.awaiting,
						b.looping,
						((b.slot == -1) ? "Any" : (b.slot == 58) ? "Cursor" : "Hotbar-" + (b.slot + 1)),
						((b.prefix == -1) ? "Any" : Lang.prefix[b.prefix].Value),
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

			if ((args.Player.TPlayer.selectedItem > 9) && (args.Player.TPlayer.selectedItem != 58))
			{
				args.Player.SendMessage("Please select an item from your Quickbar", Color.Red);
				return;
			}

			byte flagmod = 0;
			bool awaiting = false, looping = false, slot = false, clear = false;
			bool prefix = false, database = false;

			if (args.Parameters[0].StartsWith("-"))
			{
				flagmod = 1;
				for (int i = 1; i < args.Parameters[0].Length; i++)
				{
					if ((args.Parameters[0][i] == 'w') || ((args.Parameters[0][i] == 'W')))
						awaiting = true;
					else if ((args.Parameters[0][i] == 'l') || ((args.Parameters[0][i] == 'L')))
						looping = true;
					else if ((args.Parameters[0][i] == 's') || ((args.Parameters[0][i] == 'S')))
						slot = true;
					else if ((args.Parameters[0][i] == 'c') || ((args.Parameters[0][i] == 'C')))
						clear = true;
					else if ((args.Parameters[0][i] == 'p') || ((args.Parameters[0][i] == 'P')))
						prefix = true;
					else if ((args.Parameters[0][i] == 'd') || ((args.Parameters[0][i] == 'D')))
						database = true;
					else
					{
						args.Player.SendMessage("Invalid BindTool flag.", Color.LightSalmon);
						args.Player.SendMessage("Valid flags are 'w' [awaiting], 'l' [looping], 's' [slot], 'p' [prefix], " +
							"'d' [database], 'c' [clear], 'ca' [clear any], 'ce' [clear everything]", Color.BurlyWood);
						args.Player.SendMessage("You can combine flags: -spd = slot + prefix + database, -csp = clear any bind on item; -cs = clear binds on item with certain prefix, but any slot", Color.BurlyWood);
						return;
					}
				}
			}

			var item = args.Player.TPlayer.inventory[args.Player.TPlayer.selectedItem];

			if (clear)
			{
				string _prefix = Lang.prefix[item.prefix].Value;
				player.RemoveBindTool(item.netID, ((slot) ? -1 : args.Player.TPlayer.selectedItem), ((prefix) ? -1 :item.prefix));
				args.Player.SendMessage(string.Format("All commands have been removed from [i:{0}]{1}{2}", item.netID,
					((slot) ? "" : " at " + ((args.Player.TPlayer.selectedItem > 9) ? "cursor" : "hotbar-" + (args.Player.TPlayer.selectedItem + 1)) + " slot"),
					((prefix) ? "" : (" with " + (_prefix == "" ? "no" : _prefix) + " prefix"))), Color.BurlyWood);
				return;
			}

			else if (args.Parameters.Count < 1) { args.Player.SendMessage("Missing commands", Color.LightSalmon); return; }

			string NewMsg = string.Join(" ", args.Message.Replace("\"", "\\\"").Split(' ').Skip(1));
			List<string> NewArgs = ParseParameters(NewMsg);

			var cmdstring = string.Join(" ", NewArgs.GetRange(flagmod, NewArgs.Count - flagmod));
			List<string> cmdlist = cmdstring.Split(';').ToList();

			player.AddBindTool(new BindTool(item.netID, (slot ? args.Player.TPlayer.selectedItem : -1), cmdlist, awaiting, looping, (prefix ? item.prefix : -1), database), database);

			string Prefix = Lang.prefix[item.prefix].Value;

			StringBuilder builder = new StringBuilder();
			builder.Append("Bound");
			foreach (string cmd in cmdlist) { builder.AppendFormat(" '{0}'", cmd); }
			builder.AppendFormat(" to {0}{1}{2} (Database: {3})", item.Name,
				(slot ? (" at " + ((args.Player.TPlayer.selectedItem > 9) ? "cursor" : "hotbar-" + (args.Player.TPlayer.selectedItem + 1)) + " slot") : ""),
				(prefix ? (" with " + ((Prefix == "") ? "no" : Prefix) + " prefix") : ""), database);

			args.Player.SendMessage(builder.ToString(), Color.BurlyWood);
		}

		private static void BindWaitCMD(CommandArgs args)
		{
			var player = BTPlayers[args.Player.Index];
			if ((player == null) || (player.TSPlayer == null)) return;
			if (args.Parameters.Count == 0)
			{
				if (player.HasAwaitingCommands) { player.TSPlayer.SendSuccessMessage("Current command format: {0}", player.AwaitingCommand); }
				else { player.TSPlayer.SendSuccessMessage("You do not have any awaiting commands."); }
				return;
			}
			else if (args.Parameters[0].ToLower() == "help")
			{
				List<string> Help = new List<string>
				{
					string.Format("'{0}bindwait' - shows current awaiting command", TShock.Config.CommandSpecifier),
					string.Format("[c/aaaa00:'{0}bindwait listall' - shows all awaiting commands]", TShock.Config.CommandSpecifier),
					string.Format("'{0}bindwait skip <Count / \"all\">' - skips commands in queue", TShock.Config.CommandSpecifier),
					string.Format("[c/aaaa00:'{0}bindwait [Argument1] [Argument2] ...' - executes current awaiting command with certain arguments]", TShock.Config.CommandSpecifier)
				};
				PaginationTools.SendPage(args.Player, 1, Help, new PaginationTools.Settings { HeaderFormat = "Bindwait help ({0}/{1}):" });
				return;
			}
			else if (args.Parameters[0].ToLower() == "listall")
			{
				int page = 1;
				if ((args.Parameters.Count > 1)
					&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
				{ return; }
				PaginationTools.SendPage(args.Player, page, player.AwaitingCommands,
						new PaginationTools.Settings
						{
							HeaderFormat = "Binds queue ({0}/{1}):",
							FooterFormat = "Type {0}bindwait listall {{0}} for more info.".SFormat(TShock.Config.CommandSpecifier),
							NothingToDisplayString = "You do not have any awaiting commands."
						}
					);
				return;
			}
			else if (args.Parameters[0].ToLower() == "skip")
			{
				int count = 1;
				if ((args.Parameters.Count > 1) && (!int.TryParse(args.Parameters[1], out count)))
				{
					if (args.Parameters[1].ToLower() == "all") { count = player.AwaitingCommands.Count; }
					else { args.Player.SendErrorMessage("Invalid skip count!"); return; }
				}
				if (count > player.AwaitingCommands.Count) { count = player.AwaitingCommands.Count; }
				player.AwaitingCommands.RemoveRange(0, count);
				args.Player.SendSuccessMessage("Successfully skiped {0} commands.", count);
				return;
			}
			if (!player.ExecuteCommand(args.Parameters.ToArray()))
			{ args.Player.SendErrorMessage("Invalid text format!"); }
		}

		private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
		{
			BTPlayer player = BTPlayers[args.PlayerId];
			if ((player == null) || (player.TSPlayer == null)) return;
			if ((args.Control & 32) == 32)
			{
				try
				{
					Item Selected = Main.player[args.PlayerId].inventory[args.Item];
					var BT = player.GetBindTool(Selected, args.Item);
					if ((BT != null) && ((BT.slot == -1) || (BT.slot == player.TSPlayer.TPlayer.selectedItem)) && ((BT.prefix == -1) || (BT.prefix == Selected.prefix)))
					{ BT.DoCommand(player.TSPlayer); }
				}
				catch (Exception ex) { TShock.Log.ConsoleError(ex.ToString()); }
			}
		}

		#region TShock parser code

		private static List<String> ParseParameters(string str)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			bool instr = false;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (c == '\\' && ++i < str.Length)
				{
					if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
						sb.Append('\\');
					sb.Append(str[i]);
				}
				else if (c == '"')
				{
					instr = !instr;
					if (!instr)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
					else if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else if (IsWhiteSpace(c) && !instr)
				{
					if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else
					sb.Append(c);
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}

		private static bool IsWhiteSpace(char c)
		{ return c == ' ' || c == '\t' || c == '\n'; }

		#endregion
	}
}