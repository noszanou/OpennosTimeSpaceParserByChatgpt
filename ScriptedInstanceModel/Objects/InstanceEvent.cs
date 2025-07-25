using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Objects
{
    [Serializable]
    public class InstanceEvent
    {
        #region Properties

        [XmlElement]
        public CreateMap[] CreateMap { get; set; }

        #endregion Properties
    }
}