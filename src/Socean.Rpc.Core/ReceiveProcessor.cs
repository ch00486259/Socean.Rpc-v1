using Socean.Rpc.Core.Message;
using System;

namespace Socean.Rpc.Core
{
    internal class ReceiveProcessor
    {
        internal ReceiveProcessor()
        {
            _readBuffer = new byte[NetworkSettings.ReadBufferSize];
            _tempFrameHeaderData = new FrameHeaderData(); 
        }

        private Int16 _headerExtentionLength;
        private Int16 _titleLength;
        private int _contentLength;
        private byte _stateCode;
        private int _messageId;

        private byte[] _headerExtentionBytes;
        private byte[] _titleBytes;
        private byte[] _contentBytes;

        private volatile int _messageBytesReadLength = 0;

        private volatile int _bufferStartIndex = 0;
        private readonly byte[] _readBuffer;

        private volatile FrameHeaderData _currentFrameHeaderData;
        private readonly FrameHeaderData _tempFrameHeaderData;

        private volatile int _unprocessCount = 0;


        internal void Reset()
        {
            _unprocessCount = 0;
            _bufferStartIndex = 0;
            _messageBytesReadLength = 0;

            _headerExtentionBytes = FrameFormat.EmptyBytes;
            _titleBytes = FrameFormat.EmptyBytes;
            _contentBytes = FrameFormat.EmptyBytes;

            _currentFrameHeaderData = null;
        }

        internal void GetNextReceiveCallbackData(ref ReceiveCallbackData tempReceiveCallbackData)
        {
            tempReceiveCallbackData.Bind(_readBuffer, _bufferStartIndex, _readBuffer.Length - _bufferStartIndex);
        }

        private int GetBodyLength()
        {
            return _headerExtentionLength + _titleLength + _contentLength;
        }

        private int ReadExtentionBytes(int toOffset, int fromOffset, int readCount)
        {
            var returnValue = _headerExtentionLength - toOffset;
            if (returnValue > readCount)
                returnValue = readCount;

            Buffer.BlockCopy(_readBuffer, fromOffset, _headerExtentionBytes, toOffset, returnValue);
            return returnValue;
        }

        private int ReadTitleBytes(int toOffset, int fromOffset, int readCount)
        {
            var returnValue = _titleLength + _headerExtentionLength - toOffset;
            if (returnValue > readCount)
                returnValue = readCount;

            Buffer.BlockCopy(_readBuffer, fromOffset, _titleBytes, toOffset - _headerExtentionLength, returnValue);
            return returnValue;
        }

        private int ReadContentBytes(int toOffset, int fromOffset, int readCount)
        {
            var returnValue = _contentLength + _titleLength + _headerExtentionLength - toOffset;
            if (returnValue > readCount)
                returnValue = readCount;

            Buffer.BlockCopy(_readBuffer, fromOffset, _contentBytes, toOffset - _headerExtentionLength - _titleLength, returnValue);
            return returnValue;
        }

        private void ReadBufferDatas(int unprocessedByteCount)
        {
            var fromOffset = _bufferStartIndex - unprocessedByteCount;
            var toOffset = _messageBytesReadLength - FrameFormat.FrameHeaderSize - unprocessedByteCount ;
            var currentReadCount = 0;
            
            if (toOffset < _headerExtentionLength )
            {
                if (_headerExtentionLength > 0)
                {
                    if (currentReadCount == unprocessedByteCount)
                        return;
                    currentReadCount += ReadExtentionBytes(toOffset + currentReadCount, fromOffset + currentReadCount, unprocessedByteCount - currentReadCount);
                }              
            }
            if (toOffset < _titleLength + _headerExtentionLength )
            {
                if (_titleLength > 0)
                {
                    if (currentReadCount == unprocessedByteCount)
                        return;
                    currentReadCount += ReadTitleBytes(toOffset + currentReadCount, fromOffset + currentReadCount, unprocessedByteCount - currentReadCount);
                }              
            }
            if (toOffset < _contentLength + _titleLength + _headerExtentionLength )
            {
                if (_contentLength > 0)
                {
                    if (currentReadCount == unprocessedByteCount)
                        return;
                    currentReadCount += ReadContentBytes(toOffset + currentReadCount, fromOffset + currentReadCount, unprocessedByteCount - currentReadCount);
                }
            }
        }


        internal int CheckCurrentStep(int readCount)
        {
            _messageBytesReadLength += readCount;
            _bufferStartIndex += readCount;

            if (_currentFrameHeaderData == null)
            {
                if (_messageBytesReadLength < FrameFormat.FrameHeaderSize)
                    return 0;

                if (FrameFormat.CheckFrameHeader(_readBuffer) == false)
                    return -1;

                var frameHeaderData = _tempFrameHeaderData;
                FrameFormat.ReadDataFromHeaderBuffer(_readBuffer, ref frameHeaderData);

                _headerExtentionLength = frameHeaderData.HeaderExtentionLength;
                _titleLength = frameHeaderData.TitleLength;
                _contentLength = frameHeaderData.ContentLength;
                _stateCode = frameHeaderData.StateCode;
                _messageId = frameHeaderData.MessageId;

                _currentFrameHeaderData = frameHeaderData;

                _headerExtentionBytes = _headerExtentionLength == 0 ? FrameFormat.EmptyBytes : new byte[_headerExtentionLength];
                _titleBytes = _titleLength == 0 ? FrameFormat.EmptyBytes : new byte[_titleLength];
                _contentBytes = _contentLength == 0 ? FrameFormat.EmptyBytes : new byte[_contentLength];

                if (GetBodyLength() == 0)
                    return _messageBytesReadLength == FrameFormat.FrameHeaderSize ? 1 : -1;

                readCount = readCount - FrameFormat.FrameHeaderSize;
            }

            _unprocessCount += readCount;

            var messageLength = GetBodyLength() + FrameFormat.FrameHeaderSize;

            // message format error,return error code
            if (_messageBytesReadLength > messageLength)            
                return -1;            

            // read datas and return success code
            if (_messageBytesReadLength == messageLength)
            {
                ReadBufferDatas(_unprocessCount);
                return 1;
            }

            // message is not complete,return continue-reading code
            // if buffer is full,move datas to cache and clear buffer
            if (_bufferStartIndex == _readBuffer.Length)
            {
                ReadBufferDatas(_unprocessCount);
                _unprocessCount = 0;
                _bufferStartIndex = 0;
            }

            //continue to read
            return 0;            
        }

        internal FrameData GetCurrentReceiveData()
        {
            return new FrameData(_headerExtentionBytes, _titleBytes, _contentBytes, _stateCode, _messageId);
        }
    }
}
