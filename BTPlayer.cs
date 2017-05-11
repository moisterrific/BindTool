using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;

namespace BindTools
{
	public class BTPlayer
	{
		public TSPlayer TSPlayer;
		public List<BindTool> BindTools;
		public List<string> AwaitingCommands;
		public bool HasAwaitingCommands { get { return (AwaitingCommands.Count > 0); } }
		public string AwaitingCommand { get { return ((AwaitingCommands.Count > 0) ? AwaitingCommands[0] : null); } }

		public BTPlayer(int Index)
		{
			TSPlayer = TShock.Players[Index];
			BindTools = (TSPlayer.IsLoggedIn)
						? BTDatabase.BTGet(TSPlayer.User.ID)
						: new List<BindTool>();
			AwaitingCommands = new List<string>();
		}

		public void AddBindTool(BindTool NewBT, bool Database)
		{
			List<BindTool> Removed = new List<BindTool>();
			foreach (BindTool PT in BindTools)
			{
				if ((PT.item == NewBT.item)
					&& ((PT.slot == NewBT.slot)
						|| ((PT.slot != -1) && (NewBT.slot == -1))
						|| ((PT.slot == -1) && (NewBT.slot != -1)))
					&& ((PT.prefix == NewBT.prefix)
						|| ((PT.prefix != -1) && (NewBT.prefix == -1))
						|| ((PT.prefix == -1) && (NewBT.prefix != -1))))
				{
					Removed.Add(PT);
					if (Database && TSPlayer.IsLoggedIn)
					{ BTDatabase.BTDelete(TSPlayer.User.ID, PT.item, PT.slot, PT.prefix); }
				}
			}
			BindTools = (from BindTool b in BindTools
						 where !Removed.Contains(b)
						 select b).ToList();
			BindTools.Add(NewBT);
			if (Database && TSPlayer.IsLoggedIn)
			{ BTDatabase.BTAdd(TSPlayer.User.ID, NewBT); }
		}
		public BindTool GetBindTool(Item item, int Slot)
		{
			foreach (BindTool bt in BindTools)
			{
				if ((bt.item == item.netID) && ((bt.slot == -1) || (bt.slot == Slot))
					&& ((bt.prefix == -1) || (bt.prefix == item.prefix))) { return bt; }
			}
			return null;
		}
		public void RemoveBindTool(int item, int slot, int prefix)
		{
			if ((slot == -1) && (prefix == -1))
			{
				if (TSPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(TSPlayer.User.ID, item); }
				BindTools = (from BindTool b in BindTools
								where (b.item != item)
								select b).ToList();
			}
			else if (slot == -1)
			{
				if (TSPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(TSPlayer.User.ID, item, slot); }
				BindTools = (from BindTool b in BindTools
							 where !((b.item == item)
								 && (b.prefix == prefix))
							 select b).ToList();
			}
			else if (prefix == -1)
			{
				if (TSPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(TSPlayer.User.ID, item, prefix, false); }
				BindTools = (from BindTool b in BindTools
							 where !((b.item == item)
								 && (b.prefix == prefix))
							 select b).ToList();
			}
			else
			{
				if (TSPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(TSPlayer.User.ID, item, slot, prefix); }
				BindTools = (from BindTool b in BindTools
							 where !((b.item == item)
								 && (b.slot == slot)
								 && (b.prefix == prefix))
							 select b).ToList();
			}
		}

		public void AddCommand(string Format)
		{ AwaitingCommands = (new List<string> { Format }).Concat(AwaitingCommands).ToList(); }
		public bool ExecuteCommand(params string[] args)
		{
			try
			{
				string Command = string.Format(AwaitingCommands[0], args);
				Commands.HandleCommand(TSPlayer, Command);
				AwaitingCommands.RemoveAt(0);
				return true;
			}
			catch { return false; }
		}
	}
}