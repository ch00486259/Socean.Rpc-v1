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

        /// <summary>
        /// 0 7e (126)
        /// 1 7f 127
        /// 2 title length
        /// 3 title length
        /// 4 content length
        /// 5 content length
        /// 6 content length
        /// 7 content length
        /// 8 state code
        /// 9 message id
        /// 10 message id
        /// 11 message id
        /// 12 message id
        /// 13 hash code
        /// 14 hash code
        /// 15 hash code
        /// 16 hash code
        /// </summary>
        /// <returns></returns>
        public static bool CheckFrameHeader(byte[] buffer,int readCount)
        {
            if (readCount != FrameHeaderSize)
                return false;

            if (buffer.Length < FrameHeaderSize)
                return false;

            if (buffer[0] != HeaderBit0)
                return false;

            if (buffer[1] != HeaderBit1)
                return false;

            var titleLength = buffer[2] << 8 | buffer[3];
            var contentLength = buffer[4] << 24 | buffer[5] << 16 | buffer[6] << 8 | buffer[7];
            var messageId = buffer[9] << 24 | buffer[10] << 16 | buffer[11] << 8 | buffer[12];

            var hashCodeArray = ComputeHashCode((Int16)titleLength, contentLength, messageId);

            return hashCodeArray[0] == buffer[13] && hashCodeArray[1] == buffer[14] && hashCodeArray[2] == buffer[15] && hashCodeArray[3] == buffer[16];
        }

        public static void FillFrameHeader(byte[] buffer, byte[] titleBytes, byte[] contentBytes, byte stateCode, int messageId)
        {
            if (buffer == null)
                throw new Exception();

            if (titleBytes == null)
                titleBytes = EmptyBytes;

            if (titleBytes.Length > 65535)
                throw new Exception();

            var reserve1 = (byte)(messageId >> 24);
            var reserve2 = (byte)(messageId >> 16);
            var reserve3 = (byte)(messageId >> 8);
            var reserve4 = (byte)messageId;

            if (contentBytes == null)
                contentBytes = EmptyBytes;

            var titleLength = titleBytes.Length;
            var contentLength = contentBytes.Length;

            buffer[0] = HeaderBit0;
            buffer[1] = HeaderBit1;
            buffer[2] = (byte)(titleLength >> 8);
            buffer[3] = (byte)titleLength;
            buffer[4] = (byte)((contentLength >> 24));
            buffer[5] = (byte)((contentLength >> 16));
            buffer[6] = (byte)((contentLength >> 8));
            buffer[7] = (byte)(contentLength);
            buffer[8] = stateCode;

            buffer[9] = reserve1;
            buffer[10] = reserve2;
            buffer[11] = reserve3;
            buffer[12] = reserve4;

            var hashCodeArray = ComputeHashCode((Int16)titleLength, contentLength, messageId);

            buffer[13] = hashCodeArray[0];
            buffer[14] = hashCodeArray[1];
            buffer[15] = hashCodeArray[2];
            buffer[16] = hashCodeArray[3];
        }

        public static void FillFrameBody(byte[] buffer, byte[] titleBytes, byte[] contentBytes)
        {
            if (buffer == null)
                throw new Exception();

            if (titleBytes == null)
                titleBytes = EmptyBytes;

            if (contentBytes == null)
                contentBytes = EmptyBytes;

            if (titleBytes.Length > 0)
                Array.Copy(titleBytes, 0, buffer, FrameHeaderSize, titleBytes.Length);

            if (contentBytes.Length > 0)
                Array.Copy(contentBytes, 0, buffer, FrameHeaderSize + titleBytes.Length,
                    contentBytes.Length);
        }

        //[ThreadStatic] private static byte[] hashCodeByteArray1;

        private static byte[] ComputeHashCode(Int16 titleByteLength, int contentByteLength,int messageId)
        {
            var hashCode = HeaderBit0 << 8 | HeaderBit1;

            for (int i = 0; i < 8; i++)
            {
                hashCode = hashCode << 2;
                hashCode = hashCode ^ messageId;
                hashCode = hashCode ^ titleByteLength;
                hashCode = hashCode ^ contentByteLength;
            }

            var hashCodeByteArray = new byte[4];

            hashCodeByteArray[0] = (byte)((hashCode >> 24));
            hashCodeByteArray[1] = (byte)((hashCode >> 16));
            hashCodeByteArray[2] = (byte)((hashCode >> 8));
            hashCodeByteArray[3] = (byte)((hashCode));

            return hashCodeByteArray;
        }

        public static byte[] GetTitleBytes(string title)
        {
            if (title == null)
                return EmptyBytes;

            return Encoding.UTF8.GetBytes(title);
        }

        public static string GetTitle(byte[] buffer, int index, int count)
        {
            return Encoding.UTF8.GetString(buffer, index, count);
        }

        public static int ComputeFrameByteCount(byte[] titleBytes, byte[] contentBytes)
        {
            if (titleBytes == null)
                titleBytes = EmptyBytes;

            if (contentBytes == null)
                contentBytes = EmptyBytes;

            return FrameHeaderSize + titleBytes.Length + contentBytes.Length;
        }

        public static void ReadDataFromHeaderBuffer(byte[] buffer, ref FrameHeaderData frameHeaderData)
        {
            var titleLength = buffer[2] << 8 | buffer[3];
            var contentLength = buffer[4] << 24 | buffer[5] << 16 | buffer[6] << 8 | buffer[7];
            var messageId = buffer[9] << 24 | buffer[10] << 16 | buffer[11] << 8 | buffer[12];

            frameHeaderData.Bind((Int16)titleLength, contentLength, buffer[8], messageId);
        }
    }
}
