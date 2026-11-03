## PB4491: a prototype's signature describes a restricted pointer formal by the prototype it names

**Mechanism.** The signature graph (`PrototypeSignatures.Same`, asked by §10.6.2 SR2 / SR3 and §13.7.3 SR2) compared an
ELEMENTARY restricted program- or function-pointer formal by its prototype's signature class, through the
`pointersBySignature` arm PB4442 left on `ActivationRelations.SignatureDifference`, while a strong-group MEMBER of the
same graph compared by name (PB4107). So a prototype `PX USING TO Q1` was admitted for a definition `PX USING TO Q2`
whenever Q1 and Q2 had one signature, and the same pair written as group members was refused.

**Re-probe (base 11c6abfb1).** `_unassigned/pb2495_pointer_type_in_signature_checks` compiled and ran `px null` /
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
definition names the prototype through `REPOSITORY PROGRAM QQ AS "Q1"` and another type-name). Refused (COBOL1513):
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
