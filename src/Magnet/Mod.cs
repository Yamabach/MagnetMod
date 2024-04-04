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
        /// Singleton�̐e
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

            // �C���X�^���X�̏�����
            m_magnetManager = SingleInstance<MagnetManager>.Instance;
            UnityEngine.Object.DontDestroyOnLoad(m_magnetManager);
            var config = XMLDeserializer.Deserialize("config.xml");
            m_magnetManager.Coulomb = config.CoulombConstant;
            m_magnetManager.MaxDistance = config.MaxDistance;
            m_magnetManager.transform.parent = m_mod.transform;
            m_skinLoader = SingleInstance<SkinLoader>.Instance;
            UnityEngine.Object.DontDestroyOnLoad(m_skinLoader);
            m_skinLoader.transform.parent = m_mod.transform;

            // pole type�̏�����
            PoleType = new List<string>
            {
                MagnetSpace.PoleType.North.ToString(),
                MagnetSpace.PoleType.South.ToString(),
            };

            // ���W���[���̓o�^
            CustomModules.AddBlockModule<MagnetModule, Magnet>("MagnetModule", true);

			Log("Load");
		}
		public static void Log(string msg) => Debug.Log($"Magnet: {msg}");
		public static void Warning(string msg) => Debug.LogWarning($"Magnet: {msg}");
		public static void Error(string msg) => Debug.LogError($"Magnet: {msg}");
	}
    /// <summary>
    /// IMonopole���Ǘ�����Singleton
    /// </summary>
    public class MagnetManager : SingleInstance<MagnetManager>
    {
        public override string Name => "Magnet Manager";
        /// <summary>
        /// �V�[����ɑ��݂���V�~�����[�V��������IMonopole
        /// </summary>
        public List<IMonopole> Monopoles;
        /// <summary>
        /// �N�[�����萔
        /// 
        /// �Q�l�F�n���ł�6.33e+4
        /// </summary>
        public float Coulomb
        {
            set; get;
        }
        /// <summary>
        /// �N�[�����̖@�����K�p�����u���b�N�̃}���n�b�^�������̍ő�l
        /// </summary>
        public float MaxDistance
        {
            set; get;
        }

        /// <summary>
        /// Monopoles�Ɉ�����ǉ�����
        /// �v�Z�ʂ̑�����h�����߁A�݂���ɌĂяo���Ȃ�����
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
        /// Monopoles����������폜����
        /// �v�Z�ʂ̑�����h�����߁A�݂���ɌĂяo���Ȃ�����
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

            // ���ꂼ��̃u���b�N�ɗ͂�������
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
        protected Transform m_pole;
        protected MSlider m_sliderChargeGain;
        protected MKey m_keyMagnetize;
        /// <summary>
        /// m_keyEnergize���������ꍇ�̋���
        /// 
        /// true�Ȃ牟�����̂ݎ�������
        /// false�Ȃ玥�����g�O��������
        /// </summary>
        protected MToggle m_toggleHoldToMagnetize;
        protected MMenu m_menuPoleType;
        /// <summary>
        /// ���̃u���b�N���d���΂ł���
        /// �iModule.KeyMagnetize�̗L���ɂ�蔻�f����j
        /// </summary>
        public bool IsElectromagnet
        {
            private set;
            get;
        } = false;
        /// <summary>
        /// ����������
        /// </summary>
        private bool m_isMagnetized;
        private PoleType m_lastPoleType = PoleType.None;
        #endregion
        #region skin
        /// <summary>
        /// MeshRenderer of m_filterNorth
        /// </summary>
        private MeshRenderer m_renderer;
        /// <summary>
        /// MeshFilter of m_filterNorth
        /// </summary>
        private MeshFilter m_filter;
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
        /// �ɂ̏�Ԃɉ�����MagnetModMesh��^����
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
        /// �ɂ̏�Ԃɉ�����MagnetModTexture��^����
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
        public bool IsMagnetized() => m_isMagnetized;
        /// <summary>
        /// PoleType���擾����
        /// 
        /// enum PoleType �� Mod.PoleType��index����擾����
        /// </summary>
        /// <returns></returns>
        public PoleType GetPoleType()
        {
            var value = m_menuPoleType.Value; // 0=N 1=S
            var pole = value == 0 ? PoleType.North : value == 1 ? PoleType.South : PoleType.None;

            // �V�R���΂̏ꍇ
            if (!IsElectromagnet)
            {
                return pole;
            }

            // �d���΂̏ꍇ
            // �r���h����N��S
            if (!BlockBehaviour.isSimulating)
            {
                return pole;
            }
            // �V�~�����͋N������N��S�A�����łȂ����Off
            if (m_isMagnetized)
            {
                return pole;
            }
            return PoleType.None;
        }
        public Vector3 GetPolePosition() => m_pole.position;
        public float GetCharge() => Module.Charge * m_sliderChargeGain.Value * (int)GetPoleType();
        public Vector3 GetForce(IMonopole other)
        {
            var distance = other.GetPolePosition() - GetPolePosition();

            // xyz�̂����ꂩ��max distance�𒴂���ꍇ�͌v�Z���Ȃ�
            var maxDistance = MagnetManager.Instance.MaxDistance;
            if (maxDistance < distance.x || maxDistance < distance.y || maxDistance < distance.z)
            {
                return Vector3.zero;
            }

            var scalarDistance = distance.magnitude;
            return -MagnetManager.Instance.Coulomb * GetCharge() * other.GetCharge() * distance / (scalarDistance * scalarDistance * scalarDistance);
        }
        public void AddForce(Vector3 force)
        {
            BlockBehaviour.Rigidbody.AddForceAtPosition(force, m_pole.position);
        }
        #endregion
        public BlockVisualController GetVisualController() => GetComponent<BlockVisualController>();
        /// <summary>
        /// �X�L����ύX����
        /// 
        /// Mesh��Texture��ύX����^�C�~���O
        /// - �X�L����ύX������
        /// - PoleType��ύX������
        /// </summary>
        public void SetSkin()
        {
            //Mod.Log($"Set Skin test");
            m_skinName = OptionsMaster.skinsEnabled ? m_vis.selectedSkin.pack.name : SkinLoader.Instance.DefaultSkinName;
            //m_skinName = m_vis.selectedSkin.pack.name;
            var poleType = GetPoleType();
            /*
            if (m_lastPoleType != poleType)
            {
                Mod.Log($"Pole type has changed from {m_lastPoleType} to {poleType}");

                m_lastPoleType = poleType;

                // �X�L����ς���
                #region null���
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

                // �f�t�H���g�X�L��
                if (m_skinName == SkinLoader.Instance.DefaultSkinName)
                {
                    // �{�̂̃X�L����K�p����
                    if (!m_hasChangedMesh && !m_hasChangedTexture)
                    {
                        //GetModMesh(poleType).ApplyToObject(m_filter);
                        GetModMesh(poleType).ApplyToObject(m_vis.MeshFilter);
                        //GetModTexture(poleType).ApplyToObject(m_renderer);
                        GetModTexture(poleType).ApplyToObject(m_vis.renderers[0]);
                        Mod.Log($"poleType: {poleType}");

                        m_hasChangedMesh = true;
                        m_hasChangedTexture = true;
                    }
                }

                // �f�t�H���g�łȂ��X�L��
                else
                {
                    // �{�̂̃X�L����K�p����
                    var mesh = GetModMesh(poleType);
                    if (mesh.IsLoaded && !m_hasChangedMesh)
                    {
                        m_hasChangedMesh = true;
                        if (!mesh.HasError)
                        {
                            mesh.ApplyToObject(m_filter);
                        }
                    }

                    var tex = GetModTexture(poleType);
                    if (tex.IsLoaded && !m_hasChangedTexture)
                    {
                        m_hasChangedTexture = true;
                        if (!tex.HasError)
                        {
                            tex.ApplyToObject(m_renderer);
                        }
                    }
                }
            }
            */

            // �X�L�����ύX���ꂽ�ꍇ�܂��̓X�L�������[�h���Ă���Œ��̏ꍇ
            // m_skinOff m_skinNorth m_skinSouth���擾����
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

                #region m_skinOff m_skinNorth m_skinSouth���擾����
                var modSkinsOff = SkinLoader.Instance.modSkinsOff;
                var modSkinsNorth = SkinLoader.Instance.ModSkinsNorth;
                var modSkinsSouth = SkinLoader.Instance.ModSkinsSouth;

                // �X�L���p�b�N���o�^����Ă��Ȃ���Γo�^����
                if (!modSkinsOff.ContainsKey(m_skinName))
                {
                    var skinPack = new SkinLoader.SkinDataPack();
                    modSkinsOff.Add(m_skinName, skinPack);
                }
                // �X�L���p�b�N�����ɓo�^����Ă���
                else
                {
                    // ���̃u���b�N�̃X�L�����X�L���p�b�N�ɓo�^����Ă���΁A������擾����
                    if (modSkinsOff[m_skinName].Skins.ContainsKey(BlockName))
                    {
                        m_skinOff = modSkinsOff[m_skinName].Skins[BlockName];
                    }

                    // ���̃u���b�N�̃X�L�����X�L���p�b�N�ɓo�^����Ă��Ȃ�
                    else
                    {
                        // �X�L�����X�L���t�H���_���烍�[�h���ăX�L���p�b�N�ɓo�^����
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

                    // �X�L���̃��[�h���I�����
                    m_isLoadingOffSkin = false;
                }

                // N�ɂ�S�ɂ�����
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
                        // �X�L�����X�L���t�H���_���烍�[�h����
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
                        // �X�L�����X�L���t�H���_���烍�[�h����
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

            // ����ȊO�B�قږ��t���[���Ăяo��
            else
            {
                #region null���
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

                //Mod.Log("test2");
                // �f�t�H���g�X�L��
                if (m_skinName == SkinLoader.Instance.DefaultSkinName)
                {
                    //Mod.Log("test3");
                    // �{�̂̃X�L����K�p����
                    if (!m_hasChangedMesh)
                    {
                        //Mod.Log("test5");
                        GetModMesh(poleType).ApplyToObject(m_vis.MeshFilter);
                        m_hasChangedMesh = true;
                    }
                    if (!m_hasChangedTexture)
                    {
                        //Mod.Log("test6");
                        //GetModMesh(poleType).ApplyToObject(m_vis.MeshFilter);
                        //GetModTexture(poleType).ApplyToObject(m_vis.renderers[0]);
                        var tex = GetModTexture(poleType);
                        tex.ApplyToObject(m_renderer);
                        m_hasChangedTexture = true;
                    }
                }

                // �f�t�H���g�łȂ��X�L��
                else
                {
                    // �{�̂̃X�L����K�p����
                    var mesh = GetModMesh(poleType);
                    if (mesh.IsLoaded && !m_hasChangedMesh)
                    {
                        m_hasChangedMesh = true;
                        if (!mesh.HasError)
                        {
                            mesh.ApplyToObject(m_filter);
                        }
                    }

                    var tex = GetModTexture(poleType);
                    if (tex.IsLoaded && !m_hasChangedTexture)
                    {
                        m_hasChangedTexture = true;
                        if (!tex.HasError)
                        {
                            tex.ApplyToObject(m_renderer);
                        }
                    }
                }
            }
        }

        #region event
        public override void SafeAwake()
        {
            base.SafeAwake();

            #region UI����
            try
            {
                if (Module.SliderChargeGain != null) { m_sliderChargeGain = GetSlider(Module.SliderChargeGain); }
                if (IsElectromagnet = Module.KeyMagnetize != null)
                {
                    m_keyMagnetize = GetKey(Module.KeyMagnetize);
                    if (Module.ToggleHoldToMagnetize != null)
                    {
                        m_toggleHoldToMagnetize = GetToggle(Module.ToggleHoldToMagnetize);
                    }
                    m_isMagnetized = false;
                }
                else
                {
                    // �d���΂ł͂Ȃ��Ȃ��Ɏ���������
                    m_isMagnetized = true;
                }
                m_menuPoleType = AddMenu("pole-type", 0, Mod.PoleType);
            }
            catch (Exception e)
            {
                Mod.Error("Could not get mapper types for Magnet module! module will be destroyed.");
                Mod.Error($"{e}");
                Destroy(this);
                return;
            }
            #endregion

            #region pole����
            m_pole = new GameObject("Pole").transform;
            m_pole.parent = transform;
            m_pole.localPosition = Module.PoleTransform.Position;
            m_pole.localEulerAngles = Module.PoleTransform.Rotation;
            m_pole.localScale = Module.PoleTransform.Scale;
            #endregion

            #region �X�L��������
            m_vis = GetVisualController();
            m_renderer = m_vis.renderers[0];
            #endregion
        }
        /// <summary>
        /// �����ڂ��X�V����i�r���h���j
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
        /// MagnetManager�Ɏ��g��o�^����
        /// </summary>
        public override void OnSimulateStart()
        {
            MagnetManager.Instance.Add(this);
        }
        /// <summary>
        /// �L�[������󂯕t����
        /// </summary>
        public override void SimulateUpdateAlways()
        {
            // �d���΂łȂ��Ȃ牽�����Ȃ�
            if (!IsElectromagnet) { return; }

            // �������̂ݎ���
            if (m_toggleHoldToMagnetize.IsActive)
            {
                m_isMagnetized = m_keyMagnetize.IsHeld;
            }
            else
            {
                if (m_keyMagnetize.IsPressed)
                {
                    m_isMagnetized = !m_isMagnetized;
                }
            }
        }
        /// <summary>
        /// �G�~�����[�V�������󂯕t����
        /// </summary>
        public override void KeyEmulationUpdate()
        {
            // �d���΂łȂ��Ȃ牽�����Ȃ�
            if (!IsElectromagnet) { return; }

            if (m_toggleHoldToMagnetize.IsActive)
            {
                m_isMagnetized = m_keyMagnetize.EmulationHeld();
            }
            else
            {
                if (m_keyMagnetize.EmulationPressed())
                {
                    m_isMagnetized = !m_isMagnetized;
                }
            }
        }
        /// <summary>
        /// �����ڂ��X�V����i�V�~����/�z�X�g�j
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
        /// �����ڂ��X�V����i�r���h��/�N���C�A���g�j
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
        /// MagnetManager���玩�g��j������
        /// </summary>
        public void OnDestroy()
        {
            MagnetManager.Instance.Remove(this);
        }

        #endregion
    }
}
