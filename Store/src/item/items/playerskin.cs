using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_PlayerSkin
{
    public static void OnPluginStart()
    {
        Item.RegisterType("playerskin", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("playerskin");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["uniqueid"]);

            if (item.Value.TryGetValue("armModel", out string? armModel) && !string.IsNullOrEmpty(armModel))
            {
                manifest.AddResource(armModel);
            }
        }
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        SetPlayerModel(player, item["uniqueid"], item["disable_leg"]);

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        SetDefaultModel(player);

        return true;
    }
    public static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        if (player.TeamNum < 2)
        {
            return HookResult.Continue;
        }

        Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin");

        if (item == null)
        {
            SetDefaultModel(player);
        }
        else
        {
            Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

            if (itemdata == null)
            {
                return HookResult.Continue;
            }

            SetPlayerModel(player, item.UniqueId, itemdata["disable_leg"]);
        }

        return HookResult.Continue;
    }
    public static void SetDefaultModel(CCSPlayerController player)
    {
        string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Instance.Config.DefaultModels["ct"] : Instance.Config.DefaultModels["t"];
        int maxIndex = modelsArray.Length;

        if (maxIndex > 0)
        {
            int randomnumber = Instance.Random.Next(0, maxIndex - 1);

            string model = modelsArray[randomnumber];

            SetPlayerModel(player, model, Instance.Config.Settings["default_model_disable_leg"]);
        }
    }
    private static void SetPlayerModel(CCSPlayerController player, string model, string disable_leg)
    {
        float apply_delay = 0.1f;

        if (Instance.Config.Settings.TryGetValue("apply_delay", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float delay))
        {
            apply_delay = float.MaxNumber(0.1f, delay);
        }

        Instance.AddTimer(apply_delay, () =>
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                player.PlayerPawn.Value?.ChangeModel(model, disable_leg);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }
}