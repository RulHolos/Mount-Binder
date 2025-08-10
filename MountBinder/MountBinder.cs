using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using MountBinder.Windows;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;
using static FFXIVClientStructs.FFXIV.Client.UI.UIInputData;

namespace MountBinder;

public class MountBinder : IDalamudPlugin
{
    public static MountBinder Plugin { get; private set; }
    public static Configuration Configuration { get; private set; }

    public MainWindow UI;
    private bool isPluginReady = false;

    public readonly WindowSystem WindowSystem = new("MountBinder");

    public MountBinder(IDalamudPluginInterface pluginInterface)
    {
        Plugin = this;
        DalamudApi.Initialize(this, pluginInterface);

        Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize();
        Configuration.UpdateVersion();

        ReadyPlugin();
    }

    private void Update(IFramework framework)
    {
        if (!isPluginReady)
            return;

        UI.CheckSetupKeys();
        CheckForAllBinds();
    }

    public void ReadyPlugin()
    {
        try
        {
            DalamudApi.Framework.Update += Update;

            UI = new();

            WindowSystem.AddWindow(UI);

            DalamudApi.PluginInterface.UiBuilder.Draw += Draw;
            DalamudApi.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

            isPluginReady = true;
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error($"Failed to load MountBinder.\n{e}");
            isPluginReady = false;
        }
    }

    public void Reload()
    {
        Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new();
        Configuration.Initialize();
        Configuration.UpdateVersion();
        Configuration.Save();
    }

    private void Draw()
    {
        if (!isPluginReady)
            return;

        WindowSystem.Draw();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        Configuration.Save();

        DalamudApi.Framework.Update -= Update;
        DalamudApi.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        DalamudApi.PluginInterface.UiBuilder.Draw -= Draw;
        DalamudApi.Dispose();

        UI.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void ToggleMainUI() => UI.Toggle();

    [Command("/mntbnd")]
    [HelpMessage("Displays the main plugin window.")]
    public void MntBind(string command, string arguments)
    {
        ToggleMainUI();
    }

    public static unsafe void ExecuteHotbarAction(HotbarSlotType commandType, uint commandId)
    {
        var hotbarModulePtr = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule();

        var slot = new HotbarSlot
        {
            CommandType = commandType,
            CommandId = commandId
        };

        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(slot));
        Marshal.StructureToPtr(slot, ptr, false);

        hotbarModulePtr->ExecuteSlot((HotbarSlot*)ptr);

        Marshal.FreeHGlobal(ptr);
    }

    /// <summary>
    /// Does the actual mount binding checks and mounting...mounts.
    /// </summary>
    public unsafe void CheckForAllBinds()
    {
        for (int i = 0; i < Configuration.Binds.Count; i++)
        {
            try
            {
                MountBinding cfg = Configuration.Binds[i];

                // Ignore NO_KEY since it ALMOST crashes Dalamud.
                if (cfg.Keys == null || cfg.Keys.Length == 0 || cfg.Keys.Contains(VirtualKey.NO_KEY))
                    continue;

                bool allKeybindsPressed = cfg.Keys.All(key => DalamudApi.KeyState[key]);

                if (!allKeybindsPressed)
                {
                    cfg.WasPressed = false;
                    continue;
                }

                int numKeysPressed = DalamudApi.KeyState.GetValidVirtualKeys()
                    .Count(key => DalamudApi.KeyState[key]);
                if (numKeysPressed != cfg.Keys.Length)
                    continue;

                if (!cfg.WasPressed)
                {
                    cfg.WasPressed = true;

                    DalamudApi.Framework.RunOnFrameworkThread(() =>
                    {
                        ActionManager.Instance()->UseAction(ActionType.Mount, cfg.MountId);
                    });
                }
            }
            catch (Exception e)
            {
                DalamudApi.PluginLog.Error($"Failed to execute mount bind for index {i}.\n{e}");
            }
        }
    }
}
