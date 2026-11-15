// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A SEND'S DEVICE BARRIER IS ONE WRITE-THROUGH WRITE, NEVER A FLUSH PER STATEMENT (kb/Work PB5147;
/// DESIGN-communication-facility.md §6 "Devices", step 2). Each statement that transmits must make its device bytes durable
/// before page 2 lands, and it did so with <c>RandomAccess.FlushToDisk</c> (<c>FlushFileBuffers</c>), which asks the volume to
/// flush its whole write cache: on GitHub's Windows runners that call's median ran from 4 ms to 830 ms between merge groups,
/// CM102M paid it 544 times, and the test stalled past its budget. The device is opened <c>FileOptions.WriteThrough</c>
/// instead, whose write is durable with its size change when it returns; a flush is made only when the statement created the
/// device (an empty file's name is metadata a write-through write is not documented to carry). This test keeps that shape: under
/// <c>Runtime/Communication</c>, every <c>RandomAccess.FlushToDisk</c> sits under a test of the device's creation, and every
/// handle a <c>RandomAccess.Write</c> writes through is opened write-through.
/// </summary>
public sealed class McsDeviceBarrierDriftTests
{
    private static IEnumerable<(string Path, CompilationUnitSyntax Root)> CommunicationSources()
    {
        string dir = TestRepo.Src("WiseOwl.Cobol.Runtime", "Communication");
        return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(f => (Path.GetRelativePath(dir, f).Replace('\\', '/'),
                (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(File.ReadAllText(f)).GetRoot()));
    }

    private static IEnumerable<(string Path, InvocationExpressionSyntax Call)> RandomAccessCalls(string verb) =>
        CommunicationSources().SelectMany(s => s.Root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(c => c.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: "RandomAccess" }, Name.Identifier.Text: var v,
            } && v == verb)
            .Select(c => (s.Path, c)));

    private static string Where(string path, Microsoft.CodeAnalysis.SyntaxNode node) =>
        $"{path}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {node}";

    [Fact]
    public void EveryFlushInTheMcs_IsADevicesCreation()
    {
        var flushes = RandomAccessCalls("FlushToDisk").ToList();
        Assert.True(flushes.Count >= 1, "The sweep found no RandomAccess.FlushToDisk under Runtime/Communication: the device's "
            + "creation flush moved, and this test no longer measures anything.");
        var unguarded = flushes
            .Where(f => !f.Call.Ancestors().OfType<IfStatementSyntax>().Any(i => i.Condition.ToString() == "!existed"))
            .Select(f => Where(f.Path, f.Call)).ToList();
        Assert.True(unguarded.Count == 0, "A flush per statement in the MCS (kb/Work PB5147): a device's barrier is its "
            + "write-through write, and only a statement that created the device flushes it:\n  " + string.Join("\n  ", unguarded));
    }

    [Fact]
    public void EveryDeviceWrite_IsThroughAWriteThroughHandle()
    {
        var writes = RandomAccessCalls("Write").ToList();
        Assert.True(writes.Count >= 1, "The sweep found no RandomAccess.Write under Runtime/Communication: the device's write "
            + "moved, and this test no longer measures anything.");
        var cached = writes.Where(w =>
        {
            var handle = w.Call.ArgumentList.Arguments[0].Expression.ToString();
            var opened = w.Call.Ancestors().OfType<UsingStatementSyntax>().FirstOrDefault(u =>
                u.Declaration?.Variables.Any(v => v.Identifier.Text == handle) == true);
            return opened is null || !opened.Declaration!.ToString().Contains("FileOptions.WriteThrough", StringComparison.Ordinal);
        }).Select(w => Where(w.Path, w.Call)).ToList();
        Assert.True(cached.Count == 0, "A device write through a handle not opened FileOptions.WriteThrough (kb/Work PB5147): "
            + "with no flush per statement, its bytes would not be durable before page 2 lands:\n  " + string.Join("\n  ", cached));
    }
}
