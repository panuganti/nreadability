using System.Text;
using HtmlAgilityPack;

namespace NReadability
{
  public class AgilityDomSerializer
  {
    #region Public methods

    public string SerializeDocument(HtmlDocument document)
    {
      var resultSB = new StringBuilder();

      using (var stringWriter = new EncodedStringWriter(resultSB))
      {
        document.Save(stringWriter);
      }

      return resultSB.ToString();
    }

    #endregion
  }
}
