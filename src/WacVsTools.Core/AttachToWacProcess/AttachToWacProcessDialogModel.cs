using System.Collections.Generic;

namespace WacVsTools.Core.AttachToWacProcess
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
