using ScriptedInstanceModel.Objects;
using System;
using System.Xml.Serialization;

namespace ScriptedInstanceModel.Models.ScriptedInstance
{
    [XmlRoot("Definition"), Serializable]
    public class ScriptedInstanceModel
    {
        #region Properties

        public Globals Globals { get; set; }

        public InstanceEvent InstanceEvents { get; set; }

        #endregion Properties
    }
}