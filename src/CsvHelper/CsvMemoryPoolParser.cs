#if NETSTANDARD2_1
using System;
using System.Buffers;
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
	public class CsvMemoryPoolParser : IDisposable
	{
		private bool disposed = false;
		private TextReader reader;
		private CultureInfo cultureInfo;
		private int bufferSize = -1;
		private int memoryBufferPosition;
		private int charsRead = -1;
		private List<int> fieldEndPositions = new List<int>(128);
		private IMemoryOwner<char> memoryOwner;
		private Memory<char> memoryBuffer;
		private ReadOnlySequence<char> sequence;
		private int rowStartPosition;
		private int rowLength;
		private char quote = '"';
		private char escape = '"';
		private string delimiter = ",";
		private char delimiterFirstChar = ',';
		private string newLine = string.Empty;
		private char newLineFirstChar = '\0';

		public int Row { get; protected set; }

		public string RawRecord => new string(memoryBuffer.Slice(rowStartPosition, rowLength).Span);

		public CsvMemoryPoolParser(TextReader reader, CultureInfo cultureInfo)
		{
			this.reader = reader;
			this.cultureInfo = cultureInfo;
		}

		public bool Read()
		{
			Row++;
			rowStartPosition = rowStartPosition + rowLength;
			rowLength = 0;
			fieldEndPositions.Clear();

			var sequenceReader = new SequenceReader<char>();

			while (true)
			{
				if (!TryPeekChar(out var c, ref sequenceReader))
				{
					// EOF
					return false;
				}

				if (c == quote)
				{
					if (ReadQuotedField(ref sequenceReader))
					{
						break;
					}
				}
				else
				{
					if (ReadField(ref sequenceReader))
					{
						break;
					}
				}
			}

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool ReadField(ref SequenceReader<char> sequenceReader)
		{
			while (true)
			{
				if (!TryGetChar(out var c, ref sequenceReader))
				{
					// EOF
					return true;
				}

				if (c == delimiterFirstChar)
				{
					if (ReadDelimiter(ref sequenceReader))
					{
						return false;
					}

					// Not a delimiter. Keep going.
				}
				else if (c == '\r' || c == '\n' || c == newLineFirstChar)
				{
					if (ReadLineEnding(c, ref sequenceReader))
					{
						return true;
					}

					// Not a line ending. Keep going.
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ReadQuotedField(ref SequenceReader<char> sequenceReader)
		{
			if (!TryGetChar(out var c, ref sequenceReader))
			{
				// EOF
				return false;
			}

			var inQuotes = true;

			while (true)
			{
				if (!TryGetChar(out c, ref sequenceReader))
				{
					// EOF
					return false;
				}

				if (c == escape)
				{
					if (!TryPeekChar(out var cPeek, ref sequenceReader))
					{
						// EOF
						return false;
					}

					if (cPeek == quote)
					{
						// Escaped quote was found. Keep going.
						continue;
					}
				}

				if (c == quote)
				{
					inQuotes = false;
					continue;
				}

				if (!inQuotes)
				{
					if (c == delimiterFirstChar)
					{
						if (ReadDelimiter(ref sequenceReader))
						{
							return false;
						}
					}
					else if (c == '\r' || c == '\n' || c == newLineFirstChar)
					{
						if (ReadLineEnding(c, ref sequenceReader))
						{
							return true;
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool ReadDelimiter(ref SequenceReader<char> sequenceReader)
		{
			if (delimiter.Length > 1)
			{
				for (var i = 1; i < delimiter.Length; i++)
				{
					if (!TryGetChar(out var c, ref sequenceReader) || c != delimiter[i])
					{
						return false;
					}
				}
			}

			return true;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool ReadLineEnding(char c, ref SequenceReader<char> sequenceReader)
		{
			if (newLine.Length > 0 && c == newLine[0])
			{
				if (newLine.Length == 1)
				{
					return true;
				}

				for (var i = 1; i < newLine.Length; i++)
				{
					if (!TryGetChar(out c, ref sequenceReader))
					{
						// EOF
						return true;
					}

					if (c != newLine[i])
					{
						return false;
					}
				}

				return true;
			}
			else if (c == '\r')
			{
				if (!TryPeekChar(out var cPeek, ref sequenceReader))
				{
					// EOF
					return true;
				}

				if (cPeek == '\n')
				{
					sequenceReader.Advance(1);
					rowLength++;
				}
			}
			else
			{
				// \n
			}

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool TryGetChar(out char c, ref SequenceReader<char> sequenceReader)
		{
			if (sequenceReader.End && !FillSequence(ref sequenceReader))
			{
				// EOF
				c = '\0';

				return false;
			}

			sequenceReader.TryRead(out c);
			rowLength++;

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool TryPeekChar(out char c, ref SequenceReader<char> sequenceReader)
		{
			if (sequenceReader.End && !FillSequence(ref sequenceReader))
			{
				// EOF
				c = '\0';

				return false;
			}

			sequenceReader.TryPeek(out c);

			return true;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool FillSequence(ref SequenceReader<char> sequenceReader)
		{
			Debug.Assert(sequenceReader.End, "The SequenceReader must be empty to fill it.");

			if (rowStartPosition + rowLength >= charsRead)
			{
				// We only need to fill the buffer if we've read everything from it.
				if (!FillBuffer(ref sequenceReader))
				{
					return false;
				}
			}

			var start = rowStartPosition + rowLength;
			var length = charsRead - start;

			sequence = new ReadOnlySequence<char>(memoryBuffer.Slice(rowStartPosition + rowLength, length));
			sequenceReader = new SequenceReader<char>(sequence);

			return true;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool FillBuffer(ref SequenceReader<char> sequenceReader)
		{
			Debug.Assert(rowStartPosition + rowLength >= charsRead, "The buffer must be empty to fill it.");

			if (memoryOwner == null)
			{
				memoryOwner = MemoryPool<char>.Shared.Rent(bufferSize);
				memoryBuffer = memoryOwner.Memory;
				charsRead = reader.Read(memoryBuffer.Slice(memoryBufferPosition).Span);
			}
			else
			{
				// If the row is longer than the buffer, make the buffer larger.
				if (rowStartPosition == 0)
				{
					bufferSize = memoryBuffer.Length * 2;
				}

				// Copy the remainder of the row onto the new buffer.
				var tempMemoryOwner = MemoryPool<char>.Shared.Rent(bufferSize);
				var tempMemoryBuffer = tempMemoryOwner.Memory;
				memoryBuffer.Slice(rowStartPosition).CopyTo(tempMemoryBuffer);
				var start = memoryBuffer.Length - rowStartPosition;
				charsRead = reader.Read(tempMemoryBuffer.Slice(start).Span);
				if (charsRead == 0)
				{
					return false;
				}

				charsRead += start;

				rowStartPosition = 0;

				memoryBuffer = tempMemoryBuffer;
				memoryOwner.Dispose();
				memoryOwner = tempMemoryOwner;
			}

			return true;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
			{
				return;
			}

			if (disposing)
			{
			}

			disposed = true;
		}
	}
}
#endif
