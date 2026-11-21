// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ THE COMPILER RESERVES AT 85 EXACTLY THE WORDS OF THE 1985 RESERVED-WORD LIST (§1985:IV.8 p1, "The following is a list
/// of COBOL reserved words for the seven required modules and the five optional modules"; kb/Work PB5453).
/// <para>The list is the 1985 catalog's own text, rows 1985-SR-IV.8-T1 and 1985-SR-IV.8-T2 (its two tables, read from
/// <c>docs/rearchitecture/spec-rule-catalog-1985.json</c>), never a copy: every word of them must be a row of the
/// compiler's reserved-word table that is reserved at 85 with high confidence (the only rows that may REJECT a
/// user-defined word, <see cref="ReservedWordEntry.Confidence"/>), and every word that table reserves at 85 must be a
/// word of the list, so a word added to or dropped from either side is red here. The operator symbols the second table
/// prints (+ - * / ** &gt; &lt; = &gt;= &lt;=) are separators, not words, and are not compared. That a reserved word is
/// refused as a user-defined word at 85 is the binder's (ReservedWordPositionConformanceTests).</para>
/// </summary>
public sealed class ReservedWords1985TablesDriftTests
{
    private static readonly string[] TableRows = ["1985-SR-IV.8-T1", "1985-SR-IV.8-T2"];

    private static SortedSet<string> ListedWords()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.Docs("rearchitecture", "spec-rule-catalog-1985.json")));
        var texts = doc.RootElement.GetProperty("rules").EnumerateArray()
            .Where(r => TableRows.Contains(r.GetProperty("id").GetString()))
            .ToDictionary(r => r.GetProperty("id").GetString()!, r => r.GetProperty("text").GetString()!);
        Assert.True(TableRows.All(texts.ContainsKey),
            $"the 1985 catalog lacks a reserved-word table row: has {string.Join(", ", texts.Keys)}");
        // A word of the list has a letter; the HTML-escaped relational symbols (&gt; &lt;) and the arithmetic ones do not
        // once their entity is read as the one character it names.
        return new SortedSet<string>(texts.Values
            .SelectMany(t => t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Select(w => System.Net.WebUtility.HtmlDecode(w) ?? w)
            .Where(w => w.Any(char.IsLetter)), StringComparer.Ordinal);
    }

    [Fact]
    public void EveryWordOfThe1985List_IsReservedAt85_AndNoOtherWordIs()
    {
        var listed = ListedWords();
        Assert.True(listed.Count > 300, $"only {listed.Count} words read from the 1985 tables: the catalog text changed shape");
        var notReserved = listed.Where(w => ReservedWords.Find(w) is not { Confidence: "high" } e || !e.IsReservedAt(85)).ToList();
        var notListed = ReservedWords.Entries.Where(e => e.IsReservedAt(85) && !listed.Contains(e.Word))
            .Select(e => e.Word).ToList();
        Assert.True(notReserved.Count == 0 && notListed.Count == 0,
            $"the 1985 list (§1985:IV.8) and the compiler's reservations at 85 differ: listed but not reserved (with high "
            + $"confidence) [{string.Join(", ", notReserved)}]; reserved at 85 but not listed [{string.Join(", ", notListed)}]");
    }
}
