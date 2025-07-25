using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class Id
    {
        #region Properties

        [XmlAttribute]
        public short Value { get; set; }

        #endregion Properties
    }
}