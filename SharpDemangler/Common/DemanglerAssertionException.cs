using System;
using System.Runtime.Serialization;

namespace SharpDemangler.Common;

public class DemanglerAssertionException : Exception
{
    public DemanglerAssertionException()
    {
    }

    public DemanglerAssertionException(string message) : base(message)
    {
    }

    public DemanglerAssertionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
