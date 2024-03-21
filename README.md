# Magnet mod / 磁石mod
磁石modは、Besiegeに便利な磁石を追加するものである。
磁石の記述はinterfaceとして実装したうえでモジュール化する。

Magnet mod adds useful magnets to Besiege.
The script of magnetic blocks is implemented as a combination of custom module and interface.

## Specifications / 仕様
磁性ブロックは以下のように示されるクーロンの法則に従う。
ここで、$`\textbf{F}`$は磁性ブロックにかかる力[N]、$`k`$はクーロン定数、$`m_{this}`$および$`m_{other}`$は彼我の磁気量[Wb]、$`\textbf{r}`$は彼我の距離[m]である。

Magnetic blocks follows Coulomb's law described as bellow, where $`\textbf{F}`$ is a force[N] applied to magnetic blocks, $`k`$ is Coulomb's constant, $`m_{this}`$ and $`m_{other}`$ are magnetic charges[Wb] of them, and $`\textbf{r}`$ is the distance between them.
```math
\textbf{F} = - \frac {k\ m_{this}\ m_{other}} {r^3} \textbf{r}
```
$`k`$はconfigファイルによって決定される。
$`m`$は磁性モジュールの記述によって1倍の時の数値が決定される。

We define $`k`$ in the config file.
We define the 1.0x value of $`m`$ in each block file.
```math
m = m_{module}\ m_{ingame}
```
計算量の増大を防ぐため、クーロンの法則が適用されるブロックの距離の最大値$`r_{max}`$[m]をconfigファイルによって予め定める。

To avoid an increase in the number of calculations, we define the maximum distance $`r_{max}`$[m] in the config file.
At distances shorter than this, Coulomb's law applies.
```math
\textbf{F} = - \frac {k\ m_{this}\ m_{other}} {r^3} \textbf{r} \quad (r < r_{max}) \quad \mathrm{or} \quad 0 \quad (r_{max} <= r)
```

## Implementation / 実装
```C#
public interface IMonopole
{
	// 極の位置を取得する Get the position of the pole
	public Vector3 GetPolePosition();
	// 磁気量を取得する Get the magnetic charge
	public float GetCharge();
	// 磁力を計算する Calcurate the magnetic force and get it
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
                // This will be implemented as Specifications
	}

	// 毎フレーム呼ばれる関数にAddForceを追加しておくこと
        // Write AddForce() in the function called in every frames.
	public void Update()
	{
		
	}
}
```

## Blocks / ブロック
単極子（monopole）を想定する。

We assume monopole.
- テスト用磁石 Magnet for test
- __天然磁石 Magnet__ 常に磁性を持つ
- __電磁石 Electromagnet__ キー入力時に磁性を持つ

## Entities / エンティティ
実装未定。/ TBD.
