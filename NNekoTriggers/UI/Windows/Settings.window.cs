using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.Logging;
using NNekoTriggers.Helpers;

namespace NNekoTriggers.UI.Windows
{
    public sealed class SettingsWindow : Window
    {
        /// <summary>
        ///     Constructor for the Settings Window (In-Game Config GUI).
        /// </summary>
        public SettingsWindow() : base(NNekoTriggers.PluginInterface.Manifest.Name)
        {
            this.Flags = ImGuiWindowFlags.NoResize;
            this.Size = new Vector2(525, 430);
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.AllowPinning = true;
            this.TitleBarButtons = [
                new() {
                    Icon = FontAwesomeIcon.Code,
                    Click = (mouseButton) => Util.OpenLink("https://github.com/NNekoPlugins/NNekoTriggers"),
                    ShowTooltip = () => ImGui.SetTooltip("Repository"),
                },
                new() {
                    Icon = FontAwesomeIcon.Comment,
                    Click = (mouseButton) => Util.OpenLink("https://github.com/NNekoPlugins/NNekoTriggers/issues"),
                    ShowTooltip = () => ImGui.SetTooltip("Feedback"),
                },
            ];
        }
        /// <summary>
        ///     The conditions under which the GUI can be opened.
        /// </summary>
        /// <returns>boolean</returns>
        public override bool DrawConditions() => NNekoTriggers.ClientState.IsLoggedIn;

