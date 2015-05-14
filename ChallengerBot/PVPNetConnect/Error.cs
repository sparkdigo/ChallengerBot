namespace PVPNetConnect
{
    public enum ErrorType
    {
        Password,
        AuthKey,
        Handshake,
        Connect,
        Login,
        Invoke,
        Receive,
        General,
        MaxLevelReached
    }

    public class Error
    {
        public ErrorType Type;
        public string Message = "";
        public string ErrorCode = "";
    }
}