﻿#region CodeMaid is Copyright 2007-2011 Steve Cadwallader.

// CodeMaid is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License version 3
// as published by the Free Software Foundation.
//
// CodeMaid is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details <http://www.gnu.org/licenses/>.

#endregion CodeMaid is Copyright 2007-2011 Steve Cadwallader.

using EnvDTE;

namespace SteveCadwallader.CodeMaid.CodeItems
{
    /// <summary>
    /// The representation of a code using statemen.
    /// </summary>
    public class CodeItemUsingStatement : BaseCodeItemElement
    {
        /// <summary>
        /// Gets the kind.
        /// </summary>
        public override KindCodeItem Kind
        {
            get { return KindCodeItem.Using; }
        }

        /// <summary>
        /// Gets the access level.
        /// </summary>
        public override vsCMAccess Access
        {
            get { return vsCMAccess.vsCMAccessDefault; }
        }
    }
}