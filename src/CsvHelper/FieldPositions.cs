using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	/// <summary>
	/// Keeps track of field positions.
	/// </summary>
	public class FieldPositions
	{
		private FieldPosition[] positions;

		/// <summary>
		/// Gets the number of field positions.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// Gets the current field position.
		/// </summary>
		public FieldPosition Current => positions[Count - 1];

		/// <summary>
		/// Gets the <see cref="FieldPosition"/> at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <exception cref="IndexOutOfRangeException"></exception>
		public FieldPosition this[int index]
		{
			get
			{
				if (index >= Count)
				{
					throw new IndexOutOfRangeException();
				}

				return positions[index];
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldPositions"/> class.
		/// </summary>
		/// <param name="capacity">The initial capacity.</param>
		public FieldPositions(int capacity = 128)
		{
			positions = new FieldPosition[capacity];
			FillPositions();
		}

		/// <summary>
		/// Adds a new field position.
		/// </summary>
		public void Add()
		{
			if (Count >= positions.Length)
			{
				var temp = new FieldPosition[positions.Length * 2];
				Array.Copy(positions, temp, positions.Length);
				positions = temp;
				FillPositions();
			}

			Count++;
		}

		/// <summary>
		/// Clears all field positions.
		/// </summary>
		public void Clear()
		{
			Count = 0;
		}

		private void FillPositions()
		{
			for (var i = Count; i < positions.Length; i++)
			{
				positions[i] = new FieldPosition();
			}
		}
	}
}
