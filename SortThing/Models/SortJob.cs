using SortThing.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Models
{
    public class SortJob
    {
        /// <summary>
        /// The operation to perform on the original files.
        /// </summary>
        public SortOperation Operation { get; init; }
        public string[] Extensions { get; init; }
    }
}
