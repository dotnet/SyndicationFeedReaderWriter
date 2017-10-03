// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SyndicationFeed.Atom
{
    static class AtomAttributeExtentions
    {
        public static string GetAtom(this IEnumerable<ISyndicationAttribute> attributes, string name)
        {
            return attributes.FirstOrDefault(a => a.IsAtom(name))?.Value;
        }

        public static bool IsAtom(this ISyndicationAttribute attr, string name)
        {
            return attr.Name == name && (attr.Namespace == null || attr.Namespace == string.Empty || attr.Namespace == AtomConstants.Atom10Namespace);
        }
    }

    static class AtomContentExtentions
    {
        public static bool IsAtom(this ISyndicationContent content, string name)
        {
            return content.Name == name && (content.Namespace == null || content.Namespace == AtomConstants.Atom10Namespace);
        }

        public static bool IsAtom(this ISyndicationContent content)
        {
            return (content.Namespace == null || content.Namespace == AtomConstants.Atom10Namespace);
        }
    }
}
