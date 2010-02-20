﻿using System;
using System.Linq;
using NUnit.Framework;
using System.Globalization;
using HtmlAgilityPack;
using System.IO;

namespace NReadability.Tests
{
  [TestFixture]
  public class ReadabilityTranscoderTests
  {
    private ReadabilityTranscoder _readabilityTranscoder;

    private static readonly AgilityDomBuilder _agilityDomBuilder;
    private static readonly AgilityDomSerializer _agilityDomSerializer;

    #region Constructor(s)

    static ReadabilityTranscoderTests()
    {
      _agilityDomBuilder = new AgilityDomBuilder();
      _agilityDomSerializer = new AgilityDomSerializer();
    }

    #endregion

    #region SetUp and TearDown

    [SetUp]
    public void SetUp()
    {
      _readabilityTranscoder = new ReadabilityTranscoder();
    }

    #endregion

    #region StripUnlikelyCandidates tests

    [Test]
    public void Unlikely_candidates_should_be_removed()
    {
      const string content = "<div class=\"sidebar\">Some content.</div>";
      var document = _agilityDomBuilder.BuildDocument(content);
      
      _readabilityTranscoder.StripUnlikelyCandidates(document);

      string newContent = _agilityDomSerializer.SerializeDocument(document);

      AssertHtmlContentIsEmpty(newContent);
    }

    [Test]
    public void Unlikely_candidates_which_maybe_are_candidates_should_not_be_removed()
    {
      const string content = "<div id=\"article\" class=\"sidebar\"><a href=\"#\">Some widget</a></div>";
      var document = _agilityDomBuilder.BuildDocument(content);

      _readabilityTranscoder.StripUnlikelyCandidates(document);

      string newContent = _agilityDomSerializer.SerializeDocument(document);

      AssertHtmlContentsAreEqual(content, newContent);
    }

    [Test]
    public void Text_nodes_within_a_div_with_block_elements_should_be_replaced_with_paragraphs()
    {
      const string content = "<div>text node1<a href=\"#\">Link</a>text node2</div>";
      var document = _agilityDomBuilder.BuildDocument(content);

      _readabilityTranscoder.StripUnlikelyCandidates(document);

      Assert.AreEqual(2, document.DocumentNode.DescendantNodes().Count(node => node.Name == "p"));
    }

    #endregion

    #region GetLinksDensity tests

    [Test]
    public void Node_with_no_links_should_have_links_density_equal_to_zero()
    {
      const string content = "<div id=\"container\"></div>";
      var document = _agilityDomBuilder.BuildDocument(content);
      float linksDensity = _readabilityTranscoder.GetLinksDensity(document.GetElementbyId("container"));

      AssertFloatsAreEqual(0.0f, linksDensity);
    }

    [Test]
    public void Node_consisting_of_only_a_link_should_have_links_density_equal_to_one()
    {
      const string content = "<div id=\"container\"><a href=\"#\">some link</a></div>";
      var document = _agilityDomBuilder.BuildDocument(content);
      float linksDensity = _readabilityTranscoder.GetLinksDensity(document.GetElementbyId("container"));

      AssertFloatsAreEqual(1.0f, linksDensity);
    }

    [Test]
    public void Node_containing_a_link_length_of_which_is_half_the_node_length_should_have_links_density_equal_to_half()
    {
      const string content = "<div id=\"container\"><a href=\"#\">some link</a>some link</div>";
      var document = _agilityDomBuilder.BuildDocument(content);
      float linksDensity = _readabilityTranscoder.GetLinksDensity(document.GetElementbyId("container"));

      AssertFloatsAreEqual(0.5f, linksDensity);
    }

    #endregion

    #region DetermineTopCandidateNode tests

    [Test]
    public void Top_candidate_node_should_be_possible_to_determine_even_if_body_is_not_present()
    {
      const string content = "";
      var document = _agilityDomBuilder.BuildDocument(content);

      var candidatesForArticleContent = _readabilityTranscoder.FindCandidatesForArticleContent(document);

      Assert.AreEqual(0, candidatesForArticleContent.Count());

      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForArticleContent);

      Assert.IsNotNull(topCandidateNode);
    }

    [Test]
    public void DetermineTopCandidateNode_should_fallback_to_body_if_there_are_no_candidates()
    {
      const string content = "<body><p>Some paragraph.</p><p>Some paragraph.</p>some text</body>";
      var document = _agilityDomBuilder.BuildDocument(content);

      var candidatesForArticleContent = _readabilityTranscoder.FindCandidatesForArticleContent(document);
      
      Assert.AreEqual(0, candidatesForArticleContent.Count());

      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForArticleContent);

