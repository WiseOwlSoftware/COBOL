## Entry 3274 — 2026-11-07 14:20 PST — PB4893: below 2023 a statement that activates a signature with a message tag is the gate's, through every receiver, and one roster lookup

**The mechanism.** PB4868 moved the below-2023 message-tag question to the statement funnel
(`StatementBinder.NamesGatedMessageTag`): a statement whose own operands name a message-tag item this compilation declares binds to
a no-op, so COBOL0900 is the whole diagnosis (DESIGN-message-control §7.1). Its inline-invocation arm,
`OoBinder.InlineInvocationReturningItem`, answered only for a receiver that was a data item of a named class, and nothing asked an
INVOKE statement at all. Re-probed on the Debug build at 2014, each beside COBOL0900: an inline invocation through SELF (COBOL0869
in SET, COBOL0809 in MOVE), SUPER, an object view of SELF or of a universal reference, a class-name and an interface-typed reference;
every `INVOKE ... RETURNING N` of a tag-returning method through every receiver (COBOL0828); and a sibling no note named, an argument
whose same-file formal is a tag, through INVOKE USING, an inline argument list (COBOL0828) and a user function (COBOL2470).

**The fix.** The question became "does the statement name a gated tag, or ACTIVATE a function or method whose signature has one"
(its RETURNING item, whose description the operand's temporary takes, §2023:8.4.3.4.4 GR1b and §2023:8.4.3.2.4 GR1, or a formal its
argument conforms to, §2023:14.8.2). `OoBinder.InvokedMethodsOf` and `InvokedMethodOf` select the method without binding or reporting,
in `OoBindByReceiver`'s order: an object view's last AS phrase (`ViewRoster`, §2023:8.4.3.5.4 GR1, which the property-object probe
now reads too), SELF and SUPER through `OoPredefinedSearchRoot(report: false)` (§2023:8.4.3.8.4 GR2), a function-identifier's RETURNING
item, a data item's descriptor, a class-name's factory interface (§2023:14.9.23.4 GR2), and each previous segment's result
(§2023:8.4.3.1.3 SR1). A concatenated method name folds through `ConcatFolder.Peek`, the folder's reporting-free form.
`IntrinsicBinder.ReturningItemOf` became `SignatureOf`, and `UserFunctionSignature` and `OoMethodBinding` each answer their
`SignatureItems`, so the funnel takes no new dependency on `LinkageFormal` (ArchitectureEdgesDriftTests).

**The roster lookup, found on the way.** Seven sites wrote `factory ? FindFactoryMethod : FindMethod` and two searched an interface's
prototypes, and the interface arm of `OoBindInstanceInvoke` compared method-name-1 where the others compared the externalized name:
`INVOKE I "Y"` of a prototype `METHOD-ID. X AS "Y"` was refused COBOL0825 while the same call through the class bound (legal source:
§2023:14.9.23.4 GR2a resolves literal-1 "as described in 8.3.2.2", and §2023:11.7.4 GR1b makes literal-1 the externalized name). The
lookup is now one type, `MethodRoster` (Binding/Model), whose `Find` every site asks, pinned by `MethodRosterLookupDriftTests`.

**Goldens.** `MessageTagIntroductionGateTests.AStatementActivatingASignatureWithATag_IsTheGateAloneBelowTheIntroduction`: 19
statements, each the gate alone at 2014 and its class refusal with no COBOL0900 at 2023. `conformance-suite/cases/14.9.23-INVOKE/
pb4893-interface-externalized-method-name` (accepted from 2002: the class, interface and inline receivers each print 0042) and its
`-at-85` refusal (COBOL0900).

**Not fixed.** CALL Format 2 (`CALL "SUBN" AS NESTED USING N` against a nested program whose formal is a tag) still draws COBOL1688
beside COBOL0900: literal-1 of the CALL is read through `ExpressionBinder.NonNumericLiteralOperand`, which reports, so the callee
signature has no diagnostic-free reader yet; registered as a lead.

No inventory row is re-verdicted (`closes_rows: []`, with its reason).