        /// <summary>
        ///     The definition of elements in the Settings/Config Window GUI.
        /// </summary>
        public override void Draw()
        {
            var config = Utils.GetCharacterConfig();
            //var mgr = NNekoTriggers.WindowManager;
            // Top-level config options.
            if (ImGui.Checkbox($"Enable {NNekoTriggers.PluginInterface.Manifest.Name}", ref config.PluginEnabled))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(!config.PluginEnabled);
            if (ImGui.Checkbox("Show in Server Info Bar", ref config.ShowInDtr))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.BeginDisabled(!config.ShowInDtr);
            if (ImGui.Checkbox("RP Only", ref config.RpOnlyInDtr))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("RNG", ref config.RngInDtr))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("Zone Change", ref config.ZoneInDtr))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("Job Swap", ref config.GsetInDtr))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("Login", ref config.OnLoginInDtr))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("Override", ref config.OcmdInDtr))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.EndDisabled();

            if (ImGui.Checkbox("Only enable when roleplaying", ref config.EnableRpOnly))
            {
                NNekoTriggers.PluginConfiguration.Save();
                PluginLog.Information($"NNekoTriggers RP Only Module {(config.EnableRpOnly ? "Enabled" : "Disabled")}");
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }

            if (ImGui.Checkbox("Enable RNG feature", ref config.EnableRNG))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }

            ImGui.BeginDisabled(!config.EnableRNG);
            if (ImGui.BeginTable("##OddsTable", 2))
            {
                ImGui.TableSetupColumn("Min", ImGuiTableColumnFlags.WidthFixed, 250);
                ImGui.TableSetupColumn("Max", ImGuiTableColumnFlags.WidthFixed, 250);
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (ImGui.InputInt("Min", ref config.OddsMin, 1, 25))
                {
                    NNekoTriggers.PluginConfiguration.Save();
                    NNekoTriggers.WindowManager.UpdateDtrEntry();
                }
                ImGui.TableSetColumnIndex(1);
                if (ImGui.InputInt("Max", ref config.OddsMax, 1, 25))
                {
                    NNekoTriggers.PluginConfiguration.Save();
                    NNekoTriggers.WindowManager.UpdateDtrEntry();
                }
                ImGui.EndTable();
                ImGui.EndDisabled();
            }
            ImGui.EndDisabled();

            if (ImGui.Checkbox("Enable Gearset Swap feature", ref config.EnableGset))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }

            ImGui.BeginDisabled(!config.EnableGset);
            ImGui.Separator();
            ImGui.Text("Gearset Swap Commands (ランダムで1つ実行されます)");

            // Command 1
            string gcmd1 = config.GearsetCommand1.Content ?? string.Empty;
            if (ImGui.InputTextWithHint("##GearsetCmd1", "/echo ギアセット変更！ など", ref gcmd1, 256))
            {
                config.GearsetCommand1.Content = gcmd1;
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            string gtxt1 = config.GearsetDisplayText1 ?? string.Empty;
            if (ImGui.InputTextWithHint("##GearsetTxt1", "表示するテキスト1 (空欄=非表示)", ref gtxt1, 256))
            {
                config.GearsetDisplayText1 = gtxt1;
                NNekoTriggers.PluginConfiguration.Save();
            }

            // Command 2
            string gcmd2 = config.GearsetCommand2.Content ?? string.Empty;
            if (ImGui.InputTextWithHint("##GearsetCmd2", "/echo 2番目のコマンド", ref gcmd2, 256))
            {
                config.GearsetCommand2.Content = gcmd2;
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            string gtxt2 = config.GearsetDisplayText2 ?? string.Empty;
            if (ImGui.InputTextWithHint("##GearsetTxt2", "表示するテキスト2 (空欄=非表示)", ref gtxt2, 256))
            {
                config.GearsetDisplayText2 = gtxt2;
                NNekoTriggers.PluginConfiguration.Save();
            }

            // Command 3
            string gcmd3 = config.GearsetCommand3.Content ?? string.Empty;
            if (ImGui.InputTextWithHint("##GearsetCmd3", "/echo 3番目のコマンド", ref gcmd3, 256))
            {
                config.GearsetCommand3.Content = gcmd3;
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            string gtxt3 = config.GearsetDisplayText3 ?? string.Empty;
            if (ImGui.InputTextWithHint("##GearsetTxt3", "表示するテキスト3 (空欄=非表示)", ref gtxt3, 256))
            {
                config.GearsetDisplayText3 = gtxt3;
                NNekoTriggers.PluginConfiguration.Save();
            }

            ImGui.Text("※ 空欄のコマンドは無視されます");

            ImGui.Separator();
            ImGui.EndDisabled();

#pragma warning restore CS8601 // Possible null reference assignment.

            ImGui.EndDisabled();
            if (ImGui.Checkbox("Enable Zone feature", ref config.EnableZones))
            {
                NNekoTriggers.PluginConfiguration.Save();
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.BeginDisabled(!config.EnableZones);
            var territoryCmd = config.ZoneCommand;
            var tcmdslot = config.ZoneCommand.Content;
#pragma warning disable CS8601 // Possible null reference assignment.
            if (ImGui.InputTextWithHint("Zone Command", "/command here...", ref tcmdslot, 100))
            {
                unsafe
                {
                    territoryCmd.Content = tcmdslot;
                    config.ZoneCommand = territoryCmd;
                    NNekoTriggers.PluginConfiguration.Save();
                    NNekoTriggers.WindowManager.UpdateDtrEntry();
                }
            }
#pragma warning restore CS8601 // Possible null reference assignment.
            ImGui.EndDisabled();

            if (ImGui.Checkbox("Enable Login feature", ref config.EnableOnLogin))
            {
                NNekoTriggers.PluginConfiguration.Save();
                PluginLog.Information($"NNekoTriggers Login Module {(config.EnableOnLogin ? "Enabled" : "Disabled")}");
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.BeginDisabled(!config.EnableOnLogin);
            var onLoginCmd = config.OnLoginCommand;
            var logincmdslot = config.OnLoginCommand.Content;
#pragma warning disable CS8601 // Possible null reference assignment.
            if (ImGui.InputTextWithHint("Login Command", "/command here...", ref logincmdslot, 100))
            {
                unsafe
                {
                    onLoginCmd.Content = logincmdslot;
                    config.OnLoginCommand = onLoginCmd;
                    NNekoTriggers.PluginConfiguration.Save();
                }
            }
#pragma warning restore CS8601 // Possible null reference assignment.
            ImGui.EndDisabled();

            if (ImGui.Checkbox("Enable Command Override feature", ref config.EnableOcmd))
            {
                NNekoTriggers.PluginConfiguration.Save();
                PluginLog.Information($"NNekoTriggers Override Module {(config.EnableOcmd ? "Enabled" : "Disabled")}");
                NNekoTriggers.WindowManager.UpdateDtrEntry();
            }
            ImGui.BeginDisabled(!config.EnableOcmd);
            var defaultCmd = config.OverrideCommand;
            var dcmdslot = config.OverrideCommand.Content;
#pragma warning disable CS8601 // Possible null reference assignment.
            if (ImGui.InputTextWithHint("Override Command", "/command here...", ref dcmdslot, 100))
            {
                unsafe
                {
                    defaultCmd.Content = dcmdslot;
                    config.OverrideCommand = defaultCmd;
                    NNekoTriggers.PluginConfiguration.Save();
                }
            }
#pragma warning restore CS8601 // Possible null reference assignment.
            ImGui.EndDisabled();

            // ==================== Item Use (3コマンド・ランダム実行) ====================
            if (ImGui.Checkbox("Enable Item Use feature", ref config.EnableItemUse))
            {
                NNekoTriggers.PluginConfiguration.Save();
                PluginLog.Information($"NNekoTriggers Item-Use Module {(config.EnableItemUse ? "Enabled" : "Disabled")}");
            }

            ImGui.BeginDisabled(!config.EnableItemUse);
            ImGui.Separator();
            ImGui.Text("Item Use Commands (ランダムで1つ実行されます)");

            // Command 1
            string cmd1 = config.ItemUseCommand1.Content ?? string.Empty;
            if (ImGui.InputTextWithHint("##ItemUseCmd1", "/echo アイテム使った！ など", ref cmd1, 256))
            {
                config.ItemUseCommand1.Content = cmd1;
                NNekoTriggers.PluginConfiguration.Save();
            }
            string txt1 = config.ItemUseDisplayText1 ?? string.Empty;
            if (ImGui.InputTextWithHint("##ItemUseTxt1", "表示するテキスト1 (空欄=非表示)", ref txt1, 256))
            {
                config.ItemUseDisplayText1 = txt1;
                NNekoTriggers.PluginConfiguration.Save();
            }

            // Command 2
            string cmd2 = config.ItemUseCommand2.Content ?? string.Empty;
            if (ImGui.InputTextWithHint("##ItemUseCmd2", "/echo 2番目のコマンド", ref cmd2, 256))
            {
                config.ItemUseCommand2.Content = cmd2;
                NNekoTriggers.PluginConfiguration.Save();
            }
            string txt2 = config.ItemUseDisplayText2 ?? string.Empty;
            if (ImGui.InputTextWithHint("##ItemUseTxt2", "表示するテキスト2 (空欄=非表示)", ref txt2, 256))
            {
                config.ItemUseDisplayText2 = txt2;
                NNekoTriggers.PluginConfiguration.Save();
            }

            // Command 3
            string cmd3 = config.ItemUseCommand3.Content ?? string.Empty;
            if (ImGui.InputTextWithHint("##ItemUseCmd3", "/echo 3番目のコマンド", ref cmd3, 256))
            {
                config.ItemUseCommand3.Content = cmd3;
                NNekoTriggers.PluginConfiguration.Save();
            }
            string txt3 = config.ItemUseDisplayText3 ?? string.Empty;
            if (ImGui.InputTextWithHint("##ItemUseTxt3", "表示するテキスト3 (空欄=非表示)", ref txt3, 256))
            {
                config.ItemUseDisplayText3 = txt3;
                NNekoTriggers.PluginConfiguration.Save();
            }

            ImGui.Text("※ 空欄のコマンドは無視されます");
            ImGui.Separator();
            ImGui.EndDisabled();
            // ============================================================================
        }
    }
}
