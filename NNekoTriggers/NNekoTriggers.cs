using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.GameHelpers;
using NNekoTriggers.Command;
using NNekoTriggers.Configuration;
using NNekoTriggers.Helpers;
using NNekoTriggers.UI;
using Task = System.Threading.Tasks.Task;
using TerritoryType = Lumina.Excel.Sheets.TerritoryType;

namespace NNekoTriggers
{
    internal sealed class NNekoTriggers : IDalamudPlugin, IDisposable
    {
        #region
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public static ICommandManager Commands { get; private set; }
        [PluginService] public static IClientState ClientState { get; private set; }
        [PluginService] public static IDataManager DataManager { get; private set; }
        [PluginService] public static ICondition Condition { get; private set; }
        [PluginService] public static IFramework Framework { get; private set; }
        [PluginService] public static IDtrBar DtrBar { get; private set; }
        [PluginService] public static IAgentLifecycle AgentLifecycle { get; private set; }
        [PluginService] public static IPluginLog PluginLog { get; private set; }
        [PluginService] public static IPlayerState PlayerState { get; private set; }
        [PluginService] internal static IToastGui Toast { get; private set; } = null!;
        public static ToastOptions ToastOptions = new()
        {
            Speed = ToastSpeed.Fast,
            Position = ToastPosition.Top
        };
        public static CommandManager CommandManager { get; private set; }
        public static WindowManager WindowManager { get; private set; }
        public static PluginConfiguration PluginConfiguration { get; private set; }
        //internal static IDtrBarEntry DtrEntry;
        public static IEnumerable<TerritoryType> AllowedTerritories;
        private const uint ROLEPLAY_ONLINE_STATUS_ID = 22;
        private static readonly uint[] AllowedTerritoryUse = [
              0, // Town
              1, // Open World
              2, // Inn
             13, // Housing Area
             19, // Chocobo Square
             23, // Gold Saucer
             30, // Grand Company Garrison
             41, // Eureka
             45, // Masked Carnival
             46, // Ocean Fishing
             47, // Island Sanctuary
             48, // Bozja
             60, // Cosmic Exploration
        ];
        #endregion

        /// <summary>
        ///     The plugin's main entry point.
        /// </summary>
        public NNekoTriggers()
        {
            ECommonsMain.Init(PluginInterface, this, Module.DalamudReflector);
            PluginConfiguration = PluginConfiguration.Load();
            AllowedTerritories = DataManager.Excel.GetSheet<TerritoryType>().Where(x => AllowedTerritoryUse.Contains(x.TerritoryIntendedUse.RowId) && !x.IsPvpZone);

            WindowManager = new(Framework, DtrBar);
            if (WindowManager.dtrHooked)
            {
                WindowManager.UpdateDtrEntry();
            }
            CommandManager = new();
            var config = Utils.GetCharacterConfig();
            ClientState.ZoneInit += this.ClientState_ZoneInit;
            ClientState.MapIdChanged += this.ClientState_MapIdChanged;
            ClientState.TerritoryChanged += this.OnTerritoryChanged;
            ClientState.ClassJobChanged += this.ClientState_ClassJobChanged;
            ClientState.Login += ClientState_OnLogin;
        }

        /// <summary>
        ///     Disposes of the plugin's resources.
        /// </summary>
        public void Dispose()
        {
            ClientState.Login -= ClientState_OnLogin;
            ClientState.ClassJobChanged -= this.ClientState_ClassJobChanged;
            ClientState.TerritoryChanged -= this.OnTerritoryChanged;
            ClientState.MapIdChanged -= this.ClientState_MapIdChanged;
            ClientState.ZoneInit -= this.ClientState_ZoneInit;
            CommandManager.Dispose();
            WindowManager.Dispose();
            ECommonsMain.Dispose();
        }

