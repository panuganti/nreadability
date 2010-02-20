using System;
using System.IO;
using System.Text;

namespace NReadability
{
  internal class EncodedStringWriter : StringWriter
  {
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    private readonly Encoding _encoding;

    #region Constructor(s)

    public EncodedStringWriter(StringBuilder sb, Encoding encoding)
      : base(sb)
    {
      if (encoding == null)
      {
        throw new ArgumentNullException("encoding");
      }

      _encoding = encoding;
    }

    public EncodedStringWriter(StringBuilder sb)
      : this(sb, DefaultEncoding)
    {
    }

    #endregion

    #region Properties

    public override Encoding Encoding
    {
      get { return _encoding; }
    }

    #endregion
  }
}
