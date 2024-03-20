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
        /// �ɂ̈ʒu position of pole
        /// </summary>
        [XmlElement("PoleTransform")]
        [Reloadable]
        public TransformValues PoleTransform;

        /// <summary>
        /// ���C�� magnetic charge
        /// </summary>
        [XmlElement("Charge")]
        [Reloadable]
        public float Charge;

        #region visual property
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

        #region �d���� electromagnet
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