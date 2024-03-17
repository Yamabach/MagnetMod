using UnityEngine;

namespace MagnetSpace
{
    public enum PoleType
    {
        None   = -1,  // 磁性無し
        North  = 0, // N極
        South  = 1, // S極
    }
    /// <summary>
	/// 単極子
	/// </summary>
	public interface IMonopole
    {
        /// <summary>
        /// 磁性がある
        /// （電磁石等に対して使う）
        /// </summary>
        /// <returns></returns>
        bool IsMagnetized();
        /// <summary>
        /// 極の磁性を取得する
        /// </summary>
        /// <returns></returns>
        PoleType GetPoleType();
        /// <summary>
        /// 極の位置を取得する
        /// </summary>
        /// <returns></returns>
        Vector3 GetPolePosition();
        /// <summary>
        /// 磁気量を取得する
        /// </summary>
        /// <returns></returns>
        float GetCharge();
        /// <summary>
        /// 磁力を計算する
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        Vector3 GetForce(IMonopole other);
        /// <summary>
        /// Rigidbodyに対して力を与える
        /// </summary>
        void AddForce(Vector3 force);
    }
}