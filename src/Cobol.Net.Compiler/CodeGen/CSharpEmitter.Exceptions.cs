// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen;

/// <summary>The EC slice moved to <see cref="EcEmitter"/> (P7 Step 9k) — this partial keeps only the
/// bind-session TURN state the OO bind half reads, plus the migration shims the not-yet-extracted
/// collaborators reach the EC services through (the 9n composition root retargets them).</summary>
public sealed partial class CSharpEmitter
{
    private TurnState _turnState = TurnState.Empty;

    internal string EcDispatchExpr(string ecNameExpr, string fileExpr) => _ecEmit.EcDispatchExpr(ecNameExpr, fileExpr);
    internal string EcObjDispatchExpr(string objExpr) => _ecEmit.ObjDispatchExpr(objExpr);
    internal (string Stmt, string Loc) EcStmtLoc(EcStatementInfo info) => _ecEmit.EcStmtLoc(info);
    internal List<string> EcEnabledSizeNames() => _ecEmit.EnabledSizeNames();
    internal void EcEmitSizeHandling(string flag, string ecnVar, List<string> enabled, bool hasPhrase)
        => _ecEmit.EmitSizeHandling(flag, ecnVar, enabled, hasPhrase);
    internal void EcEmitOverflow(string ovfFlag, string ecName, bool hasPhrase) => _ecEmit.EmitOverflow(ovfFlag, ecName, hasPhrase);
    internal int EcIoMaskFor(FileModel file) => _ecEmit.IoMaskFor(file);
    internal void EcEmitIoCheckEc(BoundProgram bound, CodeWriter w) => _ecEmit.EmitIoCheckEc(bound, w);
}
