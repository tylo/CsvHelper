#if NETSTANDARD2_1
using System;
using System.Buffers;
using System.Collections.Generic;
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
		private int bufferSize = 2;
		private int memoryBufferPosition;
		private int charsRead;
		private List<char[]> fieldPositions = new List<char[]>(128);
		private List<char[]> delimiterPositions = new List<char[]>(128);
		private IMemoryOwner<char> memoryOwner;
		private Memory<char> memoryBuffer;
		private ReadOnlySequence<char> sequence;
		private int rowStartPosition;
		private int rowLength;

		public CsvMemoryPoolParser(TextReader reader, CultureInfo cultureInfo)
		{
			this.reader = reader;
			this.cultureInfo = cultureInfo;
		}

		public bool Read()
		{
			rowStartPosition = rowStartPosition + rowLength;
			rowLength = 0;
			fieldPositions.Clear();
			delimiterPositions.Clear();

			var sequenceReader = new SequenceReader<char>();

			while (true)
			{
				if (sequenceReader.End)
				{
					if (!FillBuffer(ref sequenceReader))
					{
						// EOF
						return false;
					}
				}

				sequenceReader.TryPeek(out var c);

				if (c == '"')
				{
					throw new NotImplementedException();
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
				if (sequenceReader.End && !FillBuffer(ref sequenceReader))
				{
					// EOF
					return true;
				}

				sequenceReader.TryRead(out var c);
				rowLength++;

				if (c == ',')
				{
					return false;
				}
				else if (c == '\r' || c == '\n')
				{
					ReadLineEnding(c, ref sequenceReader);
					return true;
				}
			}
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool ReadLineEnding(char c, ref SequenceReader<char> sequenceReader)
		{
			if (sequenceReader.End && !FillBuffer(ref sequenceReader))
			{
				// EOF.
				return true;
			}

			if (c == '\r')
			{
				sequenceReader.TryPeek(out var cPeek);
				if (cPeek == '\n')
				{
					sequenceReader.Advance(1);
					rowLength++;
				}
			}

			return true;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool FillBuffer(ref SequenceReader<char> sequenceReader)
		{
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
				var tempMemoryOwner = MemoryPool<char>.Shared.Rent(bufferSize);
				var tempMemoryBuffer = tempMemoryOwner.Memory;
				memoryBuffer.Slice(rowStartPosition).CopyTo(tempMemoryBuffer);
				charsRead = reader.Read(tempMemoryBuffer.Slice(memoryBuffer.Length - rowStartPosition).Span);
				rowStartPosition = 0;

				memoryBuffer = tempMemoryBuffer;
				memoryOwner.Dispose();
				memoryOwner = tempMemoryOwner;
			}

			sequence = new ReadOnlySequence<char>(memoryBuffer.Slice(rowLength));
			sequenceReader = new SequenceReader<char>(sequence);

			return charsRead > 0;
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
