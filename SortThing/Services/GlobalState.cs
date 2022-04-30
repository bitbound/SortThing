using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IGlobalState
    {
        string ConfigPath { get; init; }
        bool DryRun { get; init; }
        bool GenerateSample { get; init; }
        string JobName { get; init; }
        bool Watch { get; init; }
    }

    public class GlobalState : IGlobalState
    {
        public string ConfigPath { get; init; } = string.Empty;
        public bool DryRun { get; init; }
        public bool GenerateSample { get; init; }
        public string JobName { get; init; } = string.Empty;
        public bool Watch { get; init; }
    }
}
