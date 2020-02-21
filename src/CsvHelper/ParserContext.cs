#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
    public class ParserContext
    {
		internal Memory<char> Buffer { get; set; } = new Memory<char>();

		internal List<FieldPosition> FieldPositions { get; set; } = new List<FieldPosition>();

		internal List<FieldPosition> RawFieldPositions { get; set; } = new List<FieldPosition>();

		internal FieldPosition RawRecordPosition { get; set; } = new FieldPosition();

		public string GetField(int index)
		{
			if (index >= FieldPositions.Count) throw new InvalidOperationException($"Field position at index '{index}' does not exist.");

			var position = FieldPositions[index];

			return new string(Buffer.Span.Slice(position.Start, position.Length));
		}

		public string GetRawField(int index)
		{
			if (index >= RawFieldPositions.Count) throw new InvalidOperationException($"Raw field position at index '{index}' does not exist.");

			var position = RawFieldPositions[index];

			return new string(Buffer.Span.Slice(position.Start, position.Length));
		}

		public string[] GetFields()
		{
			var fields = new string[FieldPositions.Count];
			for (var i = 0; i < FieldPositions.Count; i++)
			{
				var position = FieldPositions[i];
				fields[i] = new string(Buffer.Span.Slice(position.Start, position.Length));
			}

			return fields;
		}

		public string[] GetRawFields()
		{
			var rawFields = new string[RawFieldPositions.Count];
			for (var i = 0; i < RawFieldPositions.Count; i++)
			{
				var position = RawFieldPositions[i];
				rawFields[i] = new string(Buffer.Span.Slice(position.Start, position.Length));
			}

			return rawFields;
		}

		public string GetRawRecord()
		{
			return new string(Buffer.Span.Slice(RawRecordPosition.Start, RawRecordPosition.Length));
		}
	}
}
#endif
