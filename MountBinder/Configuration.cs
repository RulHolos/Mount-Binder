using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace MountBinder;

public class MountBinding
{
    [JsonProperty("mnt"), DefaultValue(0)]
    public uint MountId; // Id of the mount row in Lumina.
    [JsonProperty("keys"), DefaultValue(VirtualKey.NO_KEY)]
    public VirtualKey[] Keys = [VirtualKey.NO_KEY];
    [JsonIgnore]
    public bool WasPressed { get; set; } = false;
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string PluginVersion { get; private set; } = ".init";
    public string GetVersion() => PluginVersion;
    public void UpdateVersion()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        PluginVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        Save();
    }
    public bool CheckVersion() => PluginVersion == Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    public List<MountBinding> Binds = [];

    public void Initialize()
    {
        
    }

    public void Save(bool failed = false)
    {
        try
        {
            DalamudApi.PluginInterface.SavePluginConfig(this);
        }
        catch (Exception ex)
        {
            if (!failed)
            {
                DalamudApi.PluginLog.Error("Failed to save. Trying again.");
                Save(true);
            }
            else
            {
                NotificationManager.Display("Failed to save. Please see /xllog for details.",
                    Dalamud.Interface.ImGuiNotification.NotificationType.Error);
                DalamudApi.PluginLog.Error(ex.ToString());
            }
        }
    }
}
