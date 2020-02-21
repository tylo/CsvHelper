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
        public void ParseFieldsTest()
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

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("c", parser[0]);
				Assert.AreEqual("d", parser[1]);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("e", parser[0]);
				Assert.AreEqual("f", parser[1]);

				Assert.IsFalse(parser.Read());
			}
		}

		[TestMethod]
		public void ParseRawFieldsTest()
		{
			var s = new StringBuilder();
			s.Append("\"a\",\"b\"\r\n");
			s.Append("\"c\",\"d\"\r");
			s.Append("\"e\",\"f\"\n");
			using (var reader = new StringReader(s.ToString()))
			using (var parser = new CsvStackParser(reader, CultureInfo.InvariantCulture))
			{
				parser.Configuration.BufferSize = 2048;

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("a", parser[0]);
				Assert.AreEqual("b", parser[1]);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("c", parser[0]);
				Assert.AreEqual("d", parser[1]);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("e", parser[0]);
				Assert.AreEqual("f", parser[1]);

				Assert.IsFalse(parser.Read());
			}
		}

		[TestMethod]
		public void ParseRawRecordTest()
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
				Assert.AreEqual("a,b\r\n", parser.RawRecord);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("c,d\r", parser.RawRecord);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("e,f\n", parser.RawRecord);

				Assert.IsFalse(parser.Read());
			}
		}
	}
}
