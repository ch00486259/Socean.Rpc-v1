using System;
using System.Text;

namespace Socean.Rpc.Core.Message
{
    internal class FrameFormat
    {
        public const int FrameHeaderSize = 20;

        public const byte HeaderBit0 = 126;

        public const byte HeaderBit1 = 127;

        public static readonly byte[] EmptyBytes = new byte[0];

        [ThreadStatic]
        static byte[] hashCodeByteArray;

        /// <summary>
        /// 0 7e 126
        /// 1 7f 127
        /// 2 header extention length
        /// 3 header extention length
        /// 4 title length
        /// 5 title length
        /// 6 content length
        /// 7 content length
        /// 8 content length
        /// 9 content length
        /// 10 state code
        /// 11 message id
        /// 12 message id
        /// 13 message id
        /// 14 message id
        /// 15 hash code
        /// 16 hash code
        /// 17 hash code
        /// 18 hash code
        /// </summary>
        /// <returns></returns>
        public static bool CheckFrameHeader(byte[] buffer)
        {
            if (buffer.Length < FrameHeaderSize)
                return false;

            if (buffer[0] != HeaderBit0)
                return false;

            if (buffer[1] != HeaderBit1)
                return false;

            var extentionLength = buffer[2] << 8 | buffer[3];
            var titleLength = buffer[4] << 8 | buffer[5];
            var contentLength = buffer[6] << 24 | buffer[7] << 16 | buffer[8] << 8 | buffer[9];
            var messageId = buffer[11] << 24 | buffer[12] << 16 | buffer[13] << 8 | buffer[14];

            if (hashCodeByteArray == null)
                hashCodeByteArray = new byte[4];

            ComputeHashCode(hashCodeByteArray,(Int16)extentionLength,(Int16)titleLength, contentLength, messageId);

            return hashCodeByteArray[0] == buffer[15] && hashCodeByteArray[1] == buffer[16] && hashCodeByteArray[2] == buffer[17] && hashCodeByteArray[3] == buffer[18];
        }

        public static void FillFrameHeader(byte[] buffer, byte[] extentionBytes, byte[] titleBytes, byte[] contentBytes, byte stateCode, int messageId)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (extentionBytes == null)
                extentionBytes = EmptyBytes;

            if (titleBytes == null)
                titleBytes = EmptyBytes;

            if (contentBytes == null)
                contentBytes = EmptyBytes;

            if (titleBytes.Length > 65535)
                throw new ArgumentOutOfRangeException("titleBytes");

            var extentionLength = extentionBytes.Length;
            var titleLength = titleBytes.Length;
            var contentLength = contentBytes.Length;

            buffer[0] = HeaderBit0;
            buffer[1] = HeaderBit1;

            buffer[2] = (byte)(extentionLength >> 8);
            buffer[3] = (byte)extentionLength;

            buffer[4] = (byte)(titleLength >> 8);
            buffer[5] = (byte)titleLength;

            buffer[6] = (byte)(contentLength >> 24);
            buffer[7] = (byte)(contentLength >> 16);
            buffer[8] = (byte)(contentLength >> 8);
            buffer[9] = (byte)(contentLength);

            buffer[10] = stateCode;

            buffer[11] = (byte)(messageId >> 24);
            buffer[12] = (byte)(messageId >> 16);
            buffer[13] = (byte)(messageId >> 8);
            buffer[14] = (byte)messageId;

            if (hashCodeByteArray == null)
                hashCodeByteArray = new byte[4];

            ComputeHashCode(hashCodeByteArray,(Int16)extentionLength,(Int16)titleLength, contentLength, messageId);

            buffer[15] = hashCodeByteArray[0];
            buffer[16] = hashCodeByteArray[1];
            buffer[17] = hashCodeByteArray[2];
            buffer[18] = hashCodeByteArray[3];
        }

        public static void FillFrameBody(byte[] buffer, byte[] extentionBytes, byte[] titleBytes, byte[] contentBytes)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (extentionBytes == null)
                extentionBytes = EmptyBytes;

            if (titleBytes == null)
                titleBytes = EmptyBytes;

            if (contentBytes == null)
                contentBytes = EmptyBytes;

            if (extentionBytes.Length > 0)
                Buffer.BlockCopy(extentionBytes, 0, buffer, FrameHeaderSize, extentionBytes.Length);

            if (titleBytes.Length > 0)
                Buffer.BlockCopy(titleBytes, 0, buffer, FrameHeaderSize + extentionBytes.Length, titleBytes.Length);

            if (contentBytes.Length > 0)
                Buffer.BlockCopy(contentBytes, 0, buffer, FrameHeaderSize + extentionBytes.Length + titleBytes.Length, contentBytes.Length);
        }

        private static void ComputeHashCode(byte[] hashCodeByteArray,Int16 extentionByteLength, Int16 titleByteLength, int contentByteLength,int messageId)
        {
            var hashCode = HeaderBit0 << 8 | HeaderBit1;

            for (int i = 0; i < 3; i++)
            {
                hashCode = (hashCode << 2) ^ messageId ^ extentionByteLength ^ titleByteLength ^ contentByteLength;
            }                                   

            hashCodeByteArray[0] = (byte)(hashCode >> 24);
            hashCodeByteArray[1] = (byte)(hashCode >> 16);
            hashCodeByteArray[2] = (byte)(hashCode >> 8);
            hashCodeByteArray[3] = (byte)(hashCode);            
        }

        public static int ComputeFrameByteCount(byte[] extentionBytes, byte[] titleBytes, byte[] contentBytes)
        {
            if (extentionBytes == null)
                extentionBytes = EmptyBytes;

            if (titleBytes == null)
                titleBytes = EmptyBytes;

            if (contentBytes == null)
                contentBytes = EmptyBytes;

            return FrameHeaderSize + extentionBytes.Length + titleBytes.Length + contentBytes.Length;
        }

        public static void ReadDataFromHeaderBuffer(byte[] buffer, ref FrameHeaderData frameHeaderData)
        {
            var extentionLength = buffer[2] << 8 | buffer[3];
            var titleLength = buffer[4] << 8 | buffer[5];
            var contentLength = buffer[6] << 24 | buffer[7] << 16 | buffer[8] << 8 | buffer[9];
            var messageId = buffer[11] << 24 | buffer[12] << 16 | buffer[13] << 8 | buffer[14];

            frameHeaderData.Bind((Int16)extentionLength,(Int16)titleLength, contentLength, buffer[10], messageId);
        }

        public static void FillFrame(byte[] sendBuffer,byte[] extentionBytes, byte[] titleBytes, byte[] contentBytes, byte stateCode, int messageId)
        {
            FillFrameHeader(sendBuffer, extentionBytes, titleBytes, contentBytes, stateCode, messageId);
            FillFrameBody(sendBuffer, extentionBytes, titleBytes, contentBytes);
        }        
    }
}
