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

using System;
using HtmlAgilityPack;

namespace NReadability
{
  internal class ChildNodesTraverser
  {
    private readonly Action<HtmlNode> _childNodeVisitor;

    #region Constructor(s)

    public ChildNodesTraverser(Action<HtmlNode> childNodeVisitor)
    {
      if (childNodeVisitor == null)
      {
        throw new ArgumentNullException("childNodeVisitor");
      }

      _childNodeVisitor = childNodeVisitor;
    }

    #endregion

    #region Public methods

    public void Traverse(HtmlNode node)
    {
      HtmlNode childNode = node.FirstChild;

      while (childNode != null)
      {
        HtmlNode nextChildNode = childNode.NextSibling;

        _childNodeVisitor(childNode);

        childNode = nextChildNode;
      }
    }

    #endregion
  }
}
