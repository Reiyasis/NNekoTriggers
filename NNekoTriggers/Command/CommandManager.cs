using Dalamud.Game.Command;
using ECommons.Logging;

namespace NNekoTriggers.Command
{
    /// <summary>
    ///     Initializes and manages all commands and command-events for the plugin.
    /// </summary>
    public sealed class CommandManager : IDisposable
    {
        /// <summary>
        ///     Defines the command prefix for all other plugin commands.
        /// </summary>
        private const string SettingsCommand = "/tconfig";
        private const string RpOnlyCmd = "/trponly";
        private const string RngCmd = "/trng";
        private const string GsetCmd = "/tgset";
        private const string ZoneCmd = "/tzone";
        private const string OverrideCmd = "/toverride";
        private const string OnLoginCmd = "/tonlogin";
        private const string ItemUseCmd = "/titemuse";

        /// <summary>
        ///     Initializes the CommandManager and its resources.
        /// </summary>
        public CommandManager()
        {
            NNekoTriggers.Commands.AddHandler(SettingsCommand, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "Opens the NNekoTriggers configuration window. " +
                $"\n\t '{SettingsCommand} toggle' disables or enables the entire plugin." +
                $"\n\t '{SettingsCommand} dtr' toggles the Server Info Bar entries.",
                ShowInHelp = true
            });

            NNekoTriggers.Commands.AddHandler(RpOnlyCmd, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "toggles the Roleplay Only trigger behavior." +
                $"\n\t '{RpOnlyCmd} on' enables the Roleplay Only trigger behavior." +
                $"\n\t '{RpOnlyCmd} off' disables the Roleplay Only trigger behavior.",
                ShowInHelp = true
            });

