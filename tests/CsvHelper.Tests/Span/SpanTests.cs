using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper.Tests.Span
{
	[TestClass]
    public class SpanTests
    {
		[TestMethod]
        public void Test()
		{
			var s = new StringBuilder();
			//s.Append("a,b\r\n");
			//s.Append("c,d\r\n");
			s.Append("a,b\r\n");
			s.Append("c,d\r");
			s.Append("e,f\n");
			using (var reader = new StringReader(s.ToString()))
			using (var parser = new CsvStackParser(reader, CultureInfo.InvariantCulture))
			{
				parser.Configuration.BufferSize = 2048;

				var row = parser.Read();
				Assert.AreEqual("a", row[0]);
				Assert.AreEqual("b", row[1]);

				row = parser.Read();
				Assert.AreEqual("c", row[0]);
				Assert.AreEqual("d", row[1]);

				row = parser.Read();
				Assert.AreEqual("e", row[0]);
				Assert.AreEqual("f", row[1]);

				row = parser.Read();
				Assert.IsNull(row);
			}
		}
    }
}
