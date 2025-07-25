using System.Xml.Serialization;

namespace NosTaleTimeSpaceParser.Models.XmlModels
{
    [XmlRoot("Definition")]
    public class ScriptedInstanceModel
    {
        [XmlElement("Globals")]
        public Globals Globals { get; set; } = new Globals();

        [XmlElement("InstanceEvents")]
        public InstanceEvents InstanceEvents { get; set; } = new InstanceEvents();
    }

    public class Globals
    {
        [XmlElement("Name")]
        public ValueAttribute Name { get; set; } = new ValueAttribute();

        [XmlElement("Label")]
        public ValueAttribute Label { get; set; } = new ValueAttribute();

        [XmlElement("LevelMinimum")]
        public ValueAttribute LevelMinimum { get; set; } = new ValueAttribute();

        [XmlElement("LevelMaximum")]
        public ValueAttribute LevelMaximum { get; set; } = new ValueAttribute();

        [XmlElement("Lives")]
        public ValueAttribute Lives { get; set; } = new ValueAttribute();

        [XmlElement("DrawItems")]
        public ItemCollection DrawItems { get; set; } = new ItemCollection();

        [XmlElement("SpecialItems")]
        public ItemCollection SpecialItems { get; set; } = new ItemCollection();

        [XmlElement("GiftItems")]
        public ItemCollection GiftItems { get; set; } = new ItemCollection();

        [XmlElement("Gold")]
        public ValueAttribute Gold { get; set; } = new ValueAttribute();

        [XmlElement("Reputation")]
        public ValueAttribute Reputation { get; set; } = new ValueAttribute();
    }

    public class ValueAttribute
    {
        [XmlAttribute("Value")]
        public string Value { get; set; } = string.Empty;
    }

    public class ItemCollection
    {
        [XmlElement("Item")]
        public List<ItemElement> Items { get; set; } = new List<ItemElement>();
    }

    public class ItemElement
    {
        [XmlAttribute("VNum")]
        public int VNum { get; set; }

        [XmlAttribute("Amount")]
        public int Amount { get; set; }
    }

    public class InstanceEvents
    {
        [XmlElement("CreateMap")]
        public List<CreateMap> CreateMaps { get; set; } = new List<CreateMap>();
    }

    public class CreateMap
    {
        [XmlAttribute("Map")]
        public int Map { get; set; }

        [XmlAttribute("VNum")]
        public int VNum { get; set; }

        [XmlAttribute("IndexX")]
        public int IndexX { get; set; }

        [XmlAttribute("IndexY")]
        public int IndexY { get; set; }

        [XmlElement("SpawnPortal")]
        public List<SpawnPortal> SpawnPortals { get; set; } = new List<SpawnPortal>();

        [XmlElement("OnCharacterDiscoveringMap")]
        public OnCharacterDiscoveringMap? OnCharacterDiscoveringMap { get; set; }

        [XmlElement("OnMoveOnMap")]
        public OnMoveOnMap? OnMoveOnMap { get; set; }

        [XmlElement("SpawnButton")]
        public List<SpawnButton> SpawnButtons { get; set; } = new List<SpawnButton>();
    }

    public class OnCharacterDiscoveringMap
    {
        [XmlElement("NpcDialog")]
        public List<ValueAttribute> NpcDialogs { get; set; } = new List<ValueAttribute>();

        [XmlElement("SendMessage")]
        public List<SendMessage> SendMessages { get; set; } = new List<SendMessage>();

        [XmlElement("SendPacket")]
        public List<ValueAttribute> SendPackets { get; set; } = new List<ValueAttribute>();

        [XmlElement("SummonNpc")]
        public List<SummonNpc> SummonNpcs { get; set; } = new List<SummonNpc>();

        [XmlElement("SummonMonster")]
        public List<SummonMonster> SummonMonsters { get; set; } = new List<SummonMonster>();

        [XmlElement("SpawnPortal")]
        public List<SpawnPortal> SpawnPortals { get; set; } = new List<SpawnPortal>();

        [XmlElement("SpawnButton")]
        public List<SpawnButton> SpawnButtons { get; set; } = new List<SpawnButton>();

        [XmlElement("GenerateMapClock")]
        public List<ValueAttribute> GenerateMapClocks { get; set; } = new List<ValueAttribute>();

