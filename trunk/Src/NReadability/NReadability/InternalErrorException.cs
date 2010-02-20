using System;
using System.Runtime.Serialization;

namespace NReadability
{
  [Serializable]
  public class InternalErrorException : Exception
  {
    #region Constructor(s)

    public InternalErrorException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    public InternalErrorException(string message)
      : base(message)
    {
    }

    public InternalErrorException()
    {
    }

    protected InternalErrorException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    #endregion
  }
}
