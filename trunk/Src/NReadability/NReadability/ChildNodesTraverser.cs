using System;
using HtmlAgilityPack;

namespace NReadability
{
  internal class ChildNodesTraverser
  {
    private readonly Action<HtmlNode> _childNodeVisitor;

    #region Constructor(s)

    public ChildNodesTraverser(Action<HtmlNode> childNodeVisitor)
    {
      if (childNodeVisitor == null)
      {
        throw new ArgumentNullException("childNodeVisitor");
      }

      _childNodeVisitor = childNodeVisitor;
    }

    #endregion

    #region Public methods

    public void Traverse(HtmlDocument document, HtmlNode node)
    {
      HtmlNode childNode = node.FirstChild;

      while (childNode != null)
      {
        HtmlNode nextChildNode = childNode.NextSibling;

        _childNodeVisitor(childNode);

        childNode = nextChildNode;
      }
    }

    #endregion
  }
}
