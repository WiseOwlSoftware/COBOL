- **D-RP1 — restricted pointers passed BY REFERENCE, matched, and returned: identity, not signature** (kb/Work PB4107;
  the deciding adjudication of PB3758, 2026-10-30 — the ISO text's reading, no latitude and no owner decision). A
  restricted data- or program-pointer is a typed item (§2023:13.18.60.3 SR18 / SR19 require the TYPEDEF clause on the entry
  that restricts it). §2023:14.8.2.3.2's "If either is a restricted pointer, both shall be restricted and of the same type"
  is §2023:8.5.3.1's relation: the two items are described with TYPE clauses that reference equivalent type declarations —
  the same type-name, and "the same … USAGE clauses" including the TO phrase. §2023:9.3.6 match rule 3 e), §2023:14.8.2.3.2 rule 2
  and §2023:14.8.3.3 compare "the same … USAGE clauses", and the TO phrase is compared by the prototype (or type) it NAMES —
  the prototype's externalized name, folded — never by its signature: the standard lists the clause texts that match on
  meaning (currency strings, DECIMAL-POINT IS COMMA, the LOCALE phrase) and none for the TO phrase, and a prototype
  whose details the external repository supplies (§2023:12.3.8.4 GR10 c)) is as much a name as one whose details the group
  does. The signature relation (§2023:8.13;
  §2023:13.18.60.4 GR25 / GR26: what a restricted pointer may CONTAIN) governs only SET (§2023:14.9.39.3 SR19 / SR20 / SR22),
  CALL SR14 and the rules that send to SET (§2023:14.8.2.3.3; §2023:9.3.6 4 c), 5 c), 6). So two pointers restricted to two prototypes
  of one signature, or alike under two type-names, are admitted by a SET or a BY CONTENT pass and refused BY REFERENCE
  (a compile-time COBOL1688 / COBOL0828 when the formal is known, else EC-PROGRAM-ARG-MISMATCH or EC-OO-UNIVERSAL at the
  activation), and a returning pair of two names is refused by §2023:14.8.3.3 — as an object reference of a subclass is
  admitted by SET and refused BY REFERENCE (§2023:14.9.39.3 SR12 a) 2. against §2023:14.8.2.3.2 rule 3). One reading the text
  leaves to the implementor: §2023:8.5.3.1 defines "the same type" for TYPED items only, and two restricted pointers are not
  always typed — an entry `USAGE FUNCTION-POINTER TO` needs no TYPEDEF, and the unique item an address-identifier creates
  (§2023:8.4.3.11.4 GR2, "restricted to the type of identifier-1"), which INVOKE passes BY REFERENCE (§2023:14.9.23.3 SR9), has no
  TYPE clause. Such an item is of the type its USAGE clause states, so opposite it only the USAGE clauses are compared
  (`ADDRESS OF S`, S of strong type T-REC, conforms to a formal `TYPE PT` with `PT TYPEDEF USAGE POINTER TO T-REC`, as
  `conformance:cases/_unassigned/pb1408_universal_address_of_subordinate_group` pins); two TYPED items are compared by
  their type declarations as well. §2023:8.5.3.1's equivalence of two type declarations compares a member pointer's USAGE
  clause the same way, by the prototype it names (`StrongTypeModel.SameAnalyzedProfile` and its key,
  `EssentialCharacteristicsKey`): one declaration `TB TYPEDEF USAGE PROGRAM-POINTER TO P` is one type in a compilation
  that holds P's signature and in one that only names P (§2023:12.3.8.4 GR10 c)), and a strongly-typed group whose pointer
  member is `TO QA` is not of the type of one whose member is `TO QB`, whatever the signatures — so two prototypes whose
  strong-group formals differ so have two signatures (§2023:9.3.8.2.3 rule 7; branch review R1-1 of kb/Work PB4107).
  The pair-mode conformance of §2023:9.3.8.2.3 asks the same identity of an ELEMENTARY pointer (kb/Work PB4442): rule 3 (a
  formal) and rule 6 (a returning item) require "the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH,
  JUSTIFIED, PICTURE, SIGN, and USAGE clauses" — the very words §2023:9.3.6 3 e) and §2023:14.8.3.3 use — and a restricted
  program- or function-pointer's USAGE clause is `USAGE PROGRAM-POINTER TO program-prototype-name`, so an override
  (§2023:11.7.3 SR9, COBOL0829), an IMPLEMENTS pair (§2023:9.3.11, COBOL0841), an interface-to-interface pair and a property
  accessor pair (§2023:8.4.3.9.3 SR7) conform exactly when both name the same prototype, whatever the two prototypes'
  signatures, as the same pair does as strong-group members (rule 7). The SIGNATURE itself reads a restricted pointer
  formal the same way (kb/Work PB4491): §2023:8.13 makes the signature "the description of the parameters", §2023:13.7.3 SR2 asks
  that the prototype's formals "match the description of the formal parameters" of the definition, and a pointer formal's
  description is its USAGE clause, so `USING TO Q1` and `USING TO Q2` are two signatures whatever Q1's and Q2's — the
  only reading under which a caller checked against a prototype passes its definition a conforming argument, since
  §2023:14.8.2.3.2 refuses a pointer TO Q1 BY REFERENCE into a formal TO Q2. A §2023:10.6.2 SR2 / SR3 prototype / definition pair
  over two names is therefore refused (COBOL1513). A TYPED pointer formal is described by its type declaration as well:
  §2023:8.13 puts "type declarations required for the description of parameters and returning items" in the signature, and
  §2023:14.8.2.3.2's "of the same type" refuses a pointer of type T1 BY REFERENCE into a formal of type T9 (§2023:8.5.3.1: "the same
  type-name"), so a prototype `USING TYPE T1` and a definition `USING TYPE T9`, both `TO Q1`, are refused too, as are an
  untyped `FUNCTION-POINTER TO F1` formal and a typed one (`PrototypeSignatures.SameDescription` over
  `ActivationDescription.PointerType`; branch review R1-1 of kb/Work PB4491) — the pair-mode relation does not ask it,
  since §2023:9.3.8.2.3 rules 3 / 6 list only the clauses. Two prototypes whose formals name each other are two signatures,
  so §2023:14.9.39.3 SR22 refuses a SET between their pointers (COBOL1959): the signature is compared as TEXT and has no fixed
  point to find. What a pointer may CONTAIN is still its prototype's signature (§2023:13.18.60.4 GR25 / GR26), spelled
  `PrototypeSignatures.Identity`, a hash of that signature's key.
  GnuCOBOL, IBM and Micro Focus have no restricted prototype pointer. Implementing site: `ActivationRelations.RestrictedPointerTypeViolation` (BY REFERENCE, both the
  CALL and the INVOKE lanes), `ActivationRelations.MatchViolation` / `ReturningConformance` over
  `ActivationDescription.Clauses` (`PrototypeSignatures.NamedRestriction`), `ActivationRelations.SignatureDifference` (pair mode and the signature graph, over `Clauses`), `PrototypeSignatures.Identity` / `StampContentRestrictions`, `ActivationRelations.MaySet` over
  `ActivationDescription.ContentRestriction` (`PrototypeSignatures.SignatureRestriction`), `StrongTypeModel.SameAnalyzedProfile`
  / `EssentialCharacteristicsKey` (`PrototypeSignatures.NamedRestriction`). Witnesses:
  `conformance:cases/14.8.2.3.2-Elementary-items-passed-by-reference/pb4107_restricted_pointer_identity_and_signature`,
  `conformance:cases/_unassigned/pb2464_prototype_pointer_signature_type`,
  `conformance:cases/_unassigned/pb2464_function_pointer_signature_type`,
  `conformance:cases/14.8.2-Formal-parameters-and-arguments/pb3686_returning_external_prototype_pointer`,
  `conformance:cases/_unassigned/pb3120_group_formal_pointer_type`,
  `conformance:cases/9.3.8.2.3-Conformance-between-interfaces/pb4442_override_pointer_same_prototype_two_type_names`,
  `conformance:cases/10.6-COBOL-compilation-group/pb4491_prototype_pointer_formal_one_prototype`,
  `conformance:cases/_unassigned/pb2495_recursive_prototype_signature`,
  `conformance-test:TypedPointerAcrossCompilationsTests`,
  `unit:PointerRestrictionRelationsTests.TheTypeOfATypedRestrictedPointer_IsTheSameText_WhetherOrNotItsPrototypeResolved`
  and the negatives `pb4107-call-byref-pointer-two-types`, `pb4107-invoke-byref-datapointer-two-types`,
  `pb4107-invoke-returning-same-signature`, `pb4107-invoke-returning-funcptr-same-signature`,
  `pb4107-group-formal-pointer-two-names`, `pb4107-group-formal-template-pointer-two-names`,
  `pb4107-set-group-member-pointers-naming-each-other`, `pb4442-override-pointer-two-prototypes`,
  `pb4442-override-returning-pointer-two-prototypes`, `pb4442-implements-pointer-two-prototypes`,
  `pb4491-prototype-pointer-formal-two-names`, `pb4491-function-prototype-pointer-formal-two-names`,
  `pb4491-prototype-pointer-formal-two-type-names`, `pb4491-function-prototype-pointer-formal-typed-untyped`,
  `pb2495-recursive-prototype-signature-mismatch`.
