// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE COMPILER HALF OF COMMIT AND ROLLBACK (ISO §9.1.18; DESIGN-runtime-library.md 2.2.1.10, "The compiler half";
/// kb/Work PB3338): the element's APPLY COMMIT clause as members of its program class, its activation, the COMMIT and
/// ROLLBACK statements, and the end of an instance that retires the clause. The run unit's half is
/// <c>CobolNet.Runtime.IO.TransactionControl</c>, reached through <c>CobolFile</c>.
/// <para><b>The data items' image.</b> Each item the clause saves (<see cref="ApplyCommitModel.Items"/>) has one string
/// field holding its STORAGE image as of the last COMMIT (or the activation): <c>__CommitSave</c> renders
/// <see cref="OperandText.RecordAreaImage"/> into it and <c>__CommitRestore</c> stores it back through
/// <see cref="SequentialIoEmitter.EmitImageInto"/> — the one record-area channel and its receiving inverse, arm for arm,
/// which carry every usage and shape a record area can have. The usage pass makes each such group take the image round
/// trip and each elementary item a character channel (<c>UsageCollectionPass.Collect</c>).</para>
/// </summary>
internal sealed class TransactionEmitter(EmitContext ctx, ReferenceResolver refs, SequentialIoEmitter seqIo)
{
    private ApplyCommitModel? Clause => ctx.Data.ApplyCommit;

    /// <summary>The program-class members of an element with an APPLY COMMIT clause: the per-instance activation flag,
    /// one image field per item, and the three methods the run unit calls back.</summary>
    public void EmitMembers(CodeWriter w)
    {
        if (Clause is not { } clause) return;
        w.Line("private bool __applyCommitActive;   // the element's APPLY COMMIT clause is active (ISO §12.4.6.3.4 GR5)");
        for (int i = 0; i < clause.Items.Count; i++)
            w.Line($"private string __commitImage{i} = \"\";   // '{clause.Items[i].CobolName}' as of the last COMMIT (§14.9.7.4 GR5)");
        using (w.Block("private void __CommitSave()   // ISO §14.9.7.4 GR5: the data items are saved"))
            for (int i = 0; i < clause.Items.Count; i++)
                if (refs.ResolveItem(clause.Items[i]) is { } place)
                    w.Line($"__commitImage{i} = {OperandText.RecordAreaImage(place)};");
        using (w.Block("private void __CommitRestore()   // ISO §14.9.36.4 GR3: the data items are restored"))
            for (int i = 0; i < clause.Items.Count; i++)
                if (refs.ResolveItem(clause.Items[i]) is { } place)
                    seqIo.EmitImageInto(place, $"__commitImage{i}");
        // The refused file of a COMMIT or ROLLBACK is told to the program by its I-O status in the element that describes
        // it (DESIGN-runtime-library.md 2.2.1.10, "What the program is told"): true when this element stored it.
        using (w.Block("private bool __CommitStatus(string __file)"))
        {
            foreach (var file in clause.CommitControlledFiles)
                using (w.Block($"if (__file == {FileKeyExpr(file)})"))
                {
                    seqIo.EmitStoreFileStatus(file);
                    w.Line($"return {(file.FileStatusName is not null ? "true" : "false")};");
                }
            w.Line("return false;");
        }
    }

    /// <summary>The clause's ACTIVATION, in <c>__Activate</c> after the connectors are registered (§12.4.6.3.4 GR5:
    /// "active when the runtime element in which it is contained has been activated"), once per instance. GR3/GR4's
    /// EC-FLOW-APPLY-COMMIT is the ACTIVATING element's: the runtime raises it under the checking the activation boundary
    /// latched from the CALL (<c>ExceptionEngine.ActivatorFlowApplyCommit</c>).</summary>
    public void EmitActivation(CodeWriter w)
    {
        if (Clause is not { } clause) return;
        using (w.Block("if (!__applyCommitActive)"))
        {
            string files = "new string[] { " + string.Join(", ", clause.CommitControlledFiles.Select(FileKeyExpr)) + " }";
            w.Line($"__applyCommitActive = {RuntimeApi.ActivateApplyCommit(files, "__CommitSave", "__CommitRestore", "__CommitStatus")};");
        }
    }

    /// <summary>The end of the instance's storage (a CANCEL, or an INITIAL program's return): the clause is retired —
    /// its items are no longer restored and the next COMMIT or ROLLBACK deactivates it (§14.9.7.4 GR3, §14.9.36.4
    /// GR4-GR6). Emitted inside <c>EndStorage</c>.</summary>
    public void EmitRetire(CodeWriter w)
    {
        if (Clause is null) return;
        w.Line($"if (__applyCommitActive) {RuntimeApi.RetireApplyCommit()};");
    }

    /// <summary>Whether the element needs <c>EndStorage</c> for its clause.</summary>
    public bool NeedsEndStorage => Clause is not null;

    /// <summary>⛔ THE COMMIT AND ROLLBACK STATEMENTS (§14.9.7, §14.9.36). The run unit commits or rolls back every active
    /// clause (§14.9.7.1). A file whose COMMIT or ROLLBACK the medium refuses is told as an input-output statement's
    /// fatal status would be (DESIGN-runtime-library.md 2.2.1.10; docs/CONFORMANCE.md §5, A.4.3): '30' in its I-O status
    /// and FILE STATUS item, EC-I-O-PERMANENT-ERROR through the file's exception processing — this element's USE
    /// procedures for a file of its own clause (<see cref="BoundCommitRollback.Files"/>, which carry the statement's
    /// (EC-I-O, file) checking), and for another element's file the condition under the checking a directive naming no
    /// file enabled, else DOC-A.1-103's termination when no element gives it a FILE STATUS item — and then
    /// §14.6.13.1.3 item 8's unconditional ROLLBACK, when checking did not end the run unit.</summary>
    public void EmitStatement(BoundCommitRollback n)
    {
        var w = ctx.Writer;
        string verb = n.IsCommit ? "COMMIT" : "ROLLBACK";
        string refused = $"__refused{ctx.Names.NextEc()}";
        w.Line($"string? {refused} = {(n.IsCommit ? RuntimeApi.Commit() : RuntimeApi.Rollback())};   // ISO {(n.IsCommit ? "§14.9.7" : "§14.9.36")}");
        using (w.Block($"if ({refused} is not null)"))
        {
            bool first = true;
            foreach (var file in n.Files)
            {
                using (w.Block($"{(first ? "" : "else ")}if ({refused} == {FileKeyExpr(file)})"))
                {
                    seqIo.EmitStoreFileStatus(file);
                    seqIo.EmitUseHook(file, verb);   // its last step is item 8's ROLLBACK
                }
                first = false;
            }
            // Another element's file: its own element stored the status (TransactionControl.Refused). The condition is
            // raised in THIS element when a directive naming no file enabled it here (§7.3.25.4 GR6/GR8 scope a FILE
            // phrase to this element's files); otherwise, or when no declarative qualified, DOC-A.1-103 decides.
            void Foreign()
            {
                string terminate = $"{RuntimeApi.TerminateOnRefusalWithoutStatusItem(refused, CsLiteral(verb))};";
                if (seqIo.EmitForeignFileIoCheck(refused) is { } noHandler) w.Line($"if ({noHandler}) {terminate}");
                else w.Line(terminate);
                w.Line($"{RuntimeApi.RollbackOnFatal(refused)};   // ISO §14.6.13.1.3 item 8");
            }
            if (first) Foreign();
            else using (w.Block("else")) Foreign();
        }
    }
}
