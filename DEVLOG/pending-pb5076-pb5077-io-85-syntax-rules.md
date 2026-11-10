## COBOL-85 mandatory exception phrase and the record-area FROM/INTO rule (PB5076, PB5077)

Two explicit syntax rules of the X3.23-1985 I-O modules compiled clean at `--std 85`. Both were re-probed on the build first: all 22
repro programs of the two notes (11 each) compiled with exit 0 and no diagnostic, as filed.

**PB5076, the mandatory phrase (COBOL6826, 85 only).** Every 1985 I-O module says READ, WRITE, REWRITE, DELETE and START "must"
carry AT END or INVALID KEY when no applicable USE AFTER STANDARD EXCEPTION procedure is specified. 2002 prints no such rule. The check
is one type, `ExceptionPhraseRequirement`, asked at the five keyed binders and the sequential READ. Which statements need the phrase
is the question FLAG-14 d already asked ("can be specified with an INVALID KEY phrase"), so `FlagConformancePass.AdmitsInvalidKey` moved
to `IoPhraseAdmission` and both passes read it; the sequential READ adds AT END. "Applicable" is §1985:VII.4.6.4 GR5: a declarative
naming the file, or one for an open mode the file can be in when the verb runs, in this program or a containing one (GR1). A USE ON
OUTPUT therefore does not cover a READ, and a contained program's READ is covered by its container's declarative. The container's
declaratives arrive through a lazy `ContainerUses` that `BinderDriver` sets on each contained unit's binder.

**PB5077, the operand rule (COBOL6827, 85, 2002 and 2014).** READ INTO and RETURN INTO may not receive into the file's record area;
WRITE, REWRITE and RELEASE FROM may not send from it. The notes called it an 85 rule; the 2002 text prints all five (§2002:14.8.29.2 SR2,
14.8.30.2 SR2, 14.8.32.2 SR1, 14.8.33.2 SR1, 14.8.47.2 SR4), so the gate is `Year < 2023`, where §2023:14.6.10 replaces it. The 2014 text
is not in the repository; the lineage records route 1985 to 2023 across it and 2014 keeps the rule (a determination, docs/CONFORMANCE.md
section 3.4). The five phrases already funnel through `MoveBinder` (D23), so the screen sits there once:
`StatementValidation.CheckPhraseOperandIsNotRecordArea`, from `BindFromPhrase` (which now takes the file) and `CheckIntoReceiver`. Its
question, `StorageSharing.IsRecordAreaOf`, is exact where `MayShare` is conservative, because it refuses a statement: a record of the file
or of a SAME RECORD AREA peer, or a subordinate that begins the record and is as long as it. A part of the record, a reference-modified
operand and another file's record stay legal. `RecordAreaRule` holds the edition scope, the "must"/"shall" wording and the one citation
per verb and organization (the 1985 text repeats the rule in three modules); the verb is a new column on `FromPhraseRules` and
`IntoPhraseRules`.

**Witnesses.** Conformance: the legal sides `pb5076_sequential_read_without_at_end_covered_by_use`,
`pb5076_keyed_statements_without_phrases_covered_by_use_io` and `pb5077_from_into_outside_the_record_area` (run at all four editions),
the 2023 acceptance `pb5077_from_own_record_area_accepted_2023`, seven `pb5076-*-85` refusals (one per verb, and a USE ON OUTPUT that does
not cover a READ) and five `pb5077-*` refusals (one per verb, refused at 85, 2002 and 2014). `IoExceptionPhraseRequirementTests` pins the scope:
every USE mode against every verb, the access-mode qualification of DELETE and REWRITE, a contained program, and the record-area boundaries.
Design: D31 of `docs/FILES_DESIGN.md`.
