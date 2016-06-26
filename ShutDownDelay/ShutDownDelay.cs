using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json;
using Thread = System.Threading.Thread;

namespace ShutDownDelay
{
    [ApiVersion(1, 23)]
    public class ShutDownDelay : TerrariaPlugin
    {
        private Config config = new Config();
        public static string configPath = Path.Combine(TShock.SavePath, "ShutDownDelayConfig.json");

        private bool shutDownInProgress = false;
        private bool shutDownPaused = false;

        private int oldDelay;

        private DateTime lastCheck = DateTime.UtcNow;

        public override string Author
        {
            get
            {
                return "Professor X";
            }
        }

        public override string Description
        {
            get
            {
                return "";
            }
        }

        public override string Name
        {
            get
            {
                return "ShutDownDelay";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 3, 0);
            }
        }

        public ShutDownDelay(Main game) : base(game)
        {

        }

        #region Initialize/Dispose
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Hooks
        public void OnInitialize(EventArgs args)
        {
            (config = config.Read(configPath)).Write(configPath);
            Commands.ChatCommands.Add(new Command("sdd.use", ShutDownCmd, "sddelay", "sdd"));
        }

        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - lastCheck).TotalSeconds >= 1)
            {
                lastCheck = DateTime.UtcNow;

                if (shutDownInProgress)
                {
                    config.delay--;

                    if (config.notifyIntervals.Contains(config.delay))
                    {
                        TSPlayer.All.SendInfoMessage("Server shutting down in {0} second{1}.", config.delay.ToString(), config.delay > 1 ? "s" : "");
                    }

                    if (config.delay == 0)
                    {
                        TShock.Utils.StopServer(true, config.reason == "" ? "Server shutting down!" : config.reason);
                    }
                }
            }
        }
        #endregion

        #region Commands
        private void ShutDownCmd(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}sddelay <start/pause/continue/cancel/reload>", TShock.Config.CommandSpecifier);
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "start":
                    {
                        if (shutDownInProgress)
                        {
                            args.Player.SendErrorMessage("A shutdown is already in progress.");
                            return;
                        }
                        else
                        {
                            oldDelay = config.delay;
                            shutDownInProgress = true;
                            TSPlayer.All.SendSuccessMessage("The server will shut down in {0} second{1}. Reason: {2}", config.delay.ToString(), config.delay > 1 ? "s" : "", config.reason == "" ? "No reason specified." : config.reason);
                        }
                    }
                    return;
                case "pause":
                    {
                        if (!shutDownInProgress)
                        {
                            args.Player.SendErrorMessage("There is no shutdown in progress.");
                            return;
                        }
                        else if (shutDownPaused)
                        {
                            args.Player.SendErrorMessage("The current shutdown is already paused.");
                            return;
                        }
                        else
                        {
                            shutDownPaused = true;
                            shutDownInProgress = false;
                            TSPlayer.All.SendSuccessMessage("The shutdown has been paused.");
                        }
                    }
                    return;
                case "continue":
                    {
                        if (!shutDownPaused)
                        {
                            args.Player.SendErrorMessage("There is no paused shutdown!");
                            return;
                        }
                        else
                        {
                            shutDownPaused = false;
                            shutDownInProgress = true;
                            TSPlayer.All.SendSuccessMessage("The shutdown has been continued.");
                        }
                    }
                    return;
                case "cancel":
                    {
                        if (!shutDownInProgress)
                        {
                            args.Player.SendErrorMessage("There is no shutdown in progress!");
                            return;
                        }
                        else
                        {
                            config.delay = oldDelay;
                            shutDownInProgress = false;
                            shutDownPaused = false;
                            TSPlayer.All.SendSuccessMessage("The shutdown has been canceled.");
                        }
                    }
                    return;
                case "reload":
                    {
                        (config = config.Read(configPath)).Write(configPath);
                        args.Player.SendSuccessMessage("ShutDownDelay config reloaded.");
                    }
                    return;
                default:
                    {
                        args.Player.SendErrorMessage("Invalid subcommand. Valid subcommands:");
                        args.Player.SendErrorMessage("Start - starts a shutdown");
                        args.Player.SendErrorMessage("Pause - pauses a shutdown");
                        args.Player.SendErrorMessage("Continue - continues a shutdown");
                        args.Player.SendErrorMessage("Cancel - cancels a shutdown");
                        args.Player.SendErrorMessage("Reload - reloads the config file");
                    }
                    return;
            }
        }
        #endregion
    }
}
