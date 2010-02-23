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
using System.Xml.Linq;

namespace NReadability
{
  internal class NodesTraverser
  {
    private readonly Action<XNode> _nodeVisitor;

    #region Constructor(s)

    public NodesTraverser(Action<XNode> nodeVisitor)
    {
      if (nodeVisitor == null)
      {
        throw new ArgumentNullException("nodeVisitor");
      }

      _nodeVisitor = nodeVisitor;
    }

    #endregion

    #region Public methods

    public void Traverse(XNode node)
    {
      _nodeVisitor(node);

      if (!(node is XContainer))
      {
        return;
      }

      var childNode = ((XContainer)node).FirstNode;

      while (childNode != null)
      {
        var nextChildNode = childNode.NextNode;
        
        Traverse(childNode);

        childNode = nextChildNode;
      }
    }

    #endregion
  }
}
