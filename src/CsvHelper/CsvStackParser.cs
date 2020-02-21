#if NETSTANDARD2_1
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	public class CsvStackParser : IDisposable
	{
		private TextReader reader;
		private Memory<char> heapBuffer = new Memory<char>();
		private int bufferPosition;
		private int charsRead;
		private int c = -1;
		private char escape = '"';
		private string delimiter = ",";
		private string newLine = string.Empty;
		private bool leaveOpen;
		private CsvConfiguration configuration;
		private List<FieldPosition> fieldPositions = new List<FieldPosition>();
		private List<FieldPosition> rawFieldPositions = new List<FieldPosition>();
		private FieldPosition rawRecordPosition = new FieldPosition();
		private FieldPosition fieldPosition = new FieldPosition();
		private FieldPosition rawFieldPosition = new FieldPosition();
		
		public CsvConfiguration Configuration => configuration;

		public int Row { get; protected set; }

		public int RawRow { get; protected set; }

		public string RawRecord => new string(heapBuffer.Span.Slice(rawRecordPosition.Start, rawRecordPosition.Length));

		public string this[int index] => GetField(index);

		public CsvStackParser(TextReader reader, CultureInfo culture) : this(reader, culture, false) { }

		public CsvStackParser(TextReader reader, CultureInfo culture, bool leaveOpen) : this(reader, new CsvConfiguration(culture)) { }

		public CsvStackParser(TextReader reader, CsvConfiguration configuration) : this(reader, configuration, false) { }

		public CsvStackParser(TextReader reader, CsvConfiguration configuration, bool leaveOpen)
		{
			this.reader = reader;
			this.configuration = configuration;
			this.leaveOpen = leaveOpen;
		}

		public bool Read()
		{
			Row++;
			RawRow++;
			fieldPositions.Clear();
			rawFieldPositions.Clear();
			rawRecordPosition = new FieldPosition
			{
				Start = bufferPosition,
			};
			
			var stackBuffer = heapBuffer.Span.Slice(0);

			while (true)
			{
				c = GetChar(ref stackBuffer);
				if (c == -1)
				{
					// End of file.
					return false;
				}

				if (c == escape)
				{
					ReadQuotedField(ref stackBuffer);
				}
				else
				{
					if (ReadField(ref stackBuffer))
					{
						break;
					}
				}
			}

			return true;
		}

		public string GetField(int index)
		{
			if (index >= fieldPositions.Count) throw new InvalidOperationException($"Field position at index '{index}' does not exist.");

			var position = fieldPositions[index];

			return new string(heapBuffer.Span.Slice(position.Start, position.Length));
		}

		public string GetRawField(int index)
		{
			if (index >= rawFieldPositions.Count) throw new InvalidOperationException($"Raw field position at index '{index}' does not exist.");

			var position = rawFieldPositions[index];

			return new string(heapBuffer.Span.Slice(position.Start, position.Length));
		}

		public string[] GetFields()
		{
			var fields = new string[fieldPositions.Count];
			for (var i = 0; i < fieldPositions.Count; i++)
			{
				var position = fieldPositions[i];
				fields[i] = new string(heapBuffer.Span.Slice(position.Start, position.Length));
			}

			return fields;
		}

		public string[] GetRawFields()
		{
			var rawFields = new string[rawFieldPositions.Count];
			for (var i = 0; i < rawFieldPositions.Count; i++)
			{
				var position = rawFieldPositions[i];
				rawFields[i] = new string(heapBuffer.Span.Slice(position.Start, position.Length));
			}

			return rawFields;
		}

		protected bool ReadField(ref Span<char> stackBuffer)
		{
			while (true)
			{
				if (c == delimiter[0])
				{
					if (ReadDelimiter(ref stackBuffer))
					{
						return false;
					}
				}
				else if ((newLine.Length > 0 && c == newLine[0]) || c == '\r' || c == '\n')
				{
					if (ReadLineEnding(ref stackBuffer))
					{
						return true;
					}
				}

				c = GetChar(ref stackBuffer);
			}
		}

		protected bool ReadQuotedField(ref Span<char> stackBuffer)
		{
			throw new NotImplementedException();
		}

		protected bool ReadDelimiter(ref Span<char> stackBuffer)
		{
			Debug.Assert(c == delimiter[0], "Tried reading a delimiter when the first delimiter char didn't match the current char.");

			if (delimiter.Length > 1)
			{
				for (var i = 1; i < delimiter.Length; i++)
				{
					c = GetChar(ref stackBuffer);
					if (c != delimiter[i])
					{
						return false;
					}
				}
			}

			// Adjust end position to account for delmiter.
			fieldPosition.Length -= delimiter.Length;
			rawRecordPosition.Length -= delimiter.Length;

			fieldPositions.Add(fieldPosition);
			rawFieldPositions.Add(rawFieldPosition);

			fieldPosition = new FieldPosition
			{
				Start = bufferPosition,
			};
			rawFieldPosition = new FieldPosition
			{
				Start = bufferPosition,
			};

			return true;
		}

		protected bool ReadLineEnding(ref Span<char> stackBuffer)
		{
			Debug.Assert((newLine.Length > 0 && c != newLine[0]) || c != '\r' || c != '\n', "Tried reading a line ending when the first delimiter char didn't match the current char and wasn't \\r or \\n.");

			if (newLine.Length > 0 && c == newLine[0])
			{
				if (newLine.Length == 1)
				{
					return true;
				}

				for (var i = 1; i < newLine.Length; i++)
				{
					c = GetChar(ref stackBuffer);
					if (c != newLine[i])
					{
						return false;
					}

					return true;
				}

				// Adjust position to account for new line.
				fieldPosition.Length -= newLine.Length;
				rawFieldPosition.Length -= newLine.Length;
			}
			else if (c == '\r')
			{
				fieldPosition.Length--;
				rawFieldPosition.Length--;

				if (PeekChar(ref stackBuffer) == '\n')
				{
					c = GetChar(ref stackBuffer);
					fieldPosition.Length--;
					rawFieldPosition.Length--;
				}
			}
			else // \n
			{
				// Adjust position to account for new line.
				fieldPosition.Length--;
				rawFieldPosition.Length--;
			}

			fieldPositions.Add(fieldPosition);
			rawFieldPositions.Add(rawFieldPosition);

			fieldPosition = new FieldPosition
			{
				Start = bufferPosition,
			};
			rawFieldPosition = new FieldPosition
			{
				Start = bufferPosition,
			};

			return true;
		}

		protected int GetChar(ref Span<char> stackBuffer)
		{
			if (!FillBuffer(ref stackBuffer))
			{
				return -1;
			}

			var c = stackBuffer[bufferPosition];
			bufferPosition++;
			fieldPosition.Length++;
			rawFieldPosition.Length++;
			rawRecordPosition.Length++;

			return c;
		}

		protected int PeekChar(ref Span<char> stackBuffer)
		{
			if (bufferPosition < charsRead)
			{
				return stackBuffer[bufferPosition];
			}

			return reader.Peek();
		}

		protected bool FillBuffer(ref Span<char> stackBuffer)
		{
			// The buffer doesn't need to be filled yet.
			if (bufferPosition < charsRead)
			{
				return true;
			}

			var charsUsed = rawRecordPosition.Start;
			var bufferLeft = charsRead - charsUsed;
			var bufferUsed = charsRead - bufferLeft;

			// Create a new buffer with room for chars that haven't been read from the current buffer.
			Span<char> tempBuffer = stackalloc char[bufferLeft + configuration.BufferSize];

			// Copy remaining old buffer to new buffer.
			stackBuffer.Slice(charsUsed).CopyTo(tempBuffer);

			// Read into the rest of the buffer.
			charsRead = reader.Read(tempBuffer.Slice(bufferLeft));
			if (charsRead == 0)
			{
				return false;
			}

			charsRead += bufferLeft;

			heapBuffer = new Memory<char>(tempBuffer.ToArray());
			stackBuffer = heapBuffer.Span.Slice(0);

			bufferPosition = bufferPosition - bufferUsed;
			fieldPosition.Start = fieldPosition.Start - bufferUsed;
			rawFieldPosition.Start = rawFieldPosition.Start - bufferUsed;
			rawRecordPosition.Start = rawRecordPosition.Start - bufferUsed;

			return true;
		}

		public void Dispose()
		{
		}
	}
}
#endif
