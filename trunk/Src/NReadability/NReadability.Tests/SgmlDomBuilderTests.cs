using NUnit.Framework;

namespace NReadability.Tests
{
  [TestFixture]
  public class SgmlDomBuilderTests
  {
    private SgmlDomBuilder _sgmlDomBuilder;

    #region SetUp and TearDown

    [SetUp]
    public void SetUp()
    {
      _sgmlDomBuilder = new SgmlDomBuilder();
    }

    #endregion

    #region Tests

    [Test]
    public void Test_BuildDom_on_content_with_html_entities()
    {
      const string htmlContent = "<html><head></head><body>&raquo;</body></html>";
      var document = _sgmlDomBuilder.BuildDocument(htmlContent);

      Assert.IsTrue(document.ToString().Contains("»"));
    }

    #endregion
  }
}
