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
