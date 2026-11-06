## Entry 3054 — 2026-11-03 14:55 PST — PB4491: a prototype's signature describes a restricted pointer formal by the prototype it names

**Mechanism.** The signature graph (`PrototypeSignatures.Same`, asked by §10.6.2 SR2 / SR3 and §13.7.3 SR2) compared an
ELEMENTARY restricted program- or function-pointer formal by its prototype's signature class, through the
`pointersBySignature` arm PB4442 left on `ActivationRelations.SignatureDifference`, while a strong-group MEMBER of the
same graph compared by name (PB4107). So a prototype `PX USING TO Q1` was admitted for a definition `PX USING TO Q2`
whenever Q1 and Q2 had one signature, and the same pair written as group members was refused.

**Re-probe (base 32d4e4db1).** `_unassigned/pb2495_pointer_type_in_signature_checks` compiled and ran `px null` /
`content null`; its `CALL PX USING A` passed a pointer TO Q1 BY REFERENCE into the definition's TO Q2 formal with no
diagnostic and no run-time check.

**Adjudication (the spec decides).** §8.13 makes the signature "the description of the parameters" and §13.7.3 SR2 asks
the prototype's formals to "match the description of the formal parameters" of the definition. A pointer formal's
description is its USAGE clause, and docs/CONFORMANCE.md D-RP1 compares a TO phrase by the prototype it names wherever
"the same ... USAGE clauses" is asked, the BY REFERENCE pass of §14.8.2.3.2 included. Under the structural reading a caller
checked against the prototype passed the definition a pointer its formal refuses, and two prototypes that name each other
were one signature, so SR22 let a pointer TO P2 hold a program whose formal a call through that pointer could not satisfy.
The nominal reading is the only one under which the prototype stands in for the definition. It reverses PB2495's verdict
for mutually naming prototypes and retires PB3101's partition refinement, whose only job was that fixed point.

**Fix.** One reading for every asker: `ActivationRelations.SignatureDifference` lost `pointersBySignature`, and
`PrototypeSignatures.DescriptionKey` keys the clauses. No key reads another identity any more, so "the same signature" is a
comparison of text, and `PrototypeSignatureClasses` (Classify, Admit, the blocks, the provisional identities) is deleted
with every `SignatureClasses` property (`BindSession`, `BoundCompilation`, `StatementBinder`) and the decoder's admission.
What a pointer may CONTAIN (§13.18.60.4 GR25, the SET relation's alone) is `PrototypeSignatures.Identity`, `SIG <hex>` of
the namespace and `SignatureKey`, stamped by `PrototypeSignatures.StampContentRestrictions` from
`BinderDriver.ResolvePointerRestrictions` and `RepositoryCodec.Decode`. `RestrictedPrototype` and `PrototypeOrigin` moved to
`RestrictedPrototype.cs`. The content identities change, so the call ABI goes to 6. Sibling arms: the function prototype
pair (§10.6.2 SR3) shares the code and has its own refusal; the member arm (PB4107) and the pair-mode arms (PB4442) already
read by name. `PrototypeSignatures.SameSignatureRestriction` had no production caller and went too (closes PB4443).

**Goldens.** Accepted at 2002: `10.6-COBOL-compilation-group/pb4491_prototype_pointer_formal_one_prototype` (the
definition names the prototype through `REPOSITORY PROGRAM QQ AS "Q1"`, under the prototype's type-name T1). Refused (COBOL1513):
`pb4491-prototype-pointer-formal-two-names` (2002, 2014, 2023) and `pb4491-function-prototype-pointer-formal-two-names`
(2014, 2023). Rewritten: `_unassigned/pb2495_recursive_prototype_signature` (one signature because every formal names
PS3), `pb2495-recursive-prototype-signature-mismatch` (the mutually naming pair, COBOL1959),
`_unassigned/pb2495_pointer_type_in_signature_checks` (lost the SR2 pair) and `12.3.8-REPOSITORY/pb3121_containing_definition_scope`
(two self-naming prototypes are two signatures). Unit: `PrototypeSignatureClassesTests` deleted; `SignatureIdentityDriftTests`
also asserts that no corpus key holds an identity; `PrototypePointerTypeDriftTests.NoSignatureDescriptionReadsWhatAPointerMayContain`
pins the relation and the key off `ContentRestriction`, with a fail proof on the deleted arm. No inventory row moved
(SR-10.6.2-2, SR-10.6.2-3 and SR-13.7.3-2 were already CONFORMS).

**Docs.** D-RP1 in docs/CONFORMANCE.md, the two signature paragraphs of docs/OO_DESIGN.md, DESIGN-external-repository §6.4
and its section 12 row, DESIGN-architecture-review, and the architecture-edges ratchet (two tolerated pairs gone).

**Branch review R1-1: a typed pointer formal's type-name is in the signature.** The reviewer showed the first cut still
admitted a prototype `USING TYPE T1` for a definition `USING TYPE T9`, both `TO Q1` (the accepted golden pinned it), though
a direct `CALL PX USING A` with A of type T1 draws COBOL1688 against the T9 formal: the failure this landing fixes for
`TO Q1` / `TO Q2`, on the other half of D-RP1's "two TYPED items are compared by their type declarations as well". §8.13
(`cite.py --check 8.13` OK) puts "type declarations required for the description of parameters and returning items" in
the signature, and §8.5.3.1 (OK) makes two type declarations equivalent only "when they have the same type-name". So
`PrototypeSignatures.SameDescription` now also compares `ActivationDescription.PointerType` (null for an untyped
`FUNCTION-POINTER TO` formal, which differs from a typed one) and `DescriptionKey` keys it, so the relation and the
identity still agree. The pair-mode relation (`ActivationRelations.SignatureDifference`) does not read it: §9.3.8.2.3
rules 3 / 6 (OK 9.3.8.2.3 3)) list only the clauses. Re-probe after the fix: the reviewer's `direct3.cob` still draws
COBOL1688, its `control3.cob` prints `px null`, and the T1/T9 pair draws COBOL1513 at (30,20); a definition recompiled
over its previous build with T9 changed to T1 draws REPO-9 (COBOL5969, "USING formal 1's description"), and recompiled
unchanged draws nothing, so the decoded type identity agrees with the source one. Goldens: the accepted golden now spells
T1 on both sides; refusals `pb4491-prototype-pointer-formal-two-type-names` (COBOL1513, 2002/2014/2023) and
`pb4491-function-prototype-pointer-formal-typed-untyped` (COBOL1513, 2014/2023, the untyped-against-typed arm);
`_unassigned/pb2495_recursive_prototype_signature`'s PSDEF pair (T3 against T9, found by the corpus round trip) now spells
T3 on both sides; `SignatureIdentityDriftTests` gains QPTRT (TO PBASE through another type-name: another identity).

