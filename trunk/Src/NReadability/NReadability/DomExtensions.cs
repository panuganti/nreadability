using HtmlAgilityPack;

namespace NReadability
{
  internal static class DomExtensions
  {
    #region Public members

    public static string GetId(this HtmlNode node)
    {
      return node.GetAttributeValue("id", "");
    }

    public static void SetId(this HtmlNode node, string id)
    {
      node.SetAttributeValue("id", id);
    }

    public static string GetClass(this HtmlNode node)
    {
      return node.GetAttributeValue("class", "");
    }

    public static void SetClass(this HtmlNode node, string @class)
    {
      node.SetAttributeValue("class", @class);
    }

    public static string GetStyle(this HtmlNode node)
    {
      return node.GetAttributeValue("style", "");
    }

    public static void SetStyle(this HtmlNode node, string style)
    {
      node.SetAttributeValue("style", style);
    }

    #endregion
  }
}
