using Socean.Rpc.Core;
using Socean.Rpc.Core.Message;
using System;
using System.Collections.Generic;

namespace Socean.Rpc.DynamicProxy
{
    public class ServiceContext
    {
        internal volatile LinkedListNode<IServiceFilter> CurrentFilterNode;

        public ServiceContext(FrameData frameData)
        {
            Request = new ServiceRequest(frameData);
            Response = new ServiceResponse();
        }

        public ServiceRequest Request { get; }
        public ServiceResponse Response { get; }
    }

    public class ServiceRequest
    {
        public FrameData OriginalFrameData { get; }
        public string Title { get; private set; }
        public string Content { get; private set; }
        public string HeaderExtention { get; private set; }

        public ServiceRequest(FrameData frameData)
        {
            OriginalFrameData = frameData;

            var encoding = RpcExtentionSettings.DefaultEncoding;
            Title = encoding.GetString(frameData.TitleBytes);
            Content = encoding.GetString(frameData.ContentBytes);
            HeaderExtention = encoding.GetString(frameData.HeaderExtentionBytes);
        }

        internal void DecryptMessage(byte[] password)
        {
            Content = EncryptHelper.AesDecrypt(Content, password);
            HeaderExtention = EncryptHelper.AesDecrypt(HeaderExtention, password);
        }
    }

    public class ServiceResponse
    {
        public string Content { get; private set; }
        public string HeaderExtention { get; private set; }
        public byte Code { get; private set; }

        public void WriteString(string content, string extention = null)
        {
            Content = content;
            HeaderExtention = extention;
            Code = 0;
        }

        public void WriteError(byte errorCode,string message = null)
        {
            Content = message;
            HeaderExtention = null;
            Code = errorCode;
        }

        internal ResponseBase FlushFinal()
        {
            var encoding = RpcExtentionSettings.DefaultEncoding;
            return new CustomResponse(encoding.GetBytes(HeaderExtention ?? string.Empty), encoding.GetBytes(Content ?? string.Empty) ,  Code);
        }

        internal void EncryptMessage(byte[] password)
        {
            Content = EncryptHelper.AesEncrypt(Content, password);
            HeaderExtention = EncryptHelper.AesEncrypt(HeaderExtention, password);
        }
    }
}
