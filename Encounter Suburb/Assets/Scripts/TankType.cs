public enum TankType { Hunter, Pummel, Heavy }

public class TankUnitArray
{
	public static readonly int Count = System.Enum.GetNames(typeof(TankType)).Length;
	public int count => Count;
	
	private readonly TankUnit[] units = new TankUnit[Count];

	public TankUnit this[TankType type]
	{
		get { return units[(int) type]; }
		set { units[(int) type] = value; }
	}

	public TankUnit this[int index]
	{
		get { return units[index]; }
		set { units[index] = value; }
	}

	public void Unload()
	{
		// Unload Units
		for (int i = 0; i < count; i++)
		{
			for (int ii = 0; ii < units[i].instances.Length; ii++)
			{
				UnityEngine.Object.Destroy(units[i].instances[ii].tank.gameObject);
			}
		}
	}
}

public class TankTypeArray<T>
{
	public static readonly int Count = System.Enum.GetNames(typeof(TankType)).Length;
	public int count => Count;
	
	private readonly T[] units = new T[Count];

	public T this[TankType type]
	{
		get { return units[(int) type]; }
		set { units[(int) type] = value; }
	}

	public T this[int index]
	{
		get { return units[index]; }
		set { units[index] = value; }
	}
}