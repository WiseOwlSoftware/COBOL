// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Diagnostics;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// P6 Step 7a — the ONE scope-aware resolver (<see cref="SymbolTable"/>) that collapses the
/// LookupData/LookupDataInScopeOf/TryGetVisibleIndexField/IndexFieldFor quadruple. The five scope/shadowing cases
/// the phase doc enumerates, mirroring the OoSpineTests method-local-shadowing conformance cases (which remain the
/// authoritative BEHAVIOR net — these pin the resolver's precedence rules in isolation): (a) program-scope
/// resolution; (b) a method-local data-name shadowing an object-level name (§2023:8.4.6.2.1 rule 3a); (c) a
/// method-local data-name shadowing an object-level INDEX-name (IndexCandidates → null); (d) a method-local
/// index-name with its OWN cell shadowing an object index-name (§2023:11.7.4 GR5); (e) an unshadowed object name
/// visible from a method (the LookupDataInScopeOf global fallback).
/// <para>Real items come from binding a small program (the RedefinesClassificationTests harness pattern); the
/// METHOD overlay is a hand-built <see cref="OoMethodDataScope"/> pointed at those items — the resolver consumes
/// scopes as data, so this exercises exactly the precedence logic.</para>
/// </summary>
public sealed class SymbolTableTests
{
    /// <summary>Bind a tiny program so the binder's global maps carry real items:
    /// WS-A (01), TAB with INDEXED BY IX (so the unit declares IX), and WS-SHARED.</summary>
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
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var data = new DataBinder(new EditionContext(2002));
            data.Bind(program);
            return data;
        }
        finally { FileHarness.TryDelete(path); }
    }

    // (a) Program-scope resolution: a global name resolves at Scope.Program; an unknown name does not.
    [Fact]
    public void ProgramScope_ResolvesGlobalName()
    {
        var data = BindFixture();
        Assert.Equal("WS-A", DataNamed(data, "WS-A", Scope.Program).Single?.CobolName);
        Assert.Equal(0, DataNamed(data, "NO-SUCH", Scope.Program).Count);
        Assert.False(data.Symbols.DeclaresDataName("NO-SUCH", Scope.Program));
    }

    // (b) §2023:8.4.6.2.1 rule 3a: a method-local data-name REPLACES (never unions with) the object/program name.
    [Fact]
    public void MethodScope_DataName_ShadowsGlobal()
    {
        var data = BindFixture();
        var methodLocal = DataNamed(data, "WS-A", Scope.Program).Single;
        Assert.NotNull(methodLocal);
        var scope = new OoMethodDataScope();
        // The method declares its OWN WS-SHARED (through the table's one insert) — the method's entry points at a DIFFERENT item (reuse WS-A's
        // item object as a stand-in; the resolver compares nothing but presence).
        data.Symbols.DeclareDataName(methodLocal!, "WS-SHARED", scope);
        var items = DataNamed(data, "WS-SHARED", new Scope(scope));
        Assert.Equal(1, items.Count);                             // the method-local wins — REPLACES, not unions
        Assert.Same(methodLocal, items.Single);
        var global = DataNamed(data, "WS-SHARED", Scope.Program).Single;
        Assert.NotNull(global);
        Assert.NotSame(methodLocal, global);                      // program scope still sees the global item
    }

    /// <summary>The data items a written, unqualified <paramref name="name"/> identifies in <paramref name="scope"/>:
    /// <c>SymbolTable.IdentifyDataName</c>, the table's one data-name lookup (listing step 6b remainder slice 3, kb/Work PB5932).</summary>
    private static NameCandidates<DataItem> DataNamed(DataBinder data, string name, Scope scope) =>
        data.Symbols.IdentifyDataName(new WrittenQualifiedName(new Antlr4.Runtime.CommonToken(1, name), []), scope, report: false);

    /// <summary>The index-name a written <paramref name="name"/> identifies in <paramref name="method"/>'s scope (the program's when
    /// null): <c>SymbolTable.IdentifyIndexName</c>, the table's one index-name lookup (listing step 6b, kb/Work PB5932), with the
    /// method entered as the active scope a reference is made in.</summary>
    private static NameCandidates<IndexDeclaration>? IndexNamed(DataBinder data, string name, OoMethodDataScope? method = null)
    {
        using var _ = data.Procedure.ActiveMethodScope.Enter(method);
        return data.Symbols.IdentifyIndexName(new WrittenQualifiedName(new Antlr4.Runtime.CommonToken(1, name), []), report: false);
    }

    // (c) §2023:8.4.6.2.3: a method-local DATA-name shadows an object-level INDEX-name of the same spelling —
    // the index-name lookup must answer NULL (the data-name wins; binding it to the object's cell would be a torn
    // read/write of the wrong storage).
    [Fact]
    public void MethodScope_DataName_ShadowsIndexName()
    {
        var data = BindFixture();
        var global = IndexNamed(data, "IX")?.Single;   // sanity: IX exists globally
        Assert.NotNull(global);
        var scope = new OoMethodDataScope();
        data.Symbols.DeclareDataName(DataNamed(data, "WS-A", Scope.Program).Single!, "IX", scope);
        Assert.Null(IndexNamed(data, "IX", scope));
    }

    // (d) §2023:11.7.4 GR5 index privacy: a method-local index-name has its OWN cell, never the shared global one.
    [Fact]
    public void MethodScope_IndexName_HasItsOwnCell()
    {
        var data = BindFixture();
        var scope = new OoMethodDataScope();
        var tab = DataNamed(data, "TAB", Scope.Program).Single!;
        data.Symbols.DeclareIndex(new IndexDeclaration("IX", tab, "_IX_42"), scope);
        Assert.Equal("_IX_42", IndexNamed(data, "IX", scope)?.Single?.Cell);   // the method's cell
        var global = IndexNamed(data, "IX")?.Single;
        Assert.NotNull(global);
        Assert.NotEqual("_IX_42", global!.Cell);
    }

    // (e) The LookupDataInScopeOf global fallback: an UNSHADOWED object/program name is visible from a method.
    [Fact]
    public void MethodScope_UnshadowedGlobal_FallsThrough()
    {
        var data = BindFixture();
        var scope = new OoMethodDataScope();                      // empty overlay — nothing shadowed
        Assert.Equal("WS-A", DataNamed(data, "WS-A", new Scope(scope)).Single?.CobolName);
        // And the index-name resolution falls through to the global declaration too.
        var viaMethod = IndexNamed(data, "IX", scope)?.Single;
        Assert.NotNull(viaMethod);
        Assert.Same(IndexNamed(data, "IX")?.Single, viaMethod);
    }
}
