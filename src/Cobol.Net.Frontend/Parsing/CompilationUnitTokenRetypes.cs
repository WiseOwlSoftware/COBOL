// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Parsing;

namespace CobolNet.Frontend.Generated;

public partial class CobolParserCore
{
    public partial class CompilationUnitContext
    {
        /// <summary>The post-lex token decisions this tree was parsed under (<see cref="Parsing.TokenRetypes"/>:
        /// the <c>&gt;&gt;COBOL-WORDS</c> map and the §8.9 words the token-level gate freed, kb/Work PB655). Every
        /// re-parse of this tree's source text — the binder's subscript and argument fragments — applies the SAME
        /// value, so a fragment reads each word exactly as the tree does. Set by <c>Frontend.LexAndParse</c>.</summary>
        public TokenRetypes TokenRetypes { get; set; } = TokenRetypes.None;
    }
}
