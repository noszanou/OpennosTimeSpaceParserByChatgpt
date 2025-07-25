using ScriptedInstanceModel.Events.Quest;
using ScriptedInstanceModel.Objects.Quest;
using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Models.Quest
{
    [XmlRoot("Definition"), Serializable]
    public class QuestModel
    {
        public QuestGiver QuestGiver { get; set; }

        public short QuestDataVNum { get; set; }

        public short QuestGoalType { get; set; }

        public Script Script { get; set; }

        public Reward Reward { get; set; }

        public KillObjective[] KillObjectives { get; set; }

        public LootObjective[] LootObjectives { get; set; }

        public WalkObjective WalkObjective { get; set; }
    }
}