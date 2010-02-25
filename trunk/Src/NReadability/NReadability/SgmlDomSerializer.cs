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
using System.Linq;
using System.Xml.Linq;

namespace NReadability
{
  /// <summary>
  /// TODO: comment
  /// </summary>
  public class SgmlDomSerializer
  {
    #region Public methods

    /// <summary>
    /// TODO: comment
    /// </summary>
    public string SerializeDocument(XDocument document, bool prettyPrint, bool dontIncludeMetaContentTypeElement, bool dontIncludeDocType)
    {
      if (!dontIncludeMetaContentTypeElement)
      {
        var documentRoot = document.Root;

        if (documentRoot == null)
        {
          throw new ArgumentException("The document must have a root.");
        }

        if (documentRoot.Name == null || !"html".Equals(documentRoot.Name.LocalName, StringComparison.OrdinalIgnoreCase))
        {
          throw new ArgumentException("The document's root must be an html element.");
        }

        var headElement = documentRoot.GetChildrenByTagName("head").FirstOrDefault();

        if (headElement == null)
        {
          headElement = new XElement("head");
          documentRoot.Add(headElement);
        }

        var metaContentTypeElement =
          (from metaElement in headElement.GetChildrenByTagName("meta")
           where "content-type".Equals(metaElement.GetAttributeValue("http-equiv", ""), StringComparison.OrdinalIgnoreCase)
           select metaElement).FirstOrDefault();

        if (metaContentTypeElement != null)
        {
          metaContentTypeElement.Remove();
        }

        metaContentTypeElement =
          new XElement(
            XName.Get("meta", headElement.Name != null ? (headElement.Name.NamespaceName ?? "") : ""),
            new XAttribute("http-equiv", "Content-Type"),
            new XAttribute("content", "text/html; charset=utf-8"));

        headElement.AddFirst(metaContentTypeElement);
      }

      string result = document.ToString(prettyPrint ? SaveOptions.None : SaveOptions.DisableFormatting);

      if (!dontIncludeDocType)
      {
        result = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\"\r\n\"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n" + result;
      }

      return result;
    }

    /// <summary>
    /// TODO: comment
    /// </summary>
    public string SerializeDocument(XDocument document)
    {
      return SerializeDocument(document, false, false, false);
    }

    #endregion
  }
}
