using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using Modding;
using Modding.Serialization;

namespace MagnetSpace
{
    public class XMLDeserializer
    {
        private static readonly string ResourcesPath = "Resources/";
        private static readonly string DataPath = Application.dataPath + "/Mods/Data/Magnet_ac980b9d-19dd-4de6-aca5-97219154ce48/";

        /// <summary>
        /// XML�f�[�^��ǂݍ���
        /// �f�[�^�t�H���_��XML�����݂��Ȃ��Ȃ�AResource�t�H���_����XML���f�[�^�t�H���_�ɕ�������
        /// </summary>
        /// <returns>XML����ǂݍ��񂾃f�[�^</returns>
        public static ConfigElement Deserialize(string FileName)
        {
            if (Modding.ModIO.ExistsFile(FileName, true))
            {
                Mod.Log($"Loaded {FileName} from {DataPath}");
                return Modding.ModIO.DeserializeXml<ConfigElement>(FileName, true);
            }
            Mod.Log($"Loaded {FileName} from resources folder");
            var label = Modding.ModIO.DeserializeXml<ConfigElement>(ResourcesPath + FileName, false);
            if (!Modding.ModIO.ExistsFile(FileName, true))
            {
                Mod.Log($"Created {FileName} in {DataPath}");
                Serialize(label, FileName);
            }
            return label;
        }
        /// <summary>
        /// XML�f�[�^��ۑ�����
        /// </summary>
        /// <param name="label">�ۑ�����f�[�^</param>
        public static void Serialize(ConfigElement label, string FileName)
        {
            Modding.ModIO.SerializeXml<ConfigElement>(label, FileName, true);
        }
    }
    [XmlRoot("Config")]
    public class ConfigElement : Element
    {
        [XmlElement("CoulombConstant")]
        public float CoulombConstant;
        [XmlElement("MaxDistance")]
        public float MaxDistance;
        [XmlElement("MinDistance")]
        public float MinDistance;
    }
}