﻿/*
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

using System.Text;
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
    public string SerializeDocument(XDocument document, bool prettyPrint)
    {
      return document.ToString(prettyPrint ? SaveOptions.None : SaveOptions.DisableFormatting);
    }

    /// <summary>
    /// TODO: comment
    /// </summary>
    public string SerializeDocument(XDocument document)
    {
      return SerializeDocument(document, false);
    }

    #endregion
  }
}
