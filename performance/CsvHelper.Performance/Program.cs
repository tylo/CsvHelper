// Copyright 2009-2019 Josh Close and Contributors
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvHelper.Configuration;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper.Performance
{
	class Program
	{
		static void Main(string[] args)
		{
			//WriteField(50, 1_000_000);
			//WriteRecords(1_000_000);

			for (var i = 0; i < 10; i++)
			{
				//Parse();
				//LumenworksParse();
				//StackParse();
				//SoftCircuitsParse();
				StefanBartelsParse();

				//ReadGetField();
				//ReadGetRecords();
				//ReadGetRecordsAsync().Wait();

				Console.WriteLine();
			}

			//BenchmarkRunner.Run<Benchmarks>();
		}

		static string GetFilePath()
		{
			var homePath = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
			var filePath = Path.Combine(homePath, "Documents", "performance.csv");
			return filePath;
		}

		static void WriteField(int columns = 50, int rows = 2_000_000)
		{
			Console.WriteLine("Writing using WriteField");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.Create(GetFilePath()))
			using (var writer = new StreamWriter(stream))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				for (var column = 1; column <= columns; column++)
				{
					csv.WriteField($"Column{column}");
				}
				csv.NextRecord();

				for (var row = 1; row <= rows; row++)
				{
					for (var column = 1; column <= columns; column++)
					{
						csv.WriteField($"{row}_{column}");
					}
					csv.NextRecord();
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void WriteRecords(int rows = 2_000_000)
		{
			Console.WriteLine("Writing using WriteRecords");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.Create(GetFilePath()))
			using (var writer = new StreamWriter(stream))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				//csv.Configuration.ShouldQuote = (field, context) => true;

				var records = new List<Columns50>();
				for (var i = 0; i < rows; i++)
				{
					var record = new Columns50
					{
						Column1 = 1,
						Column2 = 2,
						Column3 = 3,
						Column4 = 4,
						Column5 = 5,
						Column6 = 6,
						Column7 = 7,
						Column8 = 8,
						Column9 = 9,
						Column10 = 10,
						Column11 = 11,
						Column12 = 12,
						Column13 = 13,
						Column14 = 14,
						Column15 = 15,
						Column16 = 16,
						Column17 = 17,
						Column18 = 18,
						Column19 = 19,
						Column20 = 20,
						Column21 = 21,
						Column22 = 22,
						Column23 = 23,
						Column24 = 24,
						Column25 = 25,
						Column26 = 26,
						Column27 = 27,
						Column28 = 28,
						Column29 = 29,
						Column30 = 30,
						Column31 = 31,
						Column32 = 32,
						Column33 = 33,
						Column34 = 34,
						Column35 = 35,
						Column36 = 36,
						Column37 = 37,
						Column38 = 38,
						Column39 = 39,
						Column40 = 40,
						Column41 = 41,
						Column42 = 42,
						Column43 = 43,
						Column44 = 44,
						Column45 = 45,
						Column46 = 46,
						Column47 = 47,
						Column48 = 48,
						Column49 = 49,
						Column50 = 50,
					};
					records.Add(record);
				}

				csv.WriteRecords(records);
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static async Task PipeParseAsync()
		{
			Console.WriteLine("CsvHelper pipe parsing");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var parser = new CsvPipeParser(stream))
			{
				while (await parser.ReadAsync())
				{
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void StackParse()
		{
			Console.WriteLine("CsvHelper span parsing");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var reader = new StreamReader(stream))
			using (var parser = new CsvStackParser(reader, CultureInfo.InvariantCulture))
			{
				parser.Configuration.BufferSize = 1024 * 2;
				while (parser.Read())
				{
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void Parse()
		{
			Console.WriteLine("CsvHelper parsing");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var reader = new StreamReader(stream))
			using (var parser = new CsvParser(reader, CultureInfo.InvariantCulture))
			{
				string[] row;
				while ((row = parser.Read()) != null)
				{
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void ReadGetField()
		{
			Console.WriteLine("Reading using GetField");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var reader = new StreamReader(stream))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				// Read header.
				csv.Read();

				while (csv.Read())
				{
					for (var i = 0; i < 50; i++)
					{
						csv.GetField<int>(i);
					}
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void ReadGetRecords()
		{
			Console.WriteLine($"Reading using GetRecords");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var reader = new StreamReader(stream))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				var records = csv.GetRecords<Columns50>();
				foreach (var record in records)
				{
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static async Task ReadGetRecordsAsync()
		{
			Console.WriteLine("Reading using GetRecordsAsync");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var reader = new StreamReader(stream))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				while (await csv.ReadAsync())
				{
					var record = csv.GetRecord<Columns50>();
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void LumenworksParse()
		{
			Console.WriteLine("Lumenworks parsing");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var reader = new StreamReader(stream))
			using (var csv = new LumenWorks.Framework.IO.Csv.CsvReader(reader))
			{
				var fieldCount = csv.FieldCount;
				var headers = csv.GetFieldHeaders();
				while (csv.ReadNextRecord())
				{
					var row = new string[fieldCount];
					for (var i = 0; i < fieldCount; i++)
					{
						row[i] = csv[i];
					}
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void SoftCircuitsParse()
		{
			Console.WriteLine("SoftCircuits parsing");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var csv = new SoftCircuits.CsvParser.CsvReader(stream))
			{
				string[] row = null;
				while (csv.ReadRow(ref row))
				{
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		static void StefanBartelsParse()
		{
			Console.WriteLine("Stefan Bartels parsing");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var stream = File.OpenRead(GetFilePath()))
			using (var reader = new StreamReader(stream))
			{
				var csv = new StefanBartelsParser(reader, false);

				foreach (var context in csv.Read())
				{
				}
			}

			stopwatch.Stop();
			Console.WriteLine(stopwatch.Elapsed);
		}

		private class Data
		{
			public int Id { get; set; }

			public string Name { get; set; }

			public int Age { get; set; }

			public DateTimeOffset Birthday { get; set; }
		}

		private class DataMap : ClassMap<Data>
		{
			public DataMap()
			{
				Map(m => m.Id).Index(0);
				Map(m => m.Name).Index(1);
				Map(m => m.Age).Index(2);
				Map(m => m.Birthday).Index(3);
			}
		}

		private class Columns50
		{
			public int Column1 { get; set; }
			public int Column2 { get; set; }
			public int Column3 { get; set; }
			public int Column4 { get; set; }
			public int Column5 { get; set; }
			public int Column6 { get; set; }
			public int Column7 { get; set; }
			public int Column8 { get; set; }
			public int Column9 { get; set; }
			public int Column10 { get; set; }
			public int Column11 { get; set; }
			public int Column12 { get; set; }
			public int Column13 { get; set; }
			public int Column14 { get; set; }
			public int Column15 { get; set; }
			public int Column16 { get; set; }
			public int Column17 { get; set; }
			public int Column18 { get; set; }
			public int Column19 { get; set; }
			public int Column20 { get; set; }
			public int Column21 { get; set; }
			public int Column22 { get; set; }
			public int Column23 { get; set; }
			public int Column24 { get; set; }
			public int Column25 { get; set; }
			public int Column26 { get; set; }
			public int Column27 { get; set; }
			public int Column28 { get; set; }
			public int Column29 { get; set; }
			public int Column30 { get; set; }
			public int Column31 { get; set; }
			public int Column32 { get; set; }
			public int Column33 { get; set; }
			public int Column34 { get; set; }
			public int Column35 { get; set; }
			public int Column36 { get; set; }
			public int Column37 { get; set; }
			public int Column38 { get; set; }
			public int Column39 { get; set; }
			public int Column40 { get; set; }
			public int Column41 { get; set; }
			public int Column42 { get; set; }
			public int Column43 { get; set; }
			public int Column44 { get; set; }
			public int Column45 { get; set; }
			public int Column46 { get; set; }
			public int Column47 { get; set; }
			public int Column48 { get; set; }
			public int Column49 { get; set; }
			public int Column50 { get; set; }
		}
	}

	public class Benchmarks
	{
		private string s = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";

		public Benchmarks()
		{
		}

		[Benchmark]
		public void A()
		{
			var memory = new Memory<char>(s.ToCharArray());
			var slice = memory.Span.Slice(0);
			for (var i = 0; i < s.Length; i++)
			{
				var c = slice[i];
			}
		}

		[Benchmark]
		public void B()
		{
			var memory = new Memory<char>(s.ToCharArray());
			for (var i = 0; i < s.Length; i++)
			{
				var slice = memory.Span.Slice(0);
				var c = slice[i];
			}
		}
	}

	public class StefanBartelsParserContext : IDisposable
	{
		internal IMemoryOwner<char> SingleLineOwner;
		internal int DelimiterSize = 1;
		internal ReadOnlyMemory<char> Line;
		internal List<int> DelimiterAfter;

		public int FieldCount => DelimiterAfter.Count;

		public ReadOnlyMemory<char> GetRawField(int colNo) => Line.Slice(colNo == 0 ? 0 : DelimiterAfter[colNo - 1] + DelimiterSize, DelimiterAfter[colNo] - (colNo == 0 ? 0 : DelimiterAfter[colNo - 1] + DelimiterSize));

		public void Dispose()
		{
			SingleLineOwner?.Dispose();
		}
	}

	public class StefanBartelsParser
	{
		private readonly TextReader _tr;
		private char[] _buffer = new char[2];
		private int _bufferPos = 0;
		private int _bufferLen = 0; // last pos + 1
		private bool _allRead = false;

		public StefanBartelsParser(TextReader tr, bool leaveOpen)
		{
			_tr = tr;
		}

		public IEnumerable<StefanBartelsParserContext> Read()
		{
			using (var context = new StefanBartelsParserContext())
			{
				context.DelimiterAfter = new List<int>(100);
				var bufferMinSize = 100;

				var bufferOwner = MemoryPool<char>.Shared.Rent(bufferMinSize);
				try
				{
					var buffer = bufferOwner.Memory;
					var processedBufferPos = 0;
					var bufferEndPos = 0;
					var eof = false;
					var readMoreDataNext = true;

					while (true)
					{
						if (readMoreDataNext)
						{
							var readBytes = _tr.Read(buffer.Slice(bufferEndPos).Span);
							eof |= readBytes == 0;
							bufferEndPos += readBytes;
						}

						if (eof) readMoreDataNext = false;

						if (processedBufferPos >= bufferEndPos) break;

						var newPos = ParseLine(new ReadOnlySequence<char>(buffer.Slice(processedBufferPos, bufferEndPos - processedBufferPos)), eof, context);

						if (newPos >= 0)
						{
							yield return context;
							readMoreDataNext = false;
							processedBufferPos += newPos;

							if (!eof && processedBufferPos >= buffer.Length)
							{
								// buffer fully processed, mark free
								bufferEndPos = 0;
								processedBufferPos = 0;
								readMoreDataNext = true;
							}
						}
						else
						{
							// need more bytes
							if (processedBufferPos > 0)
							{
								// shift 
								buffer.Slice(processedBufferPos, bufferEndPos - processedBufferPos).CopyTo(buffer);
								bufferEndPos -= processedBufferPos;
								processedBufferPos = 0;
							}
							else
							{
								// resize
								bufferMinSize = 1 + buffer.Length * 3 / 2;
								var newBufferOwner = MemoryPool<char>.Shared.Rent(bufferMinSize);
								var newBuffer = newBufferOwner.Memory;
								buffer.Slice(0, bufferEndPos).CopyTo(newBuffer);
								buffer = newBuffer;
								bufferOwner.Dispose();
								bufferOwner = newBufferOwner;
							}
							readMoreDataNext = true;
						}

						if (eof)
						{
							Debug.Assert(newPos == bufferEndPos);
							break;
						}
					}
				}
				finally
				{
					bufferOwner.Dispose();
				}
			}
		}

		public static int ParseLine(ReadOnlySequence<char> lineBuffer, bool eof, StefanBartelsParserContext context3)
		{
			var pos = 0; //  lineBuffer.Start.GetInteger();
			var isFirstSeq = true;
			var lineDelimiterRead = false;
			var lineDelimiterHalfRead = false;

			context3.Line = ReadOnlyMemory<char>.Empty;
			context3.SingleLineOwner?.Dispose();
			context3.SingleLineOwner = null;
			context3.DelimiterAfter.Clear();

			foreach (var seq in lineBuffer)
			{
				foreach (var c in seq.Span)
				{
					if (lineDelimiterHalfRead)
					{
						if (c != '\n') pos--;
						lineDelimiterHalfRead = false;
						lineDelimiterRead = true;
						break;
					}

					if (c == '\r')
					{
						if (isFirstSeq)
						{
							context3.DelimiterAfter.Add(pos);
							context3.Line = seq.Slice(0, pos);
						}
						lineDelimiterHalfRead = true;
					}
					else if (c == '\n')
					{
						if (isFirstSeq)
						{
							context3.DelimiterAfter.Add(pos);
							context3.Line = seq.Slice(0, pos);
						}
						lineDelimiterRead = true;
						break;
					}
					else if (c == ';') // delimiter
					{
						if (isFirstSeq)
						{
							context3.DelimiterAfter.Add(pos);
						}
					}
					pos++;
				}

				if (lineDelimiterRead) break;

				isFirstSeq = false;
			}

			if (lineDelimiterRead || eof)
			{
				if (!isFirstSeq)
				{
					// copy segments to new Memory buffer
					context3.SingleLineOwner = MemoryPool<char>.Shared.Rent(pos);
					var memory = context3.SingleLineOwner.Memory;
					lineBuffer.Slice(0, pos).CopyTo(memory.Span);
					context3.Line = memory;
				}

				return lineDelimiterRead ? pos + 1 : pos;
			}
			return -1;
		}
	}
}
