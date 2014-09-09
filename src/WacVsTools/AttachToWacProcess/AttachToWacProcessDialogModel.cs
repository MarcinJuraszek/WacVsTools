using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.WacVsTools.AttachToWacProcess
{
    public class AttachToWacProcessDialogModel
    {
        public IList<WacProcessInfo> Processes { get; set; }
        public IEnumerable<int> SelectedProcesses { get; set; }
    }

    public class WacProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string App { get; set; }
        public string CommandLine { get; set; }
    }
}
