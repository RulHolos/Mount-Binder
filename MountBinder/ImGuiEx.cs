using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountBinder;

public static class ImGuiEx
{
    public static void SetItemTooltip(string s, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (ImGui.IsItemHovered(flags))
            ImGui.SetTooltip(s);
    }

    private static string search = string.Empty;
    private static HashSet<uint> filtered;
    public static bool MountCombo(
        string id,
        [NotNullWhen(true)] out Mount? selected,
        Func<ExcelSheet<Mount>, string> getPreview,
        ImGuiComboFlags flags,
        Func<Mount, string, bool> searchPredicate,
        Func<Mount, bool> selectableDrawing)
    {
        var sheet = DalamudApi.DataManager.GetExcelSheet<Mount>();
        return MountCombo(id, out selected, getPreview(sheet), flags, sheet, searchPredicate, selectableDrawing);
    }

    public unsafe static bool MountCombo(string id,
        [NotNullWhen(true)] out Mount? selected,
        string preview,
        ImGuiComboFlags flags,
        ExcelSheet<Mount> sheet,
        Func<Mount, string, bool> searchPredicate,
        Func<Mount, bool> drawRow)
    {
        selected = default;
        if (!ImGui.BeginCombo(id, preview, flags))
            return false;

        if (ImGui.IsWindowAppearing() && ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive())
        {
            search = string.Empty;
            filtered = null;
            ImGui.SetKeyboardFocusHere(0);
        }

        if (ImGui.InputText("##ExcelMountComboSearch", ref search, 128))
            filtered = null;

        filtered ??= [.. sheet.Where(s => searchPredicate(s, search) && PlayerState.Instance()->IsMountUnlocked(s.RowId)).Select(s => s.RowId)];

        var i = 0;
        foreach (var rowID in filtered)
        {
            if (sheet.GetRowOrDefault(rowID) is not { } row)
                continue;

            ImGui.PushID(i++);
            if (drawRow(row))
                selected = row;
            ImGui.PopID();

            if (selected == null)
                continue;
            ImGui.EndCombo();
            return true;
        }

        ImGui.EndCombo();
        return false;
    }
}
