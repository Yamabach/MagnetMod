using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultithreadCoroutines;
using Besiege;
using Modding;

namespace MagnetSpace
{
    /// <summary>
    /// スキンを管理するクラス
    /// 大部分をACMから移植
    /// </summary>
    public class SkinLoader : SingleInstance<SkinLoader>
    {
        #region class
        public abstract class MagnetModResource
        {
            public enum ResourceType
            {
                Texture,
                Mesh,
                AudioClip,
                AssetBundle,
            }
            public string Name;
            public ResourceType Type { get; protected set; }
            public abstract bool IsLoaded { get; }
            public abstract bool HasError { get; }
            public abstract string Error { get; }
            public string Path;
            public bool Readable;
            internal abstract IEnumerator Load();
            internal abstract void ApplyToObject(GameObject go);

            public static MagnetModMesh LoadMesh(string name, string path)
            {
                MagnetModMesh mesh = new MagnetModMesh();
                mesh.Name = name;
                mesh.Path = path;
                mesh.Type = ResourceType.Mesh;

                Instance.StartCoroutineAsync(mesh.Load());
                return mesh;
            }
            public static MagnetModTexture LoadTexture(string name, string path)
            {
                MagnetModTexture texture = new MagnetModTexture();
                texture.Name = name;
                texture.Path = path;
                texture.Type = ResourceType.Texture;

                Instance.StartCoroutine(texture.Load());
                return texture;
            }
            public static MagnetModMesh GetDefaultMesh(Mesh mesh)
            {
                var modMesh = new MagnetModMesh();
                modMesh.Set(mesh);
                return modMesh;
            }
            public static MagnetModTexture GetDefaultTexture(Texture2D texture)
            {
                var modTexture = new MagnetModTexture();
                modTexture.Set(texture);
                return modTexture;
            }
        }
        public class MagnetModMesh : MagnetModResource
        {
            private string m_error = "";
            private bool m_hasError;
            private bool m_isLoaded;
            public override bool HasError => m_hasError;
            public override string Error => m_error;
            public override bool IsLoaded => m_isLoaded;
            public Mesh Mesh { get; private set; }

