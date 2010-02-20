using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NReadability
{
  public class ReadabilityTranscoder
  {
    private const string CSS_CLASS_READABILITY_STYLED = "readability-styled";

    private static readonly Regex _UnlikelyCandidatesRegex = new Regex("combx|comment|disqus|foot|header|menu|meta|rss|shoutbox|sidebar|sponsor", RegexOptions.IgnoreCase);
    private static readonly Regex _OkMaybeItsACandidateRegex = new Regex("and|article|body|column|main", RegexOptions.IgnoreCase);
    private static readonly Regex _PositiveRegex = new Regex("article|body|content|entry|hentry|page|pagination|post|text", RegexOptions.IgnoreCase);
    private static readonly Regex _NegativeRegex = new Regex("combx|comment|contact|foot|footer|footnote|link|media|meta|promo|related|scroll|shoutbox|sponsor|tags|widget", RegexOptions.IgnoreCase);
    private static readonly Regex _DivToPElementsRegex = new Regex("\\<(a|blockquote|dl|div|img|ol|p|pre|table|ul)", RegexOptions.IgnoreCase);

    private readonly bool _dontStripUnlikelys;
    private readonly bool _dontNormalizeSpacesInTextContent;

    private readonly AgilityDomBuilder _agilityDomBuilder;
    private readonly AgilityDomSerializer _agilityDomSerializer;

    #region Constructor(s)

    public ReadabilityTranscoder(bool dontStripUnlikelys)
    {
      _dontStripUnlikelys = dontStripUnlikelys;

      _agilityDomBuilder = new AgilityDomBuilder();
      _agilityDomSerializer = new AgilityDomSerializer();
    }

    public ReadabilityTranscoder()
      : this(false)
    {
    }

    #endregion

    #region Public methods

    public string Transcode(string htmlContent)
    {
      var document = _agilityDomBuilder.BuildDocument(htmlContent);
      
      ExtractMainContent(document);
      CalculateParagraphsScores(document);

      return _agilityDomSerializer.SerializeDocument(document);
    }

    #endregion

    #region Readability algorithm

    internal void ExtractMainContent(HtmlDocument document)
    {
      StripUnlikelyCandidates(document);
    }

    internal void StripUnlikelyCandidates(HtmlDocument document)
    {
      if (_dontStripUnlikelys)
      {
        return;
      }

      var documentNode = document.DocumentNode;

      new NodesTraverser(
        node =>
        {
          string nodeName = node.Name;

          /* Remove unlikely candidates. */
          string unlikelyMatchString = node.GetClass() + node.GetId();

          if (unlikelyMatchString.Length > 0
           && !"body".Equals(nodeName, StringComparison.OrdinalIgnoreCase)
           && _UnlikelyCandidatesRegex.IsMatch(unlikelyMatchString)
           && !_OkMaybeItsACandidateRegex.IsMatch(unlikelyMatchString))
          {
            HtmlNode parentNode = node.ParentNode;

            if (parentNode != null)
            {
              parentNode.RemoveChild(node);
            }

            // node has been removed - we can go to the next one
            return;
          }

          /* Turn all divs that don't have children block level elements into p's or replace text nodes within the div with p's. */
          if ("div".Equals(nodeName, StringComparison.OrdinalIgnoreCase))
          {
            if (!_DivToPElementsRegex.IsMatch(node.InnerHtml))
            {
              // no block elements inside - change to p
              node.Name = "p";
            }
            else
            {
              // replace text nodes with p's (experimental)
              new ChildNodesTraverser(
                childNode =>
                {
                  if (childNode.NodeType != HtmlNodeType.Text
                   || string.IsNullOrEmpty(childNode.InnerText.Trim()))
                  {
                    return;
                  }

                  HtmlNode pNode = document.CreateElement("p");

                  pNode.InnerHtml = childNode.InnerText;
                  pNode.SetClass(CSS_CLASS_READABILITY_STYLED);
                  pNode.SetStyle("display: inline;");

                  node.ReplaceChild(pNode, childNode);
                }
                ).Traverse(document, node);
            }
          }
        }).Traverse(document, documentNode);
    }

    internal void CalculateParagraphsScores(HtmlDocument document)
    {
      var pNodes = document.DocumentNode.ChildNodes.Where(node => node.Name == "p");

      foreach (var pNode in pNodes)
      {
        Console.WriteLine(pNode.Name);

        
      }
    }

    #endregion
  }
}
