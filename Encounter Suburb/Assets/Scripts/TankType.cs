public enum TankType { Hunter, Pummel, Heavy }

public class TankUnitArray : TankTypeArray<TankUnit>
{
	public void Unload()
	{
		// Unload Units
		for (int i = 0; i < count; i++)
		{
			for (int ii = 0; ii < values[i].instances.Length; ii++)
			{
				UnityEngine.Object.Destroy(values[i].instances[ii].tank.gameObject);
			}
		}
	}
}

public class TankTypeArray<T>
{
	public static readonly int Count = System.Enum.GetNames(typeof(TankType)).Length;
	public int count => Count;
	
	protected readonly T[] values = new T[Count];

	public T this[TankType type]
	{
		get { return values[(int) type]; }
		set { values[(int) type] = value; }
	}

	public T this[int index]
	{
		get { return values[index]; }
		set { values[index] = value; }
	}
}