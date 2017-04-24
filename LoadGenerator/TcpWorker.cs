using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace LoadGenerator
{
    class TcpWorker : IDisposable
    {
        private readonly TcpWorkerHelper _helper;
        private readonly TcpClient _client;
        private NetworkStream _stream;
        private readonly byte[] _responseBuffer;

        public TcpWorker(TcpWorkerHelper helper)
        {
            _helper = helper;
            _client = new TcpClient() { ReceiveTimeout = 10000, NoDelay = true };
            _responseBuffer = new byte[_client.ReceiveBufferSize];
        }

        public void Connect()
        {
            _client.Connect(_helper.Uri.Host, _helper.Uri.Port);
            _stream = _client.GetStream();
        }

        public void Send()
        {
            _stream.Write(_helper.Message, 0, _helper.Message.Length);
        }

        public bool ResponseArrivedWithOK()
        {
            return _helper.ResponseLength == 0 ? DetermineResponseLength() : ReadInternal();
        }

        private bool DetermineResponseLength()
        {
            var bytesRead = 0;
            var responseBufferRewriteCount = -1;
            var overlapCount = 0;
            var statusIsOK = false;

            while (bytesRead < 11)
            {
                bytesRead += _stream.Read(_responseBuffer, bytesRead, _responseBuffer.Length - bytesRead);
                responseBufferRewriteCount++;
            }

            statusIsOK = ResponseBufferContains200();

            var start = 12;
            int contentLengthEndIndex;
            while (!ResponseContainsContentLength(start, bytesRead, out contentLengthEndIndex))
            {
                CopyEndToBeginning(_helper.Content_Length.Length, bytesRead);
                bytesRead += _stream.Read(_responseBuffer, _helper.Content_Length.Length, _responseBuffer.Length - _helper.Content_Length.Length);
                start = 0;
                responseBufferRewriteCount++;
                overlapCount += _helper.Content_Length.Length;
            }

            var contentLengthValueInBytes = new List<byte>(3);
            start = contentLengthEndIndex + 1;
            while (!ReadContentLengthValue(contentLengthValueInBytes, start, bytesRead))
            {
                bytesRead += _stream.Read(_responseBuffer, 0, _responseBuffer.Length);
                start = 0;
                responseBufferRewriteCount++;
            }

            int doubleNewLineEndIndex;
            while (!ResponseContainsDoubleNewLine(contentLengthEndIndex, bytesRead, out doubleNewLineEndIndex))
            {
                CopyEndToBeginning(_helper.Double_NewLine.Length, bytesRead);
                bytesRead += _stream.Read(_responseBuffer, _helper.Double_NewLine.Length, _responseBuffer.Length - _helper.Double_NewLine.Length);
                responseBufferRewriteCount++;
                overlapCount += _helper.Double_NewLine.Length;
            }

            var contentLength = int.Parse(Encoding.ASCII.GetString(contentLengthValueInBytes.ToArray()));
            var headerLength = responseBufferRewriteCount * _responseBuffer.Length - overlapCount + doubleNewLineEndIndex + 1;
            _helper.ResponseLength = headerLength + contentLength;
            while (bytesRead < _helper.ResponseLength)
            {
                bytesRead += _stream.Read(_responseBuffer, 0, _responseBuffer.Length);
            }

            return statusIsOK;
        }

        private bool ReadInternal()
        {
            var bytesRead = 0;
            var statusIsOK = false;

            while (bytesRead < 11)
            {
                bytesRead += _stream.Read(_responseBuffer, bytesRead, _responseBuffer.Length - bytesRead);
            }

            statusIsOK = ResponseBufferContains200();

            while (bytesRead < _helper.ResponseLength)
            {
                bytesRead += _stream.Read(_responseBuffer, 0, _responseBuffer.Length);
            }

            return statusIsOK;
        }

        private bool ResponseBufferContains200()
        {
            return _responseBuffer[9] == _helper.HTTP_200[9]       //2
                && _responseBuffer[10] == _helper.HTTP_200[10]     //0
                && _responseBuffer[11] == _helper.HTTP_200[11];    //0 
        }

        private bool ResponseContainsContentLength(int start, int bytesRead, out int endIndex)
        {
            var end = Math.Min(_responseBuffer.Length, bytesRead);
            var charMatchCount = 0;
            for (int i = start; i < end; i++)
            {
                if (_responseBuffer[i] == _helper.Content_Length[charMatchCount])
                {
                    charMatchCount++;
                }

                if (charMatchCount == _helper.Content_Length.Length)
                {
                    endIndex = i;
                    return true;
                }
            }

            endIndex = 0;
            return false;
        }

        private bool ResponseContainsDoubleNewLine(int start, int bytesRead, out int endIndex)
        {
            var end = Math.Min(_responseBuffer.Length, bytesRead);
            var charMatchCount = 0;
            for (int i = start; i < end; i++)
            {
                if (_responseBuffer[i] == _helper.Double_NewLine[charMatchCount])
                {
                    charMatchCount++;
                }

                if (charMatchCount == _helper.Double_NewLine.Length)
                {
                    endIndex = i;
                    return true;
                }
            }

            endIndex = 0;
            return false;
        }

        private bool ReadContentLengthValue(List<byte> buffer, int startIndex, int bytesRead)
        {
            var endIndex = Math.Min(_responseBuffer.Length, bytesRead);
            for (int i = startIndex; i < endIndex; i++)
            {
                if (_responseBuffer[i] != _helper.Content_Length_Value_End_Mark)
                {
                    buffer.Add(_responseBuffer[i]);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private void CopyEndToBeginning(int numberOfBytesToCopy, int bytesRead)
        {
            var lastIndex = Math.Min(_responseBuffer.Length, bytesRead);
            var startIndex = lastIndex - numberOfBytesToCopy;
            for (int i = startIndex; i < lastIndex; i++)
            {
                _responseBuffer[i - startIndex] = _responseBuffer[i];
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
            _client.Dispose();
        }
    }
}
