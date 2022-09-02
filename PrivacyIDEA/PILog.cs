using System;

namespace PrivacyIDEAClient
{
    public interface PILog
    {
        void PILog(string message);

        void PIError(string message);

        void PIError(Exception exception);
    }
}
