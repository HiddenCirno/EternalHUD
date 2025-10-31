using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using System.Collections.ObjectModel;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using VulcanCore;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using fastJSON5;
using System.Reflection;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
namespace EternalHUD;

/// <summary>
/// This is the replacement for the former package.json data. This is required for all mods.
///
/// This is where we define all the metadata associated with this mod.
/// You don't have to do anything with it, other than fill it out.
/// All properties must be overriden, properties you don't use may be left null.
/// It is read by the mod loader when this mod is loaded.
/// </summary>
public record EternalHUD : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.hiddenhiragi.eternalhud";
    public override string Name { get; init; } = "永恒HUD";
    public override string Author { get; init; } = "HiddenHiragi";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
{
    { "com.hiddenhiragi.vulcancore", new SemanticVersioning.Range(">=1.0.0") }
};
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public override bool? IsBundleMod { get; init; } = true;
    public override string? License { get; init; } = "MIT";
}

// We want to load after PreSptModLoader is complete, so we set our type priority to that, plus 1.
[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 100)]
public class Core(
    ISptLogger<VulcanCore.VulcanCore> logger, DatabaseService databaseService, ModHelper modHelper)
    : IOnLoad
{
    public Task OnLoad()
    {
        HUDUtils.ModPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        HUDUtils.ModConfig = JSON5.ToObject<Config>(modHelper.GetRawFileData(HUDUtils.ModPath, "config.json5"));
        return Task.CompletedTask;
    }
}

[Injectable]
public class EternalHUDCustomStaticRouter : StaticRouter
{
    private static HttpResponseUtil _httpResponseUtil;
    private static DatabaseService _databaseService;
    private static RagfairController _ragfairController;
    private static JsonUtil _jsonUtil;
    private static RagfairOfferService _ragfairOfferService;
    private static ItemHelper _itemHelper;
    private static ModHelper _modHelper;
    private static VulcanCore.VulcanCore _vulcanCore;
    private static ISptLogger<VulcanCore.VulcanCore> _logger;

