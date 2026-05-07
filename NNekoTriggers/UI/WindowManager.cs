using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.Logging;
using NNekoTriggers.Helpers;
using NNekoTriggers.UI.Windows;

namespace NNekoTriggers.UI
{
    internal sealed class WindowManager : IDisposable
    {
        private bool disposedValue;
        private readonly IFramework Framework;
        private readonly IDtrBar DtrBar;

        private bool dtrHooked;
        private int ticksWaited;
        private const int MaxTicks = 600; // ~10 seconds

        /// <summary>
        ///     All windows to add to the windowing system, holds all references.
        /// </summary>
        private readonly Window[] windows = [new SettingsWindow()];

        /// <summary>
        ///     The windowing system.
        /// </summary>
        private readonly WindowSystem windowingSystem;

        public IDtrBarEntry RpOnlyEntry { get; } = Svc.DtrBar.Get("TTrig-RpOnly");
        public IDtrBarEntry RngEntry { get; } = Svc.DtrBar.Get("TTrig-RNG");
        public IDtrBarEntry ZoneEntry { get; } = Svc.DtrBar.Get("TTrig-Zone");
        public IDtrBarEntry GearsetEntry { get; } = Svc.DtrBar.Get("TTrig-Gearset");
        public IDtrBarEntry OverrideEntry { get; } = Svc.DtrBar.Get("TTrig-Override");
        public IDtrBarEntry OnLoginEntry { get; } = Svc.DtrBar.Get("TTrig-OnLogin");

        /// <summary>
        ///     Initializes a new instance of the <see cref="WindowManager" /> class.
        /// </summary>
        public WindowManager(IFramework framework, IDtrBar dtrBar)
        {
            this.windowingSystem = new WindowSystem(NNekoTriggers.PluginInterface.Manifest.InternalName);
            foreach (var window in this.windows)
            {
                this.windowingSystem.AddWindow(window);
            }
            NNekoTriggers.PluginInterface.UiBuilder.Draw += this.windowingSystem.Draw;
            NNekoTriggers.ClientState.Login += this.OnLogin;
            NNekoTriggers.ClientState.Logout += this.OnLogout;
            NNekoTriggers.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigWindow;
            NNekoTriggers.PluginInterface.UiBuilder.OpenMainUi += this.ToggleConfigWindow;

            this.Framework = framework;
            this.Framework.Update += this.OnFrameworkUpdate;
            if (NNekoTriggers.PluginInterface.UiBuilder.UiPrepared)
            {
                this.DtrBar = dtrBar;
                this.RpOnlyEntry.Shown = false;
                this.RngEntry.Shown = false;
                this.ZoneEntry.Shown = false;
                this.GearsetEntry.Shown = false;
                this.OverrideEntry.Shown = false;
                this.OnLoginEntry.Shown = false;
            }
        }

