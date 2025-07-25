using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Events
{
    [Serializable]
    public class RemoveAfter
    {
        #region Properties

        [XmlAttribute]
        public short Value { get; set; }

        #endregion Properties
    }
}