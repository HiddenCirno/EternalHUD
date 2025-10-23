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
[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class Core(
    ISptLogger<VulcanCore.VulcanCore> logger, DatabaseService databaseService)
    : IOnLoad
{
    public Task OnLoad()
    {

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
    private static VulcanCore.VulcanCore _vulcanCore;
    private static ISptLogger<VulcanCore.VulcanCore> _logger;

    public EternalHUDCustomStaticRouter(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        DatabaseService databaseService,
        RagfairController ragfairController,
        RagfairOfferService ragfairOfferService,
        ItemHelper itemHelper,
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
        ISptLogger<VulcanCore.VulcanCore> logger,
        VulcanCore.VulcanCore vulcanCore
    )
    {
        var useeng = true;
        var test2 = databaseService.GetLocales().Global["en"];
        test2.Value.TryGetValue("ef27f81993b027d956614ca1 Name", out string value);
        //VulcanLog.Log(value, logger);
        //test2.AddTransformer(delegate (Dictionary<string, string> valuetest)
        //{
            //valuetest["ef27f81993b027d956614ca1 Name"] = "修改测试";
            //return valuetest;
        //});
        test2.Value.TryGetValue("ef27f81993b027d956614ca1 Name", out string value2);
        //VulcanLog.Log(value2, logger);
        //test2.Value["ef27f81993b027d956614ca1 Name"] = "修改测试2";
        // Your mods code goes here
        HUDUtils.GeneratePresetMap(databaseService, logger);
        HUDUtils.GeneratePriceMap(databaseService, logger);
        HUDUtils.GenerateLocaleMap(databaseService, logger);
        HUDUtils.GenerateQuestDataMap(databaseService, logger);
        HUDUtils.GenerateProductMap(databaseService, logger);
        if (HUDUtils.ItemRequireMap.TryGetValue("5733279d245977289b77ec24", out ItemRequireData hideoutvalue))
        {
            //VulcanLog.Log("333", logger);
        }
        //VulcanLog.Log(jsonUtil.Serialize(HUDUtils.ItemProductMap), logger);
        foreach (var item in databaseService.GetItems())
        {
            if (item.Value.Id == (MongoId)"ef27f81993b027d956614ca1")
            {
                //item.Value.Properties.BackgroundColor = "#CD8F53";
            }

            if (item.Value.Type != "Node" && item.Value.Properties != null)
            {
                var items = ItemUtils.GetItem(item.Value.Id, databaseService);
                var itemid = items.Id;
                var CacheAmmo = HUDUtils.GetAmmoInfo(item.Value.Id, databaseService);
                var CacheArmor = HUDUtils.GetArmorData(item.Value.Id, databaseService);
                var ragfair = useeng ?
                    (bool)items.Properties.CanSellOnRagfair ?
                    "<color=#CommonColor>Ragfair: </color><color=#GreenColor>Tradeable</color>\n" :
                    "<color=#CommonColor>Ragfair: </color><color=#RedsColor>Untradeable</color>\n" :
                    (bool)items.Properties.CanSellOnRagfair ?
                    "<color=#CommonColor>跳蚤市场: </color><color=#GreenColor>可交易</color>\n" :
                    "<color=#CommonColor>跳蚤市场: </color><color=#RedsColor>不可交易</color>\n";
                var AmmoString = HUDUtils.GetAmmoDataString(CacheAmmo, useeng);
                var ArmorString = HUDUtils.GetArmorDataString(CacheArmor, useeng);
                var CopyString = useeng ? "<i>Press Ctrl+C to copy ID to clipboard</i>\n" : "<i>按下Ctrl+C复制物品ID</i>\n";
                var QuestString = HUDUtils.GetItemQuestString(itemid, useeng);
                var QuestHandoverString = HUDUtils.GetItemQuestHandoverString(itemid, useeng);
                var QuestLeaveString = HUDUtils.GetItemLeaveString(itemid, useeng);
                var HideoutString = HUDUtils.GetItemAreaString(itemid, useeng);
                var ProductString = HUDUtils.GetItemProductString(itemid, useeng);
                HUDUtils.ClientCache.Add(new NameInfo
                {
                    ID = $"<color=#CommonColor>ID: {itemid}</color>",
                    RealID = itemid,
                    Name = useeng ? "Name: " : "名称: ",
                    TrueName = HUDUtils.GetItemName(itemid),
                    StringName = itemid,
                    EName = useeng ? string.Empty : $"<color=#CommonColor>英文: {HUDUtils.GetItemName(itemid, "en")}</color>\n",
                    AmmoString = AmmoString != string.Empty ? $"{AmmoString}\n" : string.Empty,
                    ArmorString = string.Empty, //TradeUse
                    ArmorString2 = string.Empty,//ProductUse
                    QuestString = QuestString != string.Empty ? QuestString : string.Empty,
                    QuestHandoverString = QuestHandoverString != string.Empty ? QuestHandoverString : string.Empty,
                    QuestLeaveString = QuestLeaveString != string.Empty ? QuestLeaveString : string.Empty,
                    HideoutString = HideoutString != string.Empty ? HideoutString : string.Empty,
                    TradeString = string.Empty,
                    ProductString = ProductString != string.Empty ? ProductString : string.Empty,
                    RewardString = string.Empty,
                    RagfairString = ragfair,
                    Tag = string.Empty,
                    PricesString = ArmorString != string.Empty ? $"{ArmorString}\n" : string.Empty,
                    SellPrice = HUDUtils.GetPresetPrice(items.Id, databaseService),
                    CanSell = useeng,
                    CopyTip = CopyString
                });
            }
        }
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
            int price = HUDUtils.GetItemPrice(itemId, databaseService);
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