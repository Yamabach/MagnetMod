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


        #region mappers
        [XmlElement("SliderChargeGain")]
        [Reloadable]
        public MSliderReference SliderChargeGain;

        #region �d����
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