            /// <summary>
            /// ctr
            /// </summary>
            internal MagnetModMesh() { }
            internal override IEnumerator Load()
            {
                AssetImporter.meshData meshData;
                try
                {
                    meshData = new AssetImporter.meshData();
                    AssetImporter.LoadMeshData(ref meshData, Path);
                }
                catch (Exception ex)
                {
                    m_hasError = true;
                    m_error = ex.ToString();
                    m_isLoaded = true;
                    yield break;
                }

                yield return Ninja.JumpToUnity;

                try
                {
                    Mesh mesh = new Mesh();
                    meshData.PassNewDataToMesh(ref mesh);
                    mesh.name = Name;
                    this.Mesh = mesh;
                    m_isLoaded = true;
                }
                catch (Exception ex)
                {
                    m_hasError = true;
                    m_error = ex.ToString();
                    m_isLoaded = true;
                }
                //Mod.Log("MagnetModMesh.Load finish"); // 呼ばれてる
            }
            internal override void ApplyToObject(GameObject go)
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    Mod.Error($"The argument of SkinLoader.MagnetModMesh.ApplyToObject() {go.name} has no MeshFilter!");
                }
                else
                {
                    ApplyToObject(filter);
                }
            }
            internal void ApplyToObject(MeshFilter filter)
            {
                filter.mesh = Mesh;
            }
            public void GetMeshFromObject(GameObject go)
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    Mod.Error($"The argument of SkinLoader.MagnetModMesh.GetMeshFromObject() {go.name} has no MeshFilter!");
                }
                else
                {
                    Mesh = filter.mesh;
                }
            }
            public void Set(Mesh mesh)
            {
                Mesh = new Mesh();
                Mesh = mesh;
            }
            public static implicit operator Mesh(MagnetModMesh mmmesh)
            {
                return mmmesh.Mesh;
            }
        }
        public class MagnetModTexture : MagnetModResource
        {
            private string m_error = "";
            private bool m_hasError;
            private bool m_isLoaded;
            public override bool HasError => m_hasError;
            public override string Error => m_error;
            public override bool IsLoaded => m_isLoaded;
            public Texture2D Texture { get; private set; }

            /// <summary>
            /// ctr
            /// </summary>
            internal MagnetModTexture() { }
            internal override IEnumerator Load()
            {
                AssetImporter.LoadingObject loadingObject = AssetImporter.StartImport.Texture(Path, null, !Readable);
                
                yield return loadingObject.routine;

                Texture = loadingObject.tex;
                m_isLoaded = true;
                m_hasError = !string.IsNullOrEmpty(loadingObject.texError);
                m_error = loadingObject.texError;
            }
            internal override void ApplyToObject(GameObject go)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Mod.Error($"The argument of SkinLoader.MagnetModTexture.ApplyToObject() {go.name} has no Renderer!");
                }
                else
                {
                    ApplyToObject(renderer);
                }
            }
            internal void ApplyToObject(Renderer renderer)
            {
                renderer.material.mainTexture = Texture;
            }
            public void GetTextureFromObject(GameObject go)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Mod.Error($"The argument of SkinLoader.MagnetModTexture.GetTextureFromObject() {go.name} has no Renderer!");
                }
                else
                {
                    Texture = (Texture2D)renderer.material.mainTexture;
                }
            }
            public void Set(Texture2D t)
            {
                Texture = t;
            }
            public static implicit operator Texture2D(MagnetModTexture t)
            {
                return t?.Texture;
            }
            public static explicit operator MagnetModTexture(Texture2D t)
            {
                if (t == null)
                {
                    return null;
                }

                var modTexture = new MagnetModTexture();
                modTexture.Texture = t;
                modTexture.m_isLoaded = true;
                modTexture.m_hasError = false;
                modTexture.m_error = string.Empty;
                return modTexture;
            }
        }
        public class SkinDataPack
        {
            public class SkinData
            {
                public string ModBlockName;
                public string Path;
                public string MeshPath;
                public string TexturePath;

                public MagnetModMesh Mesh;
                public MagnetModTexture Texture;

                public bool IsDefaultSkin;
                public bool IsLoading;
                public bool HasLoaded;

                public bool IsLoadingMesh;
                public bool HasLoadedMesh;
                public bool MeshEnable;

                public bool IsLoadingTexture;
                public bool HasLoadedTexture;
                public bool TextureEnable;

                /// <summary>
                /// ctr
                /// </summary>
                internal SkinData() { }
                /// <summary>
                /// スキンを適用する
                /// </summary>
                /// <param name="name"></param>
                /// <param name="meshPath"></param>
                /// <param name="texturePath"></param>
                public void SetSkin(string name, string meshPath = "none", string texturePath = "none")
                {
                    ModBlockName = name;
                    MeshPath = meshPath;
                    TexturePath = texturePath;

                    IsLoading = true;

                    if (MeshPath != "none")
                    {
                        Mesh = MagnetModResource.LoadMesh(ModBlockName, MeshPath);
                        IsLoadingMesh = true;
                    }
                    if (TexturePath != "none")
                    {
                        Texture = MagnetModResource.LoadTexture(ModBlockName, TexturePath);
                        IsLoadingTexture = true;
                    }
                    HasLoaded = true;
                }
                /// <summary>
                /// デフォルトスキンを適用する
                /// </summary>
                /// <param name="dMesh"></param>
                /// <param name="dTexture"></param>
                public void SetDefaultSkin(Mesh dMesh, Texture2D dTexture)
                {
                    Mesh = MagnetModResource.GetDefaultMesh(dMesh);
                    Texture = MagnetModResource.GetDefaultTexture(dTexture);
                    IsLoadingMesh = true;
                    IsLoadingTexture = true;
                    HasLoaded = true;
                }
            }
            public string SkinName;
            public Dictionary<string, SkinData> Skins = new Dictionary<string, SkinData>();
        }
        #endregion

        // SkinVariables
        private Dictionary<string, ISkinVariable> m_skinVariables;
        private Dictionary<string, ISkinVariable> SkinVariables
        {
            get
            {
                if (m_skinVariables is null)
                {
                    m_skinVariables = new Dictionary<string, ISkinVariable>();
                }
                return m_skinVariables;
            }
        }
        public void AddSkinVariable(string name, ISkinVariable skinVariable)
        {
            if (SkinVariables.ContainsKey(name)) { return; }
            SkinVariables.Add(name, skinVariable);
        }
        public void RemoveSkinVariable(string name)
        {
            if (!SkinVariables.ContainsKey (name)) { return; }
            SkinVariables.Remove(name);
        }

        // message
        public MessageType MessagePoleType;
        public void InitializeMessages()
        {
            MessagePoleType = ModNetworking.CreateMessageType(DataType.String, DataType.Integer);
            ModNetworking.Callbacks[MessagePoleType] += new Action<Message>((msg) =>
            {
                //Mod.Log($"{(string)msg.GetData(0)} {(int)msg.GetData(1)}"); // 受け取れてる
                if (SkinVariables.TryGetValue((string)msg.GetData(0), out var sv))
                {
                    sv.SetSkinVariation((int)msg.GetData(1));
                }
            });
        }
        public void SendMessagePoleType(string name, int poleType)
        {
            if (StatMaster.isClient) { return; }
            ModNetworking.SendToAll(MessagePoleType.CreateMessage(name, poleType));
        }


        public override string Name => "MagnetSkinLoader";
        /// <summary>
        /// 現在の言語における「デフォルト」の文字列を取り出す
        /// 
        /// DEFAULT
        /// デフォルト
        /// PREDEFINITO
        /// など
        /// </summary>
        public string DefaultSkinName
        {
            get
            {
                return Localisation.LocalisationManager.Instance.GetTranslationById(781);
            }
        }
        public Dictionary<string, SkinDataPack> ModSkinsOff = new Dictionary<string, SkinDataPack>();
        /// <summary>
        /// N極のスキン
        /// </summary>
        public Dictionary<string, SkinDataPack> ModSkinsNorth = new Dictionary<string, SkinDataPack>();
        /// <summary>
        /// S極のスキン
        /// </summary>
        public Dictionary<string, SkinDataPack> ModSkinsSouth = new Dictionary<string, SkinDataPack>();
        /// <summary>
        /// カバーのスキン
        /// </summary>
        public Dictionary<string, SkinDataPack> ModSkinsCover = new Dictionary<string, SkinDataPack>();
        public Dictionary<string, SkinDataPack> ModSkinsCoverNorth = new Dictionary<string, SkinDataPack>();
        public Dictionary<string, SkinDataPack> ModSkinsCoverSouth = new Dictionary<string, SkinDataPack>();
    }
}