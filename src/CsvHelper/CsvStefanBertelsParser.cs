#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	public ref struct ReaderContext
	{
		public List<(int Pos, int Len)> FieldPositions;
		public Span<char> currentBuffer;

		public Span<char> GetFieldSpan(int i) => currentBuffer.Slice(FieldPositions[i].Pos, FieldPositions[i].Len);
	}

	public class CsvStefanBertelsParser : IDisposable
    {
		private readonly TextReader _tr;
		private char[] _buffer;
		private int _bufferPos = 0;
		private int _bufferLen = 0; // last pos + 1
		private bool _allRead = false;

		public CsvStefanBertelsParser(TextReader tr, int bufferSize = 4096)
		{
			_tr = tr;
			_buffer = new char[bufferSize];
		}

		public ReaderContext Read()
		{
			// return true if line is parsed
			var context = new ReaderContext();

			if (_bufferPos >= _bufferLen)
			{
				// empty
				_bufferPos = 0;
				_bufferLen = _allRead ? 0 : _tr.ReadBlock(_buffer, 0, _buffer.Length);
			}

			if (_bufferLen == 0) return new ReaderContext(); // buffer leer und nix eingelesen


			// now parse line beginning at _bufferPos
			while (true)
			{
				context.FieldPositions = new List<(int Pos, int Len)>();
				context.currentBuffer = _buffer.AsSpan();

				while (true)
				{
					int lineStartPos = _bufferPos;
					int currentPos = lineStartPos;
					bool eol = false;
					int fieldStartPos = lineStartPos;

					while (currentPos < _bufferLen)
					{
						var c = _buffer[currentPos++];
						if (c == '\r')
						{
							// peek/read LF
							if (currentPos >= _bufferLen) break; // end of buffer
							if (_buffer[currentPos] == '\n')
							{
								// found LF
								context.FieldPositions.Add((fieldStartPos, currentPos - fieldStartPos - 1));
								++currentPos; // skip LF
								_bufferPos = currentPos;
								return context;
							}
						}
						else if (c == '\n')
						{
							context.FieldPositions.Add((fieldStartPos, currentPos - fieldStartPos - 1));
							_bufferPos = currentPos;
							return context;
						}
						else if (c == ';') // delimiter
						{
							context.FieldPositions.Add((fieldStartPos, currentPos - fieldStartPos - 1));
							fieldStartPos = currentPos;
						}
						else // field content
						{
						}
					}

					if (_allRead)
					{
						context.FieldPositions.Add((fieldStartPos, currentPos - fieldStartPos));
						_bufferPos = currentPos;
						return context;
					}

					break;
				}

				// end of buffer but probably not eol/eof
				// re-fill buffer
				if (_bufferPos > 0)
				{
					// shift
					_buffer.AsSpan(_bufferPos, _bufferLen - _bufferPos).CopyTo(_buffer.AsSpan());
					_bufferLen -= _bufferPos;
					_bufferPos = 0;
				}
				else
				{
					// increase buffer size
					var newBuffer = new char[_buffer.Length * 2];
					_buffer.AsSpan().CopyTo(newBuffer.AsSpan());
				}

				// fill the new buffer space
				var readLen = _tr.ReadBlock(_buffer, _bufferLen, _buffer.Length - _bufferLen);
				_allRead |= readLen == 0;
				_bufferLen += readLen;
			}
		}

		public void Dispose()
		{
			_tr?.Dispose();
		}
	}
}
#endif
