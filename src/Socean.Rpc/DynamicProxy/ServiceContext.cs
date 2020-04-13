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
        public byte[] ContentBytes { get; private set; }
        public string HeaderExtention { get; private set; }

        public ServiceRequest(FrameData frameData)
        {
            OriginalFrameData = frameData;

            var encoding = NetworkSettings.TitleExtentionEncoding;

            Title = encoding.GetString(frameData.TitleBytes);
            ContentBytes = frameData.ContentBytes;
            HeaderExtention = frameData.HeaderExtentionBytes == null ? string.Empty : encoding.GetString(frameData.HeaderExtentionBytes);
        }

        internal void DecryptMessage(byte[] password)
        {
            ContentBytes = EncryptHelper.AesDecrypt(ContentBytes, password);
            HeaderExtention = EncryptHelper.AesDecrypt(HeaderExtention, password);
        }
    }

    public class ServiceResponse
    {
        public byte[] ContentBytes { get; private set; }
        public string HeaderExtention { get; private set; }
        public byte Code { get; private set; }

        //public void WriteString(string content, string extention = null)
        //{
        //    Content = content;
        //    HeaderExtention = extention;
        //    Code = 0;
        //}

        public void WriteBytes(byte[] content, string extention = null)
        {
            ContentBytes = content;
            HeaderExtention = extention;
            Code = 0;
        }

        public void WriteError(byte errorCode,string message = null)
        {
            ContentBytes = NetworkSettings.ErrorContentEncoding.GetBytes(message ?? string.Empty) ;
            HeaderExtention = null;
            Code = errorCode;
        }

        internal ResponseBase FlushFinal()
        {
            var encoding = NetworkSettings.TitleExtentionEncoding;
            return new CustomResponse(HeaderExtention == null ? null: encoding.GetBytes(HeaderExtention), ContentBytes ?? FrameFormat.EmptyBytes,  Code);
        }

        internal void EncryptMessage(byte[] password)
        {
            ContentBytes = EncryptHelper.AesEncrypt(ContentBytes, password);
            HeaderExtention = EncryptHelper.AesEncrypt(HeaderExtention, password);
        }
    }
}
