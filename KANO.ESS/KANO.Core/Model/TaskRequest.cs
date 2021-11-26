using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class TaskRequest<T>
    {
        public string Label { set; get; }
        public T Result { set; get; }

        public static TaskRequest<T> Create(string label, T result)
        {
            return new TaskRequest<T>
            {
                Label = label,
                Result = result
            };
        }
    }
    
}
