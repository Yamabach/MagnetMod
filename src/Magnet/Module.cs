using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Modding;
using Modding.Modules;
using Modding.Serialization;
using UnityEngine;

namespace MagnetSpace.Module
{
    [XmlRoot("MagnetModule")]
    [Reloadable]
    public class MagnetModule : BlockModule
    {
        /// <summary>
        /// ã…ÇÃà íu position of pole
        /// </summary>
        [XmlElement("PoleTransform")]
        [Reloadable]
        public TransformValues PoleTransform;

        /// <summary>
        /// é•ãCó  magnetic charge
        /// </summary>
        [XmlElement("Charge")]
        [Reloadable]
        public float Charge;

        #region visual property
        [XmlElement]
        [Reloadable]
        public MeshReference MeshNorth;
        [XmlElement]
        [Reloadable]
        public ResourceReference TextureNorth;
        [XmlElement]
        [Reloadable]
        public MeshReference MeshSouth;
        [XmlElement]
        [Reloadable]
        public ResourceReference TextureSouth;
        #endregion

        #region mappers
        [XmlElement("SliderChargeGain")]
        [Reloadable]
        public MSliderReference SliderChargeGain;

        #region ìdé•êŒ electromagnet
        [XmlElement("KeyMagnetize")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MKeyReference KeyMagnetize;
        [XmlElement("ToggleHoldToMagnetize")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MToggleReference ToggleHoldToMagnetize;
        #endregion
        #endregion
    }
}