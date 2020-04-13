using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socean.Rpc.DynamicProxy
{
    public interface IBinarySerializer
    {
        byte[] Serialize(object obj);

        object Deserialize(byte[] content, Type type);
    }
}