        /// <summary>
        ///     Handles class/job changes and custom command execution.
        /// </summary>
        /// <param name="classJobId"></param>
        private void ClientState_ClassJobChanged(uint classJobId)
        {
            if (!ClientState.IsLoggedIn)
            {
                return;
            }
            var characterConfig = Utils.GetCharacterConfig();
            if (characterConfig.PluginEnabled && (!characterConfig.EnableRpOnly || Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)) &&
                characterConfig.EnableGset && PlayerState.ClassJob.Value.ClassJobCategory.IsValid)
            {
                PluginLog.Information("ClientState_ClassJobChanged: Job Swap Command Triggered");
                new Task(() =>
                {
                    if (ShouldDoENF())
                    {
                        try
                        {
                            var cmd = "/echo NNekoTriggers: Job Swap Command is Unset.";
                            if (characterConfig.EnableOcmd)
                            {
                                cmd = characterConfig.OverrideCommand.Content;
                            }
                            else if (!GenericHelpers.IsNullOrEmpty(characterConfig.GearsetCommand.Content))
                            {
                                cmd = characterConfig.GearsetCommand.Content;
                            }
                            else
                            {
                                PluginLog.Information("Unable to execute, because no Override or Job Swap commands were found.");
                                return;
                            }
                            if (cmd == null)
                            {
                                PluginLog.Error("Unable to execute, because the command appears to be empty.");
                                return;
                            }
                            else if (cmd != null)
                            {
                                PluginLog.Information("ClientState_ClassJobChanged: Trigger Successful. Processing Job Swap Command.");
                                if (!Player.Mounted)
                                {
                                    Commands.ProcessCommand(cmd);
                                }
                            }
                        }
                        catch (Exception e) { PluginLog.Error(e, "ClientState_ClassJobChanged: An error occured processing ClientState_ClassJobChanged."); }
                    }
                }).Start();
            }
        }

