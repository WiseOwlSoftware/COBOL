## Filed the leads of fourteen fix-lane reports as PB4115-PB4140

The enqueue unit's clerk filed the new defects and non-blocking review leads of fourteen finished fix-lane reports, one note per mechanism. Compiler and tooling behaviour is unchanged; only `kb/Work/` moved.

- PB4115 (analysis): INSPECT with a function operand answers "may read" for every function-identifier, and the disjointness walk is StringEmitter's private `ReachOf` instead of one mechanism beside `StorageOverlap.MayShareStorage` (PB1908 lead 2, R2-2).
- PB4116: row GR-14.9.43.4-10's notes read as GnuCOBOL agreement on an order that was never measured (PB1908 R3-1).
- PB4117 (decision): SEND/RECEIVE message-tag SR4 is unscreened because USAGE MESSAGE-TAG is refused; screening is the owner's call (PB4090 lead 1).
- PB4118: `ClassifySend` accepts a subscripted, reference-modified or qualified reference after TO as a server name (PB4090 R1-1).
- PB4119 (analysis): `ReferenceResolver.ProbeDeclaration` duplicates `FlagConformancePass.ResolveItem` and already differs from it (PB4090 R1-2).
- PB4120: a bare figurative ZERO in an arithmeticExpression-only slot is refused, and PB3981's wrong-error survives there (PB3961 R1-1, R1-2).
- PB4121: three SetBinder guards open with an elided `SET …`, which the drift test cannot see (PB3966 R1-1).
- PB4122: PB1063's "next implementer" paragraph names a rule arm PB3966 deleted (PB3966 R1-2). PB2625 gained a dated paragraph asking for its re-verdict (PB3966 lead).
- PB4123 (analysis): is an always-integral expression argument an Integer argument of §15.3/§15.59.1 (PB4036 lead 1).
- PB4124: a frozen function argument's FunctionValueField description differs from the unfrozen one, so LENGTH answers differ (PB4036 lead 2).
- PB4125 (analysis): CONVERT's node carries two type facts that can disagree (PB4036 R1-1).
- PB4126: `cobol --std COBOL-2002` throws an unhandled InvalidOperationException (PB4031 lead 3). The report's two EC-SIZE leads are PB4092 and PB4093, already filed, so nothing was added for them.
- PB4127: a quotation in inventory-schema.json fails `cite.py --check` (PB593 R1-1).
- PB4128 (analysis), PB4129: line-number citations in inventory notes; two rows' present-tense text about a closed defect (PB3848 carried lead, R2-1).
- PB4130, PB4131: the COBOL0900 "extended letter" wording under COBOL-85, and the effect-before-cause diagnostic order (PB3845 leads 1 and 2).
- PB4132, PB4133, PB4134, PB4135: the reservation gate hijack of END-DISPLAY and RETRY, header single-token-deletion recovery, the tail discard running through END PROGRAM, and the wrong DESIGN section number in an entry (PB3843).
- PB4136, PB4137, PB4138: the FORESEEN path has no live comparison, the conflict brief's false claim about the finisher's tip, and a stale docstring (PB3772).
- PB4139: three stale comments after PB3841's rewrite.
- PB4140: `prune_worktrees.lifecycle` turns a CLEAN-reviewed branch with unique commits into DECISION when its lead note is already landed, so a reviewed follow-up never reads WAITING TO LAND (the enqueue unit's own lead; witness `tooling-suite-partitions`, `tooling-semgrep-1183`).

Not filed: PB4075's wide-argument consumers and comment leads (PB4111, PB4112 hold them), PB1908's STRING lead (PB4089), PB4031's EC-SIZE leads (PB4092, PB4093), PB3429's `DataBinder.WrittenText` forwarder lead (PB2733 holds it), and the stale-comment leads of PB3845 and PB3429, whose code is on branches not yet on main.
