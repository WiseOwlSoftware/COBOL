// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// ⛔ THE DATA FILES THE DOCUMENTS ARE CHECKED AGAINST (<c>tests/doc-facts/</c>, kb/Work PB4453). No compiler test opens a
/// Markdown file: a number, a name list or an example the Markdown states and a test needs is a JSON file here, and
/// <c>scripts/doc_audits.py</c> holds the Markdown to it. <c>compiler-facts.json</c> is GENERATED from the compiler by
/// <c>DocumentedFactsDriftTests</c>; the other files are the tests' own data.
/// </summary>
internal static class DocFacts
{
    /// <summary>A file of <c>tests/doc-facts/</c>, parsed.</summary>
    internal static JsonElement Load(string file) =>
        JsonDocument.Parse(File.ReadAllText(TestRepo.At("tests", "doc-facts", file))).RootElement;

    private static readonly Lazy<JsonElement> Compiler = new(() => Load("compiler-facts.json"));

    /// <summary>A numeric fact of <c>compiler-facts.json</c>.</summary>
    internal static long Number(string name) => Compiler.Value.GetProperty(name).GetInt64();
}
