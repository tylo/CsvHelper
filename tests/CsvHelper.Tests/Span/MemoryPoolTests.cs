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
    public class MemoryPoolTests
    {
		[TestMethod]
		public void UnQuotedTest()
		{
			var s = new StringBuilder();
			s.Append("a,b\r\n");
			s.Append("c,d\r");
			s.Append("e,f\n");
			using (var reader = new StringReader(s.ToString()))
			using (var parser = new CsvMemoryPoolParser(reader, CultureInfo.InvariantCulture))
			{
				Assert.IsTrue(parser.Read());
				Assert.AreEqual("a,b\r\n", parser.RawRecord);
				//Assert.AreEqual("a", parser[0]);
				//Assert.AreEqual("b", parser[1]);
				//Assert.AreEqual("a,b\r\n", parser.RawRecord);
				//Assert.AreEqual("a", parser.Record[0]);
				//Assert.AreEqual("b", parser.Record[1]);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("c,d\r", parser.RawRecord);
				//Assert.AreEqual("c", parser[0]);
				//Assert.AreEqual("d", parser[1]);
				//Assert.AreEqual("c,d\r", parser.RawRecord);
				//Assert.AreEqual("c", parser.Record[0]);
				//Assert.AreEqual("d", parser.Record[1]);

				Assert.IsTrue(parser.Read());
				Assert.AreEqual("e,f\n", parser.RawRecord);
				//Assert.AreEqual("e", parser[0]);
				//Assert.AreEqual("f", parser[1]);
				//Assert.AreEqual("e,f\n", parser.RawRecord);
				//Assert.AreEqual("e", parser.Record[0]);
				//Assert.AreEqual("f", parser.Record[1]);

				Assert.IsFalse(parser.Read());
			}
		}

		[TestMethod]
		public void Test()
		{
			var s = new StringBuilder();
			s.Append("abcde,fghij,klmno,pqrst,uvwxy,z\r\n");
			using (var reader = new StringReader(s.ToString()))
			using (var parser = new CsvMemoryPoolParser(reader, CultureInfo.InvariantCulture))
			{
				Assert.IsTrue(parser.Read());
				Assert.IsFalse(parser.Read());
			}
		}
	}
}
