using System;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    internal class ReceiveProcessor
    {
        internal ReceiveProcessor()
        {
            _currentStepReadBuffer = FrameFormat.EmptyBytes;

            _readBufferCache = new byte[NetworkSettings.ReadBufferSize];
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

        /// <summary>
        /// 0 header 1 body 2 finished
        /// </summary>
        private int _step;

        private int _currentStepReadCount;
        private int _currentStepReadBufferSize;
        private byte[] _currentStepReadBuffer;

        private readonly byte[] _readBufferCache;
        private readonly FrameHeaderData _tempFrameHeaderData;

        internal void Reset()
        {
            StepTo(0);
        }

        private void ChangeToStep(int step, byte[] buffer, int bufferSize)
        {
            _step = step;
            _currentStepReadBuffer = buffer;
            _currentStepReadBufferSize = bufferSize;
            _currentStepReadCount = 0;
        }

        private byte[] GetBodyReadBuffer(int byteCount)
        {
            if (byteCount <= _readBufferCache.Length)
            {
                return _readBufferCache;               
            }
            else
            {
                return new byte[byteCount];               
            }
        }

        private void StepTo(int step)
        {
            if (step == 0)
            {
                var readBuffer = GetBodyReadBuffer(FrameFormat.FrameHeaderSize);
                ChangeToStep(0, readBuffer, FrameFormat.FrameHeaderSize);
            }

            if (step == 1)
            {
                var bodyByteLength = _headerExtentionLength + _titleLength + _contentLength;
                var readBuffer = GetBodyReadBuffer(bodyByteLength);

                ChangeToStep(1, readBuffer, bodyByteLength);
            }

            if (step == 2)
            {
                ChangeToStep(2, FrameFormat.EmptyBytes, 0); 
            }
        }


        internal void CheckCurrentReceive(int readCount)
        {
            if (_step == 0)
            {
                _currentStepReadCount += readCount;

                if (FrameFormat.CheckFrameHeader(_currentStepReadBuffer, _currentStepReadCount) == false)
                    throw new Exception("step:0, CheckHeader error");

                var frameHeaderData = _tempFrameHeaderData;
                FrameFormat.ReadDataFromHeaderBuffer(_currentStepReadBuffer,ref frameHeaderData);

                _headerExtentionLength = frameHeaderData.HeaderExtentionLength;
                _titleLength = frameHeaderData.TitleLength;
                _contentLength = frameHeaderData.ContentLength;
                _stateCode = frameHeaderData.StateCode;
                _messageId = frameHeaderData.MessageId;

                if (_headerExtentionLength + _titleLength + _contentLength == 0)
                {
                    _headerExtentionBytes = FrameFormat.EmptyBytes;
                    _titleBytes = FrameFormat.EmptyBytes;
                    _contentBytes = FrameFormat.EmptyBytes; 

                    StepTo(2);
                    return;
                }

                StepTo(1);
                return;
            }

            if (_step == 1)
            {
                _currentStepReadCount += readCount;

                if (_currentStepReadCount < _headerExtentionLength + _titleLength + _contentLength)
                    return;

                if (_currentStepReadCount != _headerExtentionLength + _titleLength + _contentLength)
                    throw new Exception("message body length error");

                _headerExtentionBytes = new byte[_headerExtentionLength];
                if (_headerExtentionLength > 0)
                {
                    Array.Copy(_currentStepReadBuffer, 0, _headerExtentionBytes, 0, _headerExtentionLength);
                }

                _titleBytes = new byte[_titleLength];

                if (_titleLength > 0)
                {
                    Array.Copy(_currentStepReadBuffer, _headerExtentionLength, _titleBytes, 0, _titleLength);
                }

                _contentBytes = new byte[_contentLength];

                if (_contentLength > 0)
                {
                    Array.Copy(_currentStepReadBuffer, _headerExtentionLength + _titleLength, _contentBytes, 0, _contentLength);
                }

                StepTo(2);
                return;
            }

            if (_step == 2)
                throw new Exception("CheckCurrentReceive error,step 2 is not valid");
        }

        internal void GetNextReceiveCallbackData(ref ReceiveCallbackData tempReceiveCallbackData)
        {
            tempReceiveCallbackData.Bind(_currentStepReadBuffer, _currentStepReadCount, _currentStepReadBufferSize - _currentStepReadCount);
        }

        internal FrameData GetCurrentReceiveData()
        {
            if (_step == 2)
            {
                return new FrameData(_headerExtentionBytes, _titleBytes, _contentBytes, _stateCode, _messageId);
            }

            return null;
        }
    }
}
