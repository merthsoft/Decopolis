using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Meebey.SmartIrc4net;
using Merthsoft.DynamicConfig;

namespace Merthsoft.DecBot3
{
    partial class DecBot {
		private static bool QuitFlag = false;

		private static IrcClient irc;
				
		private static Dictionary<string, Action<string[]>> ConsoleCommandProcessors;

		private static TextWriter logWriter;
		private static string configLocation;

        private static HashSet<string> autoOpHosts;

        private static dynamic config;

		public class CommandLineOptions
		{
			[Option(shortName: 'l', longName: "logfile", Required = false, HelpText = "Name of a file to log to, or stderr by default", Default = null)]
			public string LogPath { get; set; }

			[Value(index: 0, Default = "decopolis.ini", HelpText = "Path to the configuration file to use")]
			public string ConfigFile { get; set; }
		}

		static void Main(string[] args) {
			Parser.Default.ParseArguments<CommandLineOptions>(args)
				.WithParsed(Run)
				.WithNotParsed(e => {
					foreach (var err in e) {
						Console.Error.WriteLine(err.ToString());
					}
				});
		}

		static void Quit()
			=> QuitFlag = true;

		static void Run(CommandLineOptions options) {
			if (options.LogPath != null) {
				logWriter = new StreamWriter(options.LogPath, true, Encoding.UTF8);
			} else {
				logWriter = Console.Error;
			}
			configLocation = options.ConfigFile;

			WriteConsoleMessage("Starting Decopolis.", MessageDirection.Notification);
			InitializeCommandProcessors();
			config = ReloadConfig();
			
			using var t = Task.Run(ReadConsoleCommands);
            connect();

            WriteConsoleMessage("Shutting down.", MessageDirection.Notification);
        }

		private static void connect() {
			WriteConsoleMessage(string.Format("Connecting to: {0} ", config.IRC.server), MessageDirection.Notification);
			irc = new IrcClient() {
				SendDelay = 400,
				ActiveChannelSyncing = true,
				CtcpVersion = config.IRC.realname, 
				AutoReconnect = true,
				AutoRejoin = true,
				AutoRejoinOnKick = true,
				AutoRelogin = true
			};

			irc.OnRawMessage += OnRawMessage;
			irc.OnWriteLine += OnWriteLine;
			irc.OnDisconnected += OnDisconnected;
            irc.OnJoin += OnJoin;
            irc.OnOp += OnOp;
            irc.OnDeop += OnDeOp;

            // the server we want to connect to, could be also a simple string
            var serverList = new string[] { config.IRC.server };
			var port = 6667;

			try {
				irc.Connect(serverList, port);
			} catch (ConnectionException e) {
                Console.WriteLine("Couldn't connect! Reason: " + e.Message);
				//Exit();
			}

			irc.Login(config.IRC.nickname, config.IRC.realname, 0, config.IRC.nickname);

			foreach (string chan in config.IRC.channels.Split(',')) {
				irc.RfcJoin(chan);
			}

			while (!QuitFlag)
			{
				try
				{
					irc.Listen(false);
				} catch (Exception ex)
				{
					WriteExceptionToConsole("Exception listening to irc.", ex);
				}
				Thread.Sleep(1);
			}
			try
			{
				irc.RfcQuit();
				irc.Disconnect();
			}
			catch { }
		}

		private static dynamic ReloadConfig() {
			var config = Config.ReadIni(configLocation);

            autoOpHosts = [];
            foreach (var kvp in config.AutoOp)
			{
				try
				{
					var host = (kvp.Value as string)!;
					var hostParts = host.Split('~');
					if (hostParts.Length > 1)
						host = hostParts[1];
					autoOpHosts.Add(host.ToUpper());
				} catch (Exception ex)
				{
					WriteExceptionToConsole($"Couldn't handle autoop {kvp}.", ex);
				}
			}

			Console.Title = config.IRC.realname;

			return config;
		}

		static bool shouldOp(string ident, string host)
			=> autoOpHosts.Contains($"{ident.Trim('~')}@{host}".ToUpper());

        static void OnDeOp(object sender, DeopEventArgs e)
		{
			try
			{
				var user = irc.GetChannelUser(e.Channel, e.Whom);
				if (user != null && shouldOp(user.Ident, user.Host))
					irc.Op(e.Channel, e.Whom);

				AutoOpChannel(e.Channel);
			} catch (Exception ex)
			{
                WriteExceptionToConsole("Exception in OnDeOp", ex);
            }
		}

        static void OnOp(object sender, OpEventArgs e)
        {
            if (e.Whom != irc.Nickname)
                return;

            try
            {
                AutoOpChannel(e.Channel);
            }
            catch (Exception ex)
            {
                WriteExceptionToConsole("Exception in OnOp", ex);
            }
        }

        private static void AutoOpChannel(string channelName)
        {
            var channel = irc.GetChannel(channelName);
            foreach (var entry in channel.Users.Cast<DictionaryEntry>())
            {
                var user = entry.Value as ChannelUser;
                try
                {
                    if (shouldOp(user.Ident, user.Host) && !user.IsOp)
                        irc.Op(channel.Name, user.Nick);
                }
                catch (Exception ex)
                {
                    WriteExceptionToConsole($"Exception in OnOp trying to op {user.Nick} {user.Ident} {user.Host}", ex);
                }
            }
        }

