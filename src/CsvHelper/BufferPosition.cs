using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvHelper
{
	[DebuggerDisplay("Start = {Start}, Length = {Length}")]
    public class BufferPosition
    {
        public int Start { get; set; }

		public int Length { get; set; }
    }
}
