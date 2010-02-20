using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NReadability
{
  public class ReadabilityTranscoder
  {
    private const string CSS_CLASS_READABILITY_STYLED = "readability-styled";
    private const string ID_READABILITY_CONTENT_DIV = "readability-content";
    
    private const int MIN_PARAGRAPH_LENGTH = 25;
    private const int PARAGRAPH_SEGMENT_LENGTH = 100;
    private const int MAX_POINTS_FOR_SEGMENTS_COUNT = 3;
    private const float SIBLING_SCORE_TRESHOLD_COEFFICIENT = 0.2f;
    private const float MAX_SIBLING_SCORE_TRESHOLD = 10.0f;
    private const int MIN_SIBLING_PARAGRAPH_LENGTH = 80;
    private const float MAX_SIBLING_PARAGRAPH_LINKS_DENSITY = 0.25f;

    private static readonly Regex _UnlikelyCandidatesRegex = new Regex("combx|comment|disqus|foot|header|menu|meta|rss|shoutbox|sidebar|sponsor", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _OkMaybeItsACandidateRegex = new Regex("and|article|body|column|main", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _PositiveRegex = new Regex("article|body|content|entry|hentry|page|pagination|post|text", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _NegativeRegex = new Regex("combx|comment|contact|foot|footer|footnote|link|media|meta|promo|related|scroll|shoutbox|sponsor|tags|widget", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _DivToPElementsRegex = new Regex("\\<(a|blockquote|dl|div|img|ol|p|pre|table|ul)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _EndOfSentenceRegex = new Regex("\\.( |$)", RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly bool _dontStripUnlikelys;
    private readonly bool _dontNormalizeSpacesInTextContent;

    private readonly AgilityDomBuilder _agilityDomBuilder;
    private readonly AgilityDomSerializer _agilityDomSerializer;
    private readonly Dictionary<HtmlNode, float> _nodesScores;

    #region Constructor(s)

    public ReadabilityTranscoder(bool dontStripUnlikelys, bool dontNormalizeSpacesInTextContent)
    {
      _dontStripUnlikelys = dontStripUnlikelys;
      _dontNormalizeSpacesInTextContent = dontNormalizeSpacesInTextContent;

      _agilityDomBuilder = new AgilityDomBuilder();
      _agilityDomSerializer = new AgilityDomSerializer();
      _nodesScores = new Dictionary<HtmlNode, float>();
    }

    public ReadabilityTranscoder()
      : this(false, false)
    {
    }

    #endregion

    #region Public methods

    public string Transcode(string htmlContent)
    {
      var document = _agilityDomBuilder.BuildDocument(htmlContent);
      var mainContentNode = ExtractMainContent(document);

      // TODO: embed main content node inside a document

      return _agilityDomSerializer.SerializeDocument(document);
    }

    #endregion

    #region Readability algorithm

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

                  HtmlNode paraNode = document.CreateElement("p");

                  paraNode.InnerHtml = childNode.InnerText;
                  paraNode.SetClass(CSS_CLASS_READABILITY_STYLED);
                  paraNode.SetStyle("display: inline;");

                  node.ReplaceChild(paraNode, childNode);
                }
                ).Traverse(document, node);
            }
          }
        }).Traverse(document, documentNode);
    }

    internal IEnumerable<HtmlNode> FindCandidatesForMainContent(HtmlDocument document)
    {
      var paraNodes = document.DocumentNode.GetElementsByTagName("p");
      var candidateNodes = new HashSet<HtmlNode>();

      _nodesScores.Clear();

      foreach (var paraNode in paraNodes)
      {
        string innerText = paraNode.InnerText;

        if (innerText == null || innerText.Length < MIN_PARAGRAPH_LENGTH)
        {
          continue;
        }

        var parentNode = paraNode.ParentNode;
        var grandParentNode = parentNode != null ? parentNode.ParentNode : null;
        int score = 1; // 1 point for having a paragraph

        // Add points for any commas within this paragraph.
        score += innerText.Count(ch => ch == ',');

        // For every PARAGRAPH_SEGMENT_LENGTH characters in this paragraph, add another point. Up to MAX_POINTS_FOR_SEGMENTS_COUNT points.
        score += Math.Min(innerText.Length / PARAGRAPH_SEGMENT_LENGTH, MAX_POINTS_FOR_SEGMENTS_COUNT);

        // Add the score to the parent.
        if (parentNode != null)
        {
          candidateNodes.Add(parentNode);
          AddPointsToNodeScore(parentNode, score);
        }

        // Add half the score to the grandparent.
        if (grandParentNode != null)
        {
          candidateNodes.Add(grandParentNode);
          AddPointsToNodeScore(grandParentNode, score / 2);
        }
      }

      return candidateNodes;
    }

    internal HtmlNode DetermineTopCandidateNode(HtmlDocument document, IEnumerable<HtmlNode> candidatesForMainContent)
    {
      HtmlNode topCandidateNode = null;

      foreach (HtmlNode candidateNode in candidatesForMainContent)
      {
        float candidateScore = GetNodeScore(candidateNode);

        // Scale the final candidates score based on link density. Good content should have a
        // relatively small link density (5% or less) and be mostly unaffected by this operation.
        float newCandidateScore = (1.0f - GetLinksDensity(candidateNode)) * candidateScore;

        SetNodeScore(candidateNode, newCandidateScore);

        if (topCandidateNode == null
         || newCandidateScore > GetNodeScore(topCandidateNode))
        {
          topCandidateNode = candidateNode;
        }
      }

      if (topCandidateNode == null
       || "body".Equals(topCandidateNode.Name, StringComparison.OrdinalIgnoreCase))
      {
        topCandidateNode = document.CreateElement("div");

        var documentBody = document.GetBody();

        if (documentBody != null)
        {
          topCandidateNode.AppendChildren(documentBody.ChildNodes);
        }
      }

      return topCandidateNode;
    }

    internal HtmlNode CreateArticleContentNode(HtmlDocument document, HtmlNode topCandidateNode)
    {
      // Now that we have the top candidate, look through its siblings for content that might also be related.
      // Things like preambles, content split by ads that we removed, etc.

      var articleContentNode = document.CreateElement("div");

      articleContentNode.SetId(ID_READABILITY_CONTENT_DIV);

      HtmlNode parentNode = topCandidateNode.ParentNode;

      if (parentNode == null)
      {
        // shouldn't happen (unless the found top candidate node has no parent for some reason)
        return articleContentNode;
      }

      IEnumerable<HtmlNode> siblingNodes = parentNode.ChildNodes;
      
      float topCandidateNodeScore = GetNodeScore(topCandidateNode);
      float siblingScoreThreshold =
        Math.Max(
          MAX_SIBLING_SCORE_TRESHOLD,
          SIBLING_SCORE_TRESHOLD_COEFFICIENT * topCandidateNodeScore);

      // iterate through the sibling nodes and decide whether append them
      foreach (var siblingNode in siblingNodes)
      {
        bool append = false;

        if (siblingNode == articleContentNode)
        {
          // we'll append the article content node (created from the top candidate node during an earlier step)
          append = true;
        }
        else if (GetNodeScore(siblingNode) >= siblingScoreThreshold)
        {
          // we'll append this node if the calculated score is higher than a treshold (derived from the score of the top candidate node)
          append = true;
        }
        else if ("p".Equals(siblingNode.Name, StringComparison.OrdinalIgnoreCase))
        {
          // we have to somehow decide whether we should append this paragraph

          string siblingNodeInnerText = siblingNode.InnerText;

          // we won't append an empty paragraph
          if (!string.IsNullOrEmpty(siblingNodeInnerText))
          {
            int siblingNodeInnerTextLength = siblingNodeInnerText.Length;

            if (siblingNodeInnerTextLength >= MIN_SIBLING_PARAGRAPH_LENGTH)
            {
              // we'll append this paragraph if the links density is not higher than a treshold
              append = GetLinksDensity(siblingNode) < MAX_SIBLING_PARAGRAPH_LINKS_DENSITY;
            }
            else
            {
              // we'll append this paragraph if there are no links inside and if it contains a probable end of sentence indicator
              append = GetLinksDensity(siblingNode).IsCloseToZero()
                    && _EndOfSentenceRegex.IsMatch(siblingNodeInnerText);
            }
          }
        }

        if (append)
        {
          // TODO:
        }
      }

      return articleContentNode;
    }

    internal void PrepareArticleContentNode(HtmlNode articleContentNode)
    {
      // TODO:
    }

    internal float GetLinksDensity(HtmlNode node)
    {
      int linksLength = 0;

      foreach (var anchorNode in node.GetElementsByTagName("a"))
      {
        string anchorInnerText = anchorNode.InnerText ?? "";

        linksLength += anchorInnerText.Length;
      }

      string nodeInnerText = node.InnerText;

      if (string.IsNullOrEmpty(nodeInnerText))
      {
        // we won't divide by zero
        return 0.0f;
      }

      return (float)linksLength / nodeInnerText.Length;
    }

    private HtmlNode ExtractMainContent(HtmlDocument document)
    {
      StripUnlikelyCandidates(document);

      var candidatesForMainContent = FindCandidatesForMainContent(document);

      // TODO: refactor?
      HtmlNode topCandidateNode = DetermineTopCandidateNode(document, candidatesForMainContent);
      HtmlNode articleContentNode = CreateArticleContentNode(document, topCandidateNode);

      PrepareArticleContentNode(articleContentNode);

      return articleContentNode;
    }

    #endregion

    #region Private helper methods

    private void AddPointsToNodeScore(HtmlNode node, int pointsToAdd)
    {
      float currentScore = _nodesScores.ContainsKey(node) ? _nodesScores[node] : 0.0f;

      _nodesScores[node] = currentScore + pointsToAdd;
    }

    private float GetNodeScore(HtmlNode node)
    {
      return _nodesScores.ContainsKey(node) ? _nodesScores[node] : 0.0f;
    }

    private void SetNodeScore(HtmlNode node, float score)
    {
      _nodesScores[node] = score;
    }

    #endregion
  }
}
