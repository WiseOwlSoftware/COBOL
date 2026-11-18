// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// A grammar file's text as a test that reads its STRUCTURE needs it: the file with every name-slot label removed.
/// <para>Every place the COBOL grammar lets the programmer write a name carries an ANTLR element label whose prefix
/// classifies it (<c>refDataReference1=dataReference</c>, <c>nnBoolean1=BOOLEAN</c>; docs/rearchitecture/DESIGN-listing.md
/// section 6.7.1, held by <c>GrammarNameSlotDriftTests</c>). A label changes no parse, but a test that reads a rule's
/// shape from the text (an alternative <c>| BOOLEAN</c>, a phrase <c>IN cobolWord</c>) would read the label as part of
/// it. <see cref="Read"/> and <see cref="WithoutLabels"/> give such a test the grammar it means; <see cref="Label"/> is
/// the one statement of what a name-slot label is, which the counter of <c>GrammarNameSlotDriftTests</c> reads too.</para>
/// </summary>
public static partial class GrammarSource
{
    /// <summary>A name-slot label: a classifying prefix (<c>ref</c>, <c>qual</c>, <c>def</c>, <c>nn</c>, <c>name</c>),
    /// a capitalized name and an ordinal, so no two elements of one rule share one.</summary>
    public const string LabelPattern = "(?:ref|qual|def|nn|name)[A-Z][A-Za-z]*[0-9]+";

    /// <summary>A name-slot label in grammar text, with the <c>=</c> that binds it to its element.</summary>
    [GeneratedRegex(@"\b" + LabelPattern + @"=(?=[A-Za-z_(])", RegexOptions.CultureInvariant)]
    private static partial Regex Labelled();

    /// <summary>A generated context field that is a name-slot label.</summary>
    [GeneratedRegex("^" + LabelPattern + "$", RegexOptions.CultureInvariant)]
    public static partial Regex Label();

    /// <summary>The grammar file at <paramref name="path"/>, every name-slot label removed.</summary>
    public static string Read(string path) => WithoutLabels(File.ReadAllText(path));

    /// <summary><paramref name="g4"/> with every name-slot label removed; nothing else changes, so line numbers hold.</summary>
    public static string WithoutLabels(string g4) => Labelled().Replace(g4, string.Empty);
}
