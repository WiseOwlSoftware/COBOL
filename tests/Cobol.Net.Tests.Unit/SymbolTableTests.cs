// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// P6 Step 7a — the ONE scope-aware resolver (<see cref="SymbolTable"/>) that collapses the
/// LookupData/LookupDataInScopeOf/TryGetVisibleIndexField/IndexFieldFor quadruple. The five scope/shadowing cases
/// the phase doc enumerates, mirroring the OoSpineTests method-local-shadowing conformance cases (which remain the
/// authoritative BEHAVIOR net — these pin the resolver's precedence rules in isolation): (a) program-scope
/// resolution; (b) a method-local data-name shadowing an object-level name (§8.4.6.2.1 rule 3a); (c) a
/// method-local data-name shadowing an object-level INDEX-name (TryResolveIndex → false); (d) a method-local
/// index-name with its OWN cell shadowing an object index-name (§11.7.4 GR5); (e) an unshadowed object name
/// visible from a method (the LookupDataInScopeOf global fallback).
/// <para>Real items come from binding a small program (the RedefinesClassificationTests harness pattern); the
/// METHOD overlay is a hand-built <see cref="OoMethodDataScope"/> pointed at those items — the resolver consumes
/// scopes as data, so this exercises exactly the precedence logic.</para>
/// </summary>
public sealed class SymbolTableTests
{
    /// <summary>Bind a tiny program so the binder's global maps carry real items:
    /// WS-A (01), TAB with INDEXED BY IX (so IndexFields["IX"] exists), and WS-SHARED.</summary>
    private static DataBinder BindFixture()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMFIX.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(4).
            01 WS-SHARED PIC X(3).
            01 TAB-GRP.
               05 TAB PIC 9(2) OCCURS 5 TIMES INDEXED BY IX.
            PROCEDURE DIVISION.
            MAIN.
                SET IX TO 1.
                DISPLAY TAB (IX).
                STOP RUN.
            """;
        string path = Path.Combine(Path.GetTempPath(), "cn_sym_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var data = new DataBinder(new EditionContext(2002));
            data.Bind(program);
            return data;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    // (a) Program-scope resolution: a global name resolves at Scope.Program; an unknown name does not.
    [Fact]
    public void ProgramScope_ResolvesGlobalName()
    {
        var data = BindFixture();
        Assert.True(data.Symbols.TryResolve("WS-A", Scope.Program, out var items));
        Assert.Equal("WS-A", items[0].CobolName);
        Assert.False(data.Symbols.TryResolve("NO-SUCH", Scope.Program, out _));
    }

    // (b) §8.4.6.2.1 rule 3a: a method-local data-name REPLACES (never unions with) the object/program name.
    [Fact]
    public void MethodScope_DataName_ShadowsGlobal()
    {
        var data = BindFixture();
        var methodLocal = data.Symbols.TryResolve("WS-A", Scope.Program, out var g) ? g[0] : null;
        Assert.NotNull(methodLocal);
        var scope = new OoMethodDataScope();
        // The method declares its OWN WS-SHARED — the overlay entry points at a DIFFERENT item (reuse WS-A's
        // item object as a stand-in; the resolver compares nothing but presence).
        scope.ByName["WS-SHARED"] = [methodLocal!];
        Assert.True(data.Symbols.TryResolve("WS-SHARED", new Scope(scope), out var items));
        Assert.Same(methodLocal, items[0]);                       // the method-local wins — REPLACES, not unions
        Assert.Single(items);
        Assert.True(data.Symbols.TryResolve("WS-SHARED", Scope.Program, out var globals));
        Assert.NotSame(methodLocal, globals[0]);                  // program scope still sees the global item
    }

    // (c) §8.4.6.2.3: a method-local DATA-name shadows an object-level INDEX-name of the same spelling —
    // TryResolveIndex must return FALSE (the data-name wins; binding it to the object's cell would be a torn
    // read/write of the wrong storage).
    [Fact]
    public void MethodScope_DataName_ShadowsIndexName()
    {
        var data = BindFixture();
        Assert.True(data.Symbols.TryResolveIndex("IX", Scope.Program, out var globalCell));   // sanity: IX exists globally
        var scope = new OoMethodDataScope();
        scope.ByName["IX"] = [data.Symbols.TryResolve("WS-A", Scope.Program, out var g) ? g[0] : null!];
        Assert.False(data.Symbols.TryResolveIndex("IX", new Scope(scope), out _));
        Assert.NotEmpty(globalCell);
    }

    // (d) §11.7.4 GR5 index privacy: a method-local index-name has its OWN cell, never the shared global one.
    [Fact]
    public void MethodScope_IndexName_HasItsOwnCell()
    {
        var data = BindFixture();
        var scope = new OoMethodDataScope();
        scope.IndexFields["IX"] = "_MIX_42";
        Assert.True(data.Symbols.TryResolveIndex("IX", new Scope(scope), out var cell));
        Assert.Equal("_MIX_42", cell);                            // the method's cell, not the global _IX_*
        Assert.Equal("_MIX_42", data.Symbols.IndexCellOf("IX", new Scope(scope)));
        Assert.True(data.Symbols.TryResolveIndex("IX", Scope.Program, out var globalCell));
        Assert.NotEqual("_MIX_42", globalCell);
    }

    // (e) The LookupDataInScopeOf global fallback: an UNSHADOWED object/program name is visible from a method.
    [Fact]
    public void MethodScope_UnshadowedGlobal_FallsThrough()
    {
        var data = BindFixture();
        var scope = new OoMethodDataScope();                      // empty overlay — nothing shadowed
        Assert.True(data.Symbols.TryResolve("WS-A", new Scope(scope), out var items));
        Assert.Equal("WS-A", items[0].CobolName);
        // And the resolved-cell accessor falls through to the global cell too.
        Assert.True(data.Symbols.TryResolveIndex("IX", new Scope(scope), out var cell));
        Assert.Equal(data.Symbols.IndexCellOf("IX", Scope.Program), cell);
    }
}
