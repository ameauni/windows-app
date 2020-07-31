﻿using System;
using System.Runtime.Serialization;

namespace EduroamConfigure
{
    public class EduroamAppUserError : Exception
    {
        public string UserFacingMessage { get; }

        public EduroamAppUserError(string message, string userFacingMessage = null) : base(message)
        {
#if DEBUG
            UserFacingMessage = userFacingMessage ?? ("NON-USER-FACING-MESSAGE: " + message);
#else
            UserFacingMessage = userFacingMessage ?? "NO REASON PROVIDED";
#endif
        }
    }
    public class ApiException : Exception
    {
        private const string DefaultMessage = "Error with Api";

        public ApiException() : base(DefaultMessage) { }
        public ApiException(string message) : base(message) { }
        public ApiException(string message, System.Exception innerException) : base(message, innerException) { }
    }

    public class ApiUnreachableException : ApiException
    {
        private const string DefaultMessage = "Api could not be reached";
        
        public ApiUnreachableException() : base(DefaultMessage) { }
        public ApiUnreachableException(string message) : base(message) { }
        public ApiUnreachableException(string message, System.Exception innerException) : base(message, innerException) { }
    }

    public class ApiParsingException : Exception
    {
        private const string DefaultMessage = "Api response could not be parsed";

        public ApiParsingException() : base(DefaultMessage) { }
        public ApiParsingException(string message) : base(message) { }
        public ApiParsingException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}

