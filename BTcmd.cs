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
        private int count;
        public BindTool(Item i, List<string> cmd, bool loop = false)
        {
            item = i;
            commands = cmd;
            looping = loop;
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
