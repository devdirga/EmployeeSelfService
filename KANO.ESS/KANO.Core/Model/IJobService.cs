using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Api.BatchJob.Jobs
{
    public interface IJobService
    {
        void Run();
    }
}
