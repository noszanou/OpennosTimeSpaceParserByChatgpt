using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class Level
    {
        #region Properties

        [XmlAttribute]
        public byte Value { get; set; }

        #endregion Properties
    }
}