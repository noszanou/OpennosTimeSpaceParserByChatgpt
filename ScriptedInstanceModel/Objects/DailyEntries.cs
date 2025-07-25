using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class DailyEntries
    {
        #region Properties

        [XmlAttribute]
        public short Value { get; set; }

        #endregion Properties
    }
}