            NNekoTriggers.Commands.AddHandler(OverrideCmd, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "toggles the Command Override feature." +
                $"\n\t '{OverrideCmd} on' enables the Command Override feature." +
                $"\n\t '{OverrideCmd} off' disables the Command Override feature.",
                ShowInHelp = true
            });

            NNekoTriggers.Commands.AddHandler(RngCmd, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "toggles the RNG trigger behavior." +
                $"\n\t '{RngCmd} min <number>' sets Min to a fixed value. (default is 25)" +
                $"\n\t '{RngCmd} max <number>' sets Max to a fixed value. (default is 100)" +
                $"\n\t '{RngCmd} minAdd <number>' Adds <number> to the existing Min value." +
                $"\n\t '{RngCmd} minSub <number>' Subtracts <number> from the existing Min value." +
                $"\n\t '{RngCmd} maxAdd <number>' Adds <number> to the existing Max value." +
                $"\n\t '{RngCmd} maxSub <number>' Subtracts <number> from the existing Max value." +
                $"\n\t '{RngCmd} on' enables the RNG trigger behavior." +
                $"\n\t '{RngCmd} off' disables the RNG trigger behavior.",
                ShowInHelp = true
            });

            NNekoTriggers.Commands.AddHandler(GsetCmd, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "toggles the Job-Swap trigger." +
                $"\n\t '{GsetCmd} on' enables the Job-Swap trigger." +
                $"\n\t '{GsetCmd} off' disables the Job-Swap trigger.",
                ShowInHelp = true
            });

            NNekoTriggers.Commands.AddHandler(ZoneCmd, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "toggles the Zone change trigger." +
                $"\n\t '{ZoneCmd} on' enables the Zone change trigger." +
                $"\n\t '{ZoneCmd} off' disables the Zone change trigger.",
                ShowInHelp = true
            });

            NNekoTriggers.Commands.AddHandler(OnLoginCmd, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "toggles the Login trigger." +
                $"\n\t '{OnLoginCmd} on' enables the Login trigger." +
                $"\n\t '{OnLoginCmd} off' disables the Login trigger.",
                ShowInHelp = true
            });

            NNekoTriggers.Commands.AddHandler(ItemUseCmd, new CommandInfo(this.OnCommand)
            {
                HelpMessage = "toggles the Item-Use trigger." +
        $"\n\t '{ItemUseCmd} on' enables the Item-Use trigger." +
        $"\n\t '{ItemUseCmd} off' disables the Item-Use trigger.",
                ShowInHelp = true
            });

        }

        /// <summary>
        ///     Dispose of the PluginCommandManager and its resources.
        /// </summary>
        public void Dispose()
        {
            NNekoTriggers.Commands.RemoveHandler(SettingsCommand);
            NNekoTriggers.Commands.RemoveHandler(RpOnlyCmd);
            NNekoTriggers.Commands.RemoveHandler(RngCmd);
            NNekoTriggers.Commands.RemoveHandler(OverrideCmd);
            NNekoTriggers.Commands.RemoveHandler(GsetCmd);
            NNekoTriggers.Commands.RemoveHandler(ZoneCmd);
            NNekoTriggers.Commands.RemoveHandler(ItemUseCmd);
        }

        /// <summary>
        ///     Event handler for when a command is issued by the user.
        /// </summary>
        /// <param name="command">The command that was issued.</param>
        /// <param name="args">The arguments that were passed with the command.</param>
        ///
        private void OnCommand(string command, string args)
        {
            var config = Helpers.Utils.GetCharacterConfig();
            if (config is null)
            {
                return;
            }
            switch (command.ToLowerInvariant())
            {
                case SettingsCommand when args?.Length == 0:
                    NNekoTriggers.WindowManager.ToggleConfigWindow();
                    break;
                case SettingsCommand when args?.Length > 0 && args.Split(" ")[0] == "toggle":
                    config.PluginEnabled = !config.PluginEnabled;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers {(config.PluginEnabled ? "Enabled" : "Disabled")}");
                    NNekoTriggers.WindowManager.UpdateDtrEntry();
                    break;
                case SettingsCommand when args?.Length > 0 && args.Split(" ")[0] == "dtr":
                    config.ShowInDtr = !config.ShowInDtr;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers DTR Features {(config.ShowInDtr ? "Enabled" : "Disabled")}");
                    NNekoTriggers.WindowManager.UpdateDtrEntry();
                    break;
                case RpOnlyCmd when args?.Length == 0:
                    if (config != null)
                    {
                        config.EnableRpOnly = !config.EnableRpOnly;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Roleplay Only Module {(config.EnableRpOnly ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case RpOnlyCmd when args?.Length > 0 && args.Split(" ")[0] == "on":
                    if (config != null)
                    {
                        config.EnableRpOnly = true;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Roleplay Only Module {(config.EnableRpOnly ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case RpOnlyCmd when args?.Length > 0 && args.Split(" ")[0] == "off":
                    if (config != null)
                    {
                        config.EnableRpOnly = false;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Roleplay Only Module {(config.EnableRpOnly ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case RngCmd when args?.Length == 0:
                    if (config != null)
                    {
                        config.EnableRNG = !config.EnableRNG;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers RNG Module {(config.EnableRNG ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case RngCmd when args?.Length > 0 && args.Split(" ")[0] == "on":
                    if (config != null)
                    {
                        config.EnableRNG = true;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers RNG Module {(config.EnableRNG ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case RngCmd when args?.Length > 0 && args.Split(" ")[0] == "off":
                    if (config != null)
                    {
                        config.EnableRNG = false;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers  Module {(config.EnableRNG ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case RngCmd when args?.Length > 0:
                    if (config != null)
                    {
                        if (args.Split(' ')[0].Equals("min", StringComparison.OrdinalIgnoreCase))
                        {
                            config.OddsMin = int.Parse(args.Split(' ')[1]);
                            NNekoTriggers.PluginConfiguration.Save();
                            PluginLog.Information($"NNekoTriggers OddsMin set to {config.OddsMin}");
                            NNekoTriggers.WindowManager.UpdateDtrEntry();
                        }
                        if (args.Split(' ')[0].Equals("max", StringComparison.OrdinalIgnoreCase))
                        {
                            config.OddsMax = int.Parse(args.Split(' ')[1]);
                            NNekoTriggers.PluginConfiguration.Save();
                            PluginLog.Information($"NNekoTriggers OddsMax set to {config.OddsMax}");
                            NNekoTriggers.WindowManager.UpdateDtrEntry();
                        }
                        if (args.Split(' ')[0].Equals("minAdd", StringComparison.OrdinalIgnoreCase))
                        {
                            config.OddsMin += int.Parse(args.Split(' ')[1]);
                            NNekoTriggers.PluginConfiguration.Save();
                            PluginLog.Information($"NNekoTriggers OddsMin set to {config.OddsMin}");
                            NNekoTriggers.WindowManager.UpdateDtrEntry();
                        }
                        if (args.Split(' ')[0].Equals("minSub", StringComparison.OrdinalIgnoreCase))
                        {
                            config.OddsMin -= int.Parse(args.Split(' ')[1]);
                            NNekoTriggers.PluginConfiguration.Save();
                            PluginLog.Information($"NNekoTriggers OddsMin set to {config.OddsMin}");
                            NNekoTriggers.WindowManager.UpdateDtrEntry();
                        }
                        if (args.Split(' ')[0].Equals("maxAdd", StringComparison.OrdinalIgnoreCase))
                        {
                            config.OddsMax += int.Parse(args.Split(' ')[1]);
                            NNekoTriggers.PluginConfiguration.Save();
                            PluginLog.Information($"NNekoTriggers OddsMax set to {config.OddsMax}");
                            NNekoTriggers.WindowManager.UpdateDtrEntry();
                        }
                        if (args.Split(' ')[0].Equals("maxSub", StringComparison.OrdinalIgnoreCase))
                        {
                            config.OddsMax -= int.Parse(args.Split(' ')[1]);
                            NNekoTriggers.PluginConfiguration.Save();
                            PluginLog.Information($"NNekoTriggers OddsMax set to {config.OddsMax}");
                            NNekoTriggers.WindowManager.UpdateDtrEntry();
                        }
                    }
                    break;
                case GsetCmd when args?.Length == 0:
                    if (config != null)
                    {
                        config.EnableGset = !config.EnableGset;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Gearset Module {(config.EnableGset ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case GsetCmd when args?.Length > 0 && args.Split(" ")[0] == "on":
                    if (config != null)
                    {
                        config.EnableGset = true;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Gearset Module {(config.EnableGset ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case GsetCmd when args?.Length > 0 && args.Split(" ")[0] == "off":
                    if (config != null)
                    {
                        config.EnableGset = false;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Gearset Module {(config.EnableGset ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case ZoneCmd when args?.Length == 0:
                    if (config != null)
                    {
                        config.EnableZones = !config.EnableZones;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Zone Module {(config.EnableZones ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case ZoneCmd when args?.Length > 0 && args.Split(" ")[0] == "on":
                    if (config != null)
                    {
                        config.EnableZones = true;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Zone Module {(config.EnableZones ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case ZoneCmd when args?.Length > 0 && args.Split(" ")[0] == "off":
                    if (config != null)
                    {
                        config.EnableZones = false;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Zone Module {(config.EnableZones ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case OverrideCmd when args?.Length == 0:
                    if (config != null)
                    {
                        config.EnableOcmd = !config.EnableOcmd;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Override Module {(config.EnableOcmd ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case OverrideCmd when args?.Length > 0 && args.Split(" ")[0] == "on":
                    if (config != null)
                    {
                        config.EnableOcmd = true;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Override Module {(config.EnableOcmd ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case OverrideCmd when args?.Length > 0 && args.Split(" ")[0] == "off":
                    if (config != null)
                    {
                        config.EnableOcmd = false;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Override Module {(config.EnableOcmd ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case OnLoginCmd when args?.Length == 0:
                    if (config != null)
                    {
                        config.EnableOnLogin = !config.EnableOnLogin;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Login Module {(config.EnableOnLogin ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case OnLoginCmd when args?.Length > 0 && args.Split(" ")[0] == "on":
                    if (config != null)
                    {
                        config.EnableOnLogin = true;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Login Module {(config.EnableOnLogin ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case OnLoginCmd when args?.Length > 0 && args.Split(" ")[0] == "off":
                    if (config != null)
                    {
                        config.EnableOnLogin = false;
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Login Module {(config.EnableOnLogin ? "Enabled" : "Disabled")}");
                        NNekoTriggers.WindowManager.UpdateDtrEntry();
                    }
                    break;
                case ItemUseCmd when args?.Length == 0:
                case ItemUseCmd when args?.Length > 0 && args.Split(" ")[0] == "on":
                case ItemUseCmd when args?.Length > 0 && args.Split(" ")[0] == "off":
                    if (config != null)
                    {
                        config.EnableItemUse = args?.Length == 0 ? !config.EnableItemUse : args.Split(" ")[0] == "on";
                        NNekoTriggers.PluginConfiguration.Save();
                        PluginLog.Information($"NNekoTriggers Item-Use Module {(config.EnableItemUse ? "Enabled" : "Disabled")}");
                        // UpdateDtrEntry() は削除（DTR機能自体を無効化）
                    }
                    break;
            }
        }
    }
}
