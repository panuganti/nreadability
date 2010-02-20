using System;
using HtmlAgilityPack;

namespace NReadability
{
  internal class NodesTraverser
  {
    private readonly Action<HtmlNode> _nodeVisitor;

    #region Constructor(s)

    public NodesTraverser(Action<HtmlNode> nodeVisitor)
    {
      if (nodeVisitor == null)
      {
        throw new ArgumentNullException("nodeVisitor");
      }

      _nodeVisitor = nodeVisitor;
    }

    #endregion

    #region Public methods

    public void Traverse(HtmlNode node)
    {
      _nodeVisitor(node);

      HtmlNode childNode = node.FirstChild;

      while (childNode != null)
      {
        HtmlNode nextChildNode = childNode.NextSibling;
        
        Traverse(childNode);

        childNode = nextChildNode;
      }
    }

    #endregion
  }
}
