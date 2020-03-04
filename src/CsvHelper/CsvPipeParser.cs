#if NETSTANDARD2_1
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
    public class CsvPipeParser : IDisposable
    {
		private Stream stream;
		private PipeReader reader;
		private int bufferPosition;
		private byte delimiter = (byte)',';
		private byte quote = (byte)'"';
		private byte carriageReturn = (byte)'\r';
		private byte lineFeed = (byte)'\n';
		private Encoding encoding = Encoding.UTF8;
		private List<string> record = new List<string>(128);

		public IReadOnlyList<string> Record => record;

		public CsvPipeParser(Stream stream, bool leaveOpen = false)
		{
			this.stream = stream;
			reader = PipeReader.Create(this.stream, new StreamPipeReaderOptions(bufferSize: 2, leaveOpen: leaveOpen));
		}

		public async Task<bool> ReadAsync()
		{
			record.Clear();

			while (true)
			{
				var result = await reader.ReadAsync();
				var sequence = result.Buffer;

				var isRowComplete = ReadField(sequence, result.IsCompleted, out var position);

				reader.AdvanceTo(position, sequence.End);

				if (isRowComplete)
				{
					return true;
				}

				if (result.IsCompleted)
				{
					return false;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool ReadField(in ReadOnlySequence<byte> sequence, bool isCompleted, out SequencePosition position)
		{
			var commaCrLf = new Span<byte>(new byte[] { (byte)',', (byte)'\r', (byte)'\n' });
			var endOfLine = false;

			var reader = new SequenceReader<byte>(sequence);
			while (!reader.End)
			{
				if (reader.TryReadToAny(out ReadOnlySpan<byte> fieldSpan, commaCrLf, advancePastDelimiter: false))
				{
					// Delimiter or line ending characters were found.

					record.Add(encoding.GetString(fieldSpan));

					if (reader.TryRead(out byte b))
					{
						// See that character was found so we can proceed accordingly.
						if (b == delimiter)
						{
							// End of field.
							ReadDelimiter(b, reader);
						}
						else if (b == carriageReturn || b == lineFeed)
						{
							// End of line.
							endOfLine = true;
							ReadLineEnding(b, reader);
						}
					}
				}
				else if (isCompleted)
				{
					// End of file.
					break;
				}
				else
				{
					// Buffer ran out and field isn't done reading.
					break;
				}
			}

			position = reader.Position;

			return endOfLine;
		}

		protected void ReadQuotedField()
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void ReadDelimiter(byte b1, SequenceReader<byte> reader)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void ReadLineEnding(byte b1, SequenceReader<byte> reader)
		{
			if (b1 == carriageReturn && reader.TryPeek(out var b2) && b2 == lineFeed)
			{
				// Advance the reader 1 byte.
				reader.TryRead(out var _);
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				stream.Dispose();
			}
		}
    }
}
#endif
