namespace RcloneEncrypt.Core;

public class RcloneCryptException : Exception
{
    public RcloneCryptException(string message)
        : base(message)
    {
    }

    public RcloneCryptException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
