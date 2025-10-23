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
namespace EternalHUD;
public class HUDUtils
{
    public static List<NameInfo> ClientCache = new List<NameInfo>();
    public static Dictionary<string, int> PriceMap = new Dictionary<string, int>();
    public static Dictionary<string, List<Item>> PresetMap = new Dictionary<string, List<Item>>();
    public static Dictionary<string, Dictionary<string, string>> LocaleMap = new Dictionary<string, Dictionary<string, string>>();
    public static Dictionary<string, ItemRequireData> ItemRequireMap = new Dictionary<string, ItemRequireData>();
    public static Dictionary<string, ProductMapData> ItemProductMap = new Dictionary<string, ProductMapData>();
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
        var minprice = PriceMap[itemid];
        if ((bool)item?.Properties?.CanSellOnRagfair)
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
                    price += PriceMap[items.Template];
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
        var mongoid = (MongoId)itemid;
        var priceTable = databaseService.GetPrices();
        var handbook = databaseService.GetHandbook().Items;
        //var ragfairPrice = offers.Min;
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
                return (int)(handbookdata.Price * 0.6);
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
    public static void GenerateQuestDataMap(DatabaseService databaseService, ISptLogger<VulcanCore.VulcanCore> logger)
    {
        var areas = databaseService.GetHideout().Areas;
        var quests = databaseService.GetQuests().Values.ToList();
        var items = databaseService.GetItems().Values.ToList();
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
                                    questid = (string)item.QuestId;
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
    public static bool IsAmmo(string itemid, DatabaseService databaseService)
    {
        return ItemUtils.GetItem(itemid, databaseService).Parent == (MongoId)"5485a8684bdc2da71d8b4567" ? true : false;
    }
    public static bool IsAmmoBox(string itemid, DatabaseService databaseService)
    {
        return ItemUtils.GetItemRagfairTag(itemid, databaseService) == (MongoId)"5b47574386f77428ca22b33c" ? true : false;
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
                    result = setTextColor(result, "HandOverRaidColor");
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
                    result = setTextColor(result, "HandOverColor");
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
                    result = setTextColor(result, "LeaveColor");
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
                    result = setTextColor(result, "HideoutColor");
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
                //var queststring = recipe.Locked ? 
                //我草, 这里得先写questdata, 不然商人拿不到
                var text = useeng ?
                    $"{areaname} level {recipe.AreaLevel}" :
                    $"{areaname}{recipe.AreaLevel}级";
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
                    var questname = GetQuestName(recipe.Quest, lang);
                    questtext = useeng ?
                        $"(complete quest「{questname}」to unlock recipe)" :
                        $"(完成任务「{questname}」后解锁)";
                }
                result += $" ({recipetext}) ";
                result += questtext;
                result += "\n";
            }
            result = setTextColor(result, "ProductColor");
            result = $"<color=#CommonColor>{datahead}</color>" + result;
        }
        return result;
    }
    public static string setTextColor(string text, string Color)
    {
        return $"<color=#{Color}><b>{text}</b></color>";
    }
}