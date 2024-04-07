using System;
using System.Collections.Generic;
using MagnetSpace.Module;
using Modding;
using Modding.Modules;
using UnityEngine;

namespace MagnetSpace
{
	public class Mod : ModEntryPoint
	{
        /// <summary>
        /// Singletonの親
        /// </summary>
        private GameObject m_mod;
        #region singleton
        private MagnetManager m_magnetManager;
        private SkinLoader m_skinLoader;
        #endregion
        #region message
        #endregion
        public static List<string> PoleType;

        public override void OnLoad()
		{
            m_mod = new GameObject("Magnet Mod");

            // インスタンスの初期化
            m_magnetManager = SingleInstance<MagnetManager>.Instance;
            UnityEngine.Object.DontDestroyOnLoad(m_magnetManager);
            var config = XMLDeserializer.Deserialize("config.xml");
            m_magnetManager.Coulomb = config.CoulombConstant;
            m_magnetManager.MaxDistance = config.MaxDistance;
            m_magnetManager.MinDistance = config.MinDistance;
            m_magnetManager.transform.parent = m_mod.transform;
            m_skinLoader = SingleInstance<SkinLoader>.Instance;
            UnityEngine.Object.DontDestroyOnLoad(m_skinLoader);
            m_skinLoader.transform.parent = m_mod.transform;

            // pole typeの初期化
            PoleType = new List<string>
            {
                MagnetSpace.PoleType.North.ToString(),
                MagnetSpace.PoleType.South.ToString(),
            };

            // モジュールの登録
            CustomModules.AddBlockModule<MagnetModule, Magnet>("MagnetModule", true);

			Log("Load");
		}
		public static void Log(string msg) => Debug.Log($"Magnet: {msg}");
		public static void Warning(string msg) => Debug.LogWarning($"Magnet: {msg}");
		public static void Error(string msg) => Debug.LogError($"Magnet: {msg}");
	}
    /// <summary>
    /// IMonopoleを管理するSingleton
    /// </summary>
    public class MagnetManager : SingleInstance<MagnetManager>
    {
        public override string Name => "Magnet Manager";
        /// <summary>
        /// シーン上に存在するシミュレーション中のIMonopole
        /// </summary>
        public List<IMonopole> Monopoles;
        /// <summary>
        /// クーロン定数
        /// 
        /// 参考：地球では6.33e+4
        /// </summary>
        public float Coulomb
        {
            set; get;
        }
        /// <summary>
        /// クーロンの法則が適用されるブロックのマンハッタン距離の最大値
        /// </summary>
        public float MaxDistance
        {
            set; get;
        }
        /// <summary>
        /// クーロンの法則が適用されるブロックのユークリッド距離の最小値
        /// これより小さい距離なら、MinDistanceの方が採用される
        /// </summary>
        public float MinDistance
        {
            set; get;
        }

        /// <summary>
        /// Monopolesに引数を追加する
        /// 計算量の増加を防ぐため、みだりに呼び出さないこと
        /// </summary>
        /// <param name="m"></param>
        public void Add(IMonopole m)
        {
            if (Monopoles is null)
            {
                Monopoles = new List<IMonopole>();
            }
            if (!Monopoles.Contains(m))
            {
                Monopoles.Add(m);
            }
        }
        /// <summary>
        /// Monopolesから引数を削除する
        /// 計算量の増加を防ぐため、みだりに呼び出さないこと
        /// </summary>
        /// <param name="m"></param>
        public void Remove(IMonopole m)
        {
            if (Monopoles is null)
            {
                Monopoles = new List<IMonopole>();
            }
            if (Monopoles.Contains(m))
            {
                Monopoles.Remove(m);
            }
        }

