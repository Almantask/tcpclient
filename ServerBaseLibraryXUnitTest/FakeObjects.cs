using System;
using System.Collections.Generic;
using System.Text;
using TcpServerBaseLibrary;
using TcpServerBaseLibrary.Interfaces;

namespace TcpServerBaseLibrary.Tests
{


    internal class DummyStringHandler : IMessageManager
    {
        public string HandledString;

        public void HandleMessage(MessageObject msgobj)
        {
            HandledString = Encoding.ASCII.GetString(msgobj.CompleteData);
        }
    }

    internal class DummyLogger : ILogger
    {
        public void Debug(string message) { }

        public void Error(string message) { }

        public void Info(string message) { }
    }

}
