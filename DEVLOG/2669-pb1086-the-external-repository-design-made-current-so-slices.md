## Entry 2669 — 2026-10-27 14:03 PDT — PB1086: the external-repository design made current, so slices 4, 5, 7 and 8 can be built from it

The owner asked the Mythos lane on 2026-10-27 to take up kb/Work PB1086, the §8.13 external repository, which holds 18
of the inventory's 31 GAP rows. The design (`docs/rearchitecture/DESIGN-external-repository.md`, approved 2026-10-05)
had been sitting under three weeks of code changes. A read-only review written at main d23857d7f found 151 references
still current, and 5 renamed, 3 moved, 4 gone and 16 that had changed shape. Its verdict was that slice 4 (PB2100) could
not be implemented from the document without improvising. This change re-verifies those findings against main 832e5f512
and corrects the design in one edit. It touches documents and kb/Work notes, plus one test fix in
`scripts/orchestrator/test_plan_wave.py`: its R3 slice-wait case now draws its test file from the live slice set,
because the §12 rewrite changed which files that set holds.

The serious finding was a rule the document would have regressed. The approved §6.4 typed a restricted program-pointer
or function-pointer by its prototype's IDENTITY, and §12 deleted `PrototypeSignatureClasses` in slice 7 so that two
groups would "compare record identities". §13.18.60.4 GR25 types such a pointer by the prototype's SIGNATURE
(`cite.py --check` OK), and PB2464, PB2495, PB3101, PB3120 and PB3162 put that rule in the code. Built as written,
slice 7 would have typed pointers by name across units. The design now adopts the code. The record carries the
restriction name and the prototype's full signature, and the decoder stamps the pointer through the decoding group's
classes. The tree's `PrototypeSignatureClasses.Identity` answers a name it has seen without consulting the signature, so slice 4
also re-keys the class by origin and name, admits decoded prototypes into its refinement and keeps block identities
unique; the round-trip test then fails on a record that dropped a referenced prototype's formal. Slice 7 keeps the
classes, because their partition refinement is what minimizes a recursive signature. It
replaces the group-local founder name with a canonical identity, a hash of the minimized signature graph that every
group computes the same way, and a drift test holds that identity equal to `PrototypeSignatures.Same`. It also deletes
the null-signature state PB3162 spread into the runtime. Those changes alter the description text and remove public
members, so slice 7 needs both a call-ABI bump and a runtime-major bump.

Slice 4 needed decisions the document had left open. The design now makes them:
- Enums: the schema's enums are the runtime's own persisted vocabulary. Each value is explicit and pinned by
  `PublicAPI.Shipped.txt`, and one translation per enum is held by a round-trip drift test. A discard-free enum switch
  is not possible here: C# reports CS8524, which `TreatWarningsAsErrors` turns into an error.
- Encoding: a value that may be null is written only when present. `PictureClauseIdentity` and `LocaleEditSpec` are
  flattened into prefixed named arguments, and `EditingRules` becomes five parallel arrays.
- Method attributes: they sit on the carrier of PB2087's three-parameter group. The `__retLen` parameter, the covariant
  adapters and the other two parameters carry none.
- Reading: the header and `Units` index reader (`RepositoryIndex`) and `RepositoryLocator` live in the runtime. The
  description decoder `RepositoryReader` lives in the compiler. `RuntimeApi.ModuleRecordAttribute` moves into
  `RepositoryAttributeRenderer`.
- Schema: slice 4 moves to schema version 2, and a version-1 module is treated as another version.
- Diagnostics: the REPO codes are allocated in slice 7, which is the first slice that reports them. §15.2's read caps,
  §17.4 and §17.7 belong to slice 4. `CompileState` moves to slice 8 and gains `NumericDisplay`, `>>LEAP-SECOND` and
  `>>DEFINE`, and its drift test now covers `BindSession`'s directive states as well as `CompilerDriver.Options`.
- Host entries: every outermost unit publishes one. `HostEntryEmitter`'s `Published` filter goes, and a group shape
  `Renders` refuses becomes an internal error instead of a silent gap in the repository.

Probing the review's lead on `EntryUnits` found a new defect, filed as kb/Work PB3872. The prototype/definition pairing
compares externalized names ordinally, so a prototype `psub` beside a definition `PSUB` produced two host-entry classes
(`scratchpad/pb1086d/probe/fold.cob`).

Three determinations are recorded in §19. (a) User-manual text stays with kb/Work PB1610, as slice 3's did.
(b) Every fault on the way to an activation is one of the CALL's own conditions (§14.9.4.4 GR3 b) and c), both
`--check` OK), as `docs/CONFORMANCE.md` DOC-A.1-14 / -89 already determine them under CLAUDE.md rule 1. Every
registrar fault, and a registered program whose class or a dependency fails to load at instance creation, is
EC-PROGRAM-NOT-FOUND, so one missing assembly never gives a different condition depending on when the CLR loads it;
EC-PROGRAM-RESOURCES stays the stack check. The old §11.2 sentence named a test that pins a module that does not
load, not a class-load failure, and `n.Factory` failures escape today. That is slice 5's work, recorded in PB2101. (c) Multi-unit tests
are WiseOwl conformance cases. A new `build` header field and a `compile-unit` adapter operation let a case compile
several units into its working directory, which is already the default repository and the application directory.
Slice 5 lands the mechanism.

The CALL lane question PB2097 carried to PB2103 is also settled (§4.2). A CALL whose callee is compiled elsewhere keeps
describing its operands for GR3 d)'s run-time check, and that covers GR10 b) as well as c).

Smaller corrections in the same edit: the stale "slice 3 open" text, §21 item 3's lifted GR13 restriction, §12's rows
(now marked landed or assigned to a slice), the golden paths under `conformance-suite/`, `BackendOptions` /
`OutputKind`, `IsStandard`, the probe's catch list, and the reference to a `GR-12.3.8.4-11` CONFORMANCE row that never
existed. `docs/CONFORMANCE.md` §3 gains the D-R3 bullet that slice 6 shipped COBOL2969 without. The register fixes:
PB2104 is `blocked_by: ["PB2103"]`, PB1086's 2026-10-23 "PB2104 ready" gets a dated correction, and the open slice
notes' Scope lines now match the design.

