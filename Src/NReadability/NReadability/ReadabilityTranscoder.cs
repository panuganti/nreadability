using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NReadability
{
  public class ReadabilityTranscoder
  {
    #region Fields

    #region Algorithm constants

    private const string _CssClassReadabilityStyled = "readability-styled";
    private const string _IdReadabilityContentDiv = "readability-content";

    private const int _MinParagraphLength = 25;
    private const int _MinInnerTextLength = 25;
    private const int _ParagraphSegmentLength = 100;
    private const int _MaxPointsForSegmentsCount = 3;
    private const int _MinSiblingParagraphLength = 80;
    private const int _MinCommaSegments = 10;
    private const int _LisCountTreshold = 100;
    private const int _MaxImagesInShortSegmentsCount = 2;
    private const int _MinInnerTextLengthInNodesWithEmbed = 75;
    private const int _ClassWeightTreshold = 25;
    private const int _MaxEmbedsCount = 1;

    private const float _SiblingScoreTresholdCoefficient = 0.2f;
    private const float _MaxSiblingScoreTreshold = 10.0f;
    private const float _MaxSiblingParagraphLinksDensity = 0.25f;
    private const float _MaxHeaderLinksDensity = 0.33f;
    private const float _MaxDensityForNodesWithSmallerClassWeight = 0.2f;
    private const float _MaxDensityForNodesWithGreaterClassWeight = 0.5f;

    #endregion

    #region Algorithm regular expressions

    private static readonly Regex _UnlikelyCandidatesRegex = new Regex("combx|comment|disqus|foot|header|menu|meta|rss|shoutbox|sidebar|sponsor", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _OkMaybeItsACandidateRegex = new Regex("and|article|body|column|main", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _PositiveWeightRegex = new Regex("article|body|content|entry|hentry|page|pagination|post|text", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _NegativeWeightRegex = new Regex("combx|comment|contact|foot|footer|footnote|link|media|meta|promo|related|scroll|shoutbox|sponsor|tags|widget", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _DivToPElementsRegex = new Regex("<(a|blockquote|dl|div|img|ol|p|pre|table|ul)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _EndOfSentenceRegex = new Regex("\\.( |$)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex _BreakBeforeParagraphRegex = new Regex("<br[^>]*>\\s*<p", RegexOptions.Compiled);
    private static readonly Regex _NormalizeSpacesRegex = new Regex("\\s{2,}", RegexOptions.Compiled);
    private static readonly Regex _KillBreaksRegex = new Regex("(<br\\s*\\/?>(\\s|&nbsp;?)*){1,}", RegexOptions.Compiled);
    private static readonly Regex _VideoRegex = new Regex("http:\\/\\/(www\\.)?(youtube|vimeo)\\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #endregion

    #region Algorithm parameters
    
    private readonly bool _dontStripUnlikelys;
    private readonly bool _dontNormalizeSpacesInTextContent;
    private readonly bool _dontWeightClasses;

    #endregion

    #region Helper instance fields

    private readonly AgilityDomBuilder _agilityDomBuilder;
    private readonly AgilityDomSerializer _agilityDomSerializer;
    private readonly Dictionary<HtmlNode, float> _nodesScores;

    #endregion

    #endregion

    #region Constructor(s)

    public ReadabilityTranscoder(bool dontStripUnlikelys, bool dontNormalizeSpacesInTextContent, bool dontWeightClasses)
    {
      _dontStripUnlikelys = dontStripUnlikelys;
      _dontNormalizeSpacesInTextContent = dontNormalizeSpacesInTextContent;
      _dontWeightClasses = dontWeightClasses;

      _agilityDomBuilder = new AgilityDomBuilder();
      _agilityDomSerializer = new AgilityDomSerializer();
      _nodesScores = new Dictionary<HtmlNode, float>();
    }

    public ReadabilityTranscoder()
      : this(false, false, false)
    {
    }

    #endregion

    #region Public methods

    public string Transcode(string htmlContent)
    {
      var document = _agilityDomBuilder.BuildDocument(htmlContent);
      var mainContentNode = ExtractMainContent(document);

      // TODO: embed main content node inside a document
      document = new HtmlDocument();
      document.DocumentNode.AppendChild(mainContentNode);

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
                        || GetInnerText(childNode).Length == 0)
                    {
                      return;
                    }

                    HtmlNode paraNode = document.CreateElement("p");

                    paraNode.InnerHtml = GetInnerText(childNode);
                    paraNode.SetClass(_CssClassReadabilityStyled);
                    paraNode.SetStyle("display: inline;");

                    node.ReplaceChild(paraNode, childNode);
                  }
                ).Traverse(node);
            }
          }
        }).Traverse(documentNode);
    }

    internal IEnumerable<HtmlNode> FindCandidatesForMainContent(HtmlDocument document)
    {
      var paraNodes = document.DocumentNode.GetElementsByTagName("p");
      var candidateNodes = new HashSet<HtmlNode>();

      _nodesScores.Clear();

      foreach (var paraNode in paraNodes)
      {
        string innerText = GetInnerText(paraNode);

        if (innerText.Length < _MinParagraphLength)
        {
          continue;
        }

        var parentNode = paraNode.ParentNode;
        var grandParentNode = parentNode != null ? parentNode.ParentNode : null;
        int score = 1; // 1 point for having a paragraph

        // Add points for any comma-segments within this paragraph.
        score += GetSegmentsCount(innerText, ',');

        // For every PARAGRAPH_SEGMENT_LENGTH characters in this paragraph, add another point. Up to MAX_POINTS_FOR_SEGMENTS_COUNT points.
        score += Math.Min(innerText.Length / _ParagraphSegmentLength, _MaxPointsForSegmentsCount);

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
      /* Now that we have the top candidate, look through its siblings for content that might also be related.
       * Things like preambles, content split by ads that we removed, etc. */

      var articleContentNode = document.CreateElement("div");

      articleContentNode.SetId(_IdReadabilityContentDiv);

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
          _MaxSiblingScoreTreshold,
          _SiblingScoreTresholdCoefficient * topCandidateNodeScore);

      // iterate through the sibling nodes and decide whether append them
      foreach (var siblingNode in siblingNodes)
      {
        bool append = false;
        string siblingNodeName = siblingNode.Name;

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
        else if ("p".Equals(siblingNodeName, StringComparison.OrdinalIgnoreCase))
        {
          // we have to somehow decide whether we should append this paragraph

          string siblingNodeInnerText = GetInnerText(siblingNode);

          // we won't append an empty paragraph
          if (siblingNodeInnerText.Length > 0)
          {
            int siblingNodeInnerTextLength = siblingNodeInnerText.Length;

            if (siblingNodeInnerTextLength >= _MinSiblingParagraphLength)
            {
              // we'll append this paragraph if the links density is not higher than a treshold
              append = GetLinksDensity(siblingNode) < _MaxSiblingParagraphLinksDensity;
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
          HtmlNode nodeToAppend;

          if ("div".Equals(siblingNodeName, StringComparison.OrdinalIgnoreCase)
           || "p".Equals(siblingNodeName, StringComparison.OrdinalIgnoreCase))
          {
            nodeToAppend = siblingNode;
          }
          else
          {
            /* We have a node that isn't a common block level element, like a form or td tag.
             * Turn it into a div so it doesn't get filtered out later by accident. */

            nodeToAppend = document.CreateElement("div");

            nodeToAppend.SetId(siblingNode.GetId());
            nodeToAppend.SetClass(siblingNode.GetClass());
            nodeToAppend.AppendChildren(siblingNode.ChildNodes);
          }

          articleContentNode.AppendChild(nodeToAppend);
        }
      }

      return articleContentNode;
    }

    internal void PrepareArticleContentNode(HtmlNode articleContentNode)
    {
      CleanStyles(articleContentNode);
      KillBreaks(articleContentNode);

      /* Clean out junk from the article content. */
      Clean(articleContentNode, "form");
      Clean(articleContentNode, "object");
      Clean(articleContentNode, "h1");

      /* If there is only one h2, they are probably using it as a header and not a subheader,
       * so remove it since we already have a header. */
      if (articleContentNode.GetElementsByTagName("h2").Count() == 1)
      {
        Clean(articleContentNode, "h2");
      }

      Clean(articleContentNode, "iframe");
      CleanHeaders(articleContentNode);

      /* Do these last as the previous stuff may have removed junk that will affect these. */
      CleanConditionally(articleContentNode, "table");
      CleanConditionally(articleContentNode, "ul");
      CleanConditionally(articleContentNode, "div");

      /* Remove extra paragraphs. */
      var paraNodes = articleContentNode.GetElementsByTagName("p");
      var nodesToRemove = new List<HtmlNode>();

      foreach (var paraNode in paraNodes)
      {
        string innerText = GetInnerText(paraNode, false);
        if (innerText.Length > 0) { continue; }

        int imgsCount = paraNode.GetElementsByTagName("img").Count();
        if (imgsCount > 0) { continue; }

        int embedsCount = paraNode.GetElementsByTagName("embed").Count();
        if (embedsCount > 0) { continue; }

        int objectsCount = paraNode.GetElementsByTagName("object").Count();
        if (objectsCount > 0) { continue; }

        // We have a paragraph with empty inner text, with no images, no embeds and no objects.
        // Let's remove it.
        nodesToRemove.Add(paraNode);
      }

      RemoveNodes(nodesToRemove);

      /* Remove br's that are directly before paragraphs. */
      articleContentNode.InnerHtml = _BreakBeforeParagraphRegex.Replace(articleContentNode.InnerHtml, "<p");
    }

    internal float GetLinksDensity(HtmlNode node)
    {
      string nodeInnerText = GetInnerText(node);
      int nodeInnerTextLength = nodeInnerText.Length;

      if (nodeInnerTextLength == 0)
      {
        // we won't divide by zero
        return 0.0f;
      }

      int linksLength =
        node.GetElementsByTagName("a")
          .Sum(anchorNode => GetInnerText(anchorNode).Length);

      return (float)linksLength / nodeInnerTextLength;
    }

    internal int GetSegmentsCount(string s, char ch)
    {
      return s.Count(c => c == ch) + 1;
    }

    /// <summary>
    /// Get "class/id weight" of the given <paramref name="node" />. Uses regular expressions to tell if this element looks good or bad.
    /// </summary>
    internal int GetClassWeight(HtmlNode node)
    {
      if (_dontWeightClasses)
      {
        return 0;
      }

      int weight = 0;

      /* Look for a special classname. */
      string nodeClass = node.GetClass();

      if (nodeClass.Length > 0)
      {
        if (_NegativeWeightRegex.IsMatch(nodeClass))
        {
          weight -= 25;
        }

        if (_PositiveWeightRegex.IsMatch(nodeClass))
        {
          weight += 25;
        }
      }

      /* Look for a special ID */
      string nodeId = node.GetId();

      if (nodeId.Length > 0)
      {
        if (_NegativeWeightRegex.IsMatch(nodeId))
        {
          weight -= 25;
        }

        if (_PositiveWeightRegex.IsMatch(nodeId))
        {
          weight += 25;
        }
      }

      return weight;
    }

    private HtmlNode ExtractMainContent(HtmlDocument document)
    {
      StripUnlikelyCandidates(document);

      var candidatesForMainContent = FindCandidatesForMainContent(document);

      HtmlNode topCandidateNode = DetermineTopCandidateNode(document, candidatesForMainContent);
      HtmlNode articleContentNode = CreateArticleContentNode(document, topCandidateNode);

      PrepareArticleContentNode(articleContentNode);

      return articleContentNode;
    }

    internal string GetInnerText(HtmlNode node, bool dontNormalizeSpaces)
    {
      string result = node.InnerText ?? "";

      if (!dontNormalizeSpaces)
      {
        return _NormalizeSpacesRegex.Replace(result, " ");
      }

      return result;
    }

    internal string GetInnerText(HtmlNode node)
    {
      return GetInnerText(node, _dontNormalizeSpacesInTextContent);
    }

    /// <summary>
    /// Removes extraneous break tags from a <param name="node" />.
    /// </summary>
    internal void KillBreaks(HtmlNode node)
    {
      node.InnerHtml = _KillBreaksRegex.Replace(node.InnerHtml, "<br />");
    }

    /// <summary>
    /// Cleans a node of all nodes with name <paramref name="nodeName" />.
    /// (Unless it's a youtube/vimeo video. People love movies.)
    /// </summary>
    internal void Clean(HtmlNode rootNode, string nodeName)
    {
      var nodes = rootNode.GetElementsByTagName(nodeName);
      bool isEmbed = "object".Equals(nodeName, StringComparison.OrdinalIgnoreCase)
                  || "embed".Equals(nodeName, StringComparison.OrdinalIgnoreCase);
      var nodesToRemove = new List<HtmlNode>();

      foreach (var node in nodes)
      {
        /* Allow youtube and vimeo videos through as people usually want to see those. */
        if (isEmbed
         && (_VideoRegex.IsMatch(node.GetAttributesString("|"))
          || _VideoRegex.IsMatch(node.InnerHtml)))
        {
          continue;
        }

        nodesToRemove.Add(node);
      }

      RemoveNodes(nodesToRemove);
    }

    /// <summary>
    /// Cleans a <param name="rootNode" /> of all nodes with name <param name="nodeName" /> if they look fishy.
    /// "Fishy" is an algorithm based on content length, classnames, link density, number of images & embeds, etc.
    /// </summary>
    internal void CleanConditionally(HtmlNode rootNode, string nodeName)
    {
      if (nodeName == null)
      {
        throw new ArgumentNullException("nodeName");
      }

      var nodes = rootNode.GetElementsByTagName(nodeName);
      var nodesToRemove = new List<HtmlNode>();

      foreach (var node in nodes)
      {
        int weight = GetClassWeight(node);
        float score = GetNodeScore(node);

        if (weight + score < 0.0f)
        {
          nodesToRemove.Add(node);
          continue;
        }

        /* If there are not very many commas and the number of non-paragraph elements
         * is more than paragraphs or other ominous signs, remove the element. */

        string nodeInnerText = GetInnerText(node);

        if (GetSegmentsCount(nodeInnerText, ',') < _MinCommaSegments)
        {
          int psCount = node.GetElementsByTagName("p").Count();
          int imgsCount = node.GetElementsByTagName("img").Count();
          int lisCount = node.GetElementsByTagName("li").Count();
          int inputsCount = node.GetElementsByTagName("input").Count();
          
          // while counting embeds we omit video-embeds
          int embedsCount =
            node.GetElementsByTagName("embed")
              .Count(embedNode => !_VideoRegex.IsMatch(embedNode.GetAttributeValue("src", "")));

          float linksDensity = GetLinksDensity(node);
          int innerTextLength = nodeInnerText.Length;
          string nodeNameLower = nodeName.ToLower().Trim();
          bool remove = (imgsCount > psCount)
                     || (lisCount - _LisCountTreshold > psCount && nodeNameLower != "ul" && nodeNameLower != "ol")
                     || (inputsCount > psCount / 3)
                     || (innerTextLength < _MinInnerTextLength && (imgsCount == 0 || imgsCount > _MaxImagesInShortSegmentsCount))
                     || (weight < _ClassWeightTreshold && linksDensity > _MaxDensityForNodesWithSmallerClassWeight)
                     || (weight >= _ClassWeightTreshold && linksDensity > _MaxDensityForNodesWithGreaterClassWeight)
                     || (embedsCount > _MaxEmbedsCount || (embedsCount == _MaxEmbedsCount && innerTextLength < _MinInnerTextLengthInNodesWithEmbed));

          if (remove)
          {
            nodesToRemove.Add(node);
          }

        }
      } /* end foreach */

      RemoveNodes(nodesToRemove);
    }

    /// <summary>
    /// Cleans out spurious headers from a <param name="node" />. Checks things like classnames and link density.
    /// </summary>
    internal void CleanHeaders(HtmlNode node)
    {
      var nodesToRemove = new List<HtmlNode>();

      for (int headerLevel = 1; headerLevel < 7; headerLevel++)
      {
        var headerNodes = node.GetElementsByTagName("h" + headerLevel);

        foreach (var headerNode in headerNodes)
        {
          if (GetClassWeight(headerNode) < 0
           || GetLinksDensity(headerNode) > _MaxHeaderLinksDensity)
          {
            nodesToRemove.Add(headerNode);
          }
        }
      }

      RemoveNodes(nodesToRemove);
    }

    /// <summary>
    /// Removes the style attribute from the specified <param name="rootNode" /> and all nodes underneath it.
    /// </summary>
    internal void CleanStyles(HtmlNode rootNode)
    {
      new NodesTraverser(
        node =>
          {
            if (node.NodeType != HtmlNodeType.Element)
            {
              return;
            }

            string nodeStyle = node.GetStyle();

            if (!nodeStyle.Contains(_CssClassReadabilityStyled))
            {
              node.SetStyle(null);
            }
          }).Traverse(rootNode);
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

    private void RemoveNodes(IEnumerable<HtmlNode> nodesToRemove)
    {
      nodesToRemove.ForEach(nodeToRemove => nodeToRemove.Remove());
    }

    #endregion
  }
}
