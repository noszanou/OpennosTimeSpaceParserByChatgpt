using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class Bool
    {
        #region Properties

        [XmlAttribute]
        public bool Value { get; set; }

        #endregion Properties
    }
}