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
        public static List<string> PoleType;

        public override void OnLoad()
		{
            m_mod = new GameObject("Magnet Mod");

            // インスタンスの初期化
            m_magnetManager = SingleInstance<MagnetManager>.Instance;
            var config = XMLDeserializer.Deserialize("config.xml");
            m_magnetManager.Coulomb = config.CoulombConstant;
            m_magnetManager.MaxDistance = config.MaxDistance;
            m_magnetManager.MinDistance = config.MinDistance;
            m_magnetManager.transform.parent = m_mod.transform;

            m_skinLoader = SingleInstance<SkinLoader>.Instance;
            m_skinLoader.transform.parent = m_mod.transform;
            m_skinLoader.InitializeMessages();
            UnityEngine.Object.DontDestroyOnLoad(m_mod);

            // pole typeの初期化
            PoleType = new List<string>
            {
                MagnetSpace.PoleType.North.ToString(),
                MagnetSpace.PoleType.South.ToString(),
            };

            // モジュールの登録
            CustomModules.AddBlockModule<MagnetModule, Magnet>("MagnetModule", true);
            CustomModules.AddBlockModule<GaussmeterModule, Gaussmeter>("GaussmeterModule", true);

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
        private List<IMonopole> m_monopoles;
        public List<IMonopole> Monopoles
        {
            get
            {
                if (m_monopoles is null)
                {
                    m_monopoles = new List<IMonopole>();
                }
                return m_monopoles;
            }
        }
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
            if (m_monopoles is null)
            {
                m_monopoles = new List<IMonopole>();
            }
            if (!m_monopoles.Contains(m))
            {
                m_monopoles.Add(m);
            }
        }
        /// <summary>
        /// Monopolesから引数を削除する
        /// 計算量の増加を防ぐため、みだりに呼び出さないこと
        /// </summary>
        /// <param name="m"></param>
        public void Remove(IMonopole m)
        {
            if (m_monopoles is null)
            {
                m_monopoles = new List<IMonopole>();
            }
            if (m_monopoles.Contains(m))
            {
                m_monopoles.Remove(m);
            }
        }

        public void FixedUpdate()
        {
            // Simulate FixedUpdate Host
            if (StatMaster.isMP && !StatMaster.isHosting)
            {
                return;
            }
            if (m_monopoles == null)
            {
                return;
            }

            // それぞれのブロックに力をかける
            for (int i=0; i<m_monopoles.Count; i++)
            {
                var m_i = m_monopoles[i];
                if (!m_i.IsMagnetized())
                {
                    continue;
                }
                for (int j=i+1; j<m_monopoles.Count; j++)
                {
                    var m_j = m_monopoles[j];
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
    public class Magnet : BlockModuleBehaviour<MagnetModule>, IMonopole, ISkinVariable
    {
        public int OwnerId => StatMaster.isMP ? Machine.InternalObject.PlayerID : 0;
        public string BlockName => transform.name.Replace("(Clone)", "");
        public string IdenticalName => $"{OwnerId.ToString()}-{BlockBehaviour.identifier.ToString()}";
        #region magnetic function
        /// <summary>
        /// 極の位置
        /// </summary>
        private Transform m_pole;
        /// <summary>
        /// 磁気量の倍率
        /// </summary>
        private MSlider m_sliderChargeGain;
        /// <summary>
        /// N極を有効化するキー（電磁石）
        /// </summary>
        private MKey m_keyMagnetizeNorth;
        /// <summary>
        /// S極を有効化するキー（電磁石）
        /// </summary>
        private MKey m_keyMagnetizeSouth;
        /// <summary>
        /// 磁性を選択するメニュー（自然磁石）
        /// </summary>
        private MMenu m_menuPoleType;
        /// <summary>
        /// m_keyEnergizeを押した場合の挙動
        /// 
        /// trueなら押下時のみ磁化する
        /// falseなら磁化がトグル化する
        /// </summary>
        private MToggle m_toggleHoldToMagnetize;
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
        private string PathOff => "off";
        private string PathNorth => "north";
        private string PathSouth => "south";
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
        private SkinLoader.MagnetModMesh GetModMesh(PoleType pole)
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
        private SkinLoader.MagnetModTexture GetModTexture(PoleType pole)
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
        public Vector3 GetMagneticFluxDensity(Vector3 position)
        {
            var distance = position - GetPolePosition();

            // xyzのいずれかがmax distanceを超える場合は計算しない
            var maxDistance = MagnetManager.Instance.MaxDistance;
            if (maxDistance < distance.x || maxDistance < distance.y || maxDistance < distance.z)
            {
                return Vector3.zero;
            }

            var magnitude = distance.magnitude;
            var normalizedDistance = distance / magnitude;
            var scalarDistance = Mathf.Max(magnitude, MagnetManager.Instance.MinDistance);
            return (1e3f / 4f * Mathf.PI) * GetCharge() / (scalarDistance * scalarDistance) * normalizedDistance;
        }
        public void SetSkinVariation(int poleType)
        {
            if (StatMaster.isHosting) { return; }

            m_poleType = (PoleType)poleType;
        }
        #endregion

        private BlockVisualController GetVisualController() => GetComponent<BlockVisualController>();
        /// <summary>
        /// スキンを変更する
        /// 
        /// MeshとTextureを変更するタイミング
        /// - スキンを変更した時
        /// - PoleTypeを変更した時
        /// </summary>
        private void UpdateSkin()
        {
            m_skinName = OptionsMaster.skinsEnabled ? m_vis.selectedSkin.pack.name : SkinLoader.Instance.DefaultSkinName;
            var poleType = m_poleType; //GetPoleType();

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

                Mod.Log($"{poleType}");
                SkinLoader.Instance.SendMessagePoleType(IdenticalName, (int)m_poleType);

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
        private ValueHandler OnMenuValueChanged;

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
            if (IsSimulating)
            {
                SkinLoader.Instance.AddSkinVariable(IdenticalName, this);
            }
            #endregion
        }
        /// <summary>
        /// 見た目を更新する（ビルド中）
        /// </summary>
        public override void BuildingFixedUpdate() => UpdateSkin();
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
        public override void SimulateUpdateHost()
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
        public override void SimulateFixedUpdateHost() => UpdateSkin();
        /// <summary>
        /// 見た目を更新する（シミュ中/クライアント）
        /// </summary>
        public override void SimulateUpdateClient() => UpdateSkin();
        public override void OnSimulateStop()
        {
            MagnetManager.Instance.Remove(this);
        }
        /// <summary>
        /// MagnetManagerから自身を破棄する
        /// </summary>
        public void OnDestroy()
        {
            MagnetManager.Instance.Remove(this);
            if (IsSimulating)
            {
                SkinLoader.Instance.RemoveSkinVariable(IdenticalName);
            }
        }
        #endregion
    }
    public class Gaussmeter : BlockModuleBehaviour<GaussmeterModule>, IGaussmeter, ISkinVariable
    {
        public int OwnerId => StatMaster.isMP ? Machine.InternalObject.PlayerID : 0;
        public string BlockName => transform.name.Replace("(Clone)", "");
        public string IdenticalName => $"{OwnerId.ToString()}-{BlockBehaviour.identifier.ToString()}";
        public override bool EmulatesAnyKeys => true;
        /// <summary>
        /// 極の位置
        /// </summary>
        private Transform m_pole;
        private MKey m_keyActivate;
        /// <summary>
        /// 計測を始めるためのキー
        /// </summary>
        private MKey[] m_keysActivate;
        /// <summary>
        /// キーで計測を行う？
        /// </summary>
        private MToggle m_toggleActivateByKey;
        /// <summary>
        /// 磁束密度の閾値
        /// </summary>
        private MSlider m_sliderThreshold;
        /// <summary>
        /// N極側のエミュレーション
        /// </summary>
        private MKey m_emulationNorth;
        /// <summary>
        /// S極側のエミュレーション
        /// </summary>
        private MKey m_emulationSouth;
        /// <summary>
        /// 現在の磁性
        /// </summary>
        private PoleType m_poleType = PoleType.None;
        /// <summary>
        /// 1F前の磁性（スキン用）
        /// </summary>
        private PoleType m_lastPoleType = PoleType.None;
        /// <summary>
        /// 現在磁束密度を測定している？
        /// </summary>
        private bool m_isProbingByEmulate = false;

        /// <summary>
        /// 磁束密度
        /// </summary>
        private float m_density = 0f;
        /// <summary>
        /// 1F前の磁束密度
        /// </summary>
        private float m_lastDensity = 0f;

        #region skin
        private BlockVisualController m_vis;
        private string m_skinName = SkinLoader.Instance.DefaultSkinName;
        private string m_lastSkinName = "";
        private string PathOff => "off";
        private string PathNorth => "north";
        private string PathSouth => "south";
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
        private SkinLoader.MagnetModMesh GetModMesh(PoleType pole)
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
        private SkinLoader.MagnetModTexture GetModTexture(PoleType pole)
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

        #region interface implementation
        public Vector3 GetPolePosition() => m_pole.position;
        public Vector3 GetPoleDirection() => m_pole.forward;
        public float GetMagneticFluxDensity(IMonopole monopole) 
            => Vector3.Dot(monopole.GetMagneticFluxDensity(GetPolePosition()), GetPoleDirection());
        private float GetMagneticFluxDensity(params IMonopole[] monopoles)
        {
            float density = 0f;
            foreach (var monopole in monopoles)
            {
                density += GetMagneticFluxDensity(monopole);
            }
            return density;
        }
        private float GetMagneticFluxDensity(List<IMonopole> monopoles) => GetMagneticFluxDensity(monopoles.ToArray());
        
        public void SetSkinVariation(int poleType)
        {
            if (StatMaster.isHosting) { return; }

            m_poleType = (PoleType)poleType;
        }
        #endregion

        /// <summary>
        /// 磁束密度から極の種類を求める
        /// 磁束密度が小さい→N極
        /// 磁束密度が大きい→S極
        /// </summary>
        /// <param name="density"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private PoleType GetPoleType(float density, float threshold)
        {
            var poleType = PoleType.None;
            if (density < -threshold)
            {
                poleType = PoleType.North;
            }
            else if (threshold <= density)
            {
                poleType = PoleType.South;
            }
            return poleType;
        }
        private BlockVisualController GetVisualController() => GetComponent<BlockVisualController>();
        /// <summary>
        /// スキンを変更する
        /// 
        /// MeshとTextureを変更するタイミング
        /// - スキンを変更した時
        /// - PoleTypeを変更した時
        /// </summary>
        private void UpdateSkin()
        {
            m_skinName = OptionsMaster.skinsEnabled ? m_vis.selectedSkin.pack.name : SkinLoader.Instance.DefaultSkinName;
            var poleType = m_poleType;

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

                Mod.Log($"{poleType}");

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

        #region event
        public override void SafeAwake()
        {
            base.SafeAwake();

            #region UI生成
            try
            {
                if (Module.Threshold != null)
                {
                    m_sliderThreshold = GetSlider(Module.Threshold);
                }
                if (Module.KeyActivate != null)
                {
                    m_keyActivate = GetKey(Module.KeyActivate);
                    m_keysActivate = new MKey[] { m_keyActivate };
                }
                if (Module.ToggleActivateByKey != null)
                {
                    m_toggleActivateByKey = GetToggle(Module.ToggleActivateByKey);
                    m_toggleActivateByKey.Toggled += (isActive) =>
                    {
                        m_keyActivate.DisplayInMapper = isActive;
                    };
                }
                //if (Module.EmulateNorth != null)
                //{
                    //m_emulationNorth = GetKey(Module.EmulateNorth);
                    //m_emulationNorth = AddEmulatorKey(m_emulationNorth.DisplayName, m_emulationNorth.Key, m_emulationNorth.GetKey(0));
                m_emulationNorth = AddEmulatorKey("Emulate N", "gaussmeter-north", KeyCode.C);
                //}
                //if (Module.EmulateSouth != null)
                //{
                    //m_emulationSouth = GetKey(Module.EmulateSouth);
                    //m_emulationSouth = AddEmulatorKey(m_emulationSouth.DisplayName, m_emulationSouth.Key, m_emulationSouth.GetKey(0));
                m_emulationSouth = AddEmulatorKey("Emulate S", "gaussmeter-south", KeyCode.V);
                //}
            }
            catch (Exception e)
            {
                Mod.Error("Could not get mapper types for Gaussmeter module! module will be destroyed.");
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
            if (IsSimulating)
            {
                SkinLoader.Instance.AddSkinVariable(IdenticalName, this);
            }
            #endregion
        }
        /// <summary>
        /// 見た目を更新する（ビルド中）
        /// </summary>
        public override void BuildingFixedUpdate() => UpdateSkin();
        /// <summary>
        /// キー操作を受け付ける
        /// </summary>
        public override void SendKeyEmulationUpdateHost()
        {
            // キー入力で有効化する場合はキー入力している場合のみ受け付ける
            bool isInvalid = m_toggleActivateByKey.IsActive && !(m_keyActivate.IsHeld || m_isProbingByEmulate);

            // 全てのIMonopoleの磁束密度を計算する
            m_lastDensity = m_density;
            m_density = isInvalid ? 0f : GetMagneticFluxDensity(MagnetManager.Instance.Monopoles);

            // PoleTypeを更新する
            m_poleType = GetPoleType(m_density, m_sliderThreshold.Value);

            // 磁束密度が規定値を前後した時に限り、エミュレーションを変更する
            // 磁束密度が小さい→North
            if ((m_sliderThreshold.Value + m_density) * (m_sliderThreshold.Value + m_lastDensity) < 0f)
            {
                EmulateKeys(m_keysActivate, m_emulationSouth, m_density < -m_sliderThreshold.Value);
                SkinLoader.Instance.SendMessagePoleType(IdenticalName, (int)m_poleType);
            }
            // 磁束密度が大きい→South
            if ((m_density - m_sliderThreshold.Value) * (m_lastDensity - m_sliderThreshold.Value) < 0f)
            {
                EmulateKeys(m_keysActivate, m_emulationNorth, m_sliderThreshold.Value <= m_density);
                SkinLoader.Instance.SendMessagePoleType(IdenticalName, (int)m_poleType);
            }
        }
        /// <summary>
        /// エミュレーションを受け付ける
        /// </summary>
        public override void KeyEmulationUpdate()
        {
            // キー入力で有効化する場合のみ受け付ける
            m_isProbingByEmulate = m_keyActivate.EmulationHeld();
        }
        /// <summary>
        /// 見た目を更新する（シミュ中/ホスト）
        /// </summary>
        public override void SimulateFixedUpdateHost() => UpdateSkin();
        /// <summary>
        /// 見た目を更新する（シミュ中/クライアント）
        /// </summary>
        public override void SimulateUpdateClient() => UpdateSkin();
        public void OnDestroy()
        {
            if (IsSimulating)
            {
                SkinLoader.Instance.RemoveSkinVariable(IdenticalName);
            }
        }
        #endregion
    }
}
