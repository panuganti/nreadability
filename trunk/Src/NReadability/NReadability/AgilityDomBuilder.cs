using HtmlAgilityPack;

namespace NReadability
{
  public class AgilityDomBuilder
  {
    #region Public methods

    public HtmlDocument BuildDocument(string htmlContent)
    {
      var document = new HtmlDocument();

      document.LoadHtml(htmlContent);

      return document;
    }

    #endregion
  }
}
