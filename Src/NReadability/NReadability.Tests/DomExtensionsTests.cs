using System;
using NUnit.Framework;
using HtmlAgilityPack;

namespace NReadability.Tests
{
  [TestFixture]
  public class DomExtensionsTests
  {
    #region GetAttributesString Tests

    [Test]
    public void GetAttributesString_should_throw_exception_if_separator_is_null()
    {
      var document = new HtmlDocument();
      var node = document.CreateElement("div");

      Assert.Throws(typeof(ArgumentNullException), () => node.GetAttributesString(null));
    }

    [Test]
    public void GetAttributesString_should_return_empty_string_if_node_has_no_attributes()
    {
      var document = new HtmlDocument();
      var node = document.CreateElement("div");

      Assert.AreEqual("", node.GetAttributesString("|"));
    }

    [Test]
    public void GetAttributesString_should_return_a_string_with_a_single_attribute_if_node_has_only_one_attribute()
    {
      var document = new HtmlDocument();
      var node = document.CreateElement("div");
      const string attributeValue = "container";

      node.Attributes.Add("id", attributeValue);

      Assert.AreEqual(attributeValue, node.GetAttributesString("|"));
    }

    [Test]
    public void GetAttributesString_should_return_a_string_with_separated_attributes_if_node_has_more_than_one_attribute()
    {
      var document = new HtmlDocument();
      var node = document.CreateElement("div");
      const string attributeValue1 = "container";
      const string attributeValue2 = "widget";
      const string separator = "|";

      node.Attributes.Add("id", attributeValue1);
      node.Attributes.Add("class", attributeValue2);

      Assert.AreEqual(attributeValue1 + separator + attributeValue2, node.GetAttributesString(separator));
    }

    #endregion
  }
}
