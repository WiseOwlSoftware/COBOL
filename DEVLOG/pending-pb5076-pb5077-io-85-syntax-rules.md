## COBOL-85 mandatory exception phrase and the record-area FROM/INTO rule (PB5076, PB5077)

Two explicit syntax rules of the X3.23-1985 I-O modules compiled clean at `--std 85`. Both were re-probed on the build first: all 22
repro programs of the two notes (11 each) compiled with exit 0 and no diagnostic, as filed.

**PB5076, the mandatory phrase (COBOL6826, 85 only).** Every 1985 I-O module says READ, WRITE, REWRITE, DELETE and START "must"
carry AT END or INVALID KEY when no applicable USE AFTER STANDARD EXCEPTION procedure is specified. 2002 prints no such rule. The check
is one type, `ExceptionPhraseRequirement`, asked at the five keyed binders and the sequential READ. Which statements need the phrase
is the question FLAG-14 d already asked ("can be specified with an INVALID KEY phrase"), so `FlagConformancePass.AdmitsInvalidKey` moved
to `IoPhraseAdmission` and both passes read it; the sequential READ adds AT END. "Applicable" is §1985:VII.4.6.4 GR5: a declarative
naming the file, or one for an open mode, in this program or, written GLOBAL, a containing one (§1985:X.5.5.4 GR1 b; the branch
review found the first cut counted a container's USE without GLOBAL, which no contained statement selects). My first reading let a mode declarative cover only
the verbs the mode suits (a USE ON OUTPUT not a READ); CCVS-85 SQ148A refuted it: it READs a file open OUTPUT on purpose and the OUTPUT
declarative takes the status 47. A mode declarative now covers every statement, since the rule refuses statements and a program that might
be covered is not refused. A contained program's READ is covered by its container's declarative. The container's
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

**What the gate found.** The first full gate failed 60 tests, every one COBOL6826: the 1985 corpus was full of programs that
omit the phrase, which no earlier compiler said. CCVS-85 SQ148A was the one that corrected the design (a mode declarative covers
every statement). The rest were our own tests, written when `accept: [85]` meant "compiles at 85". They were migrated, not exempted:
227 statements in 48 conformance programs now carry `AT END CONTINUE`, `INVALID KEY CONTINUE` or the right pair, each with its
`END-` terminator (a bare phrase inside a longer sentence draws COBOL2796); the phrase is `INVALID KEY` for a random read and
`AT END` for a sequential one, because the other draws COBOL1720. `pb3830_phrase_format_selectors` keeps its inner statements
phrase-free, since the association rule is what it tests, and gets a do-nothing `USE` procedure instead. `l1c11_invalid_key_no_phrase_no_use`
pins the no-phrase behavior of 2023:9.1.14 GR4, so it is accepted from 2002 only. Six inline programs in `KeyedIoDifferentialTests`,
`ClauseOperandQualificationSpecTests` and `Ansi85AcceptanceTests` were patched the same way; the four differential goldens are named by
a hash of the source, so they were renamed with it (their expected output did not change). `RecordAreaRule` moved to `Binding.Validation`
(its text-returning members are diagnostic wording, and the backend contract refused them under `Binding.Model`).

**Witnesses.** Conformance: the legal sides `pb5076_sequential_read_without_at_end_covered_by_use`,
`pb5076_keyed_statements_without_phrases_covered_by_use_io` and `pb5077_from_into_outside_the_record_area` (run at all four editions),
the 2023 acceptance `pb5077_from_own_record_area_accepted_2023`, six `pb5076-*-85` refusals (one per verb) and five `pb5077-*` refusals (one per verb, refused at 85, 2002 and 2014). `IoExceptionPhraseRequirementTests` pins the scope:
every USE mode against every verb, the access-mode qualification of DELETE and REWRITE, a contained program, and the record-area boundaries.
Design: D31 of `docs/FILES_DESIGN.md`.

**The 1985 cases main added since.** Four of the 1985 draft-triage cases main placed after this branch was cut WRITE a relative or indexed file with no INVALID KEY phrase and no USE procedure (`1985-GR-IX_4_5_4-18`, `1985-GR-VIII_2_6_3-1`, `1985-GR-VIII_4_5_4-16`, `1985-GR-VIII_4_6_4-5`): the merged tree's gate refused them with COBOL6826, as §1985:VIII.4.9.3 SR3 and §1985:IX.4.9.3 SR3 require. Each WRITE now carries the phrase; no write takes it, so the expected outputs stand.
