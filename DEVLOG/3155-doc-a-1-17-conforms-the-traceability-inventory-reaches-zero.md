## Entry 3155 — 2026-11-05 12:27 PST — DOC-A.1-17 CONFORMS: the traceability inventory reaches zero GAP

**What.** The owner assigned the inventory's last GAP row, DOC-A.1-17 (A.1 item 17: the other effects of a CALL that was
not successful and whose condition is handled by exception processing, §14.9.4.4 GR3 h) 2.), to the Mythos lane. Its
2026-09-24 verdict was PARTIAL because the ARG-MISMATCH and EC-EXTERNAL arms were never reached. Re-probed at ab64e9cf7,
every condition GR3 h) 2. covers for a CALL behaves as docs/CONFORMANCE.md determines: the declarative runs, and after
RESUME AT NEXT STATEMENT no program was activated, the USING and RETURNING items keep their values, NOT ON EXCEPTION is not
executed and the last exception status names the condition. The row is now CONFORMS, the inventory is at GAP 0 of 4,214,
and `audit_annex_a1.py` reports 183 of 183 required A.1 items documented, no obligation remaining. kb/Work A11 lands.

**Evidence.** Two goldens under `conformance-suite/cases/A.1-Implementor-defined-language-element-list/`:
`doc_a1_17_call_other_effects` (2023: EC-PROGRAM-NOT-FOUND, -PTR-NULL, -ARG-MISMATCH, -RESOURCES, -RECURSIVE-CALL and
EC-EXTERNAL-FORMAT-CONFLICT, after a successful control CALL) and `doc_a1_17_call_other_effects_2002` (2002 and 2014,
the same without EC-EXTERNAL, a 2023 name). The abnormal half, a declarative that ends without RESUME (§14.6.3 3)), is
`ExceptionConditionConformanceTests.CallFails_Enabled_DeclarativeWithoutResume_FatalTerminates` (NOT-FOUND, PTR-NULL,
ARG-MISMATCH); nothing pinned it before. The CONFORMANCE.md row now names each condition with its GR3 item and edition.

**Found on the way.** kb/Work PB4768 (crash): a formal passed on BY REFERENCE through each level of a recursion is wrapped
in one more view per level by `CobolArgAdapt.Text`, so each access costs O(depth), and at about 21,000 levels reading it
kills the process with a .NET stack overflow where EC-PROGRAM-RESOURCES should be handled. The RESOURCES golden passes each
level's own LOCAL-STORAGE item. And `work.py check` failed at GAP 0: its population guards required some row to be
defective and some note to claim a row, which a finished inventory need not have. They now ask whether the schema still
marks defective verdicts, whether rows carry a `verdict`, and whether notes carry the `inventory_rows` key
(`work_inventory.py`, with self-tests for a GAP-0 register and a schema without `resolves` flags).