        public void FixedUpdate()
        {
            // Simulate FixedUpdate Host
            if (!StatMaster.isHosting)
            {
                //Mod.Warning("FixedUpdate was skipped!");
                return;
            }
            if (Monopoles is null || Monopoles.Count == 0) { return; }

            // それぞれのブロックに力をかける
            for (int i=0; i<Monopoles.Count; i++)
            {
                var m_i = Monopoles[i];
                if (!m_i.IsMagnetized())
                {
                    continue;
                }
                for (int j=i+1; j<Monopoles.Count; j++)
                {
                    var m_j = Monopoles[j];
                    if (!m_j.IsMagnetized())
                    {
                        continue;
                    }

                    var force = m_i.GetForce(m_j);
                    m_i.AddForce(force);
                    m_j.AddForce(-force);
                }
            }
        }
    }
    /// <summary>
    /// Block module behaviour for magnet module.
    /// </summary>
    public class Magnet : BlockModuleBehaviour<MagnetModule>, IMonopole
    {
        public string BlockName => transform.name.Replace("(Clone)", "");
        #region magnetic function
        /// <summary>
        /// 極の位置
        /// </summary>
        protected Transform m_pole;
        /// <summary>
        /// 磁気量の倍率
        /// </summary>
        protected MSlider m_sliderChargeGain;
        /// <summary>
        /// N極を有効化するキー（電磁石）
        /// </summary>
        protected MKey m_keyMagnetizeNorth;
        /// <summary>
        /// S極を有効化するキー（電磁石）
        /// </summary>
        protected MKey m_keyMagnetizeSouth;
        /// <summary>
        /// 磁性を選択するメニュー（自然磁石）
        /// </summary>
        protected MMenu m_menuPoleType;
        /// <summary>
        /// m_keyEnergizeを押した場合の挙動
        /// 
        /// trueなら押下時のみ磁化する
        /// falseなら磁化がトグル化する
        /// </summary>
        protected MToggle m_toggleHoldToMagnetize;
        /// <summary>
        /// このブロックが電磁石である
        /// （Module.KeyMagnetizeの有無により判断する）
        /// </summary>
        public bool IsElectromagnet
        {
            get
            {
                return m_keyMagnetizeNorth != null || m_keyMagnetizeSouth != null;
            }
        }
        /// <summary>
        /// 現在の磁性
        /// </summary>
        private PoleType m_poleType = PoleType.None;
        /// <summary>
        /// 1F前の磁性（スキン用）
        /// </summary>
        private PoleType m_lastPoleType = PoleType.None;
        #endregion
        #region emulation
        private bool m_keyNorthEmulationHeld = false;
        private bool m_keySouthEmulationHeld = false;
        private bool m_keyNorthEmulationPressed = false;
        private bool m_keySouthEmulationPressed = false;
        #endregion
        #region skin
        private BlockVisualController m_vis;
        private string m_skinName = SkinLoader.Instance.DefaultSkinName;
        private string m_lastSkinName = "";
        public string PathOff => "off";
        public string PathNorth => "north";
        public string PathSouth => "south";
        private SkinLoader.SkinDataPack.SkinData m_skinOff;
        private SkinLoader.SkinDataPack.SkinData m_skinNorth;
        private SkinLoader.SkinDataPack.SkinData m_skinSouth;
        private bool m_isLoadingOffSkin = false;
        private bool m_isLoadingNorthSkin = false;
        private bool m_isLoadingSouthSkin = false;
        private bool m_hasChangedMesh = false;
        private bool m_hasChangedTexture = false;
        /// <summary>
        /// 極の状態に応じてMagnetModMeshを与える
        /// </summary>
        /// <param name="pole"></param>
        /// <returns></returns>
        public SkinLoader.MagnetModMesh GetModMesh(PoleType pole)
        {
            switch (pole)
            {
                case PoleType.None:
                default:
                    return m_skinOff.Mesh;
                case PoleType.North:
                    return m_skinNorth.Mesh;
                case PoleType.South:
                    return m_skinSouth.Mesh;
            }
        }
        /// <summary>
        /// 極の状態に応じてMagnetModTextureを与える
        /// </summary>
        /// <param name="pole"></param>
        /// <returns></returns>
        public SkinLoader.MagnetModTexture GetModTexture(PoleType pole)
        {
            switch (pole)
            {
                case PoleType.None:
                default:
                    return m_skinOff.Texture;
                case PoleType.North:
                    return m_skinNorth.Texture;
                case PoleType.South:
                    return m_skinSouth.Texture;
            }
        }
        #endregion