        [XmlElement("GenerateClock")]
        public List<ValueAttribute> GenerateClocks { get; set; } = new List<ValueAttribute>();

        [XmlElement("StartClock")]
        public List<object> StartClocks { get; set; } = new List<object>();

        [XmlElement("StartMapClock")]
        public List<StartMapClock> StartMapClocks { get; set; } = new List<StartMapClock>();

        [XmlElement("StopClock")]
        public List<object> StopClocks { get; set; } = new List<object>();

        [XmlElement("StopMapClock")]
        public List<object> StopMapClocks { get; set; } = new List<object>();

        [XmlElement("OnMoveOnMap")]
        public OnMoveOnMap? OnMoveOnMap { get; set; }

        [XmlElement("OnMapClean")]
        public OnMapClean? OnMapClean { get; set; }

        [XmlElement("SetButtonLockers")]
        public List<object> SetButtonLockers { get; set; } = new List<object>();

        [XmlElement("SetMonsterLockers")]
        public List<object> SetMonsterLockers { get; set; } = new List<object>();

        [XmlElement("Wave")]
        public List<object> Waves { get; set; } = new List<object>();

        [XmlElement("RefreshRaidGoals")]
        public List<object> RefreshRaidGoals { get; set; } = new List<object>();

        [XmlElement("Effect")]
        public List<object> Effects { get; set; } = new List<object>();
    }

    public class OnMoveOnMap
    {
        [XmlElement("SendMessage")]
        public List<SendMessage> SendMessages { get; set; } = new List<SendMessage>();

        [XmlElement("SendPacket")]
        public List<ValueAttribute> SendPackets { get; set; } = new List<ValueAttribute>();

        [XmlElement("SummonMonster")]
        public List<SummonMonster> SummonMonsters { get; set; } = new List<SummonMonster>();

        [XmlElement("SummonNpc")]
        public List<SummonNpc> SummonNpcs { get; set; } = new List<SummonNpc>();

        [XmlElement("GenerateClock")]
        public List<ValueAttribute> GenerateClocks { get; set; } = new List<ValueAttribute>();

        [XmlElement("StartClock")]
        public List<object> StartClocks { get; set; } = new List<object>();

        [XmlElement("GenerateMapClock")]
        public List<ValueAttribute> GenerateMapClocks { get; set; } = new List<ValueAttribute>();

        [XmlElement("StartMapClock")]
        public List<StartMapClock> StartMapClocks { get; set; } = new List<StartMapClock>();

        [XmlElement("StopClock")]
        public List<object> StopClocks { get; set; } = new List<object>();

        [XmlElement("StopMapClock")]
        public List<object> StopMapClocks { get; set; } = new List<object>();

        [XmlElement("OnMapClean")]
        public OnMapClean? OnMapClean { get; set; }

        [XmlElement("RefreshRaidGoals")]
        public List<object> RefreshRaidGoals { get; set; } = new List<object>();

        [XmlElement("RemoveButtonLocker")]
        public List<object> RemoveButtonLockers { get; set; } = new List<object>();

        [XmlElement("RemoveMonsterLocker")]
        public List<object> RemoveMonsterLockers { get; set; } = new List<object>();

        [XmlElement("SetButtonLockers")]
        public List<object> SetButtonLockers { get; set; } = new List<object>();

        [XmlElement("SetMonsterLockers")]
        public List<object> SetMonsterLockers { get; set; } = new List<object>();

        [XmlElement("Wave")]
        public List<object> Waves { get; set; } = new List<object>();
    }

    public class StartMapClock
    {
        [XmlElement("OnStop")]
        public OnStop? OnStop { get; set; }

        [XmlElement("OnTimeout")]
        public OnTimeout? OnTimeout { get; set; }
    }

    public class OnStop
    {
        [XmlElement("ChangePortalType")]
        public List<ChangePortalType> ChangePortalTypes { get; set; } = new List<ChangePortalType>();

        [XmlElement("RefreshMapItems")]
        public List<object> RefreshMapItems { get; set; } = new List<object>();

        [XmlElement("SendPacket")]
        public List<ValueAttribute> SendPackets { get; set; } = new List<ValueAttribute>();

