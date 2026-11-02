## Entry 2996 — 2026-11-02 06:36 PST — PB4107: restricted pointers passed BY REFERENCE, matched or returned compare by identity; the signature is SET's

**The defect.** kb/Work PB4107 (from the deciding adjudication of PB3758): a restricted program-, function- or
data-pointer passed BY REFERENCE, matched by §9.3.6 3 e) or returned (§14.8.3.3) was compared by its prototype's
SIGNATURE, which only the SET relation asks (§14.9.39.3 SR19 / SR20 / SR22, and what sends to it: §14.8.2.3.3, §9.3.6
4 c) / 5 c) / 6). Two pointers to two prototypes of one signature, or alike under two type-names, conformed in CALL,
INVOKE and RETURNING. Re-probed at 273328079: the adjudication's t5, t6, t10 and t11 compiled and ran, and the pb3162
golden's BY REFERENCE CALL printed "ref ok".

**The fix.** `ActivationDescription` carried one string, `Clauses`, for three facts the standard keeps apart. It now
carries them separately. `Clauses` holds the USAGE clause, with the TO phrase as the folded externalized name of the
prototype it names (`PrototypeSignatures.NamedRestriction`); §9.3.6 3 e) and §14.8.3.3 read it.
`ContentRestriction` holds what the pointer may CONTAIN: the signature class with the GR10 c) external mark
(`PrototypeSignatures.SignatureRestriction`, renamed from `RestrictionIdentity`); `MaySet` alone reads it.
`PointerType` holds the §8.5.3.1 type, the key of the TYPE clause's declaration; §14.8.2.3.2's "both shall be
restricted and of the same type" reads it through ONE copy, `ActivationRelations.RestrictedPointerTypeViolation`, which
the CALL lane's BY REFERENCE arm and both INVOKE lanes' `ParameterViolation` ask. 3 e) proves the USAGE clause, not
the type. `SamePointerRestriction`'s unknown-signature arm is now the SET relation's only. A program-address-identifier
carries both restrictions (`StatementBinder.ProgramRestrictionOf`, `PrototypeSignatures.PrototypeRestriction`).
The text a description carries changed, so `abi_pin.py bump --call-abi` raised the call ABI to 4. The
determination is docs/CONFORMANCE.md D-RP1. It records one reading the text leaves open: an untyped restricted
pointer (a `FUNCTION-POINTER TO` entry, or the unique item an address-identifier creates, which INVOKE passes BY
REFERENCE by §14.9.23.3 SR9) is of the type its USAGE clause states. The first gate found that reading was needed:
pb1408, pb480 and pb3120 pass `ADDRESS OF S` to a formal `TYPE PT` through a universal INVOKE, and a strict
§8.5.3.1 reading had turned all three into EC-OO-UNIVERSAL.

**Goldens.** New: `14.8.2.3.2/pb4107_restricted_pointer_identity_and_signature`. It shows SET, BY CONTENT,
BY REFERENCE of the same type, BY REFERENCE across two names and across two types (EC-PROGRAM-ARG-MISMATCH), and the
universal INVOKE across two types (EC-OO-UNIVERSAL). It has four negatives: `pb4107-call-byref-pointer-two-types`
(COBOL1688), `pb4107-invoke-byref-datapointer-two-types`, `pb4107-invoke-returning-same-signature` and
`pb4107-invoke-returning-funcptr-same-signature` (COBOL0828). Re-verdicted: pb3162 (its BY REFERENCE CALL now
EC-PROGRAM-ARG-MISMATCH) and pb3686 (both CALLs EC-PROGRAM-ARG-MISMATCH; PB3686's reading is overturned). The
two pb2464 goldens lost their typed legs to the negatives, and their universal legs now raise EC-OO-UNIVERSAL. The
signature-class goldens pb2495_recursive, pb2495_pointer_type and pb3101 now pass their cross-prototype pointer BY
CONTENT, the SET relation they witness. pb2495_recursive's universal RETURNING raises EC-OO-UNIVERSAL. The adjudication
expected a compile-time SR25 refusal for pb3162 and pb3686. Their callees are specified later in the group, so the
checks are GR10 c)'s at the activation. The unit `PointerRestrictionLanesAgreeTests` asserted the overturned
one-verdict reading. It is replaced by `PointerRestrictionRelationsTests`. Inventory: witnesses were added to
GR-14.8.2.3.2-4 and GR-14.8.3.3-2 (both CONFORMS; GAP unchanged, 19).

**The branch review (R1-1) and its fix.** The reviewer compiled a callee that declares `TB TYPEDEF USAGE
PROGRAM-POINTER TO P9` without P9's prototype (§12.3.8.4 GR10 c)), and a caller that declares the same TB with the
prototype. The caller's `CALL ... BY REFERENCE PB` (`TYPE TB`) then failed with EC-PROGRAM-ARG-MISMATCH. On main it
printed "ref ok". `PointerType` came from `StrongTypeModel.TypeIdentityKey`, and the key embedded the pointer's
signature class: `SIG <hash>` where the compilation held the signature, `EXTERNAL TO P9` where it did not. So one
declaration had two keys. The strong group's `StrongType` used the same key, so a strong group holding such a pointer
already failed across that boundary on main. The root was the §8.5.3.1 relation, which compared a member pointer's
USAGE clause by signature class. D-RP1 says the TO phrase compares by the prototype it names (this was the landing's
lead 2). `StrongTypeModel.SameAnalyzedProfile` and `EssentialCharacteristicsKey` now read
`PrototypeSignatures.NamedRestriction`, and the key is one text in every compilation.
`PrototypeSignatures.RestrictedPointersOf` now numbers only the formals that ARE restricted pointers, because no
signature key reads a member's class anymore. §9.3.8.2.3 rule 7 makes a strong-group formal's signature its §8.5.3.1
type, so two verdicts change. A prototype and definition whose strong-group member is `TO QA` on one side and `TO QB`
on the other (one signature, two names) are now COBOL1513. Strong-group formals that name each other (`GS1` holding
`TO GS2`, `GS2` holding `TO GS1`) are two signatures, and a SET between pointers to them is now COBOL1959. The pb3120
goldens kept their same-name legs. The flipped legs became the negatives `pb4107-group-formal-pointer-two-names`,
`pb4107-group-formal-template-pointer-two-names` and `pb4107-set-group-member-pointers-naming-each-other`. The corpus
cannot hold the cross-compilation case, because §10.6.2 SR1 puts every prototype of a group first. Two tests witness it
instead. `TypedPointerAcrossCompilationsTests` compiles four modules and calls in both directions; before the fix it
printed `T4107K EXC EC-PROGRAM-ARG-MISMATCH`. `PointerRestrictionRelationsTests.TheTypeOfATypedRestrictedPointer_IsTheSameText_WhetherOrNotItsPrototypeResolved`
compares the real builder's descriptions.

**Left as a lead.** The pair-mode relation (§9.3.8.2.3, and through it `PrototypeSignatures.Same`'s graph) still
compares two ELEMENTARY class pointers by signature class. A pointer that is a member of a typed or group formal now
compares by name. D-RP1's clause-identity reading suggests the elementary arm should compare by name too. That would
change GR25's signature classes, which DESIGN-external-repository §6.4 rests on, so it stays a lead. kb/Work PB2103 (the
group's other note) was not started: part 2 needs eleven codes and the wave allocated three.