        static void DoAutoOp()
		{
			if (autoOpHosts.Count == 0 || irc.JoinedChannels.Count == 0)
				return;

			foreach (var channel in irc.JoinedChannels)
			{
				try
				{
					AutoOpChannel(channel);
				}
                catch (Exception ex)
                {
                    WriteExceptionToConsole($"Exception in DoAutoOp trying to op in channel {channel}.", ex);
                }
            }
		}

        static void OnJoin(object sender, JoinEventArgs e)
        {
            try
            {
                if (shouldOp(e.Data.Ident, e.Data.Host))
                    irc.Op(e.Data.Channel, e.Data.Nick);
            }
            catch (Exception ex)
            {
                WriteExceptionToConsole("Exception in OnJoin", ex);
            }
        }

        static void OnDisconnected(object sender, EventArgs e)
        {
            try
            {
                LogMessage("server", MessageDirection.Notification, "Disconnected from server. Attempting reconnect.");
                WriteConsoleMessage("Disconnected from server. Attempting reconnect.", MessageDirection.Notification);
                while (!irc.IsConnected)
                {
                    connect();
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                WriteExceptionToConsole("Exception in OnDisconnected", ex);
            }
        }

        private static void InitializeCommandProcessors() {
			ConsoleCommandProcessors = new Dictionary<string, Action<string[]>>() {
				{"/say", SendMessage},
				{"/me", SendAction},
				{"/join", JoinChannels},
				{"/j", JoinChannels},
                {"/part", PartChannels},
                {"/p", PartChannels},
                {"/cycle", CycleChannels},
                {"/c", CycleChannels},
                {"/list", ListChannels},
				{"/ls", ListChannels},
				{"/l", ListChannels},
				{"/listchannels", ListChannels},
				{"/sayall", SendMessageToAll},
				{"/reload", ReloadConfig},
				{"/help", PrintHelp},
				{"/nick", ChangeName},
				{"/op", OpUser},
                {"/quit", Quit},
                {"/q", Quit},
            };
		}

        public static void ReadConsoleCommands()
        {
            while (!QuitFlag)
            {
                try
                {
                    string message = Console.ReadLine();
                    string command;
                    string[] args;
                    string[] msg = message.TrimEnd(' ').Split(' ');

                    command = msg[0];
                    args = msg.Skip(1).ToArray();

					try
					{
						if (ConsoleCommandProcessors.TryGetValue(command, out var value))
						{
							value(args);

						}
						else
						{
							irc?.WriteLine(message);
						}
					}
					catch (Exception ex)
					{
						WriteConsoleMessage(string.Format("Unable to perform command: {0}", ex.Message), MessageDirection.Error);
					}
                }
                catch (Exception ex)
                {
                    WriteExceptionToConsole("Exception in ReadCommands", ex);
                }

                Thread.Sleep(1);
            }
        }

        private static void WriteExceptionToConsole(string text, Exception ex)
        {
            WriteConsoleMessage(ex.ToString(), MessageDirection.Error);
            WriteConsoleMessage(text, MessageDirection.Error);
        }

        private static void WriteErrorToConsole(string format, params object[] args) {
			WriteConsoleMessage(string.Format(format, args), MessageDirection.Error);
		}

		private static void SendMessage(string channel, string format, params object[] args) {
			irc.SendMessage(SendType.Message, channel, string.Format(format, args));
			LogMessage(channel, MessageDirection.Out, format, args);
		}

		private static void SendAction(string channel, string format, params object[] args) {
			irc.SendMessage(SendType.Action, channel, string.Format(format, args));
			LogMessage(channel, MessageDirection.Out, "/me " + format, args);
		}

		private static string getDirectionString(MessageDirection direction) {
			ConsoleColor color;
			return getDirectionString(direction, out color);
		}

		private static string getDirectionString(MessageDirection direction, out ConsoleColor color) {
			var dirString = "";
			switch (direction) {
				case MessageDirection.In:
					color = ConsoleColor.Green;
					dirString = ">>>";
					break;
				case MessageDirection.Out:
					color = ConsoleColor.Cyan;
					dirString = "<<<";
					break;
				default:
				case MessageDirection.Notification:
					color = ConsoleColor.Yellow;
					dirString = "---";
					break;
				case MessageDirection.Error:
					color = ConsoleColor.Red;
					dirString = "!!!";
					break;
			}
			return dirString;
		}

		private static void LogMessage(string channel, MessageDirection direction, string format, params object[] args) {
			var dir = getDirectionString(direction);

			logWriter.WriteLine("{0} [{1}] {2} {3}", dir,
				DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff"),
				channel, string.Format(format, args)
			);
			logWriter.Flush();
		}

        private static void OnRawMessage(object sender, IrcEventArgs e)
        {
            try
            {
                WriteConsoleMessage(e.Data.RawMessage, MessageDirection.In);
            }
            catch (Exception ex)
            {
                WriteExceptionToConsole("Exception in OnRawMessage", ex);
            }
        }

        private static void OnWriteLine(object sender, WriteLineEventArgs e)
        {
            try
            {
                WriteConsoleMessage(e.Line, MessageDirection.Out);
            }
            catch (Exception ex)
            {
                WriteExceptionToConsole("Exception in OnWriteLine", ex);
            }
        }

        private static void WriteConsoleMessage(string message, MessageDirection direction) {
			var color = ConsoleColor.White;
			var dirString = getDirectionString(direction, out color);

			Console.ForegroundColor = color;
			Console.WriteLine("[{0}] {1} {2}", DateTime.Now.ToString("HH:mm:ss"), dirString, message);
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
