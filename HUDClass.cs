using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using VulcanCore;

namespace EternalHUD;

public class NameInfo
{
    public string Name { get; set; }
    public string EName { get; set; }
    public string RealName { get; set; }
    public string TrueName { get; set; }
    public string StringName { get; set; }
    public string ID { get; set; }
    public string RealID { get; set; }
    public string AmmoString { get; set; }
    public string ArmorString { get; set; }
    public string ArmorString2 { get; set; }
    public string QuestString { get; set; }
    public string QuestHandoverString { get; set; }
    public string QuestLeaveString { get; set; }
    public string HideoutString { get; set; }
    public string TradeString { get; set; }
    public string RewardString { get; set; }
    public string ProductString { get; set; }
    public string RagfairString { get; set; }
    public string Tag { get; set; }
    public string PricesString { get; set; }
    public bool CanSell { get; set; }
    public int SellPrice { get; set; }
    public int Level { get; set; }
    public string CopyTip { get; set; }
    public bool ChangeTime { get; set; }
}
public class AmmoInfo
{
    public int Pent { get; set; }
    public int Damage { get; set; }
    public int ArmorDamage { get; set; }
    public int BulletCount { get; set; }
    public string ColorString { get; set; }
}
public class ArmorInfo
{
    public int Level { get; set; }
    public string Blunt { get; set; }
    public int MaxDurability { get; set; }
    public ArmorMaterial Material { get; set; }
    public string Weight { get; set; }
    public string ColorString { get; set; }
}
public class HideoutData
{
    public string Id { get; set; }
    public int Type { get; set; }
    public List<HideoutLevelData> Level { get; set; }
}
public class HideoutLevelData
{
    public string AreaId { get; set; }
    public string AreaName { get; set; }
    public int AreaLevel { get; set; }
    public List<HideoutRequirementData> Requirement { get; set; }
}
public class HideoutRequirementData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
}
public class QuestData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string TraderId { get; set; }
    public string TraderName { get; set; }
    public bool IsKappaPreQuest { get; set; }
    public bool IsLightkeeperPreQuest { get; set; }
    public QuestLogic LogicData { get; set; }
    //public QuestFinishData FinishData { get; set; }
}
public class QuestLogic
{
    public int Level { get; set; }
    public List<QuestLogicData> PreQuestData { get; set; }
    public List<QuestLogicData> UnlockQuestData { get; set; }
}
public class QuestLogicData
{
    public string QuestId { get; set; }
    public string QuestName { get; set; }
    public string TraderId { get; set; }
    public string TraderName { get; set; }
}
public class QuestFinishData
{
    public List<QuestItemData> HandoverData { get; set; }
    public List<QuestItemData> PlacementData { get; set; }
    public List<QuestItemData> ModifyData { get; set; }
}
public class QuestItemData
{
    public string QuestId { get; set; }
    public string QuestName { get; set; }
    public string TraderId { get; set; }
    public string TraderName { get; set; }
    public bool FindInRaid { get; set; }
    public int Count { get; set; }
    public string Type { get; set; }
}
public class HideoutItemData
{
    public HideoutAreas Type { get; set; }
    public int AreaLevel { get; set; }
    public int Count { get; set; }
}
public class ItemRequireData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsQuestItem { get; set; }
    public List<QuestItemData> QuestList { get; set; }
    public List<HideoutItemData> AreaList { get; set; }
}
public class ProductMapData
{
    public string Name { set; get; }
    public List<ProductRecipeData> Recipe { get; set; }
}
public class ProductRecipeData
{
    public string Name { get; set; }
    public int Count { get; set; }
    public int Time { get; set; }
    public bool Locked { get; set; }
    public string Quest { get; set; }
    public HideoutAreas AreaType { get; set; }
    public string AreaName { get; set; }
    public int AreaLevel { get; set; }
    public string Result { get; set; }
    public ProductRecipe Recipe { get; set; }
}
public class ProductRecipe
{
    public Dictionary<string, int> Items { get; set; }
    public List<string> Tools { get; set; }
}
public class TradeMapData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<TradeData> Recipe { get; set; }
}
public class TradeData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Dictionary<string, double> Barter { get; set; }
    public int TrustLevel { get; set; }
    public string TraderId { get; set; }
    public bool IsLocked { get; set; }
    public string QuestId { get; set; }
    public EQuestStageType QuestStage { get; set; }
}
public class QuestAssortData
{
    public Dictionary<string, string> Start { get; set; }
    public Dictionary<string, string> Finish { get; set; }
}
public class QuestRewardMapData
{
    public string Id { set; get; }
    public string Name { set; get; }
    public List<QuestRewardData> Reward { get; set; }
}
public class QuestRewardData
{
    public string QuestId { set; get; }
    public string QuestName { set; get; }
    public string TraderId { set; get; }
    public string TraderName { set; get; }
    public int Count {  get; set; }
    public EQuestStageType QuestStage { get; set; }
}
public class Config
{
    // 物品信息显示总开关
    public bool ShowItemInfo { get; set; }

