using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class Reputation
    {
        #region Properties

        [XmlAttribute]
        public int Value { get; set; }

        #endregion Properties
    }
}