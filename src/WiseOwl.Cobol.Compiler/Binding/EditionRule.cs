// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// ⛔ ONE RULE WITH ITS CITATION IN EACH EDITION, passed as DATA to a screen shared by several statements (kb/Work PB4950):
/// the caller names the 1985 rule beside the 2002-and-later one, and the screen renders the edition in force through
/// <see cref="EditionInfo.Cite"/>. A shared screen takes this instead of a pre-rendered <c>string clause</c>, so the next
/// statement that asks the screen supplies its 1985 arm with its 2023 one and the message cannot cite the wrong edition.
/// <para>Both citations are in the edition form of kb/Work PB4887 (<c>§1985:VI.6.6.3 SR1</c>, <c>§2023:14.9.2.3 SR4</c>)
/// and are checked by <c>scripts/spec/audit_code_citations.py</c>.</para>
/// </summary>
internal readonly record struct EditionRule(string In1985, string InLater)
{
    /// <summary>The citation of the edition in force, without the "ISO " lead-in.</summary>
    public string In(EditionInfo edition) => edition.Cite(In1985, InLater);
}
