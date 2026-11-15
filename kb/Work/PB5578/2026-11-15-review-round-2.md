**2026-11-15, review round 2 (R2-1): SELECT WHEN has effect for a WRITE or REWRITE only with the FILE phrase from 2014; 2002 selects for every one.**

§2023:13.18.51.4 GR2 (and §2014:13.18.51.3 GR2): SELECT WHEN clauses of the file section "have no effect for statements other than READ statements, REWRITE statements with the FILE phrase, and WRITE
statements with the FILE phrase"; §2002:13.16.49.3 GR1 evaluates them "for READ, REWRITE, and WRITE statements associated with that file". The compiler's connector evaluated them for every WRITE and
REWRITE at every edition, so at 2014 and 2023 `WRITE G-A` of a record no clause selects answered 45 and dropped the record, and the round-1 golden pinned it. Fixed at the root: the statement carries the
record description entry it names (`BoundWrite.FilePhrase` / `BoundRewrite.FilePhrase` tell the FILE-phrase statement, whose record is the FROM operand, from record-name-1's; the emitter's
`SequentialIoEmitter.NamedRecordEntry` renders the entry index when `DialectBehavior.SelectWhenOnlyForFilePhraseStatements` (VCR row 7.78, from 2014) is active), the registry hands it to the connector for the
statement (`FileConnector.NamedRecordEntry`, cleared as `CommitGate` is), and `RecordFormat.ToExternal(namedEntry)` formats by it instead of evaluating the selector. READ, the FILE phrase, SORT and MERGE
select by the conditions at every edition. Goldens: `13.18.51-SELECT-WHEN-clause/select_when_record_name_write_by_edition` (WRITE; 2002 45, 2014 and 2023 00 then the READ's 45 and 46) and
`13.18.13-CODE-SET/select_when_code_set_rewrite_record_name_by_edition` (REWRITE; 2002 selects the other entry, 2014 and 2023 convert by the named one). The earlier
`select_when_write_no_record_selected_status_45` became the first; `select_when_code_set_rewrite`'s header no longer cites the FILE-phrase restriction for a plain REWRITE.

**Sites:** `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/SequentialIoEmitter.cs` (`SequentialIoEmitter.NamedRecordEntry`); `src/WiseOwl.Cobol.Compiler/CodeGen/Roslyn/RuntimeApi.cs` (`RuntimeApi.FileWriteShared`, `RuntimeApi.FileRewriteShared`);
`src/WiseOwl.Cobol.Compiler/Binding/Bound/BoundTree.cs` (`BoundWrite.FilePhrase`, `BoundRewrite.FilePhrase`); `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SequentialIoBinder.cs` (`SequentialIoBinder.BindWrite`, `SequentialIoBinder.BindRewrite`);
`src/WiseOwl.Cobol.Runtime/IO/FileRegistry.cs` (`FileRegistry.WriteShared`, `FileRegistry.RewriteShared`); `src/WiseOwl.Cobol.Runtime/IO/CobolFile.cs` (`CobolFile.WriteShared`, `CobolFile.RewriteShared`);
`src/WiseOwl.Cobol.Runtime/IO/FileConnector.cs` (`FileConnector.NamedRecordEntry`); `src/WiseOwl.Cobol.Runtime/IO/RecordFormat.cs` (`RecordFormat.ToExternal`); `src/WiseOwl.Cobol.Runtime/IO/SequentialConnector.cs` (`SequentialConnector.ExternalRecord`);
`src/WiseOwl.Cobol.Runtime/Control/DialectBehavior.cs` (`DialectBehavior.SelectWhenOnlyForFilePhraseStatements`); `src/WiseOwl.Cobol.Compiler/Binding/Passes/CheckCodeSetRecordItemsPass.cs` (remarks corrected, R2-2).
