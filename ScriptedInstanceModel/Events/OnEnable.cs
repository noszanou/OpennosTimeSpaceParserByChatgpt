using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Events
{
    [Serializable]
    public class OnEnable
    {
        #region Properties

        [XmlElement]
        public Teleport Teleport { get; set; }

        [XmlElement]
        public NpcDialog[] NpcDialog { get; set; }

        #endregion Properties
    }
}