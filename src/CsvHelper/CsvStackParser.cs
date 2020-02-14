#if NETSTANDARD2_1
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	public class CsvStackParser : IDisposable
	{
		private TextReader reader;
		private int bufferPosition;
		private int fieldStartPosition;
		private int fieldEndPosition;
		private int rawRecordStartPosition;
		private int rawRecordEndPosition;
		private List<string> row = new List<string>();
		private Memory<char> heapBuffer = new Memory<char>();
		private int charsRead;
		private int c = -1;
		private List<string> rawRecord = new List<string>();
		private char escape = '"';
		private string delimiter = ",";
		private string newLine = string.Empty;
		private bool leaveOpen;
		private CsvConfiguration configuration;

		public CsvConfiguration Configuration => configuration;

		public int Row { get; protected set; }

		public int RawRow { get; protected set; }

		public string RawRecord => BuildRawRecord();

		public CsvStackParser(TextReader reader, CultureInfo culture) : this(reader, culture, false) { }

		public CsvStackParser(TextReader reader, CultureInfo culture, bool leaveOpen) : this(reader, new CsvConfiguration(culture)) { }

		public CsvStackParser(TextReader reader, CsvConfiguration configuration) : this(reader, configuration, false) { }

		public CsvStackParser(TextReader reader, CsvConfiguration configuration, bool leaveOpen)
		{
			this.reader = reader;
			this.configuration = configuration;
			this.leaveOpen = leaveOpen;
		}

		public string[] Read()
		{
			Row++;
			RawRow++;
			row.Clear();
			rawRecord.Clear();

			var stackBuffer = heapBuffer.Span.Slice(0);

			while (true)
			{
				c = GetChar(ref stackBuffer);
				if (c == -1)
				{
					// End of file.
					if (row.Count > 0)
					{
						break;
					}

					return null;
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

			return row.ToArray();
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
			fieldEndPosition -= delimiter.Length;
			AppendField(ref stackBuffer);
			fieldStartPosition = bufferPosition;
			fieldEndPosition = fieldStartPosition;

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
				fieldEndPosition -= newLine.Length;
			}
			else if (c == '\r')
			{
				fieldEndPosition--;

				if (PeekChar(stackBuffer) == '\n')
				{
					c = GetChar(ref stackBuffer);
					fieldEndPosition--;
				}
			}
			else // \n
			{
				// Adjust position to account for new line.
				fieldEndPosition--;
			}

			AppendField(ref stackBuffer);
			fieldStartPosition = bufferPosition;
			fieldEndPosition = fieldStartPosition;

			//var start = rawRecordStartPosition;
			//var length = rawRecordEndPosition - rawRecordStartPosition;
			//rawRecord.Add(new string(heapBuffer.Span.Slice(start, length)));

			return true;
		}

		protected void AppendField(ref Span<char> stackBuffer)
		{
			var start = fieldStartPosition;
			var length = fieldEndPosition - fieldStartPosition;

			row.Add(new string(stackBuffer.Slice(start, length)));
		}

		protected int GetChar(ref Span<char> stackBuffer)
		{
			if (!FillBuffer(ref stackBuffer))
			{
				return -1;
			}

			var c = stackBuffer[bufferPosition];
			bufferPosition++;
			fieldEndPosition++;
			rawRecordEndPosition++;

			return c;
		}

		protected int PeekChar(Span<char> stackBuffer)
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

			// TODO: Only do this if a config flag is set to true.
			//if (rawRecordStartPosition < rawRecordEndPosition)
			//{
			//	// Append the raw record.
			//	rawRecord.Add(new string(heapBuffer.Span.Slice(rawRecordStartPosition, rawRecordEndPosition - rawRecordStartPosition)));
			//}

			var charsUsed = fieldStartPosition;
			var bufferLeft = charsRead - charsUsed;
			var bufferUsed = charsRead - bufferLeft;

			// Create a new buffer with room for chars that haven't been read from the current buffer.
			var tempBuffer = new char[bufferLeft + configuration.BufferSize];

			// Copy remaining old buffer to new buffer.
			stackBuffer.Slice(charsUsed).CopyTo(tempBuffer);

			// Read into the rest of the buffer.
			charsRead = reader.Read(tempBuffer, bufferLeft, configuration.BufferSize);
			if (charsRead == 0)
			{
				return false;
			}

			charsRead += bufferLeft;

			heapBuffer = new Memory<char>(tempBuffer);
			stackBuffer = heapBuffer.Span.Slice(0);

			bufferPosition = bufferPosition - bufferUsed;
			fieldStartPosition = fieldStartPosition - bufferUsed;
			fieldEndPosition = fieldEndPosition - bufferUsed;
			//rawRecordStartPosition = Math.Max(rawRecordStartPosition - bufferUsed, 0);
			//rawRecordEndPosition = rawRecordEndPosition - bufferUsed;

			return true;
		}

		protected string BuildRawRecord()
		{
			var length = rawRecord.Sum(x => x.Length);
			return string.Create(length, rawRecord, (span, rawRecord) =>
			{
				var position = 0;
				foreach (var item in rawRecord)
				{
					item.AsSpan().CopyTo(span.Slice(position));
					position += item.Length;
				}
			});
		}

		public void Dispose()
		{
		}
	}
}
#endif
