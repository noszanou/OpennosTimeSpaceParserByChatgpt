using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Events
{
    [Serializable]
    public class Move
    {
        #region Properties

        [XmlElement]
        public Effect Effect { get; set; }

        [XmlElement]
        public OnTarget OnTarget { get; set; }

        [XmlAttribute]
        public short PositionX { get; set; }

        [XmlAttribute]
        public short PositionY { get; set; }

        #endregion Properties
    }
}