/*
 * NReadability
 * http://code.google.com/p/nreadability/
 * 
 * Copyright 2010 Marek Stój
 * http://immortal.pl/
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace NReadability
{
  /// <summary>
  /// TODO:
  /// </summary>
  public class NReadabilityTranscoder
  {
    #region Fields

    #region Resources constants

    private static readonly string _ReadabilityStylesheetResourceName = typeof(NReadabilityTranscoder).Namespace + ".Resources.readability.css";

    #endregion

    #region Algorithm constants

    /// <summary>
    /// TODO:
    /// </summary>
    public const ReadingStyle DefaultReadingStyle = ReadingStyle.Newspaper;

    /// <summary>
    /// TODO:
    /// </summary>
    public const ReadingMargin DefaultReadingMargin = ReadingMargin.Wide;

    /// <summary>
    /// TODO:
    /// </summary>
    public const ReadingSize DefaultReadingSize = ReadingSize.Medium;

    internal const string OverlayDivId = "readOverlay";
    internal const string InnerDivId = "readInner";
    internal const string ContentDivId = "readability-content";
    internal const string ReadabilityStyledCssClass = "readability-styled";

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
    private const int _MaxArticleTitleLength = 150;
    private const int _MinArticleTitleLength = 15;
    private const int _MinArticleTitleWordsCount1 = 3;
    private const int _MinArticleTitleWordsCount2 = 4;

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
    private static readonly Regex _ReplaceDoubleBrsRegex = new Regex("(<br[^>]*>[ \\n\\r\\t]*){2,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _ReplaceFontsRegex = new Regex("<(\\/?)font[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _ArticleTitleDashRegex1 = new Regex(" [\\|\\-] ", RegexOptions.Compiled);
    private static readonly Regex _ArticleTitleDashRegex2 = new Regex("(.*)[\\|\\-] .*", RegexOptions.Compiled);
    private static readonly Regex _ArticleTitleDashRegex3 = new Regex("[^\\|\\-]*[\\|\\-](.*)", RegexOptions.Compiled);
    private static readonly Regex _ArticleTitleColonRegex1 = new Regex(".*:(.*)", RegexOptions.Compiled);
    private static readonly Regex _ArticleTitleColonRegex2 = new Regex("[^:]*[:](.*)", RegexOptions.Compiled);

    #endregion

    #region Algorithm parameters

    private readonly bool _dontStripUnlikelys;
    private readonly bool _dontNormalizeSpacesInTextContent;
    private readonly bool _dontWeightClasses;
    private readonly ReadingStyle _readingStyle;
    private readonly ReadingSize _readingSize;
    private readonly ReadingMargin _readingMargin;

    #endregion

    #region Helper instance fields

    private readonly SgmlDomBuilder _agilityDomBuilder;
    private readonly SgmlDomSerializer _agilityDomSerializer;
    private readonly Dictionary<XNode, float> _nodesScores;

    #endregion

    #endregion

    #region Constructor(s)

    /// <summary>
    /// TODO:
    /// </summary>
    // TODO: should those first 3 flags be in a public constructor?
    public NReadabilityTranscoder(
      bool dontStripUnlikelys,
      bool dontNormalizeSpacesInTextContent,
      bool dontWeightClasses,
      ReadingStyle readingStyle,
      ReadingMargin readingMargin,
      ReadingSize readingSize)
    {
      _dontStripUnlikelys = dontStripUnlikelys;
      _dontNormalizeSpacesInTextContent = dontNormalizeSpacesInTextContent;
      _dontWeightClasses = dontWeightClasses;
      _readingStyle = readingStyle;
      _readingMargin = readingMargin;
      _readingSize = readingSize;

      _agilityDomBuilder = new SgmlDomBuilder();
      _agilityDomSerializer = new SgmlDomSerializer();
      _nodesScores = new Dictionary<XNode, float>();
    }

    /// <summary>
    /// TODO:
    /// </summary>
    public NReadabilityTranscoder(ReadingStyle readingStyle, ReadingMargin readingMargin, ReadingSize readingSize)
      : this(false, false, false, readingStyle, readingMargin, readingSize)
    {
    }

    /// <summary>
    /// TODO:
    /// </summary>
    public NReadabilityTranscoder()
      : this(DefaultReadingStyle, DefaultReadingMargin, DefaultReadingSize)
    {
    }

    #endregion

    #region Public methods

    /// <summary>
    /// TODO:
    /// </summary>
    public string Transcode(string htmlContent)
    {
      var document = _agilityDomBuilder.BuildDocument(htmlContent);

      PrepareDocument(document);

      var articleTitleNode = ExtractArticleTitle(document);
      var articleContentNode = ExtractArticleContent(document);

      GlueDocument(document, articleTitleNode, articleContentNode);

      // TODO: implement a fallback behaviour - rerun one more time with _dontStripUnlikelys and then with _dontWeightClasses

      return _agilityDomSerializer.SerializeDocument(document);
    }

    #endregion

    #region Readability algorithm

    internal void PrepareDocument(XDocument document)
    {
      /* In some cases a body element can't be found (if the HTML is totally hosed for example),
       * so we create a new body node and append it to the document. */
      var documentBody = GetOrCreateBody(document);
      var rootNode = document.Root;

      // TODO: handle HTML frames

      var elementsToRemove = new List<XElement>();

      /* Remove all scripts that are not readability. */
      elementsToRemove.Clear();

      rootNode.GetElementsByTagName("script")
        .ForEach(scriptNode =>
                   {
                     string scriptSrc = scriptNode.GetAttributeValue("src", null);

                     if (string.IsNullOrEmpty(scriptSrc) || scriptSrc.LastIndexOf("readability") == -1)
                     {
                       elementsToRemove.Add(scriptNode);
                     }
                   });

      RemoveElements(elementsToRemove);

      /* Remove all external stylesheets. */
      elementsToRemove.Clear();
      elementsToRemove.AddRange(
        rootNode.GetElementsByTagName("link")
          .Where(node => node.GetAttributeValue("rel", "").Trim().ToLower() == "stylesheet"
                      && node.GetAttributeValue("href", "").LastIndexOf("readability") == -1));
      RemoveElements(elementsToRemove);

      /* Remove all style tags. */
      elementsToRemove.Clear();
      elementsToRemove.AddRange(rootNode.GetElementsByTagName("style"));
      RemoveElements(elementsToRemove);

      /* Turn all double br's into p's and all font's into span's. */
      // TODO: optimize?
      string bodyInnerHtml = documentBody.GetInnerHtml();

      bodyInnerHtml = _ReplaceDoubleBrsRegex.Replace(bodyInnerHtml, "<p></p>");
      bodyInnerHtml = _ReplaceFontsRegex.Replace(bodyInnerHtml, "<$1span>");

      documentBody.SetInnerHtml(bodyInnerHtml);
    }

    internal XNode ExtractArticleTitle(XDocument document)
    {
      var documentBody = GetOrCreateBody(document);
      string documentTitle = document.GetTitle() ?? "";
      string currentTitle = documentTitle;

      if (_ArticleTitleDashRegex1.IsMatch(currentTitle))
      {
        currentTitle = _ArticleTitleDashRegex2.Replace(documentTitle, "$1");

        if (currentTitle.Split(' ').Length < _MinArticleTitleWordsCount1)
        {
          currentTitle = _ArticleTitleDashRegex3.Replace(documentTitle, "$1");
        }
      }
      else if (currentTitle.IndexOf(": ") != -1)
      {
        currentTitle = _ArticleTitleColonRegex1.Replace(documentTitle, "$1");

        if (currentTitle.Split(' ').Length < _MinArticleTitleWordsCount1)
        {
          currentTitle = _ArticleTitleColonRegex2.Replace(documentTitle, "$1");
        }
      }
      else if (currentTitle.Length > _MaxArticleTitleLength || currentTitle.Length < _MinArticleTitleLength)
      {
        var levelOneHeaders = documentBody.GetElementsByTagName("h1");

        if (levelOneHeaders.Count() == 1)
        {
          currentTitle = GetInnerText(levelOneHeaders.First());
        }
      }

      currentTitle = (currentTitle ?? "").Trim();

      if (currentTitle.Split(' ').Length <= _MinArticleTitleWordsCount2)
      {
        currentTitle = documentTitle;
      }

      var articleTitleNode = new XElement("h1");

      articleTitleNode.SetInnerHtml(currentTitle);

      return articleTitleNode;
    }

    internal XNode ExtractArticleContent(XDocument document)
    {
      StripUnlikelyCandidates(document);

      var candidatesForArticleContent = FindCandidatesForArticleContent(document);

      XElement topCandidateNode = DetermineTopCandidateNode(document, candidatesForArticleContent);
      XElement articleContentNode = CreateArticleContentNode(document, topCandidateNode);

      PrepareArticleContentNode(articleContentNode);

      return articleContentNode;
    }

    internal void GlueDocument(XDocument document, XNode articleTitleNode, XNode articleContentNode)
    {
      var documentBody = GetOrCreateBody(document);

      /* Include readability.css stylesheet. */
      var headNode = document.GetElementsByTagName("head").FirstOrDefault();

      if (headNode == null)
      {
        headNode = new XElement("head");
        // TODO: test this
        documentBody.AddBeforeSelf(headNode);
      }

      var styleNode = new XElement("style");

      styleNode.SetAttributeValue("type", "text/css");

      var readabilityStylesheetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_ReadabilityStylesheetResourceName);

      if (readabilityStylesheetStream == null)
      {
        throw new InternalErrorException("Couldn't load the NReadability stylesheet embedded resource.");
      }

      using (var sr = new StreamReader(readabilityStylesheetStream))
      {
        styleNode.SetInnerHtml(sr.ReadToEnd());
      }

      headNode.Add(styleNode);

      /* Apply reading style to body. */
      string readingStyleClass = GetReadingStyleClass(_readingStyle);

      documentBody.SetClass(readingStyleClass);
      documentBody.SetStyle("display: block;");

      /* Create inner div. */
      var innerDiv = new XElement("div");

      innerDiv.SetId(InnerDivId);
      innerDiv.SetClass(GetReadingMarginClass(_readingMargin) + " " + GetReadingSizeClass(_readingSize));

      if (articleTitleNode != null)
      {
        innerDiv.Add(articleTitleNode);
      }

      if (articleContentNode != null)
      {
        innerDiv.Add(articleContentNode);
      }

      /* Create overlay div. */
      var overlayDiv = new XElement("div");

      overlayDiv.SetId(OverlayDivId);
      overlayDiv.SetClass(readingStyleClass);
      overlayDiv.Add(innerDiv);

      /* Clear the old HTML, insert the new content. */
      documentBody.RemoveAll();
      documentBody.Add(overlayDiv);
    }

    internal void StripUnlikelyCandidates(XDocument document)
    {
      if (_dontStripUnlikelys)
      {
        return;
      }

      var documentNode = document;

      new NodesTraverser(
        node =>
        {
          // TODO: maybe traverser should traverse only elements?
          var element = node as XElement;

          if (element == null)
          {
            return;
          }

          string elementName = element.Name != null ? (element.Name.LocalName ?? "") : "";

          /* Remove unlikely candidates. */
          string unlikelyMatchString = element.GetClass() + element.GetId();

          if (unlikelyMatchString.Length > 0
           && !"body".Equals(elementName, StringComparison.OrdinalIgnoreCase)
           && _UnlikelyCandidatesRegex.IsMatch(unlikelyMatchString)
           && !_OkMaybeItsACandidateRegex.IsMatch(unlikelyMatchString))
          {
            var parentElement = node.Parent;

            if (parentElement != null)
            {
              element.Remove();
            }

            // node has been removed - we can go to the next one
            return;
          }

          /* Turn all divs that don't have children block level elements into p's or replace text nodes within the div with p's. */
          if ("div".Equals(elementName, StringComparison.OrdinalIgnoreCase))
          {
            if (!_DivToPElementsRegex.IsMatch(element.GetInnerHtml()))
            {
              // no block elements inside - change to p
              element.Name = "p";
            }
            else
            {
              // replace text nodes with p's (experimental)
              new ChildNodesTraverser(
                childNode =>
                  {
                    if (childNode.NodeType != XmlNodeType.Text
                     || GetInnerText(childNode).Length == 0)
                    {
                      return;
                    }

                    var paraElement = new XElement("p");

                    // NOTE: we're not using GetInnerText() here; instead we're getting raw InnerText to preserver whitespaces
                    paraElement.SetInnerHtml(((XText)childNode).Value);

                    paraElement.SetClass(ReadabilityStyledCssClass);
                    paraElement.SetStyle("display: inline;");

                    childNode.ReplaceWith(paraElement);
                  }
                ).Traverse(node);
            }
          }
        }).Traverse(documentNode);
    }

    internal IEnumerable<XElement> FindCandidatesForArticleContent(XDocument document)
    {
      var paraNodes = document.GetElementsByTagName("p");
      var candidateNodes = new HashSet<XElement>();

      _nodesScores.Clear();

      foreach (var paraNode in paraNodes)
      {
        string innerText = GetInnerText(paraNode);

        if (innerText.Length < _MinParagraphLength)
        {
          continue;
        }

        var parentNode = paraNode.Parent;
        var grandParentNode = parentNode != null ? parentNode.Parent : null;
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

    internal XElement DetermineTopCandidateNode(XDocument document, IEnumerable<XElement> candidatesForArticleContent)
    {
      XElement topCandidateNode = null;

      foreach (XElement candidateNode in candidatesForArticleContent)
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
        || "body".Equals(topCandidateNode.Name != null ? topCandidateNode.Name.LocalName : null, StringComparison.OrdinalIgnoreCase))
      {
        topCandidateNode = new XElement("div");

        var documentBody = GetOrCreateBody(document);

        // TODO: search for all references to Descendants(), DescendantNodes() and Elements()
        topCandidateNode.Add(documentBody.Nodes());
      }

      return topCandidateNode;
    }

    internal XElement CreateArticleContentNode(XDocument document, XNode topCandidateNode)
    {
      /* Now that we have the top candidate, look through its siblings for content that might also be related.
       * Things like preambles, content split by ads that we removed, etc. */

      var articleContentNode = new XElement("div");

      articleContentNode.SetId(ContentDivId);

      var parentNode = topCandidateNode.Parent;

      if (parentNode == null)
      {
        // shouldn't happen (unless the found top candidate node has no parent for some reason)
        return articleContentNode;
      }

      IEnumerable<XNode> siblingNodes = parentNode.Nodes();

      float topCandidateNodeScore = GetNodeScore(topCandidateNode);
      float siblingScoreThreshold =
        Math.Max(
          _MaxSiblingScoreTreshold,
          _SiblingScoreTresholdCoefficient * topCandidateNodeScore);

      // iterate through the sibling nodes and decide whether append them
      foreach (var siblingNode in siblingNodes)
      {
        bool append = false;
        // TODO: only XElement?
        var siblingElement = siblingNode as XElement; 
        string siblingNodeName = siblingElement != null && siblingElement.Name != null ? (siblingElement.Name.LocalName ?? "") : "";

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
          XNode nodeToAppend;

          if ("div".Equals(siblingNodeName, StringComparison.OrdinalIgnoreCase)
           || "p".Equals(siblingNodeName, StringComparison.OrdinalIgnoreCase))
          {
            nodeToAppend = siblingNode;
          }
          else
          {
            /* We have a node that isn't a common block level element, like a form or td tag.
             * Turn it into a div so it doesn't get filtered out later by accident. */

            var elementToAppend = new XElement("div");

            nodeToAppend = elementToAppend;

            if (siblingElement != null)
            {
              elementToAppend.SetId(siblingElement.GetId());
              elementToAppend.SetClass(siblingElement.GetClass());
              elementToAppend.Add(siblingElement.Nodes());
            }
            else
            {
              // TODO: test this
              elementToAppend.Add(siblingNode);
            }
          }

          articleContentNode.Add(nodeToAppend);
        }
      }

      return articleContentNode;
    }

    internal void PrepareArticleContentNode(XElement articleContentNode)
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
      var nodesToRemove = new List<XElement>();

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

      RemoveElements(nodesToRemove);

      /* Remove br's that are directly before paragraphs. */
      articleContentNode.SetInnerHtml(_BreakBeforeParagraphRegex.Replace(articleContentNode.GetInnerHtml(), "<p"));
    }

    internal float GetLinksDensity(XNode node)
    {
      var element = node as XElement;

      if (element == null)
      {
        return 0.0f;
      }

      string nodeInnerText = GetInnerText(element);
      int nodeInnerTextLength = nodeInnerText.Length;

      if (nodeInnerTextLength == 0)
      {
        // we won't divide by zero
        return 0.0f;
      }

      int linksLength =
        element.GetElementsByTagName("a")
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
    internal int GetClassWeight(XElement element)
    {
      if (_dontWeightClasses)
      {
        return 0;
      }

      int weight = 0;

      /* Look for a special classname. */
      string nodeClass = element.GetClass();

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
      string nodeId = element.GetId();

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

    internal string GetInnerText(XNode node, bool dontNormalizeSpaces)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      string result;

      if (node is XElement)
      {
        result = ((XElement)node).Value;
      }
      else if (node is XText)
      {
        result = ((XText)node).Value;
      }
      else
      {
        throw new NotSupportedException(string.Format("Nodes of type '{0}' are not supported.", node.GetType()));
      }

      result = (result ?? "").Trim();

      if (!dontNormalizeSpaces)
      {
        return _NormalizeSpacesRegex.Replace(result, " ");
      }

      return result;
    }

    internal string GetInnerText(XNode node)
    {
      return GetInnerText(node, _dontNormalizeSpacesInTextContent);
    }

    /// <summary>
    /// Removes extraneous break tags from a <param name="node" />.
    /// </summary>
    internal void KillBreaks(XElement node)
    {
      node.SetInnerHtml(_KillBreaksRegex.Replace(node.GetInnerHtml(), "<br />"));
    }

    /// <summary>
    /// Cleans a node of all nodes with name <paramref name="nodeName" />.
    /// (Unless it's a youtube/vimeo video. People love movies.)
    /// </summary>
    internal void Clean(XElement rootNode, string nodeName)
    {
      var nodes = rootNode.GetElementsByTagName(nodeName);
      bool isEmbed = "object".Equals(nodeName, StringComparison.OrdinalIgnoreCase)
                  || "embed".Equals(nodeName, StringComparison.OrdinalIgnoreCase);
      var nodesToRemove = new List<XElement>();

      foreach (var node in nodes)
      {
        /* Allow youtube and vimeo videos through as people usually want to see those. */
        if (isEmbed
         && (_VideoRegex.IsMatch(node.GetAttributesString("|"))
          || _VideoRegex.IsMatch(node.GetInnerHtml())))
        {
          continue;
        }

        nodesToRemove.Add(node);
      }

      RemoveElements(nodesToRemove);
    }

    /// <summary>
    /// Cleans a <param name="rootNode" /> of all nodes with name <param name="nodeName" /> if they look fishy.
    /// "Fishy" is an algorithm based on content length, classnames, link density, number of images and embeds, etc.
    /// </summary>
    internal void CleanConditionally(XElement rootNode, string nodeName)
    {
      if (nodeName == null)
      {
        throw new ArgumentNullException("nodeName");
      }

      var nodes = rootNode.GetElementsByTagName(nodeName);
      var nodesToRemove = new List<XElement>();

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
          string nodeNameLower = nodeName.Trim().ToLower();
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

      RemoveElements(nodesToRemove);
    }

    /// <summary>
    /// Cleans out spurious headers from a <param name="node" />. Checks things like classnames and link density.
    /// </summary>
    internal void CleanHeaders(XElement element)
    {
      var nodesToRemove = new List<XElement>();

      for (int headerLevel = 1; headerLevel < 7; headerLevel++)
      {
        var headerNodes = element.GetElementsByTagName("h" + headerLevel);

        foreach (var headerNode in headerNodes)
        {
          if (GetClassWeight(headerNode) < 0
           || GetLinksDensity(headerNode) > _MaxHeaderLinksDensity)
          {
            nodesToRemove.Add(headerNode);
          }
        }
      }

      RemoveElements(nodesToRemove);
    }

    /// <summary>
    /// Removes the style attribute from the specified <param name="rootNode" /> and all nodes underneath it.
    /// </summary>
    internal void CleanStyles(XNode rootNode)
    {
      new NodesTraverser(
        node =>
        {
          var element = node as XElement;

          if (element == null)
          {
            return;
          }

          string nodeClass = element.GetClass();

          if (nodeClass.Contains(ReadabilityStyledCssClass))
          {
            // don't remove the style if that's we who have styled this node
            return;
          }

          element.SetStyle(null);
        }).Traverse(rootNode);
    }

    internal string GetUserStyleClass(string prefix, string enumStr)
    {
      var suffixSB = new StringBuilder();
      bool wasUpperCaseCharacterSeen = false;

      enumStr.Aggregate(
        suffixSB,
        (sb, ch) =>
        {
          if (Char.IsUpper(ch))
          {
            if (wasUpperCaseCharacterSeen)
            {
              sb.Append('-');
            }

            wasUpperCaseCharacterSeen = true;

            sb.Append(Char.ToLower(ch));
          }
          else
          {
            sb.Append(ch);
          }

          return sb;
        });

      return string.Format("{0}-{1}", prefix, suffixSB).TrimEnd('-');
    }

    #endregion

    #region Private helper methods

    private XElement GetOrCreateBody(XDocument document)
    {
      var documentBody = document.GetBody();

      if (documentBody == null)
      {
        var htmlNode = document.GetChildrenByTagName("html").FirstOrDefault();

        if (htmlNode == null)
        {
          htmlNode = new XElement("html");
          document.Add(htmlNode);
        }

        documentBody = new XElement("body");
        htmlNode.Add(documentBody);
      }

      return documentBody;
    }

    private string GetReadingStyleClass(ReadingStyle readingStyle)
    {
      return GetUserStyleClass("style", readingStyle.ToString());
    }

    private string GetReadingMarginClass(ReadingMargin readingMargin)
    {
      return GetUserStyleClass("margin", readingMargin.ToString());
    }

    private string GetReadingSizeClass(ReadingSize readingSize)
    {
      return GetUserStyleClass("size", readingSize.ToString());
    }

    private void AddPointsToNodeScore(XNode node, int pointsToAdd)
    {
      float currentScore = _nodesScores.ContainsKey(node) ? _nodesScores[node] : 0.0f;

      _nodesScores[node] = currentScore + pointsToAdd;
    }

    private float GetNodeScore(XNode node)
    {
      return _nodesScores.ContainsKey(node) ? _nodesScores[node] : 0.0f;
    }

    private void SetNodeScore(XNode node, float score)
    {
      _nodesScores[node] = score;
    }

    private void RemoveElements(IEnumerable<XElement> elementsToRemove)
    {
      elementsToRemove.ForEach(nodeToRemove => nodeToRemove.Remove());
    }

    #endregion
  }
}