        #region implementation of interface
        /// <summary>
        /// 磁性がある（電磁石）
        /// </summary>
        /// <returns></returns>
        public bool IsMagnetized()
        {
            var poleType = GetPoleType();
            return poleType == PoleType.North || poleType == PoleType.South;
        }
        /// <summary>
        /// PoleTypeを取得する
        /// 
        /// enum PoleType と Mod.PoleTypeのindexから取得する
        /// </summary>
        /// <returns></returns>
        public PoleType GetPoleType() => m_poleType;
        public Vector3 GetPolePosition() => m_pole.position;
        public float GetCharge() => Module.Charge * m_sliderChargeGain.Value * (int)GetPoleType();
        public Vector3 GetForce(IMonopole other)
        {
            var distance = other.GetPolePosition() - GetPolePosition();

            // xyzのいずれかがmax distanceを超える場合は計算しない
            var maxDistance = MagnetManager.Instance.MaxDistance;
            if (maxDistance < distance.x || maxDistance < distance.y || maxDistance < distance.z)
            {
                return Vector3.zero;
            }

            var magnitude = distance.magnitude;
            var normalizedDistance = distance / magnitude;
            var scalarDistance = Mathf.Max(magnitude, MagnetManager.Instance.MinDistance);
            return -MagnetManager.Instance.Coulomb * GetCharge() * other.GetCharge() / (scalarDistance * scalarDistance) * normalizedDistance;
        }
        public void AddForce(Vector3 force)
        {
            BlockBehaviour.Rigidbody.AddForceAtPosition(force, m_pole.position);
        }
        #endregion
        public BlockVisualController GetVisualController() => GetComponent<BlockVisualController>();
        /// <summary>
        /// スキンを変更する
        /// 
        /// MeshとTextureを変更するタイミング
        /// - スキンを変更した時
        /// - PoleTypeを変更した時
        /// </summary>
        public void SetSkin()
        {
            m_skinName = OptionsMaster.skinsEnabled ? m_vis.selectedSkin.pack.name : SkinLoader.Instance.DefaultSkinName;
            var poleType = GetPoleType();

            // スキンが変更された場合またはスキンをロードしている最中の場合
            // m_skinOff m_skinNorth m_skinSouthを取得する
            if (m_skinName != m_lastSkinName || poleType != m_lastPoleType || 
                m_isLoadingNorthSkin || m_isLoadingSouthSkin || m_isLoadingOffSkin)
            {
                m_lastSkinName = m_skinName;
                m_lastPoleType = poleType;
                m_isLoadingOffSkin = true;
                m_isLoadingNorthSkin = true;
                m_isLoadingSouthSkin = true;
                m_hasChangedMesh = false;
                m_hasChangedTexture = false;

                #region m_skinOff m_skinNorth m_skinSouthを取得する
                var modSkinsOff = SkinLoader.Instance.modSkinsOff;
                var modSkinsNorth = SkinLoader.Instance.ModSkinsNorth;
                var modSkinsSouth = SkinLoader.Instance.ModSkinsSouth;

                // スキンパックが登録されていなければ登録する
                if (!modSkinsOff.ContainsKey(m_skinName))
                {
                    var skinPack = new SkinLoader.SkinDataPack();
                    modSkinsOff.Add(m_skinName, skinPack);
                }
                // スキンパックが既に登録されている
                else
                {
                    // このブロックのスキンがスキンパックに登録されていれば、それを取得する
                    if (modSkinsOff[m_skinName].Skins.ContainsKey(BlockName))
                    {
                        m_skinOff = modSkinsOff[m_skinName].Skins[BlockName];
                    }

                    // このブロックのスキンがスキンパックに登録されていない
                    else
                    {
                        // スキンをスキンフォルダからロードしてスキンパックに登録する
                        var skin = new SkinLoader.SkinDataPack.SkinData();
                        if (m_skinName == SkinLoader.Instance.DefaultSkinName)
                        {
                            ModMesh modMesh = (ModMesh)GetResource(Module.MeshOff);
                            ModTexture modTexture = (ModTexture)GetResource(Module.TextureOff);
                            skin.SetDefaultSkin(modMesh, modTexture);
                            m_skinOff = skin;
                        }
                        else
                        {
                            var path = $"{m_vis.selectedSkin.pack.path}/{BlockName}/{PathOff}/";
                            var meshPath = $"{path}{BlockName}.obj";
                            var texturePath = $"{path}{BlockName}.png";
                            skin.SetSkin(BlockName, meshPath, texturePath);
                            m_skinOff = skin;
                        }
                        modSkinsOff[m_skinName].Skins.Add(BlockName, skin);
                    }

                    // スキンのロードが終わった
                    m_isLoadingOffSkin = false;
                }

                // N極とS極も同じ
                if (!modSkinsNorth.ContainsKey(m_skinName))
                {
                    var skinPack = new SkinLoader.SkinDataPack();
                    modSkinsNorth.Add(m_skinName, skinPack);
                }
                else
                {
                    if (modSkinsNorth[m_skinName].Skins.ContainsKey(BlockName))
                    {
                        m_skinNorth = modSkinsNorth[m_skinName].Skins[BlockName];
                    }
                    else
                    {
                        // スキンをスキンフォルダからロードする
                        var skin = new SkinLoader.SkinDataPack.SkinData();
                        if (m_skinName == SkinLoader.Instance.DefaultSkinName)
                        {
                            ModMesh modMesh = (ModMesh)GetResource(Module.MeshNorth);
                            ModTexture modTexture = (ModTexture)GetResource(Module.TextureNorth);
                            skin.SetDefaultSkin(modMesh, modTexture);
                            m_skinNorth = skin;
                        }
                        else
                        {
                            var path = $"{m_vis.selectedSkin.pack.path}/{BlockName}/{PathNorth}/";
                            var meshPath = $"{path}{BlockName}.obj";
                            var texturePath = $"{path}{BlockName}.png";
                            skin.SetSkin(BlockName, meshPath, texturePath);
                            m_skinNorth = skin;
                        }
                        modSkinsNorth[m_skinName].Skins.Add(BlockName, skin);
                    }
                    m_isLoadingNorthSkin = false;
                }
                if (!modSkinsSouth.ContainsKey(m_skinName))
                {
                    var skinPack = new SkinLoader.SkinDataPack();
                    modSkinsSouth.Add(m_skinName, skinPack);
                }
                else
                {
                    if (modSkinsSouth[m_skinName].Skins.ContainsKey(BlockName))
                    {
                        m_skinSouth = modSkinsSouth[m_skinName].Skins[BlockName];
                    }
                    else
                    {
                        // スキンをスキンフォルダからロードする
                        var skin = new SkinLoader.SkinDataPack.SkinData();
                        if (m_skinName == SkinLoader.Instance.DefaultSkinName)
                        {
                            ModMesh modMesh = (ModMesh)GetResource(Module.MeshSouth);
                            ModTexture modTexture = (ModTexture)GetResource(Module.TextureSouth);
                            skin.SetDefaultSkin(modMesh, modTexture);
                            m_skinSouth = skin;
                        }
                        else
                        {
                            var path = $"{m_vis.selectedSkin.pack.path}/{BlockName}/{PathSouth}/";
                            var meshPath = $"{path}{BlockName}.obj";
                            var texturePath = $"{path}{BlockName}.png";
                            skin.SetSkin(BlockName, meshPath, texturePath);
                            m_skinSouth = skin;
                        }
                        modSkinsSouth[m_skinName].Skins.Add(BlockName, skin);
                    }
                    m_isLoadingSouthSkin = false;
                }
                #endregion
            }

            // それ以外。ほぼ毎フレーム呼び出す
            else
            {
                #region null回避
                if (m_skinOff == null)
                {
                    m_skinOff = new SkinLoader.SkinDataPack.SkinData();
                }
                if (m_skinNorth == null)
                {
                    m_skinNorth = new SkinLoader.SkinDataPack.SkinData();
                }
                if (m_skinSouth == null)
                {
                    m_skinSouth = new SkinLoader.SkinDataPack.SkinData();
                }
                #endregion

                // デフォルトスキン
                if (m_skinName == SkinLoader.Instance.DefaultSkinName)
                {
                    // 本体のスキンを適用する
                    if (!m_hasChangedMesh)
                    {
                        GetModMesh(poleType).ApplyToObject(m_vis.MeshFilter);
                        m_hasChangedMesh = true;
                    }
                    if (!m_hasChangedTexture)
                    {
                        var tex = GetModTexture(poleType);
                        tex.ApplyToObject(m_vis.renderers[0]);
                        m_hasChangedTexture = true;
                    }
                }

                // デフォルトでないスキン
                else
                {
                    // 本体のスキンを適用する
                    var mesh = GetModMesh(poleType);
                    if (mesh.IsLoaded && !m_hasChangedMesh)
                    {
                        if (!mesh.HasError)
                        {
                            mesh.ApplyToObject(m_vis.MeshFilter);
                        }
                        m_hasChangedMesh = true;
                    }
                    
                    var tex = GetModTexture(poleType);
                    if (tex.IsLoaded && !m_hasChangedTexture)
                    {
                        if (!tex.HasError)
                        {
                            tex.ApplyToObject(m_vis.renderers[0]);
                        }
                        m_hasChangedTexture = true;
                    }
                }
            }
        }
        public ValueHandler OnMenuValueChanged;

