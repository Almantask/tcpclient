namespace TcpServerBaseLibrary
{
    public interface ILogger
    {

        void Info(string message);

        void Debug(string message);

        void Error(string message);
    }
}