    // 任务信息显示总开关
    public bool ShowQuestInfo { get; set; }

    // 自动检视功能开关
    public bool AutoExamine { get; set; }

    public ColorConfig Color { get; set; }
    public TagConfig Tag { get; set; }
    public PriceConfig Price { get; set; }
    public DisplayConfig Display { get; set; }

    // 可以用于存放其他没有被定义的链接
    public Dictionary<string, object> Link { get; set; }

    // 黑名单列表
    public List<string> ItemBlackList { get; set; }
    public List<string> LootBlackList { get; set; }
    public List<string> QuestBlackList { get; set; }
}

public class ColorConfig
{
    // 物品信息显示的颜色, 分别对应八个等级
    public string ColorLevel0 { get; set; }
    public string ColorLevel1 { get; set; }
    public string ColorLevel2 { get; set; }
    public string ColorLevel3 { get; set; }
    public string ColorLevel4 { get; set; }
    public string ColorLevel5 { get; set; }
    public string ColorLevel6 { get; set; }
    public string ColorLevel7 { get; set; }
    public string MainQuestColor { get; set; }
    public string LightKeeperQuestColor { get; set; }
    public string PreQuestColor { get; set; }
    public string UnlockQuestColor { get; set; }
    public string QuestLevelColor { get; set; }
}

public class TagConfig
{
    // 物品品质和任务高亮的前缀标签
    public string TagLevel0 { get; set; }
    public string TagLevel1 { get; set; }
    public string TagLevel2 { get; set; }
    public string TagLevel3 { get; set; }
    public string TagLevel4 { get; set; }
    public string TagLevel5 { get; set; }
    public string TagLevel6 { get; set; }
    public string TagLevel7 { get; set; }

    // 主线任务标签
    public string MainQuestTag { get; set; }

    // 灯塔之匙标签
    public string LightKeeperQuestTag { get; set; }
}

public class PriceConfig
{
    // 物品价值判断的界限
    public int PriceLevel1 { get; set; }
    public int PriceLevel2 { get; set; }
    public int PriceLevel3 { get; set; }
    public int PriceLevel4 { get; set; }
    public int PriceLevel5 { get; set; }
    public int PriceLevel6 { get; set; }
    public int PriceLevel7 { get; set; }
}

public class DisplayConfig
{
    // 是否修改物品名称
    public bool Name { get; set; }

    // 是否显示物品品质标签
    public bool ShowTagInName { get; set; }

    // 是否标记3x4以及灯塔商人任务前置
    public bool MainQuest { get; set; }

    // 是否显示交易相关数据
    public bool ItemBGColor { get; set; }
    public bool UseVanillaBGColor { get; set; }
    public bool DogTagBGChange { get; set; }
    public string TranslateLanguage { get; set; }
}
