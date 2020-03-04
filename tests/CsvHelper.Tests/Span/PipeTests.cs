using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper.Tests.Span
{
	[TestClass]
    public class PipeTests
    {
		[TestMethod]
        public async Task Test()
		{
			var s = new StringBuilder();
			s.Append("abcdefghijklmnopqr,stuvwxyz\r\n");
			using (var stream = new MemoryStream())
			using (var writer = new StreamWriter(stream, Encoding.UTF8))
			using (var parser = new CsvPipeParser(stream))
			{
				writer.Write(s.ToString());
				writer.Flush();
				stream.Position = 0;

				while (await parser.ReadAsync())
				{
				}
			}
		}
    }
}
