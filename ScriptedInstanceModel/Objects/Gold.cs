using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class Gold
    {
        #region Properties

        [XmlAttribute]
        public long Value { get; set; }

        #endregion Properties
    }
}