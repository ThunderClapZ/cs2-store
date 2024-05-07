using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Menu;
using System.Globalization;
using System.Text;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Menu
{
    public static void AddMenuOption(CCSPlayerController player, CenterHtmlMenu menu, Action<CCSPlayerController, ChatMenuOption> onSelect, string display, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer[display, args]);

            menu.AddMenuOption(builder.ToString(), onSelect);
        }
    }

    public static void DisplayStore(CCSPlayerController player, bool inventory)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer["menu_store<title>", Credits.Get(player)]);

            CenterHtmlMenu menu = new(builder.ToString(), Instance);

            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> category in Instance.Config.Items)
            {
                if ((inventory || Item.IsPlayerVip(player)) && !category.Value.Values.Any(item => Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
                {
                    continue;
                }

                StringBuilder builderkey = new();
                builderkey.AppendFormat(Instance.Localizer[$"menu_store<{category.Key}>"]);

                menu.AddMenuOption(builderkey.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                {
                    DisplayItems(player, builderkey.ToString(), category.Value, inventory);
                });
            }

            MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
        }
    }

    public static void DisplayItems(CCSPlayerController player, string key, Dictionary<string, Dictionary<string, string>> items, bool inventory)
    {
        DisplayItem(player, inventory, key, items);
    }

    public static void DisplayItem(CCSPlayerController player, bool inventory, string key, Dictionary<string, Dictionary<string, string>> items)
    {
        CenterHtmlMenu menu = new(key, Instance);

        foreach (KeyValuePair<string, Dictionary<string, string>> kvp in items)
        {
            Dictionary<string, string> item = kvp.Value;

            if (item["enable"] != "true")
            {
                continue;
            }

            if ((inventory || Item.IsPlayerVip(player)) && !Item.PlayerHas(player, item["type"], item["uniqueid"], false))
            {
                continue;
            }

            if (Item.PlayerHas(player, item["type"], item["uniqueid"], false))
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    DisplayItemOption(player, item);
                }, item["name"]);
            }
            else if (!inventory)
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    if (Item.Purchase(player, item))
                    {
                        DisplayItemOption(player, item);
                    }
                    else
                    {
                        MenuManager.CloseActiveMenu(player);
                    }
                }, "menu_store<purchase>", item["name"], item["price"]);
            }
        }

        MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(item["name"], Instance);

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                Item.Unequip(player, item);

                player.PrintToChatMessage("Purchase Unequip", item["name"]);

                DisplayItemOption(player, item);
            }, "menu_store<unequip>");
        }
        else
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                Item.Equip(player, item);

                player.PrintToChatMessage("Purchase Equip", item["name"]);

                DisplayItemOption(player, item);
            }, "menu_store<equip>");
        }

        if (Instance.Config.Menu["enable_selling"] == "1" && !Item.IsPlayerVip(player))
        {
            float sell_ratio = 1.0f;

            if (Instance.Config.Settings.TryGetValue("sell_ratio", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float ratio))
            {
                sell_ratio = ratio;
            }

            AddMenuOption(player, menu, (player, option) =>
            {
                Item.Sell(player, item);

                player.PrintToChatMessage("Item Sell", item["name"]);

                MenuManager.CloseActiveMenu(player);
            }, "menu_store<sell>", (int)(int.Parse(item["price"]) * sell_ratio));
        }

        Store_Item? playeritem = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item["type"] && p.UniqueId == item["uniqueid"]);

        if (playeritem != null)
        {
            if (playeritem.DateOfExpiration > DateTime.MinValue)
            {
                menu.AddMenuOption(playeritem.DateOfExpiration.ToString(), (p, o) => { }, true);
            }
        }

        MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
    }
}
