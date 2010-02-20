using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace NReadability
{
  internal static class DomExtensions
  {
    #region HtmlDocument extensions

    public static HtmlNode GetBody(this HtmlDocument document)
    {
      if (document.DocumentNode == null)
      {
        return null;
      }

      return document.DocumentNode.GetElementsByTagName("body").FirstOrDefault();
    }

    #endregion

    #region HtmlNode extensions

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

    public static IEnumerable<HtmlNode> GetElementsByTagName(this HtmlNode node, string nodeName)
    {
      return node.DescendantNodes().Where(descendantNode => descendantNode.Name == nodeName);
    }

    #endregion
  }
}
