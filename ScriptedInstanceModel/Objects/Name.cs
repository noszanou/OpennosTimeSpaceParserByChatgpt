using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class Name
    {
        #region Properties

        [XmlAttribute]
        public string Value { get; set; }

        #endregion Properties
    }
}