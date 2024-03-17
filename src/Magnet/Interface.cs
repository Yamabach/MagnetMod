using UnityEngine;

namespace MagnetSpace
{
    public enum PoleType
    {
        None   = -1,  // ��������
        North  = 0, // N��
        South  = 1, // S��
    }
    /// <summary>
	/// �P�Ɏq
	/// </summary>
	public interface IMonopole
    {
        /// <summary>
        /// ����������
        /// �i�d���Γ��ɑ΂��Ďg���j
        /// </summary>
        /// <returns></returns>
        bool IsMagnetized();
        /// <summary>
        /// �ɂ̎������擾����
        /// </summary>
        /// <returns></returns>
        PoleType GetPoleType();
        /// <summary>
        /// �ɂ̈ʒu���擾����
        /// </summary>
        /// <returns></returns>
        Vector3 GetPolePosition();
        /// <summary>
        /// ���C�ʂ��擾����
        /// </summary>
        /// <returns></returns>
        float GetCharge();
        /// <summary>
        /// ���͂��v�Z����
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        Vector3 GetForce(IMonopole other);
        /// <summary>
        /// Rigidbody�ɑ΂��ė͂�^����
        /// </summary>
        void AddForce(Vector3 force);
    }
}