        #region event
        public override void SafeAwake()
        {
            base.SafeAwake();

            #region UI生成
            try
            {
                if (Module.SliderChargeGain != null) { m_sliderChargeGain = GetSlider(Module.SliderChargeGain); }
                
                if (Module.KeyMagnetizeNorth != null)
                {
                    m_keyMagnetizeNorth = GetKey(Module.KeyMagnetizeNorth);
                }
                if (Module.KeyMagnetizeSouth != null)
                {
                    m_keyMagnetizeSouth = GetKey(Module.KeyMagnetizeSouth);
                }
                if (IsElectromagnet)
                {
                    if (Module.ToggleHoldToMagnetize != null)
                    {
                        m_toggleHoldToMagnetize = GetToggle(Module.ToggleHoldToMagnetize);
                    }
                }
                else
                {
                    // 電磁石ではないなら常に磁性がある
                    m_menuPoleType = AddMenu("pole-type", 0, Mod.PoleType);
                    OnMenuValueChanged = (value) =>
                    {
                        m_poleType = value == 0 ? PoleType.North : value == 1 ? PoleType.South : PoleType.None;
                    };
                    m_menuPoleType.ValueChanged += OnMenuValueChanged;
                }
            }
            catch (Exception e)
            {
                Mod.Error("Could not get mapper types for Magnet module! module will be destroyed.");
                Mod.Error($"{e}");
                Destroy(this);
                return;
            }
            #endregion

            #region pole生成
            m_pole = new GameObject("Pole").transform;
            m_pole.parent = transform;
            m_pole.localPosition = Module.PoleTransform.Position;
            m_pole.localEulerAngles = Module.PoleTransform.Rotation;
            m_pole.localScale = Module.PoleTransform.Scale;
            #endregion

            #region スキン初期化
            m_vis = GetVisualController();
            #endregion
        }
        /// <summary>
        /// 見た目を更新する（ビルド中）
        /// </summary>
        public override void BuildingFixedUpdate()
        {
            if (!m_vis)
            {
                m_vis = GetVisualController();
            }
            SetSkin();
        }
        /// <summary>
        /// MagnetManagerに自身を登録する
        /// </summary>
        public override void OnSimulateStart()
        {
            MagnetManager.Instance.Add(this);
        }
        /// <summary>
        /// キー操作を受け付ける
        /// </summary>
        public override void SimulateUpdateAlways()
        {
            // 電磁石でないなら何もしない
            if (!IsElectromagnet)
            {
                return;
            }

            // 押下時のみ磁化
            if (m_toggleHoldToMagnetize != null && m_toggleHoldToMagnetize.IsActive)
            {
                //m_isMagnetized = m_keyMagnetize.IsHeld;
                if ((m_keyMagnetizeNorth.IsHeld || m_keyNorthEmulationHeld) 
                    && (m_keyMagnetizeSouth.IsHeld || m_keySouthEmulationHeld))
                {
                    m_poleType = PoleType.None;
                }
                else if (m_keyMagnetizeNorth.IsHeld || m_keyNorthEmulationHeld)
                {
                    m_poleType = PoleType.North;
                }
                else if (m_keyMagnetizeSouth.IsHeld || m_keySouthEmulationHeld)
                {
                    m_poleType = PoleType.South;
                }
                else
                {
                    m_poleType = PoleType.None;
                }
            }
            else
            {
                if (m_keyMagnetizeNorth.IsPressed || m_keyNorthEmulationPressed)
                {
                    m_poleType = (m_poleType == PoleType.North) ? PoleType.None : PoleType.North;
                }
                else if (m_keyMagnetizeSouth.IsPressed || m_keySouthEmulationPressed)
                {
                    m_poleType = (m_poleType == PoleType.South) ? PoleType.None : PoleType.South;
                }
            }
        }
        /// <summary>
        /// エミュレーションを受け付ける
        /// </summary>
        public override void KeyEmulationUpdate()
        {
            // 電磁石でないなら何もしない
            if (!IsElectromagnet) { return; }

            m_keyNorthEmulationHeld = m_keyMagnetizeNorth.EmulationHeld();
            m_keySouthEmulationHeld = m_keyMagnetizeSouth.EmulationHeld();
            m_keyNorthEmulationPressed = m_keyMagnetizeNorth.EmulationPressed();
            m_keySouthEmulationPressed = m_keyMagnetizeSouth.EmulationPressed();
        }
        /// <summary>
        /// 見た目を更新する（シミュ中/ホスト）
        /// </summary>
        public override void SimulateFixedUpdateHost()
        {
            if (!m_vis)
            {
                m_vis = GetVisualController();
            }
            SetSkin();
        }
        /// <summary>
        /// 見た目を更新する（ビルド中/クライアント）
        /// </summary>
        public override void SimulateFixedUpdateClient()
        {
            if (!m_vis)
            {
                m_vis = GetVisualController();
            }
            SetSkin();
        }
        /// <summary>
        /// MagnetManagerから自身を破棄する
        /// </summary>
        public void OnDestroy()
        {
            MagnetManager.Instance.Remove(this);
        }

        #endregion
    }
}
