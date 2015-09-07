using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsPathEditor
{
    interface IReportProgress
    {
        void Begin();

        void ReportProgress(string progress);

        void FoundCandidate(string path);

        void Done();

        bool Cancelled { get; }
    }
}
