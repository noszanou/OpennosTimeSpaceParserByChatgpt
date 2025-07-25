using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class Lives
    {
        #region Properties

        [XmlAttribute]
        public byte Value { get; set; }

        #endregion Properties
    }
}