      Assert.IsNotNull(topCandidateNode);
      Assert.AreEqual(3, topCandidateNode.ChildNodes.Count);
      Assert.AreEqual("p", topCandidateNode.ChildNodes.First().Name);
      Assert.AreEqual("p", topCandidateNode.ChildNodes.Skip(1).First().Name);
      Assert.AreEqual(HtmlNodeType.Text, topCandidateNode.ChildNodes.Skip(2).First().NodeType);
    }

    [Test]
    public void DetermineTopCandidateNode_should_choose_a_container_with_longer_paragraph()
    {
      const string content = "<body><div id=\"first-div\"><p>Praesent in arcu vitae erat sodales consequat. Nam tellus purus, volutpat ac elementum tempus, sagittis sed lacus. Sed lacus ligula, sodales id vehicula at, semper a turpis. Curabitur et augue odio, sed auctor massa. Ut odio massa, fringilla eu elementum sit amet, eleifend congue erat. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ultrices turpis dignissim metus porta id iaculis purus facilisis. Curabitur auctor purus eu nulla venenatis non ultrices nibh venenatis. Aenean dapibus pellentesque felis, ac malesuada nibh fringilla malesuada. In non mi vitae ipsum vehicula adipiscing. Sed a velit ipsum. Sed at velit magna, in euismod neque. Proin feugiat diam at lectus dapibus sed malesuada orci malesuada. Mauris sit amet orci tortor. Sed mollis, turpis in cursus elementum, sapien ante semper leo, nec venenatis velit sapien id elit. Praesent vel nulla mauris, nec tincidunt ipsum. Nulla at augue vestibulum est elementum sodales.</p></div><div id=\"second-div\"><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin lacus ipsum, blandit sit amet cursus ut, posuere quis velit. Vivamus ut lectus quam, venenatis posuere erat. Sed pellentesque suscipit rhoncus. Vestibulum dictum est ut elit molestie vel facilisis dui tincidunt. Nulla adipiscing metus in nulla condimentum non mattis lacus tempus. Phasellus sed ipsum in felis molestie molestie. Sed sagittis massa orci, ut sagittis sem. Cras eget feugiat nulla. Nunc lacus turpis, porttitor eget congue quis, accumsan sed nunc. Vivamus imperdiet luctus molestie. Suspendisse eu est sed ligula pretium blandit. Proin eget metus nisl, at convallis metus. In commodo nibh a arcu pellentesque iaculis. Cras tincidunt vehicula malesuada. Duis tellus mi, ultrices sit amet dapibus sit amet, semper ac elit. Cras lobortis, urna eget consectetur consectetur, enim velit tempus neque, et tincidunt risus quam id mi. Morbi sit amet odio magna, vitae tempus sem. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Curabitur at lectus sit amet augue tincidunt ornare sed vitae lorem. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.</p></div></body>";
      var document = _agilityDomBuilder.BuildDocument(content);
      var candidatesForArticleContent = _readabilityTranscoder.FindCandidatesForArticleContent(document);

      Assert.AreEqual(3, candidatesForArticleContent.Count());

      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForArticleContent);

      Assert.IsNotNull(topCandidateNode);
      Assert.AreEqual("second-div", topCandidateNode.GetId());
    }

    #endregion

    #region CreateArticleContent tests

    [Test]
    public void CreateArticleContent_should_work_even_if_html_content_is_empty()
    {
      const string content = "";
      var document = _agilityDomBuilder.BuildDocument(content);
      var candidatesForArticleContent = _readabilityTranscoder.FindCandidatesForArticleContent(document);
      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForArticleContent);

      Assert.IsNotNull(topCandidateNode);

      var articleContentNode = _readabilityTranscoder.CreateArticleContentNode(document, topCandidateNode);

      Assert.IsNotNull(articleContentNode);
      Assert.AreEqual("div", articleContentNode.Name);
      Assert.IsNotNullOrEmpty(articleContentNode.GetId());
      Assert.AreEqual(0, articleContentNode.ChildNodes.Count);
    }

    [Test]
    public void CreateArticleContent_should_extract_a_paragraph()
    {
      const string content = "<div id=\"first-div\"><p>Praesent in arcu vitae erat sodales consequat. Nam tellus purus, volutpat ac elementum tempus, sagittis sed lacus. Sed lacus ligula, sodales id vehicula at, semper a turpis. Curabitur et augue odio, sed auctor massa. Ut odio massa, fringilla eu elementum sit amet, eleifend congue erat. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ultrices turpis dignissim metus porta id iaculis purus facilisis. Curabitur auctor purus eu nulla venenatis non ultrices nibh venenatis. Aenean dapibus pellentesque felis, ac malesuada nibh fringilla malesuada. In non mi vitae ipsum vehicula adipiscing. Sed a velit ipsum. Sed at velit magna, in euismod neque. Proin feugiat diam at lectus dapibus sed malesuada orci malesuada. Mauris sit amet orci tortor. Sed mollis, turpis in cursus elementum, sapien ante semper leo, nec venenatis velit sapien id elit. Praesent vel nulla mauris, nec tincidunt ipsum. Nulla at augue vestibulum est elementum sodales.</p></div><div id=\"\">some text</div>";
      var document = _agilityDomBuilder.BuildDocument(content);
      var candidatesForArticleContent = _readabilityTranscoder.FindCandidatesForArticleContent(document);
      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForArticleContent);

      Assert.IsNotNull(topCandidateNode);

      var articleContentNode = _readabilityTranscoder.CreateArticleContentNode(document, topCandidateNode);

      Assert.IsNotNull(articleContentNode);
      Assert.AreEqual("div", articleContentNode.Name);
      Assert.AreEqual(1, articleContentNode.ChildNodes.Count);
      Assert.AreEqual("first-div", articleContentNode.ChildNodes.First().GetId());
      Assert.AreEqual(1, articleContentNode.ChildNodes.First().ChildNodes.Count);
      Assert.AreEqual("p", articleContentNode.ChildNodes.First().ChildNodes.First().Name);
    }

    #endregion

    #region PrepareDocument tets

    [Test]
    public void PrepareDocument_should_create_body_tag_if_it_is_not_present()
    {
      const string content = "";
      var document = _agilityDomBuilder.BuildDocument(content);

      Assert.IsNull(document.GetBody());

      _readabilityTranscoder.PrepareDocument(document);

      Assert.IsNotNull(document.GetBody());
    }

    [Test]
    public void PrepareDocument_should_remove_scripts_and_stylesheets()
    {
      const string content = "<html><head><link rel=\"StyleSheet\" href=\"#\" /><style></style><style /><style type=\"text/css\"></style></head><body><script type=\"text/javascript\"></script><script type=\"text/javascript\" /><style type=\"text/css\"></style><link rel=\"styleSheet\"></link><script></script></body></html>";
      var document = _agilityDomBuilder.BuildDocument(content);

      Assert.Greater(CountTags(document, "script", "style", "link"), 0);

      _readabilityTranscoder.PrepareDocument(document);

      Assert.AreEqual(0, CountTags(document, "script", "style", "link"));
    }

    [Test]
    public void PrepareDocument_should_not_remove_neither_readability_scripts_nor_stylesheets()
    {
      const string content = "<html><head><link rel=\"stylesheet\" href=\"http://domain.com/readability.css\" /><script src=\"http://domain.com/readability.js\"></script></head><body><script src=\"http://domain.com/readability.js\"></script><link rel=\"stylesheet\" href=\"http://domain.com/readability.css\" /></body></html>";
      var document = _agilityDomBuilder.BuildDocument(content);

      int countBefore = CountTags(document, "script", "link");

      _readabilityTranscoder.PrepareDocument(document);

      int countAfter = CountTags(document, "script", "link");

      Assert.AreEqual(countBefore, countAfter);
    }

    [Test]
    public void PrepareDocument_should_replace_double_br_tags_with_p_tags()
    {
      const string content = "<html><body>some text<br /><br />some other text</body></html>";
      var document = _agilityDomBuilder.BuildDocument(content);

      Assert.AreEqual(0, CountTags(document, "p"));
      Assert.Greater(CountTags(document, "br"), 0);

      _readabilityTranscoder.PrepareDocument(document);

      Assert.AreEqual(0, CountTags(document, "br"));
      Assert.AreEqual(1, CountTags(document, "p"));
    }

    [Test]
    public void PrepareDocument_should_replace_font_tags_with_span_tags()
    {
      const string content = "<html><body><font>some text</font></body></html>";
      var document = _agilityDomBuilder.BuildDocument(content);

      Assert.AreEqual(0, CountTags(document, "span"));
      Assert.Greater(CountTags(document, "font"), 0);

      _readabilityTranscoder.PrepareDocument(document);

      Assert.AreEqual(0, CountTags(document, "font"));
      Assert.AreEqual(1, CountTags(document, "span"));
    }

    #endregion

    #region GlueDocument tests

    [Test]
    public void GlueDocument_should_include_head_node_if_it_is_not_present()
    {
      const string content = "";
      var document = _agilityDomBuilder.BuildDocument(content);

      Assert.AreEqual(0, CountTags(document, "head"));

      _readabilityTranscoder.GlueDocument(document, null, document.GetBody());

      Assert.AreEqual(1, CountTags(document, "head"));
    }

    [Test]
    public void GlueDocument_should_include_readability_stylesheet()
    {
      const string content = "";
      var document = _agilityDomBuilder.BuildDocument(content);

      Assert.AreEqual(0, CountTags(document, "style"));

      _readabilityTranscoder.GlueDocument(document, null, document.GetBody());

      Assert.AreEqual(1, CountTags(document, "style"));
    }

    [Test]
    public void GlueDocument_should_create_appropriate_containers_structure()
    {
      const string content = "";
      var document = _agilityDomBuilder.BuildDocument(content);

      _readabilityTranscoder.GlueDocument(document, null, document.GetBody());

      Assert.IsNotNull(document.GetElementbyId(ReadabilityTranscoder.OverlayDivId));
      Assert.IsNotNull(document.GetElementbyId(ReadabilityTranscoder.InnerDivId));
    }

    #endregion

    #region GetUserStyleClass tests

    [Test]
    public void TestGetUserStyleClass()
    {
      Assert.AreEqual("prefix", _readabilityTranscoder.GetUserStyleClass("prefix", ""));
      Assert.AreEqual("prefix-abc", _readabilityTranscoder.GetUserStyleClass("prefix", "abc"));
      Assert.AreEqual("prefix-abc", _readabilityTranscoder.GetUserStyleClass("prefix", "Abc"));
      Assert.AreEqual("prefix-a-bc", _readabilityTranscoder.GetUserStyleClass("prefix", "ABc"));
      Assert.AreEqual("prefix-a-bc-d", _readabilityTranscoder.GetUserStyleClass("prefix", "ABcD"));
    }

    #endregion

    #region Transcode tests

    [Test]
    [Sequential]
    public void TestSampleInputs([Values(1, 2)]int sampleInputNumber)
    {
      string sampleInputNumberStr = sampleInputNumber.ToString().PadLeft(2, '0');
      string content = File.ReadAllText(string.Format(@"SampleInput\SampleInput_{0}.html", sampleInputNumberStr));
      string transcodedContent = _readabilityTranscoder.Transcode(content);

      switch (sampleInputNumber)
      {
        case 1: // washingtonpost.com - "Court Puts Off Decision On Indefinite Detention"
          Assert.IsTrue(transcodedContent.Contains("The Supreme Court yesterday vacated a lower"));
          Assert.IsTrue(transcodedContent.Contains("The justices did not rule on the merits"));
          Assert.IsTrue(transcodedContent.Contains("But the government said the issues were now"));
          break;

        case 2: // devBlogi.pl - "Po co nam testerzy?"
          Assert.IsTrue(transcodedContent.Contains("Moja siostra sprawiła swoim dzieciom szczeniaczka"));
          Assert.IsTrue(transcodedContent.Contains("Z tresowaniem psów jest tak, że reakcja musi być"));
          Assert.IsTrue(transcodedContent.Contains("Korzystając z okazji, chcielibyśmy dowiedzieć się"));
          break;

        default:
          throw new NotSupportedException("Unknown sample input number (" + sampleInputNumber + "). Have you added another sample input? If so, then add appropriate asserts here as well.");
      }

      const string outputDir = "SampleOutput";

      if (!Directory.Exists(Path.GetDirectoryName(outputDir)))
      {
        Directory.CreateDirectory(outputDir);
      }

      File.WriteAllText(Path.Combine(outputDir, string.Format("SampleOutput_{0}.html", sampleInputNumberStr)), transcodedContent);
    }

    #endregion

    #region Private helper methods

    private static void AssertHtmlContentIsEmpty(string content)
    {
      if (content != null)
      {
        content = content.Trim();
      }

      Assert.IsNullOrEmpty(content);
    }

    private static void AssertHtmlContentsAreEqual(string expectedContent, string actualContent)
    {
      string serializedExpectedContent =
        _agilityDomSerializer.SerializeDocument(
          _agilityDomBuilder.BuildDocument(expectedContent));
      
      string serializedActualContent =
        _agilityDomSerializer.SerializeDocument(
          _agilityDomBuilder.BuildDocument(actualContent));

      Assert.AreEqual(serializedExpectedContent, serializedActualContent);
    }

    private static void AssertFloatsAreEqual(float expected, float actual)
    {
      Assert.IsTrue(
        (actual - expected).IsCloseToZero(),
        string.Format(
          CultureInfo.InvariantCulture.NumberFormat,
          "Expected {0:F} but was {1:F}.",
          expected,
          actual));
    }

    private static int CountTags(HtmlDocument document, params string[] args)
    {
      int count = 0;

      new NodesTraverser(
        node =>
          {
            string nodeName = (node.Name ?? "").Trim().ToLower();

            if (args.Any(nodeToSearch => nodeToSearch.Trim().ToLower() == nodeName))
            {
              count++;
            }
          }).Traverse(document.DocumentNode);

      return count;
    }

    #endregion
  }
}