using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    interface IAction
    {
        public string Trigger { get; set; }

        List<IContent> Content { get; set; }
    }
}