    public EternalHUDCustomStaticRouter(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        DatabaseService databaseService,
        RagfairController ragfairController,
        RagfairOfferService ragfairOfferService,
        ItemHelper itemHelper,
        ModHelper modHelper,
        VulcanCore.VulcanCore vulcanCore,
        ISptLogger<VulcanCore.VulcanCore> logger
        )
        : base(
            jsonUtil,
            GetCustomRoutes()
        )
    {
        _httpResponseUtil = httpResponseUtil;
        _databaseService = databaseService;
        _ragfairController = ragfairController;
        _ragfairOfferService = ragfairOfferService;
        _itemHelper = itemHelper;
        _modHelper = modHelper;
        _jsonUtil = jsonUtil;
        _logger = logger;
        _vulcanCore = vulcanCore;
    }
    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction(
                "/MiniHUD/getNameData",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await HandleRoute(
                        url,
                        info as NameInfo,
                        sessionId,
                        _jsonUtil,
                        _databaseService,
                        _ragfairController,
                        _ragfairOfferService,
                        _itemHelper,
                        _modHelper,
                        _logger,
                        _vulcanCore
                )
            )
        ];
    }

    private static ValueTask<string> HandleRoute(
        string url,
        NameInfo info,
        MongoId sessionId,
        JsonUtil jsonUtil,
        DatabaseService databaseService,
        RagfairController ragfairController,
        RagfairOfferService ragfairOfferService,
        ItemHelper itemHelper,
        ModHelper modHelper,
        ISptLogger<VulcanCore.VulcanCore> logger,
        VulcanCore.VulcanCore vulcanCore
    )
    {
        var useeng = false;
        var test2 = databaseService.GetLocales().Global["en"];
        databaseService.GetTraders().TryGetValue("656f0f98d80a697f855d34b1", out var trader);
        //trader.Base.AvailableInRaid = false;
        //test2.Value.TryGetValue("ef27f81993b027d956614ca1 Name", out string value);
        //VulcanLog.Log(value, logger);
        //test2.Value["ef27f81993b027d956614ca1 Name"] = "修改测试2";
        // Your mods code goes here
        if (!HUDUtils.havelogined)
        {
            HUDUtils.GenerateOriginalLocaleMap(databaseService, logger);
            foreach (var item in databaseService.GetItems())
            {
                if (item.Value.Id == (MongoId)"ef27f81993b027d956614ca1")
                {
                    //item.Value.Properties.BackgroundColor = "#CD8F53";
                }

                if (item.Value.Type != "Node" && item.Value.Properties != null)
                {
                    var itemid = (string)item.Value.Id;
                    HUDUtils.ItemList.Add(itemid);
                    HUDUtils.RagfairStatusMap.Add(itemid, (bool)item.Value.Properties.CanSellOnRagfair);
                    if (itemid == "ab520a65d655862cad07b11e")
                    {
                        //VulcanLog.Warn(HUDUtils.GetItemName(itemid), logger);
                        //VulcanLog.Warn(HUDUtils.GetItemPrice(itemid, databaseService).ToString(), logger);
                        //VulcanLog.Warn(HUDUtils.GetPresetPrice(itemid, databaseService).ToString(), logger);
                        //VulcanLog.Log(HUDUtils.GetItemName(itemid), logger);
                        //VulcanLog.Log(HUDUtils.IsPresetEquipment(itemid).ToString(), logger);
                        //VulcanLog.Log(HUDUtils.GetItemLevel(itemid, databaseService).ToString(), logger);
                        //VulcanLog.Log(HUDUtils.GetItemBackgroundColor(HUDUtils.GetItemLevel(itemid, databaseService)), logger);
                    }
                    if (HUDUtils.ModConfig.AutoExamine)
                    {
                        item.Value.Properties.ExaminedByDefault = true;
                    }
                }
            }
            //VulcanLog.Log(jsonUtil.Serialize(HUDUtils.TradeUseMap), logger);
            //test2.AddTransformer(delegate (Dictionary<string, string> valuetest)
            //{
            //    valuetest["ef27f81993b027d956614ca1 Name"] = "修改测试";
            //    return valuetest;
            //});
            //Transformer挂在这里单次执行应该就行, 问题应该不大....
            //吧?
            if (HUDUtils.ModConfig.ShowQuestInfo)
            {
                HUDUtils.SetQuestData(databaseService, logger);
            }
            if (HUDUtils.ModConfig.ShowItemInfo)
            {
                HUDUtils.SetItemData(databaseService, logger);
            }
            HUDUtils.GeneratePresetMap(databaseService, logger);
            HUDUtils.GeneratePriceMap(databaseService, logger);
            HUDUtils.GeneratePresetPriceMap(databaseService, logger);
            HUDUtils.GenerateLocaleMap(databaseService, logger);
            HUDUtils.GenerateQuestDataMap(databaseService, logger);
            HUDUtils.GenerateProductMap(databaseService, logger);
            HUDUtils.GenerateProductUseMap(databaseService, logger);
            HUDUtils.GenerateQuestAssortMap(databaseService, logger);
            HUDUtils.GenerateTradeMap(databaseService, logger);
            HUDUtils.GenerateTradeUseMap(databaseService, logger);
            HUDUtils.GenerateHandbookTagMap(databaseService, logger);
            HUDUtils.GenerateQuestRewardDataMap(databaseService, logger);
            HUDUtils.havelogined = true;
        }
        //test2.Value.TryGetValue("ef27f81993b027d956614ca1 Name", out string value2);
        //VulcanLog.Log(value2, logger);
        if (HUDUtils.ItemRequireMap.TryGetValue("5733279d245977289b77ec24", out ItemRequireData hideoutvalue))
        {
            //VulcanLog.Log("333", logger);
        }
        VulcanLog.Warn("开始处理客户端数据", logger);
        foreach (var item in HUDUtils.ItemList)
        {
            HUDUtils.GenerateItemClientCache(item, useeng, databaseService);
            if (HUDUtils.ModConfig.Display.ItemBGColor)
            {
                HUDUtils.SetItemBackgroundColor(item, databaseService, logger);
            }
            if (item == "ab520a65d655862cad07b11e")
            {
                //VulcanLog.Warn(HUDUtils.GetItemName(item), logger);
                //VulcanLog.Warn(databaseService.GetHandbook().Items.FirstOrDefault(x => x.Id == item).Price.ToString(), logger);
                //VulcanLog.Warn(databaseService.GetPrices().FirstOrDefault(x => x.Key == item).Value.ToString(), logger);
                //VulcanLog.Warn(HUDUtils.GetItemPrice(item, databaseService).ToString(), logger);
                //VulcanLog.Warn(HUDUtils.GetPresetPrice(item, databaseService).ToString(), logger);
            }
        }
        VulcanLog.Access("客户端数据处理完成", logger);
        //VulcanLog.Warn(jsonUtil.Serialize(HUDUtils.QuestRewardMap), logger);
        //return new ValueTask<string>(_httpResponseUtil.NullResponse());
        return new ValueTask<string>(jsonUtil.Serialize(HUDUtils.ClientCache));
    }
}
public class EternalHUDNameData : IRequestData
{
    public string Request { get; set; }
}

