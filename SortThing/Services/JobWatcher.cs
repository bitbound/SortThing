using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IJobWatcher
    {
        Task WatchJobs(string configPath, bool dryRun);
    }

    public class JobWatcher : IJobWatcher
    {
        public async Task WatchJobs(string configPath, bool dryRun)
        {
            throw new NotImplementedException();
        }
    }
}
