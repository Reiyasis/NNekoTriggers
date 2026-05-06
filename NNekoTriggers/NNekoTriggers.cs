using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
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
        [PluginService] public static IGameInteropProvider GameInterop { get; private set; }
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
        private Hook<ActionManager.Delegates.UseAction>? _useActionHook;
        private const uint ROLEPLAY_ONLINE_STATUS_ID = 22;
        private static readonly uint[] AllowedTerritoryUse = [
              0, // Town
              1, // Open World
              2, // Inn
             13, // Housing Area
             14, //House Indoor
             19, // Chocobo Square
             23, // Gold Saucer
             30, // Grand Company Garrison
             41, // Eureka
             45, // Masked Carnival
             46, // Ocean Fishing
             47, // Island Sanctuary
             48, // Bozja
             60, // Cosmic Exploration
             61, //Occult
             31, //Deepdungeon
             57, //Criterion
             58, //Criterion Savage
             48, //Ocean Fishing
             28, //Crystaline Conflict
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
            CommandManager = new();
            var config = Utils.GetCharacterConfig();
            WindowManager.UpdateDtrEntry();
            ClientState.ZoneInit += this.ClientState_ZoneInit;
            ClientState.MapIdChanged += this.ClientState_MapIdChanged;
            ClientState.TerritoryChanged += this.OnTerritoryChanged;
            ClientState.ClassJobChanged += this.ClientState_ClassJobChanged;
            ClientState.Login += ClientState_OnLogin;
            unsafe
            {
                _useActionHook = GameInterop.HookFromAddress<ActionManager.Delegates.UseAction>(
                    ActionManager.MemberFunctionPointers.UseAction,
                    UseActionDetour);
                _useActionHook.Enable();
            }
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
            _useActionHook?.Disable();
            _useActionHook?.Dispose();
        }

        /// <summary>
        ///     ジョブ変更を検知 + 3つのコマンドからランダムに1つ実行
        /// </summary>
        private void ClientState_ClassJobChanged(uint classJobId)
        {
            if (!ClientState.IsLoggedIn)
                return;

            var characterConfig = Utils.GetCharacterConfig();
            if (!characterConfig.PluginEnabled ||
                !characterConfig.EnableGset ||
                (characterConfig.EnableRpOnly && !Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)) ||
                !PlayerState.ClassJob.Value.ClassJobCategory.IsValid)
            {
                return;
            }

            PluginLog.Information("ClientState_ClassJobChanged: Job Swap Command Triggered");

            new Task(() =>
            {
                if (!ShouldDoENF())
                    return;

                try
                {
                    // 3つのコマンドから有効なものを集める
                    var commands = new List<string>();
                    if (!string.IsNullOrWhiteSpace(characterConfig.GearsetCommand1.Content))
                        commands.Add(characterConfig.GearsetCommand1.Content);
                    if (!string.IsNullOrWhiteSpace(characterConfig.GearsetCommand2.Content))
                        commands.Add(characterConfig.GearsetCommand2.Content);
                    if (!string.IsNullOrWhiteSpace(characterConfig.GearsetCommand3.Content))
                        commands.Add(characterConfig.GearsetCommand3.Content);

                    if (commands.Count == 0)
                    {
                        PluginLog.Warning("Job Swap Triggered but no commands are set.");
                        return;
                    }

                    // ランダムに1つ選択
                    var selectedCommand = commands[Random.Shared.Next(commands.Count)];

                    PluginLog.Information($"Job Swap Triggered → Executing random command: {selectedCommand}");

                    // Overrideが有効ならそちらを優先（既存の挙動を維持）
                    var cmd = characterConfig.EnableOcmd
                        ? characterConfig.OverrideCommand.Content
                        : selectedCommand;

                    if (string.IsNullOrWhiteSpace(cmd))
                    {
                        PluginLog.Error("Unable to execute, because the command appears to be empty.");
                        return;
                    }

                    if (!Player.Mounted)
                    {
                        Commands.ProcessCommand(cmd);
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "ClientState_ClassJobChanged: An error occured processing ClientState_ClassJobChanged.");
                }
            }).Start();
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
        ///     アイテム使用を即座に検知 + 3つのコマンドからランダムに1つ実行
        /// </summary>
        private unsafe bool UseActionDetour(
            ActionManager* actionManager,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint extraParam,
            ActionManager.UseActionMode mode,
            uint comboRouteId,
            bool* outOptAreaTargeted)
        {
            var result = _useActionHook!.Original(actionManager, actionType, actionId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);

            if (actionType != ActionType.Item || !ClientState.IsLoggedIn)
                return result;

            var characterConfig = Utils.GetCharacterConfig();
            if (characterConfig == null ||
                !characterConfig.PluginEnabled ||
                !characterConfig.EnableItemUse ||
                (characterConfig.EnableRpOnly && !Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)))
            {
                return result;
            }

            // 既存のRNG判定をそのまま利用（RNG OFFなら必ず実行）
            if (!ShouldDoENF())
                return result;

            // 3つのコマンドから有効なものをランダム選択
            var commands = new List<string>();
            if (!string.IsNullOrWhiteSpace(characterConfig.ItemUseCommand1.Content))
                commands.Add(characterConfig.ItemUseCommand1.Content);
            if (!string.IsNullOrWhiteSpace(characterConfig.ItemUseCommand2.Content))
                commands.Add(characterConfig.ItemUseCommand2.Content);
            if (!string.IsNullOrWhiteSpace(characterConfig.ItemUseCommand3.Content))
                commands.Add(characterConfig.ItemUseCommand3.Content);

            if (commands.Count == 0)
            {
                PluginLog.Warning("ItemUse Triggered but no commands are set.");
                return result;
            }

            // ランダムに1つ選択
            var selectedCommand = commands[Random.Shared.Next(commands.Count)];
            PluginLog.Information($"ItemUse Triggered (Item ID: {actionId}) → Executing random command: {selectedCommand}");

            // 実行（ゾーントリガーと同じ待機処理を再利用）
            new Task(() =>
            {
                try
                {
                    while (!Utils.CanUseGlamourPlates())
                    {
                        PluginLog.Information("Unable to execute yet, waiting for conditions to clear.");
                        Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    }

                    if (!Player.Mounted)
                    {
                        Commands.ProcessCommand(selectedCommand);
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "An error occured whilst attempting to execute item use command.");
                }
            }).Start();

            return result;
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
