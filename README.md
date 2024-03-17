# Magnet mod / 磁石mod
磁石modは、Besiegeに便利な磁石を追加するものである。
磁石の記述はinterfaceとして実装したうえでモジュール化する。

## Specifications / 仕様
磁性ブロックは以下のように示されるクーロンの法則に従う。
ここで、$`\textbf{F}`$は磁性ブロックにかかる力[N]、$`k`$はクーロン定数、$`m_{this}`$および$`m_{other}`$は彼我の磁気量[Wb]、$`\textbf{r}`$は彼我の距離[m]である。
```math
\textbf{F} = - \frac {k\ m_{this}\ m_{other}} {r^3} \textbf{r}
```
$`k`$はconfigファイルによって決定される。
$`m`$は磁性モジュールの記述によって1倍の時の数値が決定される。
```math
m = m_{module}\ m_{ingame}
```
計算量の増大を防ぐため、クーロンの法則が適用されるブロックの距離の最大値$`r_{max}`$[m]をconfigファイルによって予め定める。
```math
\textbf{F} = - \frac {k\ m_{this}\ m_{other}} {r^3} \textbf{r} \quad (r < r_{max}) \quad \mathrm{or} \quad 0 \quad (r_{max} <= r)
```

## Implementation / 実装
```C#
public interface IMonopole
{
	// 極の位置を取得する
	public Vector3 GetPolePosition();
	// 磁気量を取得する
	public float GetCharge();
	// 磁力を計算する
	public float GetForce(Monopole other);
}
public class Monopole : CustomModule, IMonopole
{
	protected Transform m_pole;
	public Vector3 GetPolePosition() => m_pole.position;
	protected float m_charge;
	public float GetCharge() => m_charge;
	public float GetForce(Monopole other)
	{
		// 仕様通りに実装する
	}

	// 毎フレーム呼ばれる関数にAddForceを追加しておくこと
	public void Update()
	{
		
	}
}
```

## Blocks / ブロック
単極子（monopole）を想定する。
- テスト用磁石
- __天然磁石__ 常に磁性を持つ
- __電磁石__ キー入力時に磁性を持つ

## Entities / エンティティ
実装未定。
