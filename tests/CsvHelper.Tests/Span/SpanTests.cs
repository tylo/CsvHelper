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
        public void UnQuotedTest()
		{
			var s = new StringBuilder();
			s.Append("a,b\r\n");
			s.Append("c,d\r");
			s.Append("e,f\n");
			using (var reader = new StringReader(s.ToString()))
			using (var parser = new CsvStackParser(reader, CultureInfo.InvariantCulture))
			{
				parser.Configuration.BufferSize = 2;

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("a", parser[0]);
				Assert.AreEqual("b", parser[1]);
				Assert.AreEqual("a,b\r\n", parser.RawRecord);
				Assert.AreEqual("a", parser.Record[0]);
				Assert.AreEqual("b", parser.Record[1]);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("c", parser[0]);
				Assert.AreEqual("d", parser[1]);
				Assert.AreEqual("c,d\r", parser.RawRecord);
				Assert.AreEqual("c", parser.Record[0]);
				Assert.AreEqual("d", parser.Record[1]);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("e", parser[0]);
				Assert.AreEqual("f", parser[1]);
				Assert.AreEqual("e,f\n", parser.RawRecord);
				Assert.AreEqual("e", parser.Record[0]);
				Assert.AreEqual("f", parser.Record[1]);

				Assert.IsFalse(parser.Read());
			}
		}

		[TestMethod]
		public void QuotedTest()
		{
			var s = new StringBuilder();
			s.Append("`a!`b`,`c`\r\n");
			s.Append("`d`,`e!`f`\r\n");
			using (var reader = new StringReader(s.ToString()))
			using (var parser = new CsvStackParser(reader, CultureInfo.InvariantCulture))
			{
				parser.Configuration.BufferSize = 2048;
				parser.Configuration.Quote = '`';
				parser.Configuration.Escape = '!';

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("a`b", parser[0]);
				Assert.AreEqual("c", parser[1]);
				Assert.AreEqual("`a!`b`,`c`\r\n", parser.RawRecord);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("d", parser[0]);
				Assert.AreEqual("e`f", parser[1]);
				Assert.AreEqual("`d`,`e!`f`\r\n", parser.RawRecord);

				Assert.IsFalse(parser.Read());
			}
		}
	}
}
