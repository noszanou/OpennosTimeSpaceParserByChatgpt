using ScriptedInstanceModel.Events;
using ScriptedInstanceModel.Objects;
using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Models.ScriptedInstance
{
    [Serializable]
    public class Globals
    {
        #region Properties

        public Item[] DrawItems { get; set; }

        public FamExp FamExp { get; set; }

        public FamMission FamMission { get; set; }

        public Item[] GiftItems { get; set; }

        public Gold Gold { get; set; }

        public Id Id { get; set; }

        public Bool IsIndividual { get; set; }

        public DailyEntries DailyEntries { get; set; }

        public Label Label { get; set; }

        public GiantTeam GiantTeam { get; set; }

        public Name Name { get; set; }

        public Level LevelMaximum { get; set; }

        public Level LevelMinimum { get; set; }

        [XmlElement("HeroLevelMinimum")]
        public HeroLevel HeroLevelMinimum { get; set; }

        [XmlElement("HeroLevelMaximum")]
        public HeroLevel HeroLevelMaximum { get; set; }

        public Lives Lives { get; set; }

        public Reputation Reputation { get; set; }

        public Item[] RequiredItems { get; set; }

        public Item[] SpecialItems { get; set; }

        public StartPosition StartX { get; set; }

        public StartPosition StartY { get; set; }

        public SpNeeded SpNeeded { get; set; }

        #endregion Properties
    }
}