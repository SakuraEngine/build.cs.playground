using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SB.Core
{
    public class MSVCArgumentDriver : IArgumentDriver
    {
        [SB.Core.Argument]
        public string Exception(bool Enable)
        {
            return Enable ? "/EHsc" : "/EHsc-";
        }

        public Dictionary<string, object?[]?> Semantics { get; } = new Dictionary<string, object?[]?>();
        public HashSet<string> RawArguments { get; } = new HashSet<string>();
    }
}