        [XmlElement("SendMessage")]
        public List<SendMessage> SendMessages { get; set; } = new List<SendMessage>();
    }

    public class OnTimeout
    {
        [XmlElement("End")]
        public List<EndElement> Ends { get; set; } = new List<EndElement>();
    }

    public class EndElement
    {
        [XmlAttribute("Type")]
        public int Type { get; set; }
    }

    public class SendMessage
    {
        [XmlAttribute("Value")]
        public string Value { get; set; } = string.Empty;

        [XmlAttribute("Type")]
        public int Type { get; set; }
    }

    public class SpawnPortal
    {
        [XmlAttribute("IdOnMap")]
        public int IdOnMap { get; set; }

        [XmlAttribute("PositionX")]
        public int PositionX { get; set; }

        [XmlAttribute("PositionY")]
        public int PositionY { get; set; }

        [XmlAttribute("Type")]
        public int Type { get; set; }

        [XmlAttribute("ToMap")]
        public int ToMap { get; set; }

        [XmlAttribute("ToX")]
        public int ToX { get; set; }

        [XmlAttribute("ToY")]
        public int ToY { get; set; }

        [XmlElement("OnTraversal")]
        public OnTraversal? OnTraversal { get; set; }
    }

    public class OnTraversal
    {
        [XmlElement("End")]
        public List<EndElement> Ends { get; set; } = new List<EndElement>();

        [XmlElement("NpcDialog")]
        public List<ValueAttribute> NpcDialogs { get; set; } = new List<ValueAttribute>();
    }

    public class SummonMonster
    {
        [XmlAttribute("VNum")]
        public int VNum { get; set; }

        [XmlAttribute("PositionX")]
        public int PositionX { get; set; }

        [XmlAttribute("PositionY")]
        public int PositionY { get; set; }

        [XmlAttribute("Move")]
        public bool Move { get; set; } = true;

        [XmlAttribute("IsBonus")]
        public bool IsBonus { get; set; }

        [XmlAttribute("IsHostile")]
        public bool IsHostile { get; set; }

        [XmlAttribute("IsTarget")]
        public bool IsTarget { get; set; }

        [XmlAttribute("IsBoss")]
        public bool IsBoss { get; set; }

        [XmlAttribute("IsMeteorite")]
        public bool IsMeteorite { get; set; }

        [XmlAttribute("Damage")]
        public int Damage { get; set; }

        [XmlAttribute("NoticeRange")]
        public int NoticeRange { get; set; }

        [XmlAttribute("HasDelay")]
        public int HasDelay { get; set; }

        [XmlElement("OnDeath")]
        public OnDeath? OnDeath { get; set; }

        [XmlElement("OnNoticing")]
        public OnNoticing? OnNoticing { get; set; }

        [XmlElement("Roam")]
        public Roam? Roam { get; set; }

        [XmlElement("SendMessage")]
        public List<SendMessage> SendMessages { get; set; } = new List<SendMessage>();

        [XmlElement("UseSkillOnDamage")]
        public List<UseSkillOnDamage> UseSkillOnDamages { get; set; } = new List<UseSkillOnDamage>();

        [XmlElement("Effect")]
        public Effect? Effect { get; set; }

        [XmlElement("RemoveAfter")]
        public RemoveAfter? RemoveAfter { get; set; }
    }

    public class SummonNpc
    {
        [XmlAttribute("VNum")]
        public int VNum { get; set; }

        [XmlAttribute("PositionX")]
        public int PositionX { get; set; }

        [XmlAttribute("PositionY")]
        public int PositionY { get; set; }

        [XmlAttribute("Move")]
        public bool Move { get; set; }

        [XmlAttribute("IsProtected")]
        public bool IsProtected { get; set; }

        [XmlElement("OnDeath")]
        public OnDeath? OnDeath { get; set; }
    }

    public class OnDeath
    {
        [XmlElement("SummonMonster")]
        public List<SummonMonster> SummonMonsters { get; set; } = new List<SummonMonster>();

        [XmlElement("SummonNpc")]
        public List<SummonNpc> SummonNpcs { get; set; } = new List<SummonNpc>();

        [XmlElement("ChangePortalType")]
        public List<ChangePortalType> ChangePortalTypes { get; set; } = new List<ChangePortalType>();

