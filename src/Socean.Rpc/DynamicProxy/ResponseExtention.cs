using Socean.Rpc.Core.Message;
using Socean.Rpc.DynamicProxy;
using System;

namespace Socean.Rpc.Core
{
    public class StringResponse : ResponseBase
    {
        public StringResponse(string value) : base(FrameFormat.EmptyBytes, DynamicProxySettings.DefaultEncoding.GetBytes(value), (byte)ResponseCode.OK)
        {

        }
    }
}
