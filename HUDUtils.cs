using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;
using VulcanCore;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using Microsoft.Extensions.Logging;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using System.Drawing;
using System.Reflection.Emit;
namespace EternalHUD;
public class HUDUtils
{
    public static List<NameInfo> ClientCache = new List<NameInfo>();
    public static Dictionary<string, int> PriceMap = new Dictionary<string, int>();
    public static Dictionary<string, List<Item>> PresetMap = new Dictionary<string, List<Item>>();
    public static Dictionary<string, Dictionary<string, string>> OriginalLocaleMap = new Dictionary<string, Dictionary<string, string>>();
    public static Dictionary<string, Dictionary<string, string>> LocaleMap = new Dictionary<string, Dictionary<string, string>>();
    public static Dictionary<string, ItemRequireData> ItemRequireMap = new Dictionary<string, ItemRequireData>();
    public static Dictionary<string, ProductMapData> ItemProductMap = new Dictionary<string, ProductMapData>();
    public static Dictionary<string, ProductMapData> ItemProductUseMap = new Dictionary<string, ProductMapData>();
    public static Dictionary<string, QuestData> QuestDataMap = new Dictionary<string, QuestData>();
    public static Dictionary<string, TradeMapData> TradeMap = new Dictionary<string, TradeMapData>();
    public static Dictionary<string, TradeMapData> TradeUseMap = new Dictionary<string, TradeMapData>();
    public static Dictionary<string, string> HandbookTagMap = new Dictionary<string, string>();
    public static QuestAssortData QuestAssortMap = new QuestAssortData
    {
        Start = new Dictionary<string, string>(),
        Finish = new Dictionary<string, string>()
    };
    public static string ModPath;
    public static Config ModConfig;
    public static List<string> ItemList = new List<string>();
    public static Dictionary<string, bool> RagfairStatusMap = new Dictionary<string, bool>();
    public static Dictionary<string, int> PresetPriceMap = new Dictionary<string, int>();
    public static Dictionary<string, QuestRewardMapData> QuestRewardMap = new Dictionary<string, QuestRewardMapData>();
    public static bool havelogined = false;
    public static List<Item> GetPreset(
        string itemid,
        DatabaseService databaseService)
    {
        var presets = databaseService.GetGlobals().ItemPresets;
        foreach (var ps in presets.Values)
        {
            if (ps.Encyclopedia != null && ps.Encyclopedia == (MongoId)itemid)
            {
                return ps.Items;
            }
        }
        return new List<Item>();
    }
    public static void GeneratePresetMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var presets = databaseService.GetGlobals().ItemPresets;
        foreach (var ps in presets.Values)
        {
            if (ps.Encyclopedia != null)
            {
                PresetMap[ps.Encyclopedia] = ps.Items;
            }
        }
        VulcanLog.Log("预设表生成完成", logger);
    }
    public static int GetPresetPrice(
        string itemid,
        DatabaseService databaseService
        )
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        var ragfairs = item?.Properties?.CanSellOnRagfair ?? false;
        RagfairStatusMap.TryGetValue(itemid, out var value);
        var ragfair = value;
        var minprice = GetItemPrice(itemid, databaseService);
        if (ragfairs)
        {
            return minprice;
        }
        else
        {
            int price = 0;
            var preset = PresetMap.TryGetValue(itemid, out var list) ? list : new List<Item>();
            if (preset.Count > 0)
            {
                foreach (Item items in preset)
                {
                    price += GetItemPrice(items.Template, databaseService);
                }
                return price;
            }
            else
            {
                return minprice;
            }
        }
    }
    public static int GetItemPrice(
        string itemid,
        DatabaseService databaseService
        )
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        var ragfairs = item?.Properties?.CanSellOnRagfair ?? false;
        RagfairStatusMap.TryGetValue(itemid, out var value);
        var ragfair = value;
        var mongoid = (MongoId)itemid;
        var priceTable = databaseService.GetPrices();
        var handbook = databaseService.GetHandbook().Items;
        //var ragfairPrice = offers.Min;
        if (ragfairs)
        {
            var tablePrice = (int)priceTable.FirstOrDefault(kv => kv.Key == mongoid).Value;
            if (tablePrice > 0)
            {
                return tablePrice;
            }
            else
            {
                var handbookdata = handbook.FirstOrDefault(i => i.Id == mongoid);
                if (handbookdata != null && handbookdata.Price > 0)
                {
                    var handbookprice = (int)handbookdata.Price;
                    return (int)(handbookprice * 0.6);
                }
                else return 1;
            }
        }
        else
        {
            var handbookdata = handbook.FirstOrDefault(i => i.Id == mongoid);
            if (handbookdata != null && handbookdata.Price > 0)
            {
                var handbookprice = (int)handbookdata.Price;
                return (int)(handbookprice * 0.6);
            }
            else return 1;
        }
    }
    public static void GeneratePriceMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {

        foreach (var item in databaseService.GetItems())
        {
            var itemId = (string)item.Value.Id;
            int price = HUDUtils.GetItemPrice(itemId, databaseService);
            if (price > 0)
            {
                HUDUtils.PriceMap[itemId] = price;
            }
        }
        VulcanLog.Log("价值表生成完成", logger);
    }
    public static void GeneratePresetPriceMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {

        foreach (var item in databaseService.GetItems())
        {
            var itemid = (string)item.Value.Id;
            var preset = PresetMap.TryGetValue(itemid, out var list) ? list : new List<Item>();
            int price = 0;
            if (preset.Count > 0)
            {
                price = GetPresetPrice(itemid, databaseService);
            }
            else
            {
                PriceMap.TryGetValue(itemid, out var value);
                price = value;
            }
            if (price > 0)
            {
                PresetPriceMap[itemid] = price;
            }
        }
        VulcanLog.Log("价值表生成完成", logger);
    }
    public static void GenerateLocaleMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var locales = databaseService.GetLocales().Global;
        foreach (var locale in locales)
        {
            LocaleMap[locale.Key] = new Dictionary<string, string>();
            var localevalue = locale.Value.Value;
            foreach (var item in localevalue)
            {
                LocaleMap[locale.Key][item.Key] = item.Value;
            }
        }
        VulcanLog.Log("语言索引生成完成", logger);
    }
    public static void GenerateOriginalLocaleMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var locales = databaseService.GetLocales().Global;
        foreach (var locale in locales)
        {
            OriginalLocaleMap[locale.Key] = new Dictionary<string, string>();
            var localevalue = locale.Value.Value;
            foreach (var item in localevalue)
            {
                OriginalLocaleMap[locale.Key][item.Key] = item.Value;
            }
        }
        VulcanLog.Log("语言索引生成完成", logger);
    }
    public static void GenerateQuestRewardDataMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var quests = databaseService.GetQuests().Values.ToList();
        foreach (var quest in quests)
        {
            var questid = (string)quest.Id;
            var traderid = (string)quest.TraderId;
            quest.Rewards.TryGetValue("Started", out var sreward);
            quest.Rewards.TryGetValue("Success", out var freward);
            if (sreward != null)
            {
                foreach (var reward in sreward)
                {
                    if (reward.Type == RewardType.Item)
                    {
                        var itemid = reward.Items[0].Template;
                        var count = (int)reward.Value;
                        QuestRewardMap.TryGetValue(itemid, out var value);
                        if (value == null)
                        {
                            value = new QuestRewardMapData
                            {
                                Id = itemid,
                                Name = GetItemName(itemid),
                                Reward = new List<QuestRewardData>()
                            };
                        }
                        if (value != null)
                        {
                            var have = value.Reward.FirstOrDefault(x => x.QuestId == questid);
                            if (have != null)
                            {
                                have.Count += count;
                            }
                            else
                            {
                                value.Reward.Add(new QuestRewardData
                                {
                                    QuestId = questid,
                                    QuestName = GetQuestName(questid),
                                    TraderId = traderid,
                                    TraderName = GetTraderName(traderid),
                                    Count = count,
                                    QuestStage = EQuestStageType.Start
                                });
                            }
                        }
                        QuestRewardMap[itemid] = value;
                    }
                }
                if (freward != null)
                {
                    foreach (var reward in freward)
                    {
                        if (reward.Type == RewardType.Item)
                        {
                            var itemid = reward.Items[0].Template;
                            var count = (int)reward.Value;
                            QuestRewardMap.TryGetValue(itemid, out var value);
                            if (value == null)
                            {
                                value = new QuestRewardMapData
                                {
                                    Id = itemid,
                                    Name = GetItemName(itemid),
                                    Reward = new List<QuestRewardData>()
                                };
                            }
                            if (value != null)
                            {
                                var have = value.Reward.FirstOrDefault(x => x.QuestId == questid);
                                if (have != null)
                                {
                                    have.Count += count;
                                }
                                else
                                {
                                    value.Reward.Add(new QuestRewardData
                                    {
                                        QuestId = questid,
                                        QuestName = GetQuestName(questid),
                                        TraderId = traderid,
                                        TraderName = GetTraderName(traderid),
                                        Count = count,
                                        QuestStage = EQuestStageType.Finish
                                    });
                                }
                            }
                            QuestRewardMap[itemid] = value;
                        }
                    }
                }
            }
        }
    }
    public static void GenerateQuestDataMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var areas = databaseService.GetHideout().Areas;
        var quests = databaseService.GetQuests().Values.ToList();
        var items = databaseService.GetItems().Values.ToList();
        var kappa = QuestUtils.GetQuest("5c51aac186f77432ea65c552", databaseService)?
                          .Conditions?
                          .AvailableForStart?
                          .Where(x => x.ConditionType == "Quest")
                          .Select(x => x.Target)
                          .Where(x => x.IsItem)
                          .Select(x => x.Item)
                          .ToList();
        var lightkeeper = QuestUtils.GetQuest("625d6ff5ddc94657c21a1625", databaseService)?
                          .Conditions?
                          .AvailableForStart?
                          .Where(x => x.ConditionType == "Quest")
                          .Select(x => x.Target)
                          .Where(x => x.IsItem)
                          .Select(x => x.Item)
                          .ToList();
        foreach (var quest in quests)
        {
            var questid = (string)quest.Id;
            var traderid = (string)quest.TraderId;
            var cachedata = new QuestData
            {
                Id = questid,
                Name = GetQuestName(questid),
                Description = GetQuestDescription(questid),
                TraderId = traderid,
                TraderName = GetTraderName(traderid),
                IsKappaPreQuest = false,
                IsLightkeeperPreQuest = false,
                LogicData = new QuestLogic
                {
                    Level = 0,
                    PreQuestData = new List<QuestLogicData>(),
                    UnlockQuestData = new List<QuestLogicData>()
                }
            };
            if (kappa.Contains(questid))
            {
                cachedata.IsKappaPreQuest = true;
            }
            if (lightkeeper.Contains(questid))
            {
                cachedata.IsLightkeeperPreQuest = true;
            }
            foreach (var condition in quest.Conditions.AvailableForStart)
            {
                if (condition != null)
                {
                    switch (condition.ConditionType)
                    {
                        case "Quest":
                            {
                                if (condition.Status.Contains(QuestStatusEnum.Success) || condition.Status.Contains(QuestStatusEnum.Fail))
                                {
                                    var conditionquest = quests.FirstOrDefault(x => x.Id == condition.Target.Item);
                                    if (conditionquest != null)
                                    {
                                        var cquestid = (string)conditionquest.Id;
                                        var ctraderid = (string)conditionquest.TraderId;
                                        cachedata.LogicData.PreQuestData.Add(new QuestLogicData
                                        {
                                            QuestId = cquestid,
                                            QuestName = GetQuestName(cquestid),
                                            TraderId = ctraderid,
                                            TraderName = GetTraderName(ctraderid),
                                        });
                                    }
                                }
                            }
                            break;
                        case "Level":
                            {
                                cachedata.LogicData.Level = (int)condition.Value;
                            }
                            break;
                    }
                }
            }
            QuestDataMap.Add(questid, cachedata);
        }
        foreach (var quest in quests)
        {
            var questid = (string)quest.Id;
            var traderid = (string)quest.TraderId;
            var conditions = quest.Conditions.AvailableForStart
                .Where(condition => condition != null &&
                    condition.ConditionType == "Quest" &&
                    (condition.Status.Contains(QuestStatusEnum.Success) || condition.Status.Contains(QuestStatusEnum.Fail)))
                .ToList();
            foreach (var condition in conditions)
            {
                if (condition != null)
                {
                    var cquestid = (string)condition.Target.Item;
                    QuestDataMap.TryGetValue(cquestid, out QuestData questdata);
                    if (questdata != null)
                    {
                        questdata.LogicData.UnlockQuestData.Add(new QuestLogicData
                        {
                            QuestId = questid,
                            QuestName = GetQuestName(questid),
                            TraderId = traderid,
                            TraderName = GetTraderName(traderid),
                        });
                        QuestDataMap[cquestid] = questdata;
                    }
                }
            }
        }
        //if (result == null)
        //    VulcanLog.Log("1231231", logger);
        List<string> blacklist = new List<string>
        {
            "5449016a4bdc2d6f028b456f", //卢布
            "5696686a4bdc2da3298b456a", //美元
            "569668774bdc2da2298b4568"  //欧元
        };
        foreach (var area in areas)
        {
            var stage = area.Stages;
            if (stage != null)
            {
                foreach (var level in stage)
                {
                    var requirement = level.Value.Requirements;
                    if (requirement != null)
                    {
                        foreach (var item in requirement)
                        {
                            if (item.Type == "Item")
                            {
                                var itemid = (string)item.TemplateId;
                                ItemRequireMap.TryGetValue(itemid, out ItemRequireData value);
                                if (value == null && !blacklist.Contains(itemid))
                                {
                                    value = new ItemRequireData
                                    {
                                        Id = itemid,
                                        Name = GetItemName(itemid),
                                        IsQuestItem = (bool)items.FirstOrDefault(x => x.Id == item.TemplateId).Properties.QuestItem,
                                        QuestList = new List<QuestItemData>(),
                                        AreaList = new List<HideoutItemData>()
                                    };
                                }
                                if (value != null)
                                {
                                    ItemRequireMap[itemid] = value;
                                    value.AreaList.Add(new HideoutItemData
                                    {
                                        Type = (HideoutAreas)area.Type,
                                        AreaLevel = int.Parse(level.Key),
                                        Count = (int)item.Count

                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
        foreach (var quest in quests)
        {
            if (quest.Conditions.AvailableForFinish != null)
            {
                foreach (var condition in quest.Conditions.AvailableForFinish)
                {
                    switch (condition.ConditionType)
                    {
                        case "HandoverItem":
                        case "LeaveItemAtLocation":
                            {
                                ResolveCondition(condition, quest, blacklist, items, logger);
                            }
                            break;
                        case "WeaponAssembly":
                            {
                                //ResolveCondition(condition, quest, blacklist, items);
                            }
                            break;
                    }
                }
            }
        }
        VulcanLog.Log("物品索引生成完成", logger);
    }
    public static void GenerateProductMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var recipes = databaseService.GetHideout().Production.Recipes;
        foreach (var recipe in recipes)
        {
            var items = new Dictionary<string, int>();
            var tools = new List<string>();
            var locked = false;
            var questid = string.Empty;
            var areatype = recipe.AreaType;
            var arealevel = 1;
            if (recipe.AreaType != HideoutAreas.WaterCollector && recipe.AreaType != HideoutAreas.BitcoinFarm)
            {
                var itemid = (string)recipe.EndProduct;
                ItemProductMap.TryGetValue(itemid, out var recipedata);
                if (recipedata == null)
                {
                    recipedata = new ProductMapData
                    {
                        Name = GetItemName(itemid),
                        Recipe = new List<ProductRecipeData>()
                    };
                }
                if (recipedata != null)
                {
                    ItemProductMap[itemid] = recipedata;
                    foreach (var item in recipe.Requirements)
                    {
                        switch (item.Type)
                        {
                            case "Item":
                                {
                                    items.Add((string)item.TemplateId, (int)item.Count);
                                }
                                break;
                            case "Tool":
                                {
                                    tools.Add((string)item.TemplateId);
                                }
                                break;
                            case "QuestComplete":
                                {
                                    locked = true;
                                    questid = (string)item.QuestId ?? "任务出现空值";//尼基塔你大爷的!!
                                }
                                break;
                            case "Area":
                                {
                                    areatype = (HideoutAreas)item.AreaType;
                                    arealevel = (int)item.RequiredLevel;
                                }
                                break;
                        }
                    }
                    recipedata.Recipe.Add(new ProductRecipeData
                    {
                        Name = GetItemName(itemid),
                        Count = (int)recipe.Count,
                        Time = (int)recipe.ProductionTime,
                        Locked = locked,
                        Quest = questid,
                        AreaType = (HideoutAreas)areatype,
                        AreaLevel = arealevel,
                        AreaName = GetLocale($"hideout_area_{(int)areatype}_name"),
                        Result = itemid,
                        Recipe = new ProductRecipe
                        {
                            Items = items,
                            Tools = tools
                        }
                    });
                }
            }
        }
        VulcanLog.Log("配方索引生成完成", logger);
    }
    public static void GenerateProductUseMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var recipes = databaseService.GetHideout().Production.Recipes;
        foreach (var recipe in recipes)
        {
            var items = new Dictionary<string, int>();
            var tools = new List<string>();
            var locked = false;
            var questid = string.Empty;
            var areatype = recipe.AreaType;
            var arealevel = 1;
            if (recipe.AreaType != HideoutAreas.WaterCollector && recipe.AreaType != HideoutAreas.BitcoinFarm)
            {
                var resultid = (string)recipe.EndProduct;
                foreach (var item in recipe.Requirements)
                {
                    var itemid = (string)item.TemplateId;
                    if (itemid != null)
                    {
                        ItemProductUseMap.TryGetValue(itemid, out var recipedata);
                        if (recipedata == null)
                        {
                            recipedata = new ProductMapData
                            {
                                Name = GetItemName(itemid),
                                Recipe = new List<ProductRecipeData>()
                            };
                            ItemProductUseMap[itemid] = recipedata;
                        }
                    }
                }
                foreach (var item in recipe.Requirements)
                {
                    var itemid = (string)item.TemplateId;
                    switch (item.Type)
                    {
                        case "Item":
                            {
                                items.Add(itemid, (int)item.Count);
                            }
                            break;
                        case "Tool":
                            {
                                tools.Add(itemid);
                            }
                            break;
                        case "QuestComplete":
                            {
                                locked = true;
                                questid = (string)item.QuestId ?? "任务出现空值";//尼基塔你大爷的!!
                            }
                            break;
                        case "Area":
                            {
                                areatype = (HideoutAreas)item.AreaType;
                                arealevel = (int)item.RequiredLevel;
                            }
                            break;
                    }
                    if (itemid != null)
                    {
                        ItemProductUseMap.TryGetValue(itemid, out var recipedata);
                        recipedata.Recipe.Add(new ProductRecipeData
                        {
                            Name = GetItemName(itemid),
                            Count = (int)recipe.Count,
                            Time = (int)recipe.ProductionTime,
                            Locked = locked,
                            Quest = questid,
                            AreaType = (HideoutAreas)areatype,
                            AreaLevel = arealevel,
                            Result = resultid,
                            AreaName = GetLocale($"hideout_area_{(int)areatype}_name"),
                            Recipe = new ProductRecipe
                            {
                                Items = items,
                                Tools = tools
                            }
                        });
                    }
                }
            }
        }
        VulcanLog.Log("配方索引生成完成", logger);
    }
    public static void GenerateTradeMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var traders = databaseService.GetTraders();
        foreach (var trader in traders)
        {
            if (trader.Value.Assort != null && trader.Value.Assort.Items != null && trader.Value.Assort.Items.Count > 0)
            {
                var items = trader.Value.Assort.Items;
                var barter = trader.Value.Assort.BarterScheme;
                var loyal = trader.Value.Assort.LoyalLevelItems;
                foreach (var item in items)
                {
                    if (item.ParentId == "hideout")
                    {
                        var id = (string)item.Id;
                        var itemid = (string)item.Template;
                        TradeMap.TryGetValue(itemid, out TradeMapData tradedata);
                        if (tradedata == null)
                        {
                            tradedata = new TradeMapData
                            {
                                Id = itemid,
                                Name = GetItemName(itemid),
                                Recipe = new List<TradeData>()
                            };
                        }
                        var cacherecipe = new TradeData
                        {
                            Id = itemid,
                            Name = GetItemName(itemid),
                            Barter = new Dictionary<string, double>(),
                            TrustLevel = loyal[id],
                            TraderId = trader.Value.Base.Id,
                            IsLocked = false,
                            QuestId = "",
                            QuestStage = EQuestStageType.Finish
                        };
                        if (QuestAssortMap.Start.ContainsKey(id))
                        {
                            cacherecipe.IsLocked = true;
                            cacherecipe.QuestId = QuestAssortMap.Start[id];
                            cacherecipe.QuestStage = EQuestStageType.Start;
                        }
                        if (QuestAssortMap.Finish.ContainsKey(id))
                        {
                            cacherecipe.IsLocked = true;
                            cacherecipe.QuestId = QuestAssortMap.Finish[id];
                        }
                        //得把任务锁定配方抽出来
                        //搞定
                        foreach (var br in barter[id][0])
                        {
                            cacherecipe.Barter.TryAdd(br.Template, (double)br.Count);
                        }
                        if (tradedata != null)
                        {
                            tradedata.Recipe.Add(cacherecipe);
                            TradeMap[itemid] = tradedata;
                        }
                    }
                }
            }
        }
    }
    public static void GenerateTradeUseMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        List<string> blacklist = new List<string>
        {
            "5449016a4bdc2d6f028b456f", //卢布
            "5696686a4bdc2da3298b456a", //美元
            "569668774bdc2da2298b4568", //欧元
            "5d235b4d86f7742e017bc88a"  //GP币
        };
        var traders = databaseService.GetTraders();
        foreach (var trader in traders)
        {
            if (trader.Value.Assort != null && trader.Value.Assort.Items != null && trader.Value.Assort.Items.Count > 0)
            {
                var items = trader.Value.Assort.Items;
                var barter = trader.Value.Assort.BarterScheme;
                var loyal = trader.Value.Assort.LoyalLevelItems;
                foreach (var item in items)
                {
                    if (item.ParentId == "hideout")
                    {
                        var id = (string)item.Id;
                        var itemid = (string)item.Template;
                        var cacherecipe = new TradeData
                        {
                            Id = itemid,
                            Name = GetItemName(itemid),
                            Barter = new Dictionary<string, double>(),
                            TrustLevel = loyal[id],
                            TraderId = trader.Value.Base.Id,
                            IsLocked = false,
                            QuestId = "",
                            QuestStage = EQuestStageType.Finish
                        };
                        if (QuestAssortMap.Start.ContainsKey(id))
                        {
                            cacherecipe.IsLocked = true;
                            cacherecipe.QuestId = QuestAssortMap.Start[id];
                            cacherecipe.QuestStage = EQuestStageType.Start;
                        }
                        if (QuestAssortMap.Finish.ContainsKey(id))
                        {
                            cacherecipe.IsLocked = true;
                            cacherecipe.QuestId = QuestAssortMap.Finish[id];
                        }
                        //得把任务锁定配方抽出来
                        //搞定
                        foreach (var br in barter[id][0])
                        {
                            cacherecipe.Barter.TryAdd(br.Template, (double)br.Count);
                        }
                        foreach (var br in barter[id][0])
                        {
                            var brid = (string)br.Template;
                            TradeUseMap.TryGetValue(brid, out TradeMapData tradedata);
                            if (tradedata == null && !blacklist.Contains(brid))
                            {
                                tradedata = new TradeMapData
                                {
                                    Id = brid,
                                    Name = GetItemName(brid),
                                    Recipe = new List<TradeData>()
                                };
                            }
                            if (tradedata != null)
                            {
                                tradedata.Recipe.Add(cacherecipe);
                                TradeUseMap[brid] = tradedata;
                            }
                        }
                    }
                }
            }
        }
    }
    public static void GenerateQuestAssortMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var traders = databaseService.GetTraders();
        foreach (var trader in traders)
        {
            var qa = trader.Value.QuestAssort;
            if (qa != null && qa.Count > 0)
            {
                if (qa.ContainsKey("started") && qa["started"] != null)
                {
                    if (qa["started"].Count > 0)
                    {
                        foreach (var key in qa["started"])
                        {
                            QuestAssortMap.Start.TryAdd((string)key.Key, (string)key.Value);
                        }
                    }
                }
                if (qa.ContainsKey("success") && qa["success"] != null)
                {
                    if (qa["success"].Count > 0)
                    {
                        foreach (var key in qa["success"])
                        {
                            if (key.Key != null && key.Value != null)
                            {
                                QuestAssortMap.Finish.TryAdd((string)key.Key, (string)key.Value);
                            }
                        }
                    }
                }
            }
        }
    }
    public static void GenerateHandbookTagMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var handbook = databaseService.GetHandbook().Items;
        foreach (var item in handbook)
        {
            HandbookTagMap.TryAdd((string)item.Id, (string)item.ParentId);
        }
    }
    public static void ResolveCondition(QuestCondition condition, Quest quest, List<string> blacklist, List<TemplateItem> items, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        List<string> list = condition.Target.IsList ? condition.Target.List : new List<string> { condition.Target.Item };
        var itemid = list[0];
        if (itemid != null)
        {
            ItemRequireMap.TryGetValue(itemid, out ItemRequireData value);
            if (value == null && !blacklist.Contains(itemid))
            {
                value = new ItemRequireData
                {
                    Id = itemid,
                    Name = GetItemName(itemid),
                    IsQuestItem = (bool)items.FirstOrDefault(x => x.Id == itemid).Properties.QuestItem,
                    QuestList = new List<QuestItemData>(),
                    AreaList = new List<HideoutItemData>()
                };
            }
            if (value != null)
            {
                ItemRequireMap[itemid] = value;
                var targetfir = (bool)condition.OnlyFoundInRaid;
                var questid = quest.Id;
                var count = (int)condition.Value;
                var have = value.QuestList.FirstOrDefault(x => x.QuestId == questid && x.FindInRaid == targetfir);
                if (have != null)
                {
                    have.Count += count;
                }
                else
                {
                    value.QuestList.Add(new QuestItemData
                    {
                        QuestId = questid,
                        QuestName = GetQuestName(questid),
                        TraderId = (string)quest.TraderId,
                        TraderName = GetLocale($"{(string)quest.TraderId} Nickname"),
                        FindInRaid = targetfir,
                        Count = count,
                        Type = condition.ConditionType
                    });
                }
            }
        }
    }
    public static string GetLocale(string localeKey, string language = "ch")
    {
        var locales = LocaleMap[language];
        locales.TryGetValue(localeKey, out string value);
        return value != null ? value : "这是一个空键";
    }
    public static string GetOriginalLocale(string localeKey, string language = "ch")
    {
        var locales = OriginalLocaleMap[language];
        locales.TryGetValue(localeKey, out string value);
        return value != null ? value : "这是一个空键";
    }
    public static string GetOriginalItemName(string itemid, string language = "ch")
    {
        return GetOriginalLocale($"{itemid} Name", language);
    }
    public static string GetOriginalItemShortName(string itemid, string language = "ch")
    {
        return GetOriginalLocale($"{itemid} ShortName", language);
    }
    public static string GetOriginalItemDescription(string itemid, string language = "ch")
    {
        return GetOriginalLocale($"{itemid} Description", language);
    }
    public static string GetOriginalQuestName(string questid, string language = "ch")
    {
        return GetOriginalLocale($"{questid} name", language);
    }
    public static string GetOriginalQuestDescription(string questid, string language = "ch")
    {
        return GetOriginalLocale($"{questid} description", language);
    }
    public static string GetOriginalTraderName(string traderid, string language = "ch")
    {
        return GetOriginalLocale($"{traderid} Nickname", language);
    }
    public static string GetItemName(string itemid, string language = "ch")
    {
        return GetLocale($"{itemid} Name", language);
    }
    public static string GetItemShortName(string itemid, string language = "ch")
    {
        return GetLocale($"{itemid} ShortName", language);
    }
    public static string GetItemDescription(string itemid, string language = "ch")
    {
        return GetLocale($"{itemid} Description", language);
    }
    public static string GetQuestName(string questid, string language = "ch")
    {
        return GetLocale($"{questid} name", language);
    }
    public static string GetQuestDescription(string questid, string language = "ch")
    {
        return GetLocale($"{questid} description", language);
    }
    public static string GetTraderName(string traderid, string language = "ch")
    {
        return GetLocale($"{traderid} Nickname", language);
    }
    public static bool IsAmmo(string itemid, DatabaseService databaseService)
    {
        return ItemUtils.GetItem(itemid, databaseService).Parent == (MongoId)"5485a8684bdc2da71d8b4567" ? true : false;
    }
    public static bool IsAmmoBox(string itemid, DatabaseService databaseService)
    {
        return ItemUtils.GetItemRagfairTag(itemid, databaseService) == (MongoId)"5b47574386f77428ca22b33c" ? true : false;
    }
    public static void SetItemBackgroundColor(string itemid, DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        var level = GetItemLevel(itemid, databaseService);
        var color = GetItemBackgroundColor(level);
        if (color != null)
        {
            if (itemid == "5648a69d4bdc2ded0b8b457b")
            {
                //VulcanLog.Warn(GetItemName(itemid), logger);
            }
            if (GetItemNotBlacked(itemid))
            {
                item.Properties.BackgroundColor = color;
            }
        }
    }
    public static bool GetItemNotBlacked(string itemid)
    {
        if (
                IsNotContainerOrSecurity(itemid) &&
                IsNotWeapon(itemid) &&
                IsNotSpecialEquipment(itemid) &&
                !IsHandbookTagItem(itemid, VulcanUtil.ConvertHashID("VulcanSpecialItem")) &&
                IsNotInBlackList(itemid)
                )
        {
            return true;
        }
        return false;
    }
    public static void GenerateItemClientCache(string itemid, bool useeng, DatabaseService databaseService)
    {

        var items = ItemUtils.GetItem(itemid, databaseService);
        var CacheAmmo = HUDUtils.GetAmmoInfo(itemid, databaseService);
        var CacheArmor = HUDUtils.GetArmorData(itemid, databaseService);
        var ragfair = useeng ?
            (bool)items.Properties.CanSellOnRagfair ?
            "<color=#CommonColor>Ragfair: </color><color=#GreenColor>Tradeable</color>\n" :
            "<color=#CommonColor>Ragfair: </color><color=#RedsColor>Untradeable</color>\n" :
            (bool)items.Properties.CanSellOnRagfair ?
            "<color=#CommonColor>跳蚤市场: </color><color=#GreenColor>可交易</color>\n" :
            "<color=#CommonColor>跳蚤市场: </color><color=#RedsColor>不可交易</color>\n";
        var TagString = useeng ?
            string.Empty :
            $"<color=#CommonColor>品质: </color><color=#{GetItemTagColor(itemid, databaseService)}>{GetItemTag(itemid, databaseService).TrimStart('[').TrimEnd(']')}</color>\n";
        var AmmoString = HUDUtils.GetAmmoDataString(CacheAmmo, useeng);
        var ArmorString = HUDUtils.GetArmorDataString(CacheArmor, useeng);
        var CopyString = useeng ? "<i>Press Ctrl+C to copy ID to clipboard</i>\n" : "<i>按下Ctrl+C复制物品ID</i>\n";
        var QuestString = HUDUtils.GetItemQuestString(itemid, useeng);
        var QuestHandoverString = HUDUtils.GetItemQuestHandoverString(itemid, useeng);
        var QuestLeaveString = HUDUtils.GetItemLeaveString(itemid, useeng);
        var HideoutString = HUDUtils.GetItemAreaString(itemid, useeng);
        var ProductString = HUDUtils.GetItemProductString(itemid, useeng);
        var ProductUseString = HUDUtils.GetItemProductUseString(itemid, useeng);
        var TradeString = HUDUtils.GetItemTradeString(itemid, useeng);
        var TradeUseString = HUDUtils.GetItemTradeUseString(itemid, useeng);
        var RewardString = GetItemQuestRewardString(itemid, useeng);
        PresetPriceMap.TryGetValue(itemid, out var price);
        HUDUtils.ClientCache.Add(new NameInfo
        {
            ID = $"<color=#CommonColor>ID: {itemid}</color>",
            RealID = itemid,
            Name = useeng ? "Name: " : "名称: ",
            TrueName = HUDUtils.GetItemName(itemid),
            StringName = itemid,
            EName = useeng ? string.Empty : $"<color=#CommonColor>英文: {HUDUtils.GetItemName(itemid, "en")}</color>\n",
            AmmoString = AmmoString != string.Empty ? $"{AmmoString}\n" : string.Empty,
            ArmorString = TradeUseString != string.Empty ? TradeUseString : string.Empty, //TradeUse
            ArmorString2 = ProductUseString != string.Empty ? ProductUseString : string.Empty,//ProductUse
            QuestString = QuestString != string.Empty ? QuestString : string.Empty,
            QuestHandoverString = QuestHandoverString != string.Empty ? QuestHandoverString : string.Empty,
            QuestLeaveString = QuestLeaveString != string.Empty ? QuestLeaveString : string.Empty,
            HideoutString = HideoutString != string.Empty ? HideoutString : string.Empty,
            TradeString = TradeString != string.Empty ? TradeString : string.Empty,
            ProductString = ProductString != string.Empty ? ProductString : string.Empty,
            RewardString = RewardString != string.Empty ? RewardString : string.Empty,
            RagfairString = ragfair,
            Tag = TagString != string.Empty ? TagString : string.Empty,
            PricesString = ArmorString != string.Empty ? $"{ArmorString}\n" : string.Empty,
            SellPrice = price,
            Level = GetItemLevel(itemid, databaseService),
            CanSell = useeng,
            CopyTip = CopyString,
            ChangeTime = GetItemNotBlacked(itemid)
        });
    }
    public static string? GetItemBackgroundColor(int level)
    {
        switch (level)
        {
            case 0:
                {
                    return "default";
                }
            case 1:
                {
                    return "#A9A9A9";
                }
            case 2:
                {
                    return "#3CB371";
                }
            case 3:
                {
                    return "#4682B4";
                }
            case 4:
                {
                    return "#9370DB";
                }
            case 5:
                {
                    return "#CD8F53";
                }
            case 6:
                {
                    return "#CD5C5C";
                }
            case 7:
                {
                    return "#CD5C5C"; //A02020
                }
            default:
                {
                    return "default";
                }
        }
    }
    public static int GetItemLevel(string itemid, DatabaseService databaseService)
    {
        if (IsPresetEquipment(itemid))
        {
            return GetEquipmentLevel(itemid, databaseService);
        }
        else if (IsArmor(itemid, databaseService))
        {
            return GetArmorLevel(itemid, databaseService);
        }
        else if (IsAmmo(itemid, databaseService) || IsAmmoBox(itemid, databaseService))
        {
            var pend = GetAmmoPendt(itemid, databaseService);
            return GetAmmoLevel(pend);
        }
        else if (IsContainerVestItem(itemid))
        {
            var size = GetContainerSize(itemid, databaseService);
            return GetVestLevelBySize(size);
        }
        else if (IsBackpackItem(itemid))
        {
            var size = GetContainerSize(itemid, databaseService);
            return GetBackpackLevelBySize(size);
        }
        else
        {
            return GetItemLevelByPrice(itemid, databaseService);
        }
        return 0;
    }
    public static int GetItemLevelByPrice(string itemid, DatabaseService databaseService)
    {
        PresetPriceMap.TryGetValue(itemid, out var value);
        var price = value;
        if (price >= ModConfig.Price.PriceLevel7)
        {
            return 7;
        }
        else if (price >= ModConfig.Price.PriceLevel6)
        {
            return 6;
        }
        else if (price >= ModConfig.Price.PriceLevel5)
        {
            return 5;
        }
        else if (price >= ModConfig.Price.PriceLevel4)
        {
            return 4;
        }
        else if (price >= ModConfig.Price.PriceLevel3)
        {
            return 3;
        }
        else if (price >= ModConfig.Price.PriceLevel2)
        {
            return 2;
        }
        else if (price >= ModConfig.Price.PriceLevel1)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
    public static bool IsNotContainerOrSecurity(string itemid)
    {
        if (IsHandbookTagItem(itemid, "5b5f6fa186f77409407a7eb7") || IsHandbookTagItem(itemid, "5b5f6fd286f774093f2ecf0d"))
        {
            return false;
        }
        return true;
    }
    public static bool IsNotSpecialEquipment(string itemid)
    {
        if (IsHandbookTagItem(itemid, "5b47574386f77428ca22b345"))
        {
            return false;
        }
        return true;
    }
    public static bool IsNotInBlackList(string itemid)
    {
        var blacklist = new List<string>
        {
            "5449016a4bdc2d6f028b456f",
            "569668774bdc2da2298b4568",
            "5696686a4bdc2da3298b456a",
            "5d235b4d86f7742e017bc88a",
            "c16f2525a89ab380719d9d15",
            "1f1850e09a36e2674e71e333",
            "1d5af804e4f8c4b0dedf22c1",
            "82d2da9e6a494c7cb99f1f77",
            "8a18dde8136f976d9970a49f",
            "e92c010793abe0496fdd1443",
            "d52b28adeca207f5365ec67c",
            "3fe5e88c9150197e28914674",
            "fbf2366e3b50894243fc54cb"
        };
        if (blacklist.Contains(itemid))
        {
            return false;
        }
        return true;
    }
    public static bool IsNotWeapon(string itemid)
    {
        if (
            IsHandbookTagItem(itemid, "5b5f78e986f77447ed5636b1")
            || IsHandbookTagItem(itemid, "5b5f78fc86f77409407a7f90")
            || IsHandbookTagItem(itemid, "5b5f791486f774093f2ed3be")
            || IsHandbookTagItem(itemid, "5b5f792486f77447ed5636b3")
            || IsHandbookTagItem(itemid, "5b5f794b86f77409407a7f92")
            || IsHandbookTagItem(itemid, "5b5f796a86f774093f2ed3c0")
            || IsHandbookTagItem(itemid, "5b5f798886f77447ed5636b5")
            || IsHandbookTagItem(itemid, "5b5f79a486f77409407a7f94")
            || IsHandbookTagItem(itemid, "5b5f79d186f774093f2ed3c2")
            || IsHandbookTagItem(itemid, "5b5f79eb86f77447ed5636b7")
            || IsHandbookTagItem(itemid, "5b5f7a0886f77409407a7f96")
            || IsHandbookTagItem(itemid, "5b5f7a2386f774093f2ed3c4")
            )
        {
            return false;
        }
        return true;
    }
    public static bool IsContainerVestItem(string itemid)
    {
        if (IsHandbookTagItem(itemid, "5b5f6f8786f77447ed563642"))
        {
            return true;
        }
        return false;
    }
    public static bool IsBackpackItem(string itemid)
    {
        if (IsHandbookTagItem(itemid, "5b5f6f6c86f774093f2ecf0b"))
        {
            return true;
        }
        return false;
    }
    public static int GetBackpackLevelBySize(int size)
    {
        if (size >= 35)
        {
            return 6;
        }
        else if (size >= 30)
        {
            return 5;
        }
        else if (size >= 25)
        {
            return 4;
        }
        else if (size >= 16)
        {
            return 3;
        }
        else if (size >= 12)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
    public static int GetVestLevelBySize(int size)
    {
        if (size >= 20)
        {
            return 5;
        }
        else if (size >= 16)
        {
            return 4;
        }
        else if (size >= 12)
        {
            return 3;
        }
        else if (size >= 8)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
    public static int GetContainerSize(string itemid, DatabaseService databaseService)
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        var size = 0;
        foreach (var grid in item.Properties.Grids)
        {
            var gridsize = grid.Properties.CellsH * grid.Properties.CellsV;
            if (gridsize != null)
            {
                size += (int)gridsize;
            }
        }
        return size;
    }
    public static int GetEquipmentLevel(string itemid, DatabaseService databaseService)
    {
        var level = 0;
        PresetMap.TryGetValue(itemid, out var preset);
        if (preset != null && preset.Count > 0)
        {
            foreach (var item in preset)
            {
                var itemlevel = GetArmorLevel(item.Template, databaseService);
                if (itemlevel > level)
                {
                    level = itemlevel;
                }
            }
            return level;
        }
        return 0;
    }
    public static int GetArmorLevel(string itemid, DatabaseService databaseService)
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        var level = item?.Properties?.ArmorClass;
        return level ?? 1;
    }
    public static int GetAmmoPendt(string itemid, DatabaseService databaseService)
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        if (IsAmmo(itemid, databaseService))
        {
            return (int)item.Properties.PenetrationPower;
        }
        else if (IsAmmoBox(itemid, databaseService))
        {
            var ammo = item.Properties?.StackSlots?.ToList()?[0]?.Properties?.Filters?.ToList()?[0]?.Filter?.ToList()[0];
            if (ammo != null)
            {
                var ammoitem = ItemUtils.GetItem(ammo, databaseService);
                if (ammoitem != null)
                {
                    return (int)ammoitem?.Properties?.PenetrationPower;
                }
            }
        }
        return 0;
    }
    public static int GetAmmoLevel(int pent)
    {
        if (pent >= 60)
        {
            return 6;
        }
        else if (pent >= 50)
        {
            return 5;
        }
        else if (pent >= 40)
        {
            return 4;
        }
        else if (pent >= 30)
        {
            return 3;
        }
        else if (pent >= 20)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
    public static bool IsPresetEquipment(string itemid)
    {
        if (
            IsHandbookTagItem(itemid, "5b47574386f77428ca22b330")
            || IsHandbookTagItem(itemid, "5b5f701386f774093f2ecf0f")
            || IsHandbookTagItem(itemid, "5b5f6f8786f77447ed563642")
            || IsHandbookTagItem(itemid, "5b47574386f77428ca22b32f")
            )
        {
            PresetMap.TryGetValue(itemid, out List<Item> preset);
            if (preset != null && preset.Count > 0)
            {
                return true;
            }
        }
        return false;
    }
    public static bool IsHandbookTagItem(string itemid, string handbooktag)
    {
        HandbookTagMap.TryGetValue(itemid, out string tag);
        if (tag != null && tag == handbooktag)
        {
            return true;
        }
        return false;
    }
    public static AmmoInfo? GetAmmoInfo(string itemid, DatabaseService databaseService)
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        if (IsAmmo(itemid, databaseService))
        {
            return new AmmoInfo
            {
                Pent = (int)item.Properties.PenetrationPower,
                Damage = (int)item.Properties.Damage,
                ArmorDamage = (int)item.Properties.ArmorDamage,
                BulletCount = (int)item.Properties.ProjectileCount,
                ColorString = GetAmmoColor((int)item.Properties.PenetrationPower)
            };
        }
        else if (IsAmmoBox(itemid, databaseService))
        {
            var ammo = item.Properties?.StackSlots?.ToList()?[0]?.Properties?.Filters?.ToList()?[0]?.Filter?.ToList()[0];
            if (ammo != null)
            {
                var ammoitem = ItemUtils.GetItem(ammo, databaseService);
                if (ammoitem != null)
                {
                    return new AmmoInfo
                    {
                        Pent = (int)ammoitem?.Properties?.PenetrationPower,
                        Damage = (int)ammoitem.Properties.Damage,
                        ArmorDamage = (int)ammoitem.Properties.ArmorDamage,
                        BulletCount = (int)ammoitem.Properties.ProjectileCount,
                        ColorString = GetAmmoColor((int)ammoitem.Properties.PenetrationPower)
                    };
                }
            }
        }
        return null;
    }
    public static string GetAmmoColor(int pent)
    {
        if (pent >= 60)
        {
            return "#AmmoColor6";
        }
        else if (pent >= 50)
        {
            return "#AmmoColor5";
        }
        else if (pent >= 40)
        {
            return "#AmmoColor4";
        }
        else if (pent >= 30)
        {
            return "#AmmoColor3";
        }
        else if (pent >= 20)
        {
            return "#AmmoColor2";
        }
        else
        {
            return "#AmmoColor1";
        }
    }
    public static string GetAmmoDataString(AmmoInfo ammoinfo, bool useeng)
    {
        if (ammoinfo != null)
        {
            if (useeng)
            {
                return $"""
                    <color=#CommonColor>Damage: </color><color=#RedsColor>{ammoinfo?.Damage}</color>
                    <color=#CommonColor>Pendent Power: </color><color={ammoinfo?.ColorString}>{ammoinfo?.Pent}</color>
                    <color=#CommonColor>Bullet Count: {ammoinfo?.BulletCount}</color>
                    """;

            }
            else
            {
                return $"""
                    <color=#CommonColor>伤害: </color><color=#RedsColor>{ammoinfo?.Damage}</color>
                    <color=#CommonColor>穿透力度: </color><color={ammoinfo?.ColorString}>{ammoinfo?.Pent}</color>
                    <color=#CommonColor>弹丸数量: {ammoinfo?.BulletCount}</color>
                    """;
            }
        }
        return string.Empty;
    }
    public static string ConvertArmorMaterial(ArmorMaterial Material, bool useeng)
    {
        switch (Material)
        {
            case ArmorMaterial.Aluminium:
                {
                    return useeng ? Material.ToString() : "铝";
                }
            case ArmorMaterial.Aramid:
                {
                    return useeng ? Material.ToString() : "芳纶";
                }
            case ArmorMaterial.ArmoredSteel:
                {
                    return useeng ? Material.ToString() : "装甲钢";
                }
            case ArmorMaterial.Ceramic:
                {
                    return useeng ? Material.ToString() : "陶瓷";
                }
            case ArmorMaterial.Combined:
                {
                    return useeng ? Material.ToString() : "复合材料";
                }
            case ArmorMaterial.Glass:
                {
                    return useeng ? Material.ToString() : "玻璃纤维";
                }
            case ArmorMaterial.Titan:
                {
                    return useeng ? Material.ToString() : "钛";
                }
            case ArmorMaterial.UHMWPE:
                {
                    return useeng ? Material.ToString() : "超高分子量聚乙烯";
                }
            default:
                {
                    return "";
                }
        }
    }
    public static ArmorMaterial? GetArmorMaterial(string itemid, DatabaseService databaseService)
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        var Material = item?.Properties?.ArmorMaterial;
        if (Material != null)
        {
            return Material;
        }
        return null;
    }
    public static bool IsArmor(string itemid, DatabaseService databaseService)
    {
        var item = ItemUtils.GetItem(itemid, databaseService);
        if (item?.Properties?.ArmorClass != null && (int)item?.Properties?.ArmorClass > 0)
        {
            return true;
        }
        return false;
    }
    public static ArmorInfo GetArmorData(string itemid, DatabaseService databaseService)
    {
        if (IsArmor(itemid, databaseService))
        {
            var item = ItemUtils.GetItem(itemid, databaseService);
            var protect = (int)item.Properties.ArmorClass;
            return new ArmorInfo
            {
                Level = protect,
                Blunt = $"{((double)(item.Properties.BluntThroughput * 100)).ToString("F2")}%",
                MaxDurability = (int)item.Properties.MaxDurability,
                Material = (ArmorMaterial)item.Properties.ArmorMaterial,
                Weight = $"{item.Properties.Weight}kg",
                ColorString = GetArmorColor(protect)
            };
        }
        return null;
    }
    public static string GetArmorColor(int protectLevel)
    {
        if (protectLevel >= 6)
        {
            return "#AmmoColor6";
        }
        else if (protectLevel >= 5)
        {
            return "#AmmoColor5";
        }
        else if (protectLevel >= 4)
        {
            return "#AmmoColor4";
        }
        else if (protectLevel >= 3)
        {
            return "#AmmoColor3";
        }
        else if (protectLevel >= 2)
        {
            return "#AmmoColor2";
        }
        else
        {
            return "#AmmoColor1";
        }
    }
    public static string GetArmorDataString(ArmorInfo armorInfo, bool useeng)
    {
        if (armorInfo != null)
        {
            if (useeng)
            {
                return $"""
                    <color=#CommonColor>Material: {ConvertArmorMaterial(armorInfo.Material, useeng)}</color>
                    <color=#CommonColor>Weight: {armorInfo.Weight}</color>
                    <color=#CommonColor>Blunt Coefficient: </color><color=#RedsColor>{armorInfo.Blunt}</color>
                    <color=#CommonColor>Protect Level: </color><color={armorInfo.ColorString}>{armorInfo.Level}</color>
                    <color=#CommonColor>Max Durability: </color><color=#GreenColor>{armorInfo.MaxDurability}</color>
                    """;
            }
            else
            {
                return $"""
                    <color=#CommonColor>材质: {ConvertArmorMaterial(armorInfo.Material, useeng)}</color>
                    <color=#CommonColor>重量: {armorInfo.Weight}</color>
                    <color=#CommonColor>钝伤指数: </color><color=#RedsColor>{armorInfo.Blunt}</color>
                    <color=#CommonColor>防护等级: </color><color={armorInfo.ColorString}>{armorInfo.Level}</color>
                    <color=#CommonColor>最大耐久: </color><color=#GreenColor>{armorInfo.MaxDurability}</color>
                    """;
            }
        }
        return string.Empty;
    }
    public static string GetItemQuestHandoverString(string itemid, bool useeng)
    {
        ItemRequireMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Handover item requirement(FIR): \n" : "上交物品需求: \n";
        var lang = useeng ? "en" : "ch";
        if (info != null)
        {
            if (info.QuestList.Count > 0)
            {
                foreach (var item in info.QuestList)
                {
                    if (item.Type == "HandoverItem" && item.FindInRaid)
                    {
                        var require = useeng ?
                            $"{GetQuestName(item.QuestId, lang)} Need {item.Count}\n" :
                            $"{GetQuestName(item.QuestId, lang)}({GetLocale($"{item.TraderId} Nickname")})需求x{item.Count}\n";
                        result += require;
                    }
                }
                if (result != string.Empty)
                {
                    result = SetTextColor(result, "HandOverRaidColor");
                    result = $"<color=#CommonColor>{datahead}</color>" + result;
                }
            }
        }
        return result;
    }
    public static string GetItemQuestString(string itemid, bool useeng)
    {
        ItemRequireMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Handover item requirement: \n" : "交付物品需求: \n";
        var lang = useeng ? "en" : "ch";
        if (info != null)
        {
            if (info.QuestList.Count > 0)
            {
                //result += datahead;
                foreach (var item in info.QuestList)
                {
                    if (item.Type == "HandoverItem" && !item.FindInRaid)
                    {
                        var require = useeng ?
                            $"{GetQuestName(item.QuestId, lang)} Need {item.Count}\n" :
                            $"{GetQuestName(item.QuestId, lang)}({GetLocale($"{item.TraderId} Nickname")})需求x{item.Count}\n";
                        result += require;
                    }
                }
                if (result != string.Empty)
                {
                    result = SetTextColor(result, "HandOverColor");
                    result = $"<color=#CommonColor>{datahead}</color>" + result;
                }
            }
        }
        return result;
    }
    public static string GetItemLeaveString(string itemid, bool useeng)
    {
        ItemRequireMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Place item requirement: \n" : "安放物品需求: \n";
        var lang = useeng ? "en" : "ch";
        if (info != null)
        {
            if (info.QuestList.Count > 0)
            {
                //result += datahead;
                foreach (var item in info.QuestList)
                {
                    if (item.Type == "LeaveItemAtLocation")
                    {
                        var require = useeng ?
                            $"{GetQuestName(item.QuestId, lang)} Need {item.Count}\n" :
                            $"{GetQuestName(item.QuestId, lang)}({GetLocale($"{item.TraderId} Nickname")})需求x{item.Count}\n";
                        result += require;
                    }
                }
                if (result != string.Empty)
                {
                    result = SetTextColor(result, "LeaveColor");
                    result = $"<color=#CommonColor>{datahead}</color>" + result;
                }
            }
        }
        return result;
    }
    public static string GetItemAreaString(string itemid, bool useeng)
    {
        ItemRequireMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Area requirement: \n" : "藏身处需求: \n";
        var lang = useeng ? "en" : "ch";
        if (info != null)
        {
            if (info.AreaList.Count > 0)
            {
                //hideout_area_7_name
                //result += datahead;
                foreach (var item in info.AreaList)
                {
                    var areaname = GetLocale($"hideout_area_{(int)item.Type}_name", lang);
                    var text = useeng ?
                        $"{areaname} level {item.AreaLevel} need {item.Count}\n" :
                        $"{areaname}{item.AreaLevel}级需求x{item.Count}\n";
                    result += text;
                }
                if (result != string.Empty)
                {
                    result = SetTextColor(result, "HideoutColor");
                    result = $"<color=#CommonColor>{datahead}</color>" + result;
                }
            }
        }
        return result;
    }
    public static string GetItemProductString(string itemid, bool useeng)
    {
        ItemProductMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Product recipe: \n" : "制作配方: \n";
        var lang = useeng ? "en" : "ch";
        if (info != null)
        {
            foreach (var recipe in info.Recipe)
            {
                var locked = recipe.Locked;
                var areaname = GetLocale($"hideout_area_{(int)recipe.AreaType}_name", lang);
                var questtext = string.Empty;
                var recipetext = string.Empty;
                //var queststring = recipe.Locked ? GetQuestName(recipe.Quest) : string.Empty;
                //我草, 这里得先写questdata, 不然商人拿不到
                //搞定了
                var text = useeng ?
                    $"{areaname} level {recipe.AreaLevel}" :
                    $"{areaname}{recipe.AreaLevel}级一次产出{recipe.Count}个({FormatSecondTime(recipe.Time)})";
                result += text;
                foreach (var recipevalue in recipe.Recipe.Items)
                {
                    var itemtext = useeng ?
                        $"{GetItemName(recipevalue.Key, lang)} x{recipevalue.Value}, " :
                        $"{GetItemName(recipevalue.Key, lang)}x{recipevalue.Value}、";
                    recipetext += itemtext;
                }
                foreach (var recipevalue in recipe.Recipe.Tools)
                {
                    var itemtext = useeng ?
                        $"{GetItemName(recipevalue, lang)}(not consume), " :
                        $"{GetItemName(recipevalue, lang)}(不消耗)、";
                    recipetext += itemtext;
                }
                if (!string.IsNullOrEmpty(recipetext))
                {
                    recipetext = recipetext.TrimEnd(' ', ',', '、');
                }
                if (locked)
                {
                    QuestDataMap.TryGetValue(recipe.Quest, out QuestData questdata);
                    var traderstring = recipe.Locked ?
                        GetTraderName(questdata?.TraderId) :
                        string.Empty;
                    var questname = GetQuestName(recipe.Quest, lang);
                    questtext = useeng ?
                        $"(complete quest「{questname}」to unlock recipe)" :
                        $"(完成任务「{questname}({traderstring})」后解锁)";
                }
                result += $" ({recipetext}) ";
                result += questtext;
                result += "\n";
            }
            result = SetTextColor(result, "ProductColor");
            result = $"<color=#CommonColor>{datahead}</color>" + result;
        }
        return result;
    }
    public static string GetItemProductUseString(string itemid, bool useeng)
    {
        ItemProductUseMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Product use: \n" : "制作用途: \n";
        var lang = useeng ? "en" : "ch";
        if (info != null)
        {
            foreach (var recipe in info.Recipe)
            {
                var locked = recipe.Locked;
                var areaname = GetLocale($"hideout_area_{(int)recipe.AreaType}_name", lang);
                var questtext = string.Empty;
                var recipetext = string.Empty;
                //var queststring = recipe.Locked ? GetQuestName(recipe.Quest) : string.Empty;
                //我草, 这里得先写questdata, 不然商人拿不到
                //搞定了
                var text = useeng ?
                    $"{areaname} level {recipe.AreaLevel} product {GetItemName(recipe.Result, lang)}" :
                    $"{areaname}{recipe.AreaLevel}级制作{GetItemName(recipe.Result, lang)}(消耗{FormatSecondTime(recipe.Time)}，一次产出{recipe.Count}个)";
                result += text;
                foreach (var recipevalue in recipe.Recipe.Items)
                {
                    var itemtext = useeng ?
                        $"{GetItemName(recipevalue.Key, lang)} x{recipevalue.Value}, " :
                        $"{GetItemName(recipevalue.Key, lang)}x{recipevalue.Value}、";
                    recipetext += itemtext;
                }
                foreach (var recipevalue in recipe.Recipe.Tools)
                {
                    var itemtext = useeng ?
                        $"{GetItemName(recipevalue, lang)}(not consume), " :
                        $"{GetItemName(recipevalue, lang)}(不消耗)、";
                    recipetext += itemtext;
                }
                if (!string.IsNullOrEmpty(recipetext))
                {
                    recipetext = recipetext.TrimEnd(' ', ',', '、');
                }
                if (locked)
                {
                    QuestDataMap.TryGetValue(recipe.Quest, out QuestData questdata);
                    var traderstring = recipe.Locked ?
                        GetTraderName(questdata?.TraderId) :
                        string.Empty;
                    var questname = GetQuestName(recipe.Quest, lang);
                    questtext = useeng ?
                        $"(complete quest「{questname}」to unlock recipe)" :
                        $"(完成任务「{questname}({traderstring})」后解锁)";
                }
                result += $"(完整配方:{recipetext})";
                result += questtext;
                result += "\n";
            }
            result = SetTextColor(result, "ProductUseColor");
            result = $"<color=#CommonColor>{datahead}</color>" + result;
        }
        return result;
    }
    public static string GetItemTradeString(string itemid, bool useeng)
    {
        List<string> moneylist = new List<string>
        {
            "5449016a4bdc2d6f028b456f", //卢布
            "5696686a4bdc2da3298b456a", //美元
            "569668774bdc2da2298b4568", //欧元
            "5d235b4d86f7742e017bc88a", //GP币
            "6656560053eaaa7a23349c86"  //Lega徽章
        };
        TradeMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Product use: \n" : "交易来源: \n";
        var lang = useeng ? "en" : "ch";
        if (useeng) return result;
        if (info != null)
        {
            foreach (var recipe in info.Recipe)
            {
                if (recipe.TraderId != "579dc571d53a0658a154fbec")
                {
                    var needstring = string.Empty;
                    var recipestring = string.Empty;
                    var queststring = string.Empty;
                    var firstKey = recipe.Barter.Keys.FirstOrDefault();
                    var firstValue = recipe.Barter.Values.FirstOrDefault();
                    if (recipe.Barter.Count == 1
                        && firstKey != null
                        && moneylist.Contains(firstKey)
                        )
                    {
                        result += $"{GetTraderName(recipe.TraderId)}{recipe.TrustLevel}级花费{firstValue}{GetItemName(firstKey)}直接购买";
                    }
                    else
                    {
                        result += $"{GetTraderName(recipe.TraderId)}{recipe.TrustLevel}级兑换";
                        foreach (var barter in recipe.Barter)
                        {
                            recipestring += $"{GetItemName(barter.Key)}x{barter.Value}、";
                        }
                        recipestring = recipestring.TrimEnd('、');
                        result += $"(完整配方: {recipestring})";
                    }
                    if (recipe.IsLocked)
                    {
                        QuestDataMap.TryGetValue(recipe.QuestId, out QuestData questdata);
                        if (questdata != null)
                        {
                            if (recipe.QuestStage == EQuestStageType.Start)
                            {
                                queststring = $"(接取「{GetQuestName(recipe.QuestId)}({GetTraderName(questdata.TraderId)})」后解锁)";
                            }
                            else
                            {
                                queststring = $"(完成「{GetQuestName(recipe.QuestId)}({GetTraderName(questdata.TraderId)})」后解锁)";
                            }
                            result += queststring;
                        }
                    }
                    result += "\n";
                }
            }
            if (result != string.Empty)
            {
                result = SetTextColor(result, "TradeColor");
                result = $"<color=#CommonColor>{datahead}</color>" + result;
            }
        }
        return result;

    }
    public static string GetItemTradeUseString(string itemid, bool useeng)
    {
        List<string> moneylist = new List<string>
        {
            "5449016a4bdc2d6f028b456f", //卢布
            "5696686a4bdc2da3298b456a", //美元
            "569668774bdc2da2298b4568", //欧元
            "5d235b4d86f7742e017bc88a", //GP币
            "6656560053eaaa7a23349c86"  //Lega徽章
        };
        TradeUseMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Product use: \n" : "交易用途: \n";
        var lang = useeng ? "en" : "ch";
        if (useeng) return result;
        if (info != null)
        {
            foreach (var recipe in info.Recipe)
            {
                if (recipe.TraderId != "579dc571d53a0658a154fbec")
                {
                    var needstring = string.Empty;
                    var recipestring = string.Empty;
                    var queststring = string.Empty;
                    var firstKey = recipe.Barter.Keys.FirstOrDefault();
                    var firstValue = recipe.Barter.Values.FirstOrDefault();
                    result += $"{GetTraderName(recipe.TraderId)}{recipe.TrustLevel}级兑换{GetItemName(recipe.Id)}";
                    foreach (var barter in recipe.Barter)
                    {
                        recipestring += $"{GetItemName(barter.Key)}x{barter.Value}、";
                    }
                    recipestring = recipestring.TrimEnd('、');
                    result += $"(完整配方: {recipestring})";
                    if (recipe.IsLocked)
                    {
                        QuestDataMap.TryGetValue(recipe.QuestId, out QuestData questdata);
                        if (questdata != null)
                        {
                            if (recipe.QuestStage == EQuestStageType.Start)
                            {
                                queststring = $"(接取「{GetQuestName(recipe.QuestId)}({GetTraderName(questdata.TraderId)})」后解锁)";
                            }
                            else
                            {
                                queststring = $"(完成「{GetQuestName(recipe.QuestId)}({GetTraderName(questdata.TraderId)})」后解锁)";
                            }
                            result += queststring;
                        }
                    }
                    result += "\n";
                }
            }
            result = SetTextColor(result, "TradeUseColor");
            result = $"<color=#CommonColor>{datahead}</color>" + result;
        }
        return result;

    }
    public static string GetItemQuestRewardString(string itemid, bool useeng)
    {
        QuestRewardMap.TryGetValue(itemid, out var info);
        string result = string.Empty;
        var datahead = useeng ? "Product use: \n" : "获取途径: \n";
        var lang = useeng ? "en" : "ch";
        if (useeng) return result;
        if (info != null)
        {
            foreach (var recipe in info.Reward)
            {
                var cachestring = string.Empty;
                if (recipe.QuestStage == EQuestStageType.Start)
                {
                    cachestring = $"接取「{GetQuestName(recipe.QuestId)}({GetTraderName(recipe.TraderId)})」后可领取{recipe.Count}个";
                }
                if (recipe.QuestStage == EQuestStageType.Finish)
                {
                    cachestring = $"完成「{GetQuestName(recipe.QuestId)}({GetTraderName(recipe.TraderId)})」后可领取{recipe.Count}个";
                }
                result += $"{cachestring}\n";
            }
            if (result != string.Empty)
            {
                result = SetTextColor(result, "RewardColor");
                result = $"<color=#CommonColor>{datahead}</color>" + result;
            }
        }
        return result;

    }
    public static void SetQuestData(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var zhCNLang = databaseService.GetLocales().Global["ch"];
        var questlist = databaseService.GetQuests().Keys.ToList();
        var blacklist = new List<string>
        {
            "5c51aac186f77432ea65c552"
        };
        zhCNLang.AddTransformer(delegate (Dictionary<string, string> lang)
        {
            foreach (var key in questlist)
            {
                QuestDataMap.TryGetValue(key, out QuestData questData);
                if (questData != null && !blacklist.Contains(key))
                {
                    var levelstring = string.Empty;
                    var prequesthead = "前置任务: 完成";
                    var prequeststring = string.Empty;
                    var unlockquesthead = "后续任务: ";
                    var unlockqueststring = string.Empty;
                    var logicdata = questData.LogicData;
                    var result = string.Empty;
                    var cachename = GetOriginalQuestName(key);
                    if (ModConfig.Display.MainQuest)
                    {
                        if (questData.IsKappaPreQuest)
                        {
                            cachename = $"<color=#{ModConfig.Color.MainQuestColor}><b>{ModConfig.Tag.MainQuestTag}</b></color>{cachename}";
                        }
                        if (questData.IsLightkeeperPreQuest)
                        {
                            cachename = $"<color=#{ModConfig.Color.LightKeeperQuestColor}><b>{ModConfig.Tag.LightKeeperQuestTag}</b></color>{cachename}";
                        }
                    }
                    if (logicdata.Level > 0)
                    {
                        levelstring = $"等级需求: PMC等级达到{logicdata.Level}级，";//SetTextColor($"等级需求: PMC等级达到{logicdata.Level}级，", "66FF66");
                    }
                    foreach (var data in logicdata.PreQuestData)
                    {
                        prequeststring += $"「{GetOriginalQuestName(data.QuestId)}」({GetOriginalTraderName(data.TraderId)})、";
                    }
                    if (prequeststring != string.Empty)
                    {
                        prequeststring = prequesthead + prequeststring;
                    }
                    foreach (var data in logicdata.UnlockQuestData)
                    {
                        unlockqueststring += $"「{GetOriginalQuestName(data.QuestId)}」({GetOriginalTraderName(data.TraderId)})、";
                    }
                    if (unlockqueststring != string.Empty)
                    {
                        unlockqueststring = unlockquesthead + unlockqueststring;
                        unlockqueststring = unlockqueststring.TrimEnd('、') + "。";
                        unlockqueststring = SetTextColor(unlockqueststring, ModConfig.Color.UnlockQuestColor);
                    }
                    if (unlockqueststring != string.Empty)
                    {
                        if (prequeststring != string.Empty)
                        {
                            prequeststring = prequeststring.TrimEnd('、') + "，";
                            prequeststring = SetTextColor(prequeststring, ModConfig.Color.PreQuestColor);
                            if (levelstring != string.Empty)
                            {
                                levelstring = SetTextColor(levelstring, ModConfig.Color.QuestLevelColor);
                                result = $"{levelstring}{prequeststring}{unlockqueststring}";
                            }
                            else
                            {
                                result = $"{prequeststring}{unlockqueststring}";
                            }
                        }
                        else
                        {
                            if (levelstring != string.Empty)
                            {
                                levelstring = SetTextColor(levelstring, ModConfig.Color.QuestLevelColor);
                                result = $"{levelstring}{unlockqueststring}";
                            }
                            else
                            {
                                result = $"{unlockqueststring}";
                            }
                        }
                    }
                    else
                    {
                        if (prequeststring != string.Empty)
                        {
                            prequeststring = prequeststring.TrimEnd('、') + "。";
                            prequeststring = SetTextColor(prequeststring, ModConfig.Color.PreQuestColor);
                            if (levelstring != string.Empty)
                            {
                                levelstring = SetTextColor(levelstring, ModConfig.Color.QuestLevelColor);
                                result = $"{levelstring}{prequeststring}";
                            }
                            else
                            {
                                result = $"{prequeststring}";
                            }
                        }
                        else
                        {
                            if (levelstring != string.Empty)
                            {
                                levelstring = levelstring.TrimEnd('，') + "。";
                                levelstring = SetTextColor(levelstring, ModConfig.Color.QuestLevelColor);
                                result = $"{levelstring}";
                            }
                        }
                    }
                    if (result != string.Empty)
                    {
                        lang[$"{key} description"] = $"{result}\n{lang[$"{key} description"]}";
                    }
                    lang[$"{key} name"] = cachename;
                    //VulcanLog.Debug(GetQuestName(key), logger);
                    //VulcanLog.Debug(levelstring, logger);
                    //VulcanLog.Debug(logicdata.Level.ToString(), logger);
                }
            }
            return lang;
        });
    }
    public static void SetItemData(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var zhCNLang = databaseService.GetLocales().Global["ch"];
        var questlist = databaseService.GetQuests().Keys.ToList();
        var blacklist = new List<string>
        {
            "5c51aac186f77432ea65c552"
        };
        zhCNLang.AddTransformer(delegate (Dictionary<string, string> lang)
        {
            foreach (var item in ItemList)
            {
                var tag = GetItemTag(item, databaseService);
                var color = GetItemTagColor(item, databaseService);
                var namekey = $"{item} Name";
                var shortnamekey = $"{item} ShortName";
                var cachename = GetOriginalItemName(item);
                var cacheshortname = GetOriginalItemShortName(item);
                if (ModConfig.Display.Name)
                {
                    lang[namekey] = $"<color=#{color}>{cachename}</color>";
                    lang[shortnamekey] = $"<color=#{color}>{cacheshortname}</color>";
                }
                if (ModConfig.Display.ShowTagInName)
                {
                    lang[namekey] = $"<color=#{color}>{tag}</color>" + lang[namekey];
                }
            }
            return lang;
        });
    }
    public static string GetItemTag(string itemid, DatabaseService databaseService)
    {
        var level = GetItemLevel(itemid, databaseService);
        if (level == 7)
        {
            return ModConfig.Tag.TagLevel7;
        }
        else if (level == 6)
        {
            return ModConfig.Tag.TagLevel6;
        }
        else if (level == 5)
        {
            return ModConfig.Tag.TagLevel5;
        }
        else if (level == 4)
        {
            return ModConfig.Tag.TagLevel4;
        }
        else if (level == 3)
        {
            return ModConfig.Tag.TagLevel3;
        }
        else if (level == 2)
        {
            return ModConfig.Tag.TagLevel2;
        }
        else if (level == 1)
        {
            return ModConfig.Tag.TagLevel1;
        }
        else
        {
            return ModConfig.Tag.TagLevel0;
        }
    }
    public static string GetItemTagColor(string itemid, DatabaseService databaseService)
    {
        var level = GetItemLevel(itemid, databaseService);
        if (level == 7)
        {
            return ModConfig.Color.ColorLevel7;
        }
        else if (level == 6)
        {
            return ModConfig.Color.ColorLevel6;
        }
        else if (level == 5)
        {
            return ModConfig.Color.ColorLevel5;
        }
        else if (level == 4)
        {
            return ModConfig.Color.ColorLevel4;
        }
        else if (level == 3)
        {
            return ModConfig.Color.ColorLevel3;
        }
        else if (level == 2)
        {
            return ModConfig.Color.ColorLevel2;
        }
        else if (level == 1)
        {
            return ModConfig.Color.ColorLevel1;
        }
        else
        {
            return ModConfig.Color.ColorLevel0;
        }
    }
    public static string SetTextColor(string text, string Color)
    {
        return $"<color=#{Color}><b>{text}</b></color>";
    }
    public static string FormatTime(long milliseconds)
    {
        // 计算小时、分钟、秒和剩余的毫秒
        int hours = (int)(milliseconds / (1000 * 60 * 60));
        int minutes = (int)((milliseconds % (1000 * 60 * 60)) / (1000 * 60));
        int seconds = (int)((milliseconds % (1000 * 60)) / 1000);
        int millisecondsRemaining = (int)(milliseconds % 1000);
        // 将毫秒转换为两位小数
        string millisecondsFormatted = (millisecondsRemaining / 1000.0).ToString("0.00").Substring(2);
        string result = "";
        if (hours > 0)
        {
            result += $"{hours}小时";
        }
        if (minutes > 0)
        {
            if (minutes >= 10)
            {
                result += $"{minutes}分";
            }
            else
            {
                if (hours > 0)
                {
                    result += $"0{minutes}分";
                }
                else
                {
                    result += $"{minutes}分";
                }
            }
        }
        if (seconds > 0)
        {
            if (seconds >= 10)
            {
                result += $"{seconds}.{millisecondsFormatted}秒";
            }
            else
            {
                if (minutes > 0)
                {
                    result += $"0{seconds}.{millisecondsFormatted}秒";
                }
                else
                {
                    result += $"{seconds}.{millisecondsFormatted}秒";
                }
            }
        }
        if (seconds == 0)
        {
            result += $"0.{millisecondsFormatted}秒";
        }

        return result.Trim();
    }
    public static string FormatSecondTime(long seconds)
    {
        // 计算小时、分钟和秒
        int hours = (int)(seconds / 3600); // 转换为小时
        int minutes = (int)((seconds % 3600) / 60); // 转换为分钟
        int remainingSeconds = (int)(seconds % 60); // 计算剩余的秒数
        string result = "";
        // 如果有小时数，添加小时
        if (hours > 0)
        {
            result += $"{hours}小时";
        }
        // 如果有分钟数，处理格式
        if (minutes > 0)
        {
            if (minutes >= 10)
            {
                result += $"{minutes}分";
            }
            else
            {
                if (hours > 0)
                {
                    result += $"0{minutes}分";
                }
                else
                {
                    result += $"{minutes}分";
                }
            }
        }
        // 如果有秒数，处理格式
        if (remainingSeconds > 0)
        {
            if (remainingSeconds >= 10)
            {
                result += $"{remainingSeconds}秒";
            }
            else
            {
                if (minutes > 0)
                {
                    result += $"0{remainingSeconds}秒";
                }
                else
                {
                    result += $"{remainingSeconds}秒";
                }
            }
        }
        return result.Trim();
    }
}