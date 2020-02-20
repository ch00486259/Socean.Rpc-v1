using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socean.Rpc.Core
{
    public class RpcException:Exception
    {
        public RpcException(string message) : base(message)
        { 
        
        }

        public RpcException() : base()
        {

        }
    }
}