        /// <summary>
        ///     Disposes of the window manager.
        /// </summary>
        public void Dispose()
        {
            if (this.disposedValue)
            {
                ObjectDisposedException.ThrowIf(this.disposedValue, nameof(this.windowingSystem));
                return;
            }
            this.Framework.Update -= this.OnFrameworkUpdate;
            this.RpOnlyEntry.Remove();
            this.RngEntry.Remove();
            this.ZoneEntry.Remove();
            this.GearsetEntry.Remove();
            this.OverrideEntry.Remove();
            this.OnLoginEntry.Remove();
            NNekoTriggers.ClientState.Login -= this.OnLogin;
            NNekoTriggers.ClientState.Logout -= this.OnLogout;
            NNekoTriggers.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigWindow;
            NNekoTriggers.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleConfigWindow;
            NNekoTriggers.PluginInterface.UiBuilder.Draw -= this.windowingSystem.Draw;
            this.windowingSystem.RemoveAllWindows();
            foreach (var disposable in this.windows.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
            this.disposedValue = true;
        }


        private void OnFrameworkUpdate(IFramework _)
        {
            if (this.dtrHooked || this.ticksWaited++ > MaxTicks)
            {
                this.Framework.Update -= this.OnFrameworkUpdate;
                return;
            }

            // Ensure the DTR bar is ready
            if (this.DtrBar.Entries.Count == 0)
            {
                return;
            }

            // Initialize your own entries safely
            this.InitializeDtrEntries();

            this.dtrHooked = true;
            this.Framework.Update -= this.OnFrameworkUpdate;

            // Now that entries exist, update their text/icons
            this.UpdateDtrEntry();
        }

        private void InitializeDtrEntries()
        {
            if (this.dtrHooked)
            {
                return;
            }

            var config = Utils.GetCharacterConfig();
            if (config == null)
            {
                return;
            }

            // Attach click handlers ONLY here
            this.RpOnlyEntry.OnClick = ev =>
            {
                if (ev.ClickType == MouseClickType.Right)
                {
                    this.ToggleConfigWindow();
                }
                else
                {
                    config.EnableRpOnly = !config.EnableRpOnly;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers Roleplay Only Module {(config.EnableRpOnly ? "Enabled" : "Disabled")}");
                    this.UpdateDtrEntry();

                }
            };
            this.RngEntry.OnClick = ev =>
            {
                if (ev.ClickType == MouseClickType.Right)
                {
                    this.ToggleConfigWindow();
                }
                else
                {
                    config.EnableRNG = !config.EnableRNG;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers RNG Module {(config.EnableRNG ? "Enabled" : "Disabled")}");
                    this.UpdateDtrEntry();

                }
            };
            this.ZoneEntry.OnClick = ev =>
            {
                if (ev.ClickType == MouseClickType.Right)
                {
                    this.ToggleConfigWindow();
                }
                else
                {
                    config.EnableZones = !config.EnableZones;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers Territory Module {(config.EnableZones ? "Enabled" : "Disabled")}");
                    this.UpdateDtrEntry();

                }
            };
            this.GearsetEntry.OnClick = ev =>
            {
                if (ev.ClickType == MouseClickType.Right)
                {
                    this.ToggleConfigWindow();
                }
                else
                {
                    config.EnableGset = !config.EnableGset;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers Gearset Module {(config.EnableGset ? "Enabled" : "Disabled")}");
                    this.UpdateDtrEntry();

                }
            };
            this.OverrideEntry.OnClick = ev =>
            {
                if (ev.ClickType == MouseClickType.Right)
                {
                    this.ToggleConfigWindow();
                }
                else
                {
                    config.EnableOcmd = !config.EnableOcmd;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers Command Override Module {(config.EnableOcmd ? "Enabled" : "Disabled")}");
                    this.UpdateDtrEntry();

                }
            };
            this.OnLoginEntry.OnClick = ev =>
            {
                if (ev.ClickType == MouseClickType.Right)
                {
                    this.ToggleConfigWindow();
                }
                else
                {
                    config.EnableOnLogin = !config.EnableOnLogin;
                    NNekoTriggers.PluginConfiguration.Save();
                    PluginLog.Information($"NNekoTriggers Login Module {(config.EnableOnLogin ? "Enabled" : "Disabled")}");
                    this.UpdateDtrEntry();

                }
            };
        }


        /// <summary>
        ///     Toggles the open state of the configuration window.
        /// </summary>
        public void ToggleConfigWindow()
        {
            if (NNekoTriggers.ClientState.IsLoggedIn)
            {
                ObjectDisposedException.ThrowIf(this.disposedValue, nameof(this.windowingSystem));
                this.windows.FirstOrDefault(window => window is SettingsWindow)?.Toggle();
            }
        }

        /// <summary>
        ///     Handles the login event
        /// </summary>
        private void OnLogin()
        {
            var config = Utils.GetCharacterConfig();
            NNekoTriggers.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigWindow;
            NNekoTriggers.PluginInterface.UiBuilder.OpenMainUi += this.ToggleConfigWindow;
            if (this.dtrHooked)
            {
                this.UpdateDtrEntry();
            }
        }

        /// <summary>
        ///     Handles the logout event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        private void OnLogout(int type, int code)
        {
            NNekoTriggers.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigWindow;
            NNekoTriggers.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleConfigWindow;
        }

        /// <summary>
        /// Updates the plugin DTR element (Server Info Text)
        /// </summary>
        public void UpdateDtrEntry()
        {
            var config = Utils.GetCharacterConfig();
            if (config is null)
            {
                return;
            }
            if (!this.dtrHooked)
            {
                return;
            }

            //this.DtrBar = dtrBar;
            this.RpOnlyEntry.Shown = false;
            this.RngEntry.Shown = false;
            this.ZoneEntry.Shown = false;
            this.GearsetEntry.Shown = false;
            this.OverrideEntry.Shown = false;
            this.OnLoginEntry.Shown = false;

            if (config.PluginEnabled && config.ShowInDtr)
            {
                if (config.RpOnlyInDtr)
                {
                    this.RpOnlyEntry.Text = new SeString(new IconPayload(BitmapFontIcon.RolePlaying), config.EnableRpOnly ? new IconPayload(BitmapFontIcon.GreenDot) : new IconPayload(BitmapFontIcon.NoCircle));
                    this.RpOnlyEntry.Tooltip = new SeString(new TextPayload("Left Click to toggle Roleplay Only Mode."), new NewLinePayload(), new TextPayload("(Right Click opens main config)"));
                    this.RpOnlyEntry.Shown = true;
                }
                else
                {
                    this.RpOnlyEntry.Shown = false;
                }
                if (config.RngInDtr)
                {
                    this.RngEntry.Text = new SeString(new IconPayload(BitmapFontIcon.Dice), config.EnableRNG ? new IconPayload(BitmapFontIcon.GreenDot) : new IconPayload(BitmapFontIcon.NoCircle), config.EnableRNG ? new TextPayload($"{config.OddsMin}/{config.OddsMax}") : new TextPayload("??/??"));
                    this.RngEntry.Tooltip = new SeString(new TextPayload("Left Click to toggle RNG Mode."), new NewLinePayload(), new TextPayload("(Right Click opens main config)"));
                    this.RngEntry.Shown = true;
                }
                else
                {
                    this.RngEntry.Shown = false;
                }
                if (config.ZoneInDtr)
                {
                    this.ZoneEntry.Text = new SeString(new IconPayload(BitmapFontIcon.Aetheryte), config.EnableZones ? new IconPayload(BitmapFontIcon.GreenDot) : new IconPayload(BitmapFontIcon.NoCircle));
                    this.ZoneEntry.Tooltip = new SeString(new TextPayload("Left Click to toggle Zone Change Mode."), new NewLinePayload(), new TextPayload("(Right Click opens main config)"));
                    this.ZoneEntry.Shown = true;
                }
                else
                {
                    this.ZoneEntry.Shown = false;
                }
                if (config.GsetInDtr)
                {
                    this.GearsetEntry.Text = new SeString(new IconPayload(BitmapFontIcon.SwordSheathed), config.EnableGset ? new IconPayload(BitmapFontIcon.GreenDot) : new IconPayload(BitmapFontIcon.NoCircle));
                    this.GearsetEntry.Tooltip = new SeString(new TextPayload("Left Click to toggle Job Swap Mode."), new NewLinePayload(), new TextPayload("(Right Click opens main config)"));
                    this.GearsetEntry.Shown = true;
                }
                else
                {
                    this.GearsetEntry.Shown = false;
                }
                if (config.OnLoginInDtr)
                {
                    this.OnLoginEntry.Text = new SeString(new IconPayload(BitmapFontIcon.Meteor), config.EnableOnLogin ? new IconPayload(BitmapFontIcon.GreenDot) : new IconPayload(BitmapFontIcon.NoCircle));
                    this.OnLoginEntry.Tooltip = new SeString(new TextPayload("Left Click to toggle Login Mode."), new NewLinePayload(), new TextPayload("(Right Click opens main config)"));
                    this.OnLoginEntry.Shown = true;
                }
                else
                {
                    this.OnLoginEntry.Shown = false;
                }
                if (config.OcmdInDtr)
                {
                    this.OverrideEntry.Text = new SeString(new IconPayload(BitmapFontIcon.Mentor), config.EnableOcmd ? new IconPayload(BitmapFontIcon.GreenDot) : new IconPayload(BitmapFontIcon.NoCircle));
                    this.OverrideEntry.Tooltip = new SeString(new TextPayload("Left Click to toggle Command Override Mode."), new NewLinePayload(), new TextPayload("(Right Click opens main config)"));
                    this.OverrideEntry.Shown = true;
                }
                else
                {
                    this.OverrideEntry.Shown = false;
                }
            }
            else
            {
                this.RpOnlyEntry.Shown = false;
                this.RngEntry.Shown = false;
                this.ZoneEntry.Shown = false;
                this.GearsetEntry.Shown = false;
                this.OnLoginEntry.Shown = false;
                this.OverrideEntry.Shown = false;
            }
        }
    }
}
