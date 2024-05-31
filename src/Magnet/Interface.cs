using Modding;
using UnityEngine;

namespace MagnetSpace
{
    public enum PoleType
    {
        None   = 0,  // ��������
        North  = 1,  // N��
        South  = -1, // S��
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

        /// <summary>
        /// �����̈ʒu�ɑ΂��鎥�����x�i�P�ʂ̓K�E�X�j���擾����
        /// </summary>
        /// <param name="position"></param>
        /// <returns>Gauss, equal to 1000T</returns>
        Vector3 GetMagneticFluxDensity(Vector3 position);
    }
    public interface IGaussmeter
    {
        PoleType GetPoleType();
        /// <summary>
        /// �ɂ̈ʒu���擾����
        /// </summary>
        /// <returns></returns>
        Vector3 GetPolePosition();
        /// <summary>
        /// �ɂ̑O���̕������擾����
        /// </summary>
        /// <returns></returns>
        Vector3 GetPoleDirection();
        /// <summary>
        /// ������IMonopole�̎������x�i�P�ʂ̓K�E�X�j���擾����
        /// </summary>
        /// <param name="monopole"></param>
        /// <returns>Gauss, equal to 1000T</returns>
        float GetMagneticFluxDensity(IMonopole monopole);
    }
}