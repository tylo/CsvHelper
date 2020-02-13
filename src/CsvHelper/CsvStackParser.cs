#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	public class CsvStackParser : IDisposable
	{
		private TextReader reader;
		private int bufferSize = 1024;
		private int heapBufferPosition;
		private int heapRawRecordStartPosition;
		private int heapRawRecordEndPosition;
		private int stackBufferPosition;
		private int stackFieldStartPosition;
		private int stackFieldEndPosition;
		private string delimiter = ",";
		private char escape = '"';
		private List<string> row = new List<string>();
		private Memory<char> heapBuffer = new Memory<char>();
		private int charsRead;
		private int c = -1;
		private List<string> rawRecord = new List<string>();
		private string newLine = "";

		public int Row { get; protected set; }

		public int RawRow { get; protected set; }

		public string RawRecord => BuildRawRecord();

		public CsvStackParser(TextReader reader)
		{
			this.reader = reader;
		}

		public virtual string[] Read()
		{
			Row++;
			RawRow++;
			row.Clear();
			rawRecord.Clear();
			stackBufferPosition = 0;
			stackFieldStartPosition = 0;
			stackFieldEndPosition = 0;
			heapRawRecordStartPosition = heapBufferPosition;
			heapRawRecordEndPosition = heapRawRecordStartPosition;

			var stackBuffer = new Span<char>();

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

		protected virtual bool ReadField(ref Span<char> stackBuffer)
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

		protected virtual bool ReadQuotedField(ref Span<char> stackBuffer)
		{
			throw new NotImplementedException();
		}

		protected virtual bool ReadDelimiter(ref Span<char> stackBuffer)
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
			stackFieldEndPosition -= delimiter.Length;
			AppendField(ref stackBuffer);
			stackFieldStartPosition = stackBufferPosition;
			stackFieldEndPosition = stackFieldStartPosition;

			return true;
		}

		protected virtual bool ReadLineEnding(ref Span<char> stackBuffer)
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
				stackFieldEndPosition -= newLine.Length;
			}
			else if (c == '\r')
			{
				stackFieldEndPosition--;

				if (PeekChar(stackBuffer) == '\n')
				{
					c = GetChar(ref stackBuffer);
					stackFieldEndPosition--;
				}
			}
			else // \n
			{
				// Adjust position to account for new line.
				stackFieldEndPosition--;
			}

			AppendField(ref stackBuffer);
			stackFieldStartPosition = stackBufferPosition;
			stackFieldEndPosition = stackFieldStartPosition;

			var start = heapRawRecordStartPosition;
			var length = heapRawRecordEndPosition - heapRawRecordStartPosition;
			rawRecord.Add(new string(heapBuffer.Span.Slice(start, length)));

			return true;
		}

		protected virtual void AppendField(ref Span<char> stackBuffer)
		{
			var start = stackFieldStartPosition;
			var length = stackFieldEndPosition - stackFieldStartPosition;

			row.Add(new string(stackBuffer.Slice(start, length)));
		}

		protected virtual int GetChar(ref Span<char> stackBuffer)
		{
			if (!FillBuffer(ref stackBuffer))
			{
				return -1;
			}

			var c = stackBuffer[stackBufferPosition];
			heapBufferPosition++;
			heapRawRecordEndPosition++;
			stackBufferPosition++;
			stackFieldEndPosition++;

			return c;
		}

		protected virtual int PeekChar(Span<char> stackBuffer)
		{
			if (stackBufferPosition < charsRead)
			{
				return stackBuffer[stackBufferPosition];
			}

			return reader.Peek();
		}

		protected virtual bool FillBuffer(ref Span<char> stackBuffer)
		{
			// Initialize the stack buffer.
			if (stackBuffer.Length == 0)
			{
				stackBuffer = heapBuffer.Span.Slice(heapBufferPosition);
			}

			// 
			if (heapBufferPosition < charsRead)
			{
				return true;
			}

			// TODO: Only do this if a config flag is set to true.
			if (heapRawRecordStartPosition < heapRawRecordEndPosition)
			{
				// Append the raw record.
				rawRecord.Add(new string(heapBuffer.Span.Slice(heapRawRecordStartPosition, heapRawRecordEndPosition - heapRawRecordStartPosition)));
				heapRawRecordStartPosition = heapBufferPosition;
				heapRawRecordEndPosition = heapRawRecordStartPosition;
			}

			var charsUsed = stackFieldStartPosition;
			var bufferLeft = charsRead - charsUsed;
			var bufferUsed = charsRead - bufferLeft;

			// Create a new buffer with room for chars that haven't been read from the current buffer.
			var tempBuffer = new char[bufferLeft + bufferSize];

			// Copy remaining old buffer to new buffer.
			stackBuffer.Slice(charsUsed).CopyTo(tempBuffer);

			// Read into the rest of the buffer.
			charsRead = reader.Read(tempBuffer, bufferLeft, bufferSize);
			if (charsRead == 0)
			{
				return false;
			}

			charsRead += bufferLeft;

			heapBuffer = new Memory<char>(tempBuffer);
			heapBufferPosition = heapBufferPosition - bufferUsed;
			heapRawRecordStartPosition = heapRawRecordStartPosition - bufferUsed;
			heapRawRecordEndPosition = stackFieldEndPosition - bufferUsed;

			stackBufferPosition = stackBufferPosition - bufferUsed;
			stackFieldStartPosition = stackFieldStartPosition - bufferUsed;
			stackFieldEndPosition = stackFieldEndPosition - bufferUsed;
			stackBuffer = heapBuffer.Span.Slice(stackFieldStartPosition);

			return true;
		}

		protected virtual string BuildRawRecord()
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

		public virtual void Dispose()
		{
		}
	}
}
#endif
