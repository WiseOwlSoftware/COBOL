// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE ONE RENDERER OF THE FORMAT-1 USE SELECTION TIERS — ISO/IEC 1989:2023 §14.9.49.4 GR3 a)/GR5 (the
/// FILE-NAME scope) followed by GR3 b)/GR6 b)–e) (the OPEN-MODE scope, "for any file open in the input mode or
/// in the process of being opened in the input mode" and its three siblings).
/// <para><b>Why it exists.</b> That switch pair was written THREE times — <c>DispatchEmitter.__IoCheck</c>,
/// <c>EcEmitter.__IoCheckEc</c> and <c>ProgramEmitter.__RunGlobalUse</c> — differing only in the per-case
/// action text (<c>return X;</c> / <c>__sel = X; break;</c> / <c>X; return true;</c>) and in the precondition
/// carried into the mode tier. Three copies of one rule is this repo's most reproducible defect shape. The
/// callers now hand in their action text; the RULE is written once.</para>
/// <para>⛔ <b>BOTH TIERS ARE EDITION-INVARIANT, and that is a DETERMINATION, not an omission</b> (kb/Work
/// PB344). Annex E.2 item 19 a)/b) records that an unphrased invalid key condition, and a READ exception that
/// is neither an invalid key nor an at end condition, did not reach an open-mode declarative before this
/// edition. It is INFORMATIVE, it speaks of exactly one prior edition (§E.1, "a list of the substantive changes
/// between the previous COBOL standard and this Working Draft International Standard" — the Introduction names
/// that standard as the 2014 one), and its own justification classes the prior state as defective TEXT rather
/// than as a required behaviour: "The previous COBOL Standard was not clear or missing processing of some I-O
/// exceptions. This change clarifies or corrects that processing", each sub-item adding "This appears to be an
/// error in previous standards." A silence is not a prohibition. For the 1985 edition the behaviour is
/// positively fixed the OTHER way by that edition's own validation suite: NIST CCVS <c>SQ122A</c>,
/// <c>SQ136A</c>, <c>SQ137A</c>, <c>SQ138A</c> and <c>SQ148A</c> each require the open-mode declarative to run
/// for a READ that ends '46' or '47' — <c>SQ137A</c> fails with the literal remark "INPUT DECLARATIVE NOT
/// EXECUTED", citing ANSI X3.23-1985 VII-2 1.3.5 / VII-51 4.6.4(5). Gating it therefore had no basis at 1985 or
/// 2002 and was refuted at 1985; the 2023 rule stands at every edition. (Annex E.2 item 22, the INDEXED
/// after-OPEN READ PREVIOUS change, is the opposite case and IS gated —
/// <see cref="DialectBehavior.IndexedReadPreviousAfterOpenAtEnd"/> — because there the annex states what the
/// prior RULE said, not that the prior text was missing.)</para>
/// </summary>
internal static class UseTierEmitter
{
    /// <summary>Emit the GR3 a)/GR5 file-name tier and then the GR3 b)/GR6 b)–e) open-mode tier.
    /// <paramref name="action"/> renders the body of one <c>case</c> for declarative <c>i</c> — the ONE thing
    /// the three call sites disagree about. <paramref name="modeTierWhen"/> is the caller's own precondition on
    /// the mode tier (null = unconditional; it is never an EDITION condition — see the type remarks);
    /// <paramref name="modeTierNote"/> is the trailing generated-source comment that explains it.
    /// <paramref name="globalOnly"/> selects only <c>USE … GLOBAL</c>
    /// declaratives (§14.9.49.4 GR4 b)'s outward walk examines the container's GLOBAL ones alone).</summary>
    public static void EmitScopeTiers(CodeWriter w, IReadOnlyList<BoundDeclarative> decls,
        Func<int, string> action, string? modeTierWhen = null, string? modeTierNote = null,
        bool globalOnly = false)
    {
        bool Qualifies(BoundDeclarative d) => !globalOnly || d.Global;
        if (decls.Any(d => Qualifies(d) && d.Files.Count > 0))
            using (w.Block("switch (__f)"))   // file-name scope first (§14.9.49.4 GR3 a)/GR5)
            {
                for (int i = 0; i < decls.Count; i++)
                    if (Qualifies(decls[i]))
                        foreach (var f in decls[i].Files)
                            // An OBJECT/FACTORY file's key is its per-instance field, not a constant, so its label is a
                            // case guard (a METHOD's USE ON an object file — kb/Work PB1010; C# CS9135 otherwise). A
                            // program file's literal key keeps its constant label.
                            w.Line(f.InstanceKeyField is null
                                ? $"case {FileKeyExpr(f)}: {action(i)}"
                                : $"case var _ when __f == {FileKeyExpr(f)}: {action(i)}");
            }
        if (!decls.Any(d => Qualifies(d) && d.ModeIndex is not null)) return;
        string head = $"switch ({RuntimeApi.FileOpenModeOf("__f")})";   // open-mode scope (GR3 b)/GR6 b)–e))
        if (modeTierWhen is not null)
            head = $"if ({modeTierWhen}) {head}{(modeTierNote is null ? "" : $"   {modeTierNote}")}";
        using (w.Block(head))
        {
            for (int i = 0; i < decls.Count; i++)
                if (Qualifies(decls[i]) && decls[i].ModeIndex is { } m)
                    w.Line($"case {m}: {action(i)}");
        }
    }
}
