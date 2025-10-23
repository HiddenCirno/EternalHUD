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
    public string CopyTip { get; set; }
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
    public QuestFinishData FinishData { get; set; }
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
    public ProductRecipe Recipe { get; set; }
}
public class ProductRecipe
{
    public Dictionary<string, int> Items { get; set; }
    public List<string> Tools { get; set; }
}
