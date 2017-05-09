using System;
using System.Collections.Generic;
using TShockAPI;
using Terraria;
namespace BindTools
{
	public class BindTool
	{
		public List<string> commands;
		public Item item;
		public bool looping;
		public int slot;
		public int prefix;
		public bool database;
		private int count;
		public BindTool(Item item, int slot, List<string> commands, bool looping = false, int prefix = -1, bool database = false)
		{
			this.item = item;
			this.commands = commands;
			this.looping = looping;
			this.slot = slot;
			this.prefix = prefix;
			this.database = database;
			count = 0;
		}
		public void DoCommand(TSPlayer player)
		{
			try
			{
				if (looping)
				{
					Commands.HandleCommand(player, commands[count]);
					count++;
					if (count >= commands.Count)
						count = 0;
				}
				else
				{
					foreach (string cmd in commands)
					{
						Commands.HandleCommand(player, cmd);
					}
				}
			}
			catch (Exception ex) { TShock.Log.ConsoleError(ex.ToString()); }
		}
	}
}