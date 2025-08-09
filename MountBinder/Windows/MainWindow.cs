using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.UI.UIInputData;

namespace MountBinder.Windows;

public class MainWindow : Window, IDisposable
{
    private bool wantsToSetupKeys = false;
    private int wantsToSetupKeysIndex = 0;
    private List<VirtualKey> currentCapture = [];

    public MainWindow()
        : base("Mount Binder Configuration", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = ImGuiHelpers.MainViewport.Size
        };
    }

    public void Reload() => Dispose();

    public unsafe override void Draw()
    {
        if (ImGui.Button("+ New Mount Bind"))
        {
            var unlockedMounts = new List<Mount>();
            foreach (var row in DalamudApi.DataManager.GetExcelSheet<Mount>())
            {
                if (PlayerState.Instance()->IsMountUnlocked(row.RowId))
                {
                    MountBinder.Configuration.Binds.Add(new MountBinding() { MountId = row.RowId });
                    MountBinder.Configuration.Save();
                    break;
                }
            }
        }

        for (int i = 0; i < MountBinder.Configuration.Binds.Count; i++)
        {
            MountBinding cfg = MountBinder.Configuration.Binds[i];

            static string formatName(Mount t) => $"{t.Singular}";

            ImGui.Separator();

            if (ImGuiEx.MountCombo($"##MountChooser{i}", out var status, s => formatName(s.GetRow(cfg.MountId)),
            ImGuiComboFlags.None, (t, s) => formatName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase),
            t => ImGui.Selectable(formatName(t), cfg.MountId == t.RowId)))
            {
                cfg.MountId = status.Value.RowId;
                MountBinder.Configuration.Save();
            }

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGui.GetIO().KeyShift ? ImGuiCol.Text : ImGuiCol.TextDisabled));
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyShift)
            {
                MountBinder.Configuration.Binds.RemoveAt(i);
                MountBinder.Configuration.Save();
            }
            ImGui.PopStyleColor();
            ImGuiEx.SetItemTooltip("Hold SHIFT to delete this mount bind.");

            if (ImGui.Button("Set"))
            {
                wantsToSetupKeys = true;
                wantsToSetupKeysIndex = i;
            }
            ImGui.SameLine();

            if (wantsToSetupKeys && wantsToSetupKeysIndex == i)
            {
                ImGui.Text(string.Join("+", currentCapture));

                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
                ImGui.TextWrapped("Press ESC to cancel. Press BACKSPACE to finish.");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.Text(string.Join("+", cfg.Keys));
            }
        }
    }

    /// <summary>
    /// Called each frame by framework.
    /// </summary>
    public void CheckSetupKeys()
    {
        if (!wantsToSetupKeys)
            return;

        var keysPressed = DalamudApi.KeyState.GetValidVirtualKeys()
            .Where(k => k != VirtualKey.NO_KEY && DalamudApi.KeyState[k])
            .ToList();

        if (keysPressed.Contains(VirtualKey.ESCAPE))
        {
            foreach (var key in keysPressed)
                DalamudApi.KeyState[key] = false;

            wantsToSetupKeys = false;
            return;
        }

        if (keysPressed.Contains(VirtualKey.BACK))
        {
            foreach (var key in keysPressed)
                DalamudApi.KeyState[key] = false;

            MountBinder.Configuration.Binds[wantsToSetupKeysIndex].Keys = [.. currentCapture];
            MountBinder.Configuration.Save();
            wantsToSetupKeys = false;
            return;
        }

        if (keysPressed.Count > 0)
            if (currentCapture.Count == 0 || keysPressed.Count > currentCapture.Count)
                currentCapture = [.. keysPressed];

        //MountBinder.Configuration.Binds[wantsToSetupKeysIndex].Keys = [.. currentCapture];
    }

    public void Dispose()
    {
        return;
    }
}
