using Discord.WebSocket;
using Evel_Bot.Commands;
using Evel_Bot.Modules;
using Evel_Bot.Util;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace Evel_Bot
{

    class Program //? Main Class
    {
        public delegate Task ShellEventType(ShellEventArgs e);
        /// <summary>
        /// Shell input and Remote input
        /// </summary>
        public static event ShellEventType ShellEvent;

        public static Account ClientAccount { get; private set; } = new Account();
        public static DiscordSocketClient Client => ClientAccount.Client;

        private static string[] args;

        static void Main(string[] args) //! Main Method
        {
            // Set Encoding
            Console.OutputEncoding = Encoding.UTF8;
            Console.OutputEncoding = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                Encoding.Unicode : Encoding.UTF8;

            // Ini methods
            Shell.WriteLine(ConsoleColor.DarkCyan, "Starting Evel-Bot client...");
            Ini();
            Program.args = args;
            Shell.WriteLine("Client ready, please connect to a bot or a user account");
            Shell.WriteLine("Exemple : \"connect [bot | user] token");

            // Is a loop requiered?
            while (true)
            {
                new Program().MainAsync().GetAwaiter().GetResult();
            }
        }

        static void Ini() //! Execute all Ini Methods
        {
            IniAccounts();
            IniModules();
        }

        static void IniAccounts() //! Get remembered accounts
        {
            if (ClientAccount.IsEmpty)
                return;

            Shell.WriteLine("Finded some accounts in \"accounts.config\" file.");
            Shell.WriteLine("You can connect to a known account with \"connect N\"");
            Shell.Write();
            Shell.WriteLine("N Type Name    Token");

            int i = 1;

            foreach(TokenId id in ClientAccount.Accounts)
            {
                Shell.WriteLine(i.ToString() + " " + ((id.Type == TokenType.Bot) ? "Bot " : "User") + " " + id.Username + " " + id.Token);
            }
            Shell.Write();
        }

        static void IniModules() //! Get all modules
        {
            Modules.Module.ModulesList = (from module in Assembly.GetEntryAssembly().GetTypes()
                                          where module.GetInterfaces().Contains(typeof(IModule)) && !module.IsInterface
                                          select Activator.CreateInstance(module) as IModule).ToList();

            //? Modules config directory
            string modulespath = Path.Combine(AppContext.BaseDirectory, "Modules");
            try
            {
                if (!Directory.Exists(modulespath))
                    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Modules"));
            }
            catch (Exception e)
            {
                Shell.WriteLineError("Can't create Modules config directory.");
                Shell.WriteLineError(e.Message);
            }

            //? Auto activated Modules
            ConfigurationFile modules = new ConfigurationFile(Misc.GetFilePath("modules.config"));

            foreach (IModule mod in Modules.Module.ModulesList)
            {
                string name = mod.GetType().Name;

                if (modules[name] == null)
                {
                    modules.Add(name, "false");
                    modules.Save();
                    continue;
                }
                if (modules[name].Value == "true")
                {
                    mod.Activate();
                    mod.IsActivated = true;
                    Shell.Write(ConsoleColor.DarkCyan, "[Autostart] ");
                    Shell.WriteLine(ConsoleColor.Cyan, name);
                    //x Log.SendLog("[Autostart] " + name);
                }
            }

            Shell.Write();
        }

        async Task MainAsync() //! Main loop
        {
            foreach (string str in args) //? Execute launch commands
            {
                await Command.ExeCommand(str);
            }

            while(true) //? Main async loop
            {
                string input = Shell.Input();
                ShellEventArgs args = new ShellEventArgs(input);

                if (ShellEvent != null)
                    await ShellEvent.Invoke(args);

                if (!args.Handled)
                    await Command.ExeCommand(input);
            }
        }

        public static async Task SendCommand(string input)
        {
            ShellEventArgs args = new ShellEventArgs(input);

            if (ShellEvent != null)
                await ShellEvent.Invoke(args);

            if (!args.Handled)
                await Command.ExeCommand(input);
        }
    }

    class ShellEventArgs //? Args for ShellEvent
    {
        public string Input { get; }
        public bool Handled { get; set; } = false;

        public ShellEventArgs(string input)
        {
            this.Input = input;
        }
    }
}
