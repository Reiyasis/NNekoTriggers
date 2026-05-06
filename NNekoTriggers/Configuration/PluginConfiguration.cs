using Dalamud.Configuration;

namespace NNekoTriggers.Configuration
{
    internal sealed class PluginConfiguration : IPluginConfiguration
    {
        public int Version { get; set; }
        public Dictionary<ulong, CharacterConfiguration> CharacterConfigurations = [];

        public void Save() => NNekoTriggers.PluginInterface.SavePluginConfig(this);
        public static PluginConfiguration Load() => NNekoTriggers.PluginInterface.GetPluginConfig() as PluginConfiguration ?? new();
    }

    internal sealed class CharacterConfiguration
    {
        public int Version { get; set; }
        public bool PluginEnabled = true;
        public bool EnableItemUse;
        public bool ShowInDtr = true;
        public bool RpOnlyInDtr;
        public bool OcmdInDtr;
        public bool RngInDtr;
        public bool ZoneInDtr;
        public bool GsetInDtr;
        public bool OnLoginInDtr;
        public bool EnableRpOnly;
        public bool EnableOcmd;
        public bool EnableRNG;
        public bool EnableZones;
        public bool EnableGset;
        public bool EnableOnLogin;
        public int OddsMax = 100;　
        public int OddsMin = 25;
        public CustomCommand OnLoginCommand = new();
        public CustomCommand OverrideCommand = new();
        public CustomCommand GearsetCommand1 = new();
        public CustomCommand GearsetCommand2 = new();
        public CustomCommand GearsetCommand3 = new();
        public CustomCommand ZoneCommand = new();
        public CustomCommand ItemUseCommand1 = new();
        public CustomCommand ItemUseCommand2 = new();
        public CustomCommand ItemUseCommand3 = new();
    }

    internal sealed class CustomCommand
    {
        public string? Content;
        public bool Enabled = true;
    }
}
