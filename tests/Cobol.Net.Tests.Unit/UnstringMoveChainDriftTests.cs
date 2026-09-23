// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding.Bound;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB979 — UNSTRING's stores ARE moves. ISO §14.9.48.4 GR11 c): the examined characters "shall be treated
/// as an elementary national data item if identifier-1 is of category national, and otherwise as an elementary
/// alphanumeric data item, and shall be moved into the current receiving area according to the rules for the MOVE
/// statement"; GR11 d) says the same of the delimiting characters. The defect was a private per-category copy of
/// the MOVE rules in <c>StringEmitter</c> plus a COMPILE-TIME examination size on the bound node — so ANY LENGTH,
/// reference-modified and dynamic-length receivers were each wrong or refused. This pins the STRUCTURE, so the next
/// receiver shape the MOVE statement learns is UNSTRING's automatically:
/// <list type="bullet">
/// <item>every receiver carries its stores as bound <see cref="BoundMove"/>s (built by <c>MoveBinder.BindMoveOf</c>),
///   and no integer examination size;</item>
/// <item>the UNSTRING emitter renders those moves and spells none of the MOVE conversions itself.</item>
/// </list>
/// </summary>
public sealed class UnstringMoveChainDriftTests
{
    [Fact]
    public void EveryUnstringStore_IsABoundMove_AndTheNodeCarriesNoStaticSize()
    {
        var t = typeof(BoundUnstringReceiver);
        Assert.Equal(typeof(BoundMove), t.GetProperty(nameof(BoundUnstringReceiver.Store))!.PropertyType);
        Assert.Equal(typeof(BoundMove), t.GetProperty(nameof(BoundUnstringReceiver.DelimiterStore))!.PropertyType);
        Assert.Equal(typeof(BoundMove), t.GetProperty(nameof(BoundUnstringReceiver.ZeroFill))!.PropertyType);
        // GR11 b)'s size belongs to the receiver AT EXECUTION (ReceivingStore.ExaminationSize) — an integer on the
        // node is the compile-time answer that examined ONE character of an ANY LENGTH receiver.
        var ints = t.GetProperties().Where(p => p.PropertyType == typeof(int)).Select(p => p.Name).ToList();
        Assert.True(ints.Count == 0, "BoundUnstringReceiver carries a static size again: " + string.Join(", ", ints));
    }

    [Fact]
    public void TheUnstringEmitter_RendersTheBoundMoves_AndNoPrivateMoveRules()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "StringEmitter.cs"));
        int start = src.IndexOf("public void EmitUnstring(", StringComparison.Ordinal);
        Assert.True(start >= 0, "EmitUnstring not found — re-point this drift test at the UNSTRING emitter");
        int end = src.IndexOf("\n    }", start, StringComparison.Ordinal);
        string body = src[start..end];

        // The positive half, so the scan below cannot pass over a body that stores nothing at all.
        Assert.Contains("move.Emit(r.Store)", body);
        Assert.Contains("ReceivingStore.ExaminationSize(", body);

        // The MOVE conversions a private copy would have to spell (MoveEmitter's, and nowhere else's).
        var rx = new Regex(@"RuntimeApi\.(NumFromAlphanumeric|EditFormat\w*|StrStore\w*|NumStore|NumFormatImage)\(|WriteGroupImage\(|PlaceRenderer\.WriteFill\(");
        var offenders = body.Split('\n')
            .Where(l => !l.TrimStart().StartsWith("//") && rx.IsMatch(l))
            .Select(l => l.Trim()).ToList();
        Assert.True(offenders.Count == 0,
            "The UNSTRING emitter spells a MOVE conversion itself instead of rendering the bound MOVE "
            + "(ISO §14.9.48.4 GR11 c)/d); kb/Work PB979):\n" + string.Join("\n", offenders));
        Assert.DoesNotContain("private void MoveString(", src);
    }
}