        [XmlElement("SendMessage")]
        public List<SendMessage> SendMessages { get; set; } = new List<SendMessage>();

        [XmlElement("SendPacket")]
        public List<ValueAttribute> SendPackets { get; set; } = new List<ValueAttribute>();

        [XmlElement("NpcDialog")]
        public List<ValueAttribute> NpcDialogs { get; set; } = new List<ValueAttribute>();

        [XmlElement("RefreshMapItems")]
        public List<object> RefreshMapItems { get; set; } = new List<object>();

        [XmlElement("RefreshRaidGoals")]
        public List<object> RefreshRaidGoals { get; set; } = new List<object>();

        [XmlElement("RemoveButtonLocker")]
        public List<object> RemoveButtonLockers { get; set; } = new List<object>();

        [XmlElement("RemoveMonsterLocker")]
        public List<object> RemoveMonsterLockers { get; set; } = new List<object>();

        [XmlElement("StopClock")]
        public List<object> StopClocks { get; set; } = new List<object>();

        [XmlElement("StopMapClock")]
        public List<object> StopMapClocks { get; set; } = new List<object>();

        [XmlElement("AddClockTime")]
        public List<AddClockTime> AddClockTimes { get; set; } = new List<AddClockTime>();

        [XmlElement("AddMapClockTime")]
        public List<AddClockTime> AddMapClockTimes { get; set; } = new List<AddClockTime>();

        [XmlElement("ThrowItem")]
        public List<ThrowItem> ThrowItems { get; set; } = new List<ThrowItem>();

        [XmlElement("Wave")]
        public List<object> Waves { get; set; } = new List<object>();

        [XmlElement("StopMapWaves")]
        public List<object> StopMapWaves { get; set; } = new List<object>();

        [XmlElement("ClearMapMonsters")]
        public List<object> ClearMapMonsters { get; set; } = new List<object>();

        [XmlElement("End")]
        public List<EndElement> Ends { get; set; } = new List<EndElement>();
    }

    public class ChangePortalType
    {
        [XmlAttribute("IdOnMap")]
        public int IdOnMap { get; set; }

        [XmlAttribute("Type")]
        public int Type { get; set; }
    }

    public class SpawnButton
    {
        [XmlAttribute("Id")]
        public int Id { get; set; }

        [XmlAttribute("PositionX")]
        public int PositionX { get; set; }

        [XmlAttribute("PositionY")]
        public int PositionY { get; set; }

        [XmlAttribute("VNumEnabled")]
        public int VNumEnabled { get; set; }

        [XmlAttribute("VNumDisabled")]
        public int VNumDisabled { get; set; }

        [XmlElement("OnFirstEnable")]
        public OnFirstEnable? OnFirstEnable { get; set; }

        [XmlElement("OnEnable")]
        public OnEnable? OnEnable { get; set; }

        [XmlElement("OnDisable")]
        public OnDisable? OnDisable { get; set; }
    }

    public class OnFirstEnable
    {
        [XmlElement("SendMessage")]
        public List<SendMessage> SendMessages { get; set; } = new List<SendMessage>();

        [XmlElement("SummonMonster")]
        public List<SummonMonster> SummonMonsters { get; set; } = new List<SummonMonster>();

        [XmlElement("OnMapClean")]
        public OnMapClean? OnMapClean { get; set; }
    }

    public class OnMapClean
    {
        [XmlElement("ChangePortalType")]
        public List<ChangePortalType> ChangePortalTypes { get; set; } = new List<ChangePortalType>();

        [XmlElement("SendMessage")]
        public List<SendMessage> SendMessages { get; set; } = new List<SendMessage>();

        [XmlElement("SendPacket")]
        public List<ValueAttribute> SendPackets { get; set; } = new List<ValueAttribute>();

        [XmlElement("NpcDialog")]
        public List<ValueAttribute> NpcDialogs { get; set; } = new List<ValueAttribute>();

        [XmlElement("RefreshMapItems")]
        public List<object> RefreshMapItems { get; set; } = new List<object>();
    }

    public class OnNoticing { }
    public class Roam { }
    public class UseSkillOnDamage { }
    public class Effect { }
    public class RemoveAfter { }
    public class AddClockTime { }
    public class ThrowItem { }
    public class OnEnable { }
    public class OnDisable { }
}