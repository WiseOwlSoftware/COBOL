// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The five statements whose FROM or INTO operand may not be the file's own record area (kb/Work PB5077): the column
/// of <c>FromPhraseRules</c> and <c>IntoPhraseRules</c> that <c>RecordAreaRule</c> (the rule's wording and edition scope, in
/// <c>Binding.Validation</c>) reads.</summary>
public enum RecordAreaVerb { Read, Return, Write, Rewrite, Release }
