﻿/* Umbra | (c) 2024 by Una              ____ ___        ___.
 * Licensed under the terms of AGPL-3  |    |   \ _____ \_ |__ _______ _____
 *                                     |    |   //     \ | __ \\_  __ \\__  \
 * https://github.com/una-xiv/umbra    |    |  /|  Y Y  \| \_\ \|  | \/ / __ \_
 *                                     |______//__|_|  /____  /|__|   (____  /
 *     Umbra is free software: you can redistribute  \/     \/             \/
 *     it and/or modify it under the terms of the GNU Affero General Public
 *     License as published by the Free Software Foundation, either version 3
 *     of the License, or (at your option) any later version.
 *
 *     Umbra UI is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 */

using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using Umbra.Common;
using Umbra.Game;
using Una.Drawing;

namespace Umbra.Widgets;

[ToolbarWidget("ItemButton", "Widget.ItemButton.Name", "Widget.ItemButton.Description")]
[ToolbarWidgetTags(["button", "item", "use", "inventory"])]
internal sealed partial class ItemButtonWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
)
    : DefaultToolbarWidget(info, guid, configValues)
{
    /// <inheritdoc/>
    public override WidgetPopup? Popup => null;

    private IDataManager DataManager { get; } = Framework.Service<IDataManager>();
    private IPlayer      Player      { get; } = Framework.Service<IPlayer>();

    private uint    ItemId   { get; set; }
    private uint?   IconId   { get; set; }
    private string? ItemName { get; set; }

    public override string GetInstanceName()
    {
        return ItemName is not null
            ? $"{I18N.Translate("Widget.ItemButton.Name")} - {ItemName}"
            : $"{I18N.Translate("Widget.ItemButton.Name")}";
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
        SetLeftIcon(14);
        Node.OnClick += UseItem;
    }

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        var itemId = (uint)GetConfigValue<int>("ItemId");

        if (itemId != ItemId) {
            ItemId = itemId;
            var item = Player.FindResolvedItem(itemId);

            ItemName = item?.Name;
            IconId   = item?.IconId;
        }

        Node.Style.IsVisible = !GetConfigValue<bool>("HideIfNotOwned")
            || Player.HasItemInInventory(ItemId, 1, GetItemUsage());

        bool showLabel = GetConfigValue<bool>("ShowLabel") && ItemName is not null;
        bool showCount = GetConfigValue<bool>("ShowCount");
        int  owned     = Player.GetItemCount(itemId, GetItemUsage());

        string name  = showLabel ? ItemName ?? "" : "";
        string count = showCount ? $"{owned}" : "";
        string label = showLabel && showCount ? $"{ItemName} x {owned}" : name + count;

        SetLabel(label);
        SetDisabled(!CanUseItem());
        SetIcon(IconId ?? 14);

        base.OnUpdate();
    }

    private void UseItem(Node _)
    {
        if (!CanUseItem()) return;

        Player.UseInventoryItem(ItemId, GetItemUsage());
    }

    private ItemUsage GetItemUsage()
    {
        return GetConfigValue<string>("ItemUsage") switch {
            "HqBeforeNq" => ItemUsage.HqBeforeNq,
            "NqBeforeHq" => ItemUsage.NqBeforeHq,
            "HqOnly"     => ItemUsage.HqOnly,
            "NqOnly"     => ItemUsage.NqOnly,
            _            => ItemUsage.HqBeforeNq
        };
    }

    private bool CanUseItem()
    {
        if (ItemId == 0 || ItemName is null || !Player.HasItemInInventory(ItemId)) return false;

        return !(
            Player.IsCasting
            || Player.IsOccupied
            || Player.IsBetweenAreas
            || Player.IsDead
            || Player.IsInCutscene
            || Player.IsInIdleCam
        );
    }
}
