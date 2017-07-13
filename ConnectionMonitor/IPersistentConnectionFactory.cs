using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectionMonitor
{
    public interface IPersistentConnectionFactory
    {
        IPersistentConnection CreateConnection();
    }
}
