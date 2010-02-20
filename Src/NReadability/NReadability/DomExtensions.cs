using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using System.Text;

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
      if (id == null)
      {
        node.Attributes.Remove("id");
      }
      else
      {
        node.SetAttributeValue("id", id);
      }
    }

    public static string GetClass(this HtmlNode node)
    {
      return node.GetAttributeValue("class", "");
    }

    public static void SetClass(this HtmlNode node, string @class)
    {
      if (@class == null)
      {
        node.Attributes.Remove("class");
      }
      else
      {
        node.SetAttributeValue("class", @class);
      }
    }

    public static string GetStyle(this HtmlNode node)
    {
      return node.GetAttributeValue("style", "");
    }

    public static void SetStyle(this HtmlNode node, string style)
    {
      if (style == null)
      {
        node.Attributes.Remove("style");
      }
      else
      {
        node.SetAttributeValue("style", style);
      }
    }

    public static IEnumerable<HtmlNode> GetElementsByTagName(this HtmlNode node, string nodeName)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (nodeName == null)
      {
        throw new ArgumentNullException("nodeName");
      }

      return node.DescendantNodes().Where(descendantNode => nodeName.Equals(descendantNode.Name, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetAttributesString(this HtmlNode node, string separator)
    {
      if (separator == null)
      {
        throw new ArgumentNullException("separator");
      }

      var resultSB = new StringBuilder();
      bool isFirst = true;

      node.Attributes.Aggregate(
        resultSB,
        (sb, attribute) =>
          {
            string attributeValue = attribute.Value;

            if (string.IsNullOrEmpty(attributeValue))
            {
              return sb;
            }

            if (!isFirst)
            {
              resultSB.Append(separator);
            }

            isFirst = false;

            sb.Append(attribute.Value);

            return sb;
          });

      return resultSB.ToString();
    }

    #endregion
  }
}
