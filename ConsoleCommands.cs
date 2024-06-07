using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Merthsoft.DecBot3 {
	partial class DecBot
	{
		private static void SendMessage(IList<string> parameters)
		{
			if (parameters.Count < 2)
			{
				WriteErrorToConsole("Must supply a channel and message.");
				return;
			}
			var channel = parameters[0];

			if (!irc.GetChannels().Contains(channel, StringComparer.OrdinalIgnoreCase))
			{
				WriteErrorToConsole("You are not in the channel '{0}'.", channel);
				return;
			}

			var message = string.Join(" ", parameters.Skip(1));

			SendMessage(channel, message);
		}

		private static void ChangeName(IList<string> parameters)
		{
			if (parameters.Count != 1)
			{
				WriteErrorToConsole("Must supply a name, and only one name.");
			}

			irc.RfcNick(parameters[0]);
		}

		private static void PrintHelp(IList<string> parameters)
		{
			foreach (var cmdGroup in ConsoleCommandProcessors.GroupBy(a => a.Value))
			{
				var commands = new StringBuilder();
				foreach (var cmd in cmdGroup)
				{
					commands.Append(cmd.Key);
					commands.Append(", ");
				}
				commands.Length -= 2;
				WriteErrorToConsole("{0} - {1}", commands, cmdGroup.Key.Method.Name);
			}
		}

		private static void SendMessageToAll(IList<string> parameters)
		{
			if (parameters.Count < 1)
			{
				WriteErrorToConsole("Must supply a message.");
				return;
			}

			var message = string.Join(" ", parameters);

			foreach (var channel in irc.GetChannels())
			{
				SendMessage(channel, message);
			}
		}

		private static void SendAction(IList<string> parameters)
		{
			if (parameters.Count < 2)
			{
				WriteErrorToConsole("Must supply a channel and message.");
				return;
			}
			var channel = parameters[0];

			if (!irc.GetChannels().Contains(channel, StringComparer.OrdinalIgnoreCase))
			{
				WriteErrorToConsole("You are not in the channel '{0}'.", channel);
				return;
			}
			var message = string.Join(" ", parameters.Skip(1));

			SendAction(channel, message);
		}

        private static void JoinChannels(IList<string> channels)
        {
            if (channels.Count == 0)
            {
                WriteErrorToConsole("Must supply at least one channel.");
                return;
            }

            foreach (var channel in channels)
            {
                try
                {
                    irc.RfcJoin(channel);
                }
                catch (Exception ex)
                {
                    WriteErrorToConsole("Could not join {0}: {1}", channel, ex.Message);
                }
            }
        }

        private static void CycleChannels(IList<string> channels)
        {
            if (channels.Count == 0)
            {
                WriteErrorToConsole("Must supply at least one channel.");
                return;
            }

            foreach (var channel in channels)
            {
                try
                {
                    irc.RfcPart(channel, "Cycling.");
                }
                catch (Exception ex)
                {
                    WriteErrorToConsole("Could not join {0}: {1}", channel, ex.Message);
                }

                try
                {
                    irc.RfcJoin(channel);
                }
                catch (Exception ex)
                {
                    WriteErrorToConsole("Could not join {0}: {1}", channel, ex.Message);
                }
            }
        }

        private static void PartChannels(IList<string> channels)
		{
			if (channels.Count == 0)
			{
				WriteErrorToConsole("Must supply at least one channel.");
				return;
			}

			foreach (var channel in channels)
			{
				if (irc.GetChannels().Contains(channel, StringComparer.OrdinalIgnoreCase))
				{
					try
					{
						irc.RfcPart(channel);
					}
					catch (Exception ex)
					{
						WriteErrorToConsole("Could not part {0}: {1}", channel, ex.Message);
					}
				}
				else
				{
					WriteErrorToConsole("You are not in the channel '{0}'.", channel);
				}
			}
		}

		private static void ListChannels(IList<string> parameters)
		{
			WriteErrorToConsole("You are on these channels: {0}", string.Join(", ", irc.GetChannels()));
		}

		private static void ReloadConfig(IList<string> toss)
		{
			WriteErrorToConsole("Reloading config.");
			config = ReloadConfig();
			WriteErrorToConsole("Config reloaded:");
            WriteErrorToConsole("\tAuto ops:");
			foreach (var op in autoOpHosts)
				WriteErrorToConsole($"\t\t{op}");

			DoAutoOp();
        }

		private static void OpUser(IList<string> parameters)
		{
			if (!parameters.Any())
			{
				DoAutoOp();
				return;
			}

			if (parameters.Count != 2)
			{
				WriteErrorToConsole("Must specify a channel name and a user nickname.");
				return;
			}

			var channel = parameters[0];
			if (!irc.GetChannels().Contains(channel, StringComparer.OrdinalIgnoreCase))
			{
				WriteErrorToConsole("You are not in the channel '{0}'.", channel);
				return;
			}

			var nickname = parameters[1];
			irc.Op(channel, nickname);
		}

		private static void Quit(IList<string> parameters)
			=> Quit();
	}
}
