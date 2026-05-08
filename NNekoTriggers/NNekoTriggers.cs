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
using Dalamud.Game.Gui.Toast;
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
        [PluginService] public static IToastGui ToastGui { get; private set; }
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
        ///     ジョブ/ギアセット変更を検知 + 3つのコマンドからランダムに1つ実行 + 各コマンド専用の複数行テキストを表示
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

            new Task(async () =>
            {
                if (!ShouldDoENF())
                    return;

                try
                {
                    var commands = new List<string>
            {
                characterConfig.GearsetCommand1.Content,
                characterConfig.GearsetCommand2.Content,
                characterConfig.GearsetCommand3.Content
            };

                    var displayTexts = new List<string>
            {
                characterConfig.GearsetDisplayText1,
                characterConfig.GearsetDisplayText2,
                characterConfig.GearsetDisplayText3
            };

                    if (commands.Count == 0)
                        return;

                    int index = Random.Shared.Next(commands.Count);
                    var selectedCommandBlock = commands[index];

                    PluginLog.Information($"Job Swap Triggered → Executing command block: {selectedCommandBlock}");

                    // ★★★ コマンドを改行で分割して順番に実行 ★★★
                    var commandLines = selectedCommandBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in commandLines)
                    {
                        var cmd = line.Trim();
                        if (!string.IsNullOrWhiteSpace(cmd))
                        {
                            var finalCmd = characterConfig.EnableOcmd ? characterConfig.OverrideCommand.Content : cmd;
                            if (!string.IsNullOrWhiteSpace(finalCmd) && !Player.Mounted)
                                Commands.ProcessCommand(finalCmd);

                            await Task.Delay(300); // コマンド同士の間に少し間隔を入れる
                        }
                    }

                    // テキスト表示（今まで通り）
                    var selectedTextBlock = displayTexts[index];
                    var textLines = selectedTextBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in textLines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            NNekoTriggers.ToastGui.ShowQuest(trimmed);
                            await Task.Delay(TimeSpan.FromSeconds(characterConfig.GearsetDisplayDelay));
                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "ClientState_ClassJobChanged: An error occured.");
                }
            }).Start();
        }

        /// <summary>
        ///     Handles Map changes and custom command execution.
        /// </summary>
        /// <param name="obj"></param>
        private void ClientState_MapIdChanged(uint mapId)
        {
            if (!ClientState.IsLoggedIn)
                return;

            var characterConfig = Utils.GetCharacterConfig();
            if (!characterConfig.PluginEnabled || !characterConfig.EnableZones ||
                (characterConfig.EnableRpOnly && !Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)))
                return;

            PluginLog.Information("ClientState_MapIdChanged: Zone Change Triggered");

            var commands = new List<string>
    {
        characterConfig.ZoneCommand1.Content,
        characterConfig.ZoneCommand2.Content,
        characterConfig.ZoneCommand3.Content
    };

            if (commands.All(string.IsNullOrWhiteSpace))
            {
                PluginLog.Warning("Zone Triggered but no commands are set.");
                return;
            }

            int index = Random.Shared.Next(commands.Count);
            HandleZoneTriggerENF(characterConfig, index);
        }
        /// <summary>
        ///     Handles Zone changes and custom command execution.
        /// </summary>
        /// <param name="obj"></param>
        private void ClientState_ZoneInit(Dalamud.Game.ClientState.ZoneInitEventArgs obj)
        {
            if (!ClientState.IsLoggedIn)
                return;

            var characterConfig = Utils.GetCharacterConfig();
            if (!characterConfig.PluginEnabled || !characterConfig.EnableZones ||
                (characterConfig.EnableRpOnly && !Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)))
                return;

            PluginLog.Information("ClientState_ZoneInit: Zone Change Triggered");

            var commands = new List<string>
    {
        characterConfig.ZoneCommand1.Content,
        characterConfig.ZoneCommand2.Content,
        characterConfig.ZoneCommand3.Content
    };

            if (commands.All(string.IsNullOrWhiteSpace))
            {
                PluginLog.Warning("Zone Triggered but no commands are set.");
                return;
            }

            int index = Random.Shared.Next(commands.Count);
            HandleZoneTriggerENF(characterConfig, index);
        }

        /// <summary>
        ///     Handles territory changes and custom command execution.
        /// </summary>
        /// <param name="territory"></param>
        private void OnTerritoryChanged(uint territory)
        {
            if (!ClientState.IsLoggedIn)
                return;

            var characterConfig = Utils.GetCharacterConfig();
            if (!characterConfig.PluginEnabled || !characterConfig.EnableZones ||
                (characterConfig.EnableRpOnly && !Player.OnlineStatus.Equals(ROLEPLAY_ONLINE_STATUS_ID)))
                return;

            if (!AllowedTerritories.Any(t => t.RowId == territory))
            {
                PluginLog.Warning($"Territory {territory} is not an allowed territoryID, skipping.");
                return;
            }

            PluginLog.Information("OnTerritoryChanged: Zone Change Triggered");

            var commands = new List<string>
    {
        characterConfig.ZoneCommand1.Content,
        characterConfig.ZoneCommand2.Content,
        characterConfig.ZoneCommand3.Content
    };

            if (commands.All(string.IsNullOrWhiteSpace))
            {
                PluginLog.Warning("Zone Triggered but no commands are set.");
                return;
            }

            int index = Random.Shared.Next(commands.Count);
            HandleZoneTriggerENF(characterConfig, index);
        }
        /// <summary>
        ///     アイテム使用を即座に検知 + 3つのコマンドからランダムに1つ実行 + 各コマンド専用の複数行テキストを表示
        /// </summary>
        /// <summary>
        ///     アイテム使用を即座に検知 + 改行で複数のコマンド実行 + 各コマンド専用の複数行テキストを表示
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

            if (!ShouldDoENF())
                return result;

            var commands = new List<string>
    {
        characterConfig.ItemUseCommand1.Content,
        characterConfig.ItemUseCommand2.Content,
        characterConfig.ItemUseCommand3.Content
    };

            var displayTexts = new List<string>
    {
        characterConfig.ItemUseDisplayText1,
        characterConfig.ItemUseDisplayText2,
        characterConfig.ItemUseDisplayText3
    };

            if (commands.Count == 0)
            {
                PluginLog.Warning("ItemUse Triggered but no commands are set.");
                return result;
            }

            int index = Random.Shared.Next(commands.Count);
            var selectedCommandBlock = commands[index];

            PluginLog.Information($"ItemUse Triggered (Item ID: {actionId}) → Executing command block: {selectedCommandBlock}");

            // コマンド実行（改行で複数対応）
            new Task(() =>
            {
                try
                {
                    while (!Utils.CanUseGlamourPlates())
                        Task.Delay(TimeSpan.FromSeconds(1)).Wait();

                    var commandLines = selectedCommandBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in commandLines)
                    {
                        var cmd = line.Trim();
                        if (!string.IsNullOrWhiteSpace(cmd) && !Player.Mounted)
                        {
                            Commands.ProcessCommand(cmd);
                            Task.Delay(300).Wait();
                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "An error occured whilst attempting to execute item use command.");
                }
            }).Start();

            // テキスト表示（改行で複数対応）
            var selectedTextBlock = displayTexts[index];
            var textLines = selectedTextBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            new Task(() =>
            {
                foreach (var line in textLines)
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        NNekoTriggers.ToastGui.ShowQuest(trimmed);
                        Task.Delay(TimeSpan.FromSeconds(characterConfig.ItemUseDisplayDelay)).Wait();
                    }
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

        private static void HandleZoneTriggerENF(CharacterConfiguration characterConfig, CustomCommand onLoginCommand) => throw new NotImplementedException();

        /// <summary>
        ///     Processes the command used for any changes in zone, map, or territory.
        /// </summary>
        /// <param name="characterConfig"></param>
        /// <summary>
        ///     Zone Changeトリガーの共通処理（改行複数コマンド + 各コマンドごとの複数行テキスト対応）
        /// </summary>
        private static void HandleZoneTriggerENF(CharacterConfiguration characterConfig, int selectedIndex)
        {
            new Task(async () =>
            {
                if (!ShouldDoENF())
                    return;

                try
                {
                    // コマンドブロック（ランダムで選ばれたもの）
                    var zoneCommands = new List<string>
            {
                characterConfig.ZoneCommand1.Content,
                characterConfig.ZoneCommand2.Content,
                characterConfig.ZoneCommand3.Content
            };
                    var selectedCommandBlock = zoneCommands[selectedIndex];

                    // コマンドを改行で分割して順番に実行
                    var commandLines = selectedCommandBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in commandLines)
                    {
                        var cmd = line.Trim();
                        if (!string.IsNullOrWhiteSpace(cmd))
                        {
                            var finalCmd = characterConfig.EnableOcmd ? characterConfig.OverrideCommand.Content : cmd;
                            if (!string.IsNullOrWhiteSpace(finalCmd) && !Player.Mounted)
                            {
                                Commands.ProcessCommand(finalCmd);
                            }
                            await Task.Delay(300);
                        }
                    }

                    // テキストを順番に表示
                    var zoneTexts = new List<string>
            {
                characterConfig.ZoneDisplayText1,
                characterConfig.ZoneDisplayText2,
                characterConfig.ZoneDisplayText3
            };
                    var selectedTextBlock = zoneTexts[selectedIndex];
                    var textLines = selectedTextBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in textLines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            NNekoTriggers.ToastGui.ShowQuest(trimmed);
                            await Task.Delay(TimeSpan.FromSeconds(characterConfig.ZoneDisplayDelay));
                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "HandleZoneTriggerENF: An error occured.");
                }
            }).Start();
        }

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
