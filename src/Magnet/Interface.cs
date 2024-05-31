using Modding;
using UnityEngine;

namespace MagnetSpace
{
    public enum PoleType
    {
        None   = 0,  // 磁性無し
        North  = 1,  // N極
        South  = -1, // S極
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

        /// <summary>
        /// 引数の位置に対する磁束密度（単位はガウス）を取得する
        /// </summary>
        /// <param name="position"></param>
        /// <returns>Gauss, equal to 1000T</returns>
        Vector3 GetMagneticFluxDensity(Vector3 position);
    }
    public interface IGaussmeter
    {
        PoleType GetPoleType();
        /// <summary>
        /// 極の位置を取得する
        /// </summary>
        /// <returns></returns>
        Vector3 GetPolePosition();
        /// <summary>
        /// 極の前方の方向を取得する
        /// </summary>
        /// <returns></returns>
        Vector3 GetPoleDirection();
        /// <summary>
        /// 引数のIMonopoleの磁束密度（単位はガウス）を取得する
        /// </summary>
        /// <param name="monopole"></param>
        /// <returns>Gauss, equal to 1000T</returns>
        float GetMagneticFluxDensity(IMonopole monopole);
    }
}