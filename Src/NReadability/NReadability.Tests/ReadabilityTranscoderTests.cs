using System;
using System.Linq;
using NUnit.Framework;
using System.Globalization;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
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

      var candidatesForMainContent = _readabilityTranscoder.FindCandidatesForMainContent(document);

      Assert.AreEqual(0, candidatesForMainContent.Count());

      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForMainContent);

      Assert.IsNotNull(topCandidateNode);
    }

    [Test]
    public void DetermineTopCandidateNode_should_fallback_to_body_if_there_are_no_candidates()
    {
      const string content = "<body><p>Some paragraph.</p><p>Some paragraph.</p>some text</body>";
      var document = _agilityDomBuilder.BuildDocument(content);

      var candidatesForMainContent = _readabilityTranscoder.FindCandidatesForMainContent(document);
      
      Assert.AreEqual(0, candidatesForMainContent.Count());

      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForMainContent);

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
      var candidatesForMainContent = _readabilityTranscoder.FindCandidatesForMainContent(document);

      Assert.AreEqual(3, candidatesForMainContent.Count());

      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForMainContent);

      Assert.IsNotNull(topCandidateNode);
      Assert.AreEqual("second-div", topCandidateNode.GetId());
    }

    #endregion

    #region CreateArticleContent tests

    [Test]
    public void Test()
    {
      const string content = "";
      var document = _agilityDomBuilder.BuildDocument(content);
      var candidatesForMainContent = _readabilityTranscoder.FindCandidatesForMainContent(document);
      var topCandidateNode = _readabilityTranscoder.DetermineTopCandidateNode(document, candidatesForMainContent);

      Assert.IsNotNull(topCandidateNode);

      var articleContentNode = _readabilityTranscoder.CreateArticleContentNode(document, topCandidateNode);

      Assert.IsNotNull(articleContentNode);

      // TODO:
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

    #endregion
  }
}
