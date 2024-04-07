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
        /// XMLデータを読み込む
        /// データフォルダにXMLが存在しないなら、Resourceフォルダ内のXMLをデータフォルダに複製する
        /// </summary>
        /// <returns>XMLから読み込んだデータ</returns>
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
        /// XMLデータを保存する
        /// </summary>
        /// <param name="label">保存するデータ</param>
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