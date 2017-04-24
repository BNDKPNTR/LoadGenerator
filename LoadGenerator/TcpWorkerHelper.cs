using System;
using System.Text;

namespace LoadGenerator
{
    class TcpWorkerHelper
    {
        private const string MESSAGE_TEMPLATE = "GET {0} HTTP/1.1\r\n\r\n";

        private int _responseLength;
        private object _syncObj = new object();

        public byte[] HTTP_200 => new byte[] { 72, 84, 84, 80, 47, 49, 46, 49, 32, 50, 48, 48 }; // "HTTP/1.1 200"
        public byte[] Content_Length => new byte[] { 67, 111, 110, 116, 101, 110, 116, 45, 76, 101, 110, 103, 116, 104, 58, 32 }; // "Content-Length: "
        public byte Content_Length_Value_End_Mark => 13; // "\r"
        public byte[] Double_NewLine => new byte[] { 13, 10, 13, 10 }; // "\r\n\r\n"
        public byte[] Message { get; }
        public Uri Uri { get; }
        public int ResponseLength
        {
            get { return _responseLength; }
            set { lock (_syncObj) _responseLength = value; }
        }

        public TcpWorkerHelper(Uri uri)
        {
            Uri = uri;

            var messageString = string.Format(MESSAGE_TEMPLATE, uri.AbsolutePath);
            Message = Encoding.ASCII.GetBytes(messageString);
        }
    }
}
