using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	/// <summary>
	/// Keeps track of field positions parts.
	/// </summary>
	public class FieldPosition : IEnumerable<FieldPositionPart>
	{
		private FieldPositionPart[] parts;

		/// <summary>
		/// Gets the number of field position parts.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// Gets the current field position part.
		/// </summary>
		public FieldPositionPart Current => parts[Count - 1];

		/// <summary>
		/// Gets the <see cref="FieldPositionPart"/> at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <exception cref="IndexOutOfRangeException"></exception>
		public FieldPositionPart this[int index]
		{
			get
			{
				if (index >= Count)
				{
					throw new IndexOutOfRangeException();
				}

				return parts[index];
			}
		}

		/// <summary>
		/// Gets the position parts.
		/// </summary>
		public FieldPositionPart[] Parts
		{
			get
			{
				var parts = new FieldPositionPart[Count];
				Array.Copy(this.parts, parts, Count);

				return parts;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldPosition"/> class.
		/// </summary>
		/// <param name="capacity">The initial capacity.</param>
		public FieldPosition(int capacity = 128)
		{
			parts = new FieldPositionPart[capacity];
			FillPositions();
		}

		/// <summary>
		/// Adds a new field position part.
		/// </summary>
		public void Add()
		{
			if (Count >= parts.Length)
			{
				var temp = new FieldPositionPart[parts.Length * 2];
				Array.Copy(parts, temp, parts.Length);
				parts = temp;
				FillPositions();
			}

			Count++;
		}

		/// <summary>
		/// Clears all field position parts.
		/// </summary>
		public void Clear()
		{
			Count = 0;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// An enumerator that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<FieldPositionPart> GetEnumerator()
		{
			for (var i = 0; i < Count; i++)
			{
				yield return parts[i];
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private void FillPositions()
		{
			for (var i = Count; i < parts.Length; i++)
			{
				parts[i] = new FieldPositionPart();
			}
		}
	}
}
