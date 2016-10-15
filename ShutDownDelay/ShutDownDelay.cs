using System;
using System.IO;
using System.Linq;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace ShutDownDelay
{
    [ApiVersion(1, 25)]
    public class ShutDownDelay : TerrariaPlugin
    {
        private Config config = new Config();
        public static string configPath = Path.Combine(TShock.SavePath, "ShutDownDelayConfig.json");

        private bool shutDownInProgress => _timer.Enabled;
        private bool shutDownPaused => !_timer.Enabled && _delay > 0;

        private int _delay = 0;
        private string _reason;
        private Timer _timer;

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

            _timer = new Timer(1000);
            _timer.Elapsed += timerUpdate;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);

                if (_timer.Enabled)
                {
                    _timer.Stop();
                }
                _timer.Elapsed -= timerUpdate;
            }
        }
        #endregion

        #region Hooks
        public void OnInitialize(EventArgs args)
        {
            (config = config.Read(configPath)).Write(configPath);
            Commands.ChatCommands.Add(new Command("sdd.use", ShutDownCmd, "sddelay", "sdd"));
        }

        private void timerUpdate(object sender, ElapsedEventArgs e)
        {
            _delay--;
            if (config.notifyIntervals.Contains(_delay))
            {
                TSPlayer.All.SendInfoMessage("Server shutting down in {0} second{1}.", _delay, _delay > 1 ? "s" : "");
            }

            if (_delay == 0)
            {
                _timer.Stop();
                TShock.Utils.StopServer(true, _reason == "" ? "Server shutting down!" : _reason);
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

            /* Manipulate the reason at will, be it through the start, stop or continue command, in the event a reason
             * other than the default one set in the config is required */
            for (int i = 0; i < args.Parameters.Count; i++)
            {
                if (args.Parameters[i].StartsWith("-reason", StringComparison.OrdinalIgnoreCase) && ++i < args.Parameters.Count)
                {
                    _reason = String.Join(" ", args.Parameters.GetRange(i, args.Parameters.Count - i));
                }
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
                            // Set custom delays on the fly with the '-delay' argument
                            for (int i = 0; i < args.Parameters.Count; i++)
                            {
                                if (args.Parameters[i].StartsWith("-delay", StringComparison.OrdinalIgnoreCase) && ++i < args.Parameters.Count)
                                {
                                    int parsedDelay;
                                    if (!Int32.TryParse(args.Parameters[i], out parsedDelay) || parsedDelay < 1)
                                    {
                                        args.Player.SendErrorMessage("Invalid delay!");
                                        return;
                                    }
                                    _delay = parsedDelay;
                                }
                            }

                            // If delay was not modified, use the configured one
                            if (_delay == 0)
                            {
                                _delay = config.delay;
                            }

                            // In the event no '-reason' argument was passed, use the configured one
                            if (String.IsNullOrEmpty(_reason))
                            {
                                _reason = config.reason;
                            }

                            _timer.Start();
                            TSPlayer.All.SendSuccessMessage("The server will shut down in {0} second{1}. Reason: {2}", _delay, _delay > 1 ? "s" : "", _reason == "" ? "No reason specified." : _reason);
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
                            _timer.Stop();
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
                            _timer.Start();
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
                            _timer.Stop();
                            _delay = 0;
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