[Injectable]
public class PriceMapStaticRouter : StaticRouter
{
    private static HttpResponseUtil _httpResponseUtil;
    private static DatabaseService _databaseService;
    private static RagfairController _ragfairController;
    private static JsonUtil _jsonUtil;
    private static RagfairOfferService _ragfairOfferService;
    private static ItemHelper _itemHelper;
    private static VulcanCore.VulcanCore _corePostDBLoad;

    public PriceMapStaticRouter(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        DatabaseService databaseService,
        RagfairController ragfairController,
        RagfairOfferService ragfairOfferService,
        ItemHelper itemHelper,
        VulcanCore.VulcanCore corePostDBLoad)
        : base(jsonUtil, GetCustomRoutes())
    {
        _httpResponseUtil = httpResponseUtil;
        _databaseService = databaseService;
        _ragfairController = ragfairController;
        _ragfairOfferService = ragfairOfferService;
        _itemHelper = itemHelper;
        _jsonUtil = jsonUtil;
        _corePostDBLoad = corePostDBLoad;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        return new List<RouteAction>
        {
            new RouteAction(
                "/ShowLootValue/pullPriceMap",
                async (url, info, sessionId, output) =>
                    await HandleRoute(
                        url,
                        info as PriceMapRequestData,
                        sessionId,
                        _jsonUtil,
                        _databaseService,
                        _ragfairController,
                        _ragfairOfferService,
                        _itemHelper,
                        _corePostDBLoad
                    )
            )
        };
    }

    private static ValueTask<string> HandleRoute(
        string url,
        PriceMapRequestData info,
        MongoId sessionId,
        JsonUtil jsonUtil,
        DatabaseService databaseService,
        RagfairController ragfairController,
        RagfairOfferService ragfairOfferService,
        ItemHelper itemHelper,
        VulcanCore.VulcanCore corePostDBLoad
        )
    {
        // 构建返回的价格字典
        var priceMap = new Dictionary<string, int>();
        foreach (var item in databaseService.GetItems())
        {
            var itemId = (string)item.Value.Id;
            int price = HUDUtils.GetPresetPrice(itemId, databaseService);
            if (price > 0)
            {
                priceMap[itemId] = price;
            }
        }

        // 使用 HttpResponseUtil 返回标准格式 JSON
        //string jsonResponse = _httpResponseUtil.GetBody(priceMap);
        //绕过SPT提供的方法直接传递原始数据
        return new ValueTask<string>(jsonUtil.Serialize(priceMap));
    }
}
public class PriceMapRequestData : IRequestData
{
    public string Request { get; set; }
}
public class GetBodyResponseData<T>
{
    public BackendErrorCodes Err { get; set; }
    public string? ErrMsg { get; set; }
    public T Data { get; set; } = default!;
}