        /// <summary>
        ///     Handles Map changes and custom command execution.
        /// </summary>
        /// <param name="obj"></param>
        private void ClientState_MapIdChanged(uint obj)
        {
            if (!ClientState.IsLoggedIn)
            {
                return;
            }
            else
            {
                var characterConfig = Utils.GetCharacterConfig();
                if (characterConfig.PluginEnabled &&
                characterConfig.EnableZones &&
                (!characterConfig.EnableRpOnly || Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)) &&
                (!GenericHelpers.IsNullOrEmpty(characterConfig.ZoneCommand.Content)))
                {
                    PluginLog.Information("OnTerritoryChanged: Territory Command Triggered");

                    if (!AllowedTerritories.Any(t => t.RowId == Player.Territory.RowId))
                    {
                        PluginLog.Warning($"Territory {Player.Territory} is not an allowed territoryID, skipping custom executions.");
                        return;
                    }
                    HandleZoneTriggerENF(characterConfig, characterConfig.ZoneCommand);
                }
            }
        }

        /// <summary>
        ///     Handles Zone changes and custom command execution.
        /// </summary>
        /// <param name="obj"></param>
        private void ClientState_ZoneInit(Dalamud.Game.ClientState.ZoneInitEventArgs obj)
        {
            if (!ClientState.IsLoggedIn)
            {
                return;
            }
            else
            {
                var characterConfig = Utils.GetCharacterConfig();
                if (characterConfig.PluginEnabled &&
                characterConfig.EnableZones &&
                (!characterConfig.EnableRpOnly || Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)) &&
                (!GenericHelpers.IsNullOrEmpty(characterConfig.ZoneCommand.Content)))
                {
                    PluginLog.Information("OnTerritoryChanged: Territory Command Triggered");

                    if (!AllowedTerritories.Any(t => t.RowId == Player.Territory.RowId))
                    {
                        PluginLog.Warning($"Territory {Player.Territory} is not an allowed territoryID, skipping custom executions.");
                        return;
                    }
                    HandleZoneTriggerENF(characterConfig, characterConfig.ZoneCommand);
                }
            }
        }

        /// <summary>
        ///     Handles territory changes and custom command execution.
        /// </summary>
        /// <param name="territory"></param>
        private void OnTerritoryChanged(uint territory)
        {

            if (!ClientState.IsLoggedIn)
            {
                return;
            }
            else
            {
                var characterConfig = Utils.GetCharacterConfig();
                if (characterConfig.PluginEnabled &&
                characterConfig.EnableZones &&
                (!characterConfig.EnableRpOnly || Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)) &&
                (!GenericHelpers.IsNullOrEmpty(characterConfig.ZoneCommand.Content)))
                {
                    PluginLog.Information("OnTerritoryChanged: Territory Command Triggered");

                    if (!AllowedTerritories.Any(t => t.RowId == territory))
                    {
                        PluginLog.Warning($"Territory {territory} is not an allowed territoryID, skipping custom executions.");
                        return;
                    }
                    HandleZoneTriggerENF(characterConfig, characterConfig.ZoneCommand);
                }
            }
        }

        /// <summary>
        ///     Handles the login trigger and custom execution.
        /// </summary>
        private static void ClientState_OnLogin()
        {
            if (!ClientState.IsLoggedIn)
            {
                return;
            }
            else
            {
                var characterConfig = Utils.GetCharacterConfig();
                if (characterConfig.PluginEnabled &&
                characterConfig.EnableOnLogin &&
                (!characterConfig.EnableRpOnly || Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)) &&
                (!GenericHelpers.IsNullOrEmpty(characterConfig.OnLoginCommand.Content)))
                {
                    PluginLog.Information("OnLogin: Login Command Triggered");

                    if (!AllowedTerritories.Any(t => t.RowId == Player.Territory.RowId))
                    {
                        PluginLog.Warning($"Territory {Player.Territory} is not an allowed territoryID, skipping custom executions.");
                        return;
                    }
                    HandleZoneTriggerENF(characterConfig, characterConfig.OnLoginCommand);
                }
            }
        }

        /// <summary>
        ///     Processes the command used for any changes in zone, map, or territory.
        /// </summary>
        /// <param name="characterConfig"></param>
        private static void HandleZoneTriggerENF(CharacterConfiguration characterConfig, CustomCommand command) => new Task(() =>
        {
            if (ShouldDoENF())
            {
                try
                {
                    /*while (Condition[ConditionFlag.BetweenAreas]
                        || Condition[ConditionFlag.BetweenAreas51]
                        || Condition[ConditionFlag.Occupied]
                        || Condition[ConditionFlag.OccupiedInCutSceneEvent]
                        || Condition[ConditionFlag.Unconscious])*/
                    while (!Utils.CanUseGlamourPlates())
                    {
                        PluginLog.Information("Unable to execute yet, waiting for conditions to clear.");
                        var delay = TimeSpan.FromSeconds(1);
                        Task.Delay(delay).Wait();
                    }
                    var cmd = "/echo NNekoTriggers: Territory/Login Command is Unset.";
                    if (characterConfig.EnableOcmd)
                    {
                        cmd = characterConfig.OverrideCommand.Content;
                    }
                    else if (!GenericHelpers.IsNullOrEmpty(command.Content))
                    {
                        cmd = characterConfig.ZoneCommand.Content;
                    }
                    else
                    {
                        PluginLog.Information("Unable to execute, because no Override, Login, or Territory commands were found.");
                        return;
                    }
                    if (cmd == null)
                    {
                        PluginLog.Error("Unable to execute, because the command appears to be empty.");
                        return;
                    }
                    else if (cmd != null)
                    {
                        PluginLog.Information("Trigger Successful. Processing Territory Command.");
                        if (!Player.Mounted)
                        {
                            Commands.ProcessCommand(cmd);
                        }
                    }
                }
                catch (Exception e) { PluginLog.Error(e, "An error occured whilst attempting to execute custom commands."); }
            }
        }).Start();

        /// <summary>
        ///     Checks the current player status and the plugin configuration to determine whether to queue an attempted execution of custom commands.
        /// </summary>
        /// <returns>True if conditions for attempting the commands are met, and False otherwise.</returns>
        private static bool ShouldDoENF()
        {
            var result = false;
            var characterConfig = Utils.GetCharacterConfig();
            if (characterConfig.PluginEnabled)
            {
                result = ((characterConfig.EnableRNG && (Random.Shared.Next(characterConfig.OddsMax) <= characterConfig.OddsMin)) ||
                          !characterConfig.EnableRNG) && !(Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.WaitingForDuty]);
                PluginLog.Information($"ShouldDoENF: " +
                    $"\nEnableRNG = {(characterConfig.EnableRNG ? "Enabled" : "Disabled")} " +
                    $"\nOddsMin = {characterConfig.OddsMin}" +
                    $"\nOddsMax = {characterConfig.OddsMax}" +
                    $"\nMounted = {(Condition[ConditionFlag.Mounted] ? "True" : "False")}" +
                    $"\nWaitingForDuty = {(Condition[ConditionFlag.WaitingForDuty] ? "True" : "False")}" +
                    $"\nResult = {(result ? "True" : "False")}");
            }
            return result;
        }
    }
}
