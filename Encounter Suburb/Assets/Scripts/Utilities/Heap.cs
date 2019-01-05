using System;

// This is mostly needed for fast contains check
public interface IHeapItem<T> : IComparable<T>
{
	int heapIndex { get; set; }
}

public class Heap<T> where T : IHeapItem<T>
{
	private readonly T[] items;
	public int Count { get; private set; }

	public Heap(int capacity)
	{
		Count = 0;
		items = new T[capacity];
	}

	public void Add(T item)
	{
		item.heapIndex = Count;
		items[Count] = item;
		SortUp(item);
		Count++;
	}

	public T PopFirst()
	{
		T popped = items[0];
		Count--;
		
		items[0] = items[Count];
		items[0].heapIndex = 0;
		SortDown(0);
		
		return popped;
	}

	public bool Contains(T item)
	{
		return Equals(item, items[item.heapIndex]);
	}

	public void RefreshItem(T item)
	{
		SortUp(item);
//		SortDown(item.heapIndex);
	}

	private void SortUp(T item)
	{
		int parentIndex = (item.heapIndex - 1) / 2;
		
		while (true)
		{
			T parent = items[parentIndex];
			if (item.CompareTo(parent) > 0)
			{
				Swap(item.heapIndex, parentIndex);
			}
			else
			{
				break;
			}

			parentIndex = (item.heapIndex - 1) / 2;
		}
	}
	
	private void SortDown(int i)
	{
		while (true)
		{
			int leftChild = i * 2 + 1;
			int rightChild = i * 2 + 2;

			if (leftChild < Count)
			{
				int swapIndex = leftChild;
				if (rightChild < Count && items[rightChild].CompareTo(items[leftChild]) > 0)
				{
					swapIndex = rightChild;
				}

				if (items[i].CompareTo(items[swapIndex]) < 0)
				{
					Swap(i, swapIndex);
					i = swapIndex;
				}
				else
				{
					return;
				}
			}
			else
			{
				return;
			}
		}
	}
	
	private void Swap(int a, int b)
	{
		T temp = items[a];
		
		items[a] = items[b];
		items[a].heapIndex = a;

		items[b] = temp;
		items[b].heapIndex = b;
	}
}