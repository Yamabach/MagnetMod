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
        #region singleton
        private MagnetManager m_magnetManager;
        public MagnetManager MagnetManager => m_magnetManager;
        #endregion
        #region message
        /// <summary>
        /// �d���΂ɒʓd�������Ƀz�X�g����N���C�A���g�ɑ����郁�b�Z�[�W
        /// </summary>
        public static MessageType msgEnergize;
        #endregion
        public static List<string> PoleType;

        public override void OnLoad()
		{
            // �C���X�^���X�̏�����
            m_magnetManager = SingleInstance<MagnetManager>.Instance;
            UnityEngine.Object.DontDestroyOnLoad(m_magnetManager);
            var config = XMLDeserializer.Deserialize("config.xml");
            m_magnetManager.Coulomb = config.CoulombConstant;
            m_magnetManager.MaxDistance = config.MaxDistance;

            // pole type�̏�����
            PoleType = new List<string>
            {
                ((PoleType)0).ToString(),
                ((PoleType)1).ToString(),
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
    public class Magnet : BlockModuleBehaviour<MagnetModule>, IMonopole
    {
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
            var value = m_menuPoleType.Value;
            return (PoleType)value;
        }
        public Vector3 GetPolePosition() => m_pole.position;
        public float GetCharge() => Module.Charge * m_sliderChargeGain.Value * (GetPoleType() == PoleType.North ? 1f : -1f);
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
        /// ���g���󂯂�͂��v�Z����Rigidbody�ɗ^����
        /// </summary>
        public override void SimulateFixedUpdateHost()
        {
            /*
            // ������������Ή������Ȃ�
            if (!m_isMagnetized || GetPoleType() == PoleType.None) { return; }

            var monopoles = MagnetManager.Instance.Monopoles;
            var force = Vector3.zero;
            for (int i= 0; i<monopoles.Count; i++)
            {
                // ���g�͏��O����
                if (monopoles[i] == null || monopoles[i] == this as IMonopole)
                {
                    continue;
                }
                // ����Ɏ�����������Ή������Ȃ�
                if (!monopoles[i].IsMagnetized() || monopoles[i].GetPoleType() == PoleType.None)
                {
                    continue;
                }

                force += GetForce(monopoles[i]);
            }
            BlockBehaviour.Rigidbody.AddForceAtPosition(force, m_pole.position);
            */
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
