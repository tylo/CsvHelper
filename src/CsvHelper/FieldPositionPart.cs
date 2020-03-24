using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	/// <summary>
	/// Represents part of a field position.
	/// </summary>
	public class FieldPositionPart
	{
		/// <summary>
		/// Gets or sets the start of the field.
		/// </summary>
		public int Start { get; set; }

		/// <summary>
		/// Gets or sets the length of the field.
		/// </summary>
		public int Length { get; set; }
	}
}
