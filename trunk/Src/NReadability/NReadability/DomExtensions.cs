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

    public static string GetTitle(this HtmlDocument document)
    {
      var headNode = document.DocumentNode.GetElementsByTagName("head").FirstOrDefault();

      if (headNode == null)
      {
        return "";
      }

      var titleNode = headNode.GetChildrenByTagName("title").FirstOrDefault();

      if (titleNode == null)
      {
        return "";
      }

      return (titleNode.InnerText ?? "").Trim();
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

    public static IEnumerable<HtmlNode> GetElementsByTagName(this HtmlNode node, string tagName)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (tagName == null)
      {
        throw new ArgumentNullException("tagName");
      }

      tagName = tagName.Trim().ToLower();

      return GetNodesByNodeName(node.DescendantNodes(), tagName);
    }

    public static IEnumerable<HtmlNode> GetChildrenByTagName(this HtmlNode node, string tagName)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (tagName == null)
      {
        throw new ArgumentNullException("tagName");
      }

      return GetNodesByNodeName(node.ChildNodes, tagName);
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

    #region Private helper methods

    private static IEnumerable<HtmlNode> GetNodesByNodeName(IEnumerable<HtmlNode> nodes, string nodeName)
    {
      if (nodes == null)
      {
        throw new ArgumentNullException("nodes");
      }

      if (nodeName == null)
      {
        throw new ArgumentNullException("nodeName");
      }

      nodeName = nodeName.Trim().ToLower();

      return nodes
        .Where(descendantNode =>
          nodeName.Equals(
            (descendantNode.Name ?? "").Trim().ToLower(),
            StringComparison.OrdinalIgnoreCase));
    }

    #endregion
  }
}
