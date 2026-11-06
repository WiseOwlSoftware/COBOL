## Entry 3003 — 2026-11-02 10:42 PST — PB4442: the pair-mode relation of §9.3.8.2.3 compares a restricted program- or function-pointer by the prototype it names

**What was wrong.** An override `USING TO Q2` over a base `USING TO Q1`, or an IMPLEMENTS pair of the same two, was admitted
when Q1 and Q2 have one signature, while the same pair as members of a strongly-typed group was refused (PB4107's group-member
arm compares the TO phrase by name). `ActivationRelations.SignatureDifference`, the ONE pair-mode relation, had a class-pointer
arm that compared `ActivationDescription.ContentRestriction` (what the pointer may contain: its prototype's signature class)
for two elementary pointers, so the elementary arm and the member arm of one relation disagreed.

**Adjudication.** The note asked which relation §9.3.8.2.3 requires of an elementary restricted pointer formal. Rule 3 asks the
corresponding formal "the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE
clauses", rule 6 the returning item the same, and §11.7.3 SR9 makes an override obey them (each quote checked with
`cite.py --check`). Those are the words §9.3.6 3 e) and §14.8.3.3 use, which D-RP1 (PB4107) reads as identity of the TO phrase's
prototype name: `USAGE PROGRAM-POINTER TO q1` and `TO q2` are two USAGE clauses whatever the signatures. The signature class
answers a different question (what a pointer may CONTAIN, §13.18.60.4 GR25 / GR26, so SET).

**Re-probe.** On main (b1f39bdc0) `pb2495_pointer_type_in_signature_checks` compiled and ran ("px null / override null /
implements null"); a minimal override probe compiled clean.

**Fix.** The class arm of `ActivationRelations.SignatureDifference` is now reached only when the caller passes
`pointersBySignature`, which `PrototypeSignatures.SameDescription` alone sets: the signature graph (§13.7.3 SR2, §10.6.2 SR2,
the classes of `PrototypeSignatureClasses`, `DescriptionKey`) is unchanged. Every other asker (override, IMPLEMENTS,
interface-to-interface, property accessors, `HostEntryEmitter`'s aliasing test, `MoveBinder.ElementMovesOf`) compares
`ActivationDescription.Clauses`. A first version put the class arm in `PrototypeSignatures` itself; `OneRelationDriftTests`
refused it (a second copy of the description comparison outside the relation), so the mode is a parameter of the one relation.
The probe now draws COBOL0829 (override, formal and returning item) and COBOL0841 (IMPLEMENTS).

**Goldens.** `9.3.8.2.3-Conformance-between-interfaces/pb4442_override_pointer_same_prototype_two_type_names` (accepted at
2002: the same prototype under two type-names conforms, formal and returning item) and the refusals
`pb4442-override-pointer-two-prototypes`, `pb4442-override-returning-pointer-two-prototypes`,
`pb4442-implements-pointer-two-prototypes`. `pb2495_pointer_type_in_signature_checks` lost its override and IMPLEMENTS legs (they
asserted the under-rejection) and keeps the prototype / definition pair and the BY CONTENT pass. Unit:
`PointerRestrictionRelationsTests.PairMode_TwoPrototypesOfOneSignature_AreTwoDescriptions_TheSignatureGraphMergesThem`, and
`PrototypePointerTypeDriftTests.OnlyThePrototypeSignatureGraphAsksTheSignatureClassOfAPairModePointer` keeps a second asker
from setting `pointersBySignature`.
Docs: D-RP1 in `docs/CONFORMANCE.md`, `docs/OO_DESIGN.md` (D-RP1 passage and a stale `Clauses` sentence), DESIGN-external-repository
§6.4.

**Left open.** The signature graph still reads an ELEMENTARY pointer formal by class and a strong-group member by name, so the
§10.6.2 SR2 pair (prototype `USING TO Q1`, definition `USING TO Q2`) is admitted while the group-member form is refused. Which
the standard wants of the graph is a question of the signature relation (it reverses PB2495), not of §9.3.8.2.3; registered in
the report. No inventory row closed (`closes_rows: []`).

