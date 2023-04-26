using System;
using System.Collections;
using System.Collections.Generic;
using BH.Model;

namespace BH.Components
{
	public sealed class ListRef<T> : IEnumerable<T> where T : struct, IEquatable<CxId>
	{
		private const int SIZE_BLOCK_I = 16;

		private struct Block
		{
			public T[] Data;
			public int Size;
		}

		private T _shared;

		private readonly List<Block> _blocks = new();

		private void CreateBlock(ref T value)
		{
			Block block;
			block.Data = new T[SIZE_BLOCK_I];
			block.Data[0] = value;
			block.Size = 1;
			_blocks.Add(block);
		}

		private void DeFragmentBlock(int indexBlock, int indexInBlock)
		{
			var block = _blocks[indexBlock];

			block.Size--;

			if(block.Size == 0)
			{
				_blocks.RemoveAt(indexBlock);
			}
			else
			{
				for(var index = indexInBlock; index < block.Size; index++)
				{
					block.Data[index] = block.Data[index + 1];
				}

				_blocks[indexBlock] = block;
			}
		}

		public void Add(T value)
		{
			var indexBlock = -1;
			while(++indexBlock < _blocks.Count)
			{
				var block = _blocks[indexBlock];
				if(block.Size < SIZE_BLOCK_I)
				{
					var indexInBlock = block.Size;
					block.Data[indexInBlock] = value;
					block.Size++;
					_blocks[indexBlock] = block;

					return;
				}
			}

			CreateBlock(ref value);
		}

		public T Remove(CxId id, out bool contains)
		{
			var indexBlock = 0;
			var indexInBlock = -1;
			var indexToRemove = -1;

			while(indexBlock < _blocks.Count)
			{
				var block = _blocks[indexBlock];
				indexInBlock++;
				if(indexInBlock == block.Size)
				{
					indexInBlock = -1;
					indexBlock++;
					continue;
				}
				if(block.Data[indexInBlock].Equals(id))
				{
					indexToRemove = indexInBlock;
					break;
				}
			}

			if(indexToRemove != -1)
			{
				contains = true;
				var result = _blocks[indexBlock].Data[indexToRemove];
				DeFragmentBlock(indexBlock, indexToRemove);

				return result;
			}

			contains = false;
			return default;
		}

		public int RemoveAll(Predicate<T> predicate)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get
			{
				var result = 0;
				for(var index = 0; index < _blocks.Count; index++)
				{
					result += _blocks[index].Size;
				}

				return result;
			}
		}

		public ref T this[int index]
		{
			get
			{
				for(var indexBlock = 0; indexBlock < _blocks.Count; indexBlock++)
				{
					var block = _blocks[indexBlock];
					if(index - block.Size < 0)
					{
						return ref block.Data[index];
					}

					index -= block.Size;
				}

				throw new IndexOutOfRangeException();
			}
		}

		public ref T Get(CxId id, out bool contains)
		{
			for(var indexBlock = 0; indexBlock < _blocks.Count; indexBlock++)
			{
				var block = _blocks[indexBlock];
				for(var indexInBlock = 0; indexInBlock < block.Size; indexInBlock++)
				{
					if(block.Data[indexInBlock].Equals(id))
					{
						contains = true;
						return ref block.Data[indexInBlock];
					}
				}
			}

			contains = false;
			return ref _shared;
		}

		public ref T Find(Predicate<T> filter, out bool contains)
		{
			for(var indexBlock = 0; indexBlock < _blocks.Count; indexBlock++)
			{
				var block = _blocks[indexBlock];
				for(var indexInBlock = 0; indexInBlock < block.Size; indexInBlock++)
				{
					if(filter.Invoke(block.Data[indexInBlock]))
					{
						contains = true;
						return ref block.Data[indexInBlock];
					}
				}
			}

			contains = false;
			return ref _shared;
		}

		public void Clear()
		{
			_blocks.Clear();
		}

		public void Enqueue(T value)
		{
			var indexBlock = _blocks.Count - 1;
			if(indexBlock == -1)
			{
				Add(value);
			}
			else
			{
				var block = _blocks[indexBlock];
				if(block.Size < SIZE_BLOCK_I)
				{
					block.Data[block.Size] = value;
					block.Size++;
					_blocks[indexBlock] = block;
				}
				else
				{
					CreateBlock(ref value);
				}
			}
		}

		public T Dequeue(out bool contains)
		{
			contains = _blocks.Count > 0 && _blocks[0].Size > 0;
			var result = contains ? _blocks[0].Data[0] : _shared;
			DeFragmentBlock(0, 0);
			return result;
		}

		public void DeFragment()
		{
			// just growing now
			// throw new NotImplementedException();
		}

		private sealed class Enumerator : IEnumerator<T>
		{
			private readonly ListRef<T> _source;
			private readonly int _size;
			private int _indexCurrent = -1;

			public Enumerator(ListRef<T> source)
			{
				_source = source;
				_size = _source.Count;
			}

			public bool MoveNext()
			{
				_indexCurrent++;
				if(_indexCurrent < _size)
				{
					Current = _source[_indexCurrent];
					return true;
				}

				_indexCurrent = _size;
				return false;
			}

			public T Current { get; private set; }

			object IEnumerator.Current => Current;

			public void Reset() { }
			public void Dispose() { }
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
