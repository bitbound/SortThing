using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IJobRunner
    {
        Task RunJobs(string configPath, bool dryRun);
    }

    public class JobRunner : IJobRunner
    {
        public async Task RunJobs(string configPath, bool dryRun)
        {
            throw new NotImplementedException();
        }
    }
}
