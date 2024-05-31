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
        /// 極の位置 position of pole
        /// </summary>
        [XmlElement("PoleTransform")]
        [Reloadable]
        public TransformValues PoleTransform;

        /// <summary>
        /// 磁気量 magnetic charge
        /// </summary>
        [XmlElement("Charge")]
        [Reloadable]
        public float Charge;

        #region visual property
        [XmlElement("MeshOff")]
        [Reloadable]
        public MeshReference MeshOff;
        [XmlElement("TextureOff")]
        [Reloadable]
        public ResourceReference TextureOff;
        [XmlElement("MeshNorth")]
        [Reloadable]
        public MeshReference MeshNorth;
        [XmlElement("TextureNorth")]
        [Reloadable]
        public ResourceReference TextureNorth;
        [XmlElement("MeshSouth")]
        [Reloadable]
        public MeshReference MeshSouth;
        [XmlElement("TextureSouth")]
        [Reloadable]
        public ResourceReference TextureSouth;
        #endregion

        #region mappers
        [XmlElement("SliderChargeGain")]
        [Reloadable]
        public MSliderReference SliderChargeGain;

        #region 電磁石 electromagnet
        [XmlElement("KeyMagnetizeNorth")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MKeyReference KeyMagnetizeNorth;
        [XmlElement("KeyMagnetizeSouth")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MKeyReference KeyMagnetizeSouth;
        [XmlElement("ToggleHoldToMagnetize")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MToggleReference ToggleHoldToMagnetize;
        #endregion
        #endregion
    }
    [XmlRoot("GaussmeterModule")]
    [Reloadable]
    public class GaussmeterModule : BlockModule
    {
        /// <summary>
        /// 極の位置 position of pole
        /// </summary>
        [XmlElement("PoleTransform")]
        [Reloadable]
        public TransformValues PoleTransform;

        #region visual property
        [XmlElement("MeshOff")]
        [Reloadable]
        public MeshReference MeshOff;
        [XmlElement("TextureOff")]
        [Reloadable]
        public ResourceReference TextureOff;
        [XmlElement("MeshNorth")]
        [Reloadable]
        public MeshReference MeshNorth;
        [XmlElement("TextureNorth")]
        [Reloadable]
        public ResourceReference TextureNorth;
        [XmlElement("MeshSouth")]
        [Reloadable]
        public MeshReference MeshSouth;
        [XmlElement("TextureSouth")]
        [Reloadable]
        public ResourceReference TextureSouth;
        #endregion

        #region mappers
        [XmlElement("Threshold")]
        [Reloadable]
        public MSliderReference Threshold;
        #endregion

        [XmlElement("KeyActivate")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MKeyReference KeyActivate;
        [XmlElement("ToggleActivateByKey")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MToggleReference ToggleActivateByKey;
        /*
        #region emulation
        [XmlElement("EmulateNorth")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MKeyReference EmulateNorth;
        [XmlElement("EmulateSouth")]
        [Reloadable]
        [CanBeEmpty]
        [DefaultValue(null)]
        public MKeyReference EmulateSouth;
        #endregion
        */
    }
}