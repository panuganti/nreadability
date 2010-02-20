using System.Linq;
using NUnit.Framework;
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

    // TODO: remove
    [Test]
    public void TempTest()
    {
      const string content = "<p>Some paragraph.</p>";
      var document = _agilityDomBuilder.BuildDocument(content);

      _readabilityTranscoder.CalculateParagraphsScores(document);
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

    #endregion
  }
}
