using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace WindowsPathEditor
{
    public class ConflictChecker
    {
        private ConcurrentQueue<PathEntry> remainder = new ConcurrentQueue<PathEntry>();

        public void Check(PathEntry path)
        {
            if (!path.Exists)
            {
                path.AddIssue("Does not exist");
                return;
            }

            remainder.Enqueue(path);
            // FIXME: Do file checking in thread
        }
    }
}
