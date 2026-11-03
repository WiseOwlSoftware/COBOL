## Entry 3013 — 2026-11-02 17:28 PST — PB2103 part 2: the external repository is read — the metadata provider, REPO-1 … -12 and the four options; PB2104 step 1 begins (class identities in run-time descriptions); PB2103 review finish: D-R3 step 3's shadow look reports nothing, and a group function prototype no longer fails on an unrelated repository entry

### PB2103 part 2: the external repository is read — the metadata provider, REPO-1 … -12 and the four options; PB2104 step 1 begins (class identities in run-time descriptions)

**Re-probe.** On this tree `cobol --repository x.dll prog.cob` answered "Unrecognized command or argument 'x.dll'", and a
caller whose `REPOSITORY. PROGRAM P2103B.` named a program compiled apart (`LINKAGE PIC 9(4)`) passed a PIC X(10) and ran
(`CALLEE ABCD`): ISO §12.3.8.4 GR10 c) ("the details are taken from the external repository") had no provider, so a
specifier the group could not answer carried no signature at all.

**The provider (DESIGN-external-repository §8.0, §8.2, §8.3, §8.6).** `MetadataRepositoryProvider` is the resolver's second
provider, after the group's. It searches `RepositorySearchSpace`: the `--repository` directories in order (the output
directory when none is named), then the `--reference` assemblies, never the assembly being produced (§9.3;
`RepositoryLocator.Find(…, excluding)`), through the runtime's `RepositoryLocator` and the compiler's `RepositoryReader`,
and decodes the record with `RepositoryCodec.Decode` into the group's signature classes, the object references re-worded
into the importing group's words. The first entry wins; a later different build of the name whose signature differs is
REPO-2. Every reader outcome is reported, once, at the specifier that first asked: REPO-3 (not a readable assembly),
REPO-4 (malformed), REPO-5 (a description the codec cannot rebuild), REPO-7 (another schema, runtime major or call ABI),
REPO-8 (built with `--repository-update off`), REPO-10 (a named place that does not exist), REPO-11 (an object reference
to a class the group lacks). A specifier none answers is REPO-1 (§12.3.8.3 SR14 / SR10), naming every place searched,
and registers no prototype. `--repository-check` is `RepositoryCheck` (§9.2): every outermost unit against the previous
build of the output and against every entry the compilation read, REPO-9 naming the first differing position
(`PrototypeSignatures.FirstDifference`, which `Same` now reads), REPO-12 for a previous build of another version. The codes
are COBOL5962 … COBOL5972, all eleven reported by this change.

**The null-signature state is gone.** `ProgramPrototype.Details` is non-nullable; `PrototypeSignatures.ExternalIdentity`,
`UnclassifiedIdentity`, `FormalCount`, `Same`'s null arm, `RestrictedPrototype`'s nullable signature, the signature classes'
external arms, `PrototypeRecord.HasSignature` / `CobolPrototypeAttribute.HasSignature`,
`ActivationDescription.ExternalRestrictionPrefix` / `IsExternalRestriction` and `SamePointerRestriction`'s
conform-to-anything arm are deleted, with the runtime-major bump to 22 (`scripts/abi_pin.py bump --runtime-major`). A binder
comparing two prototype NAMES asks `StatementBinder.SignaturesDiffer`: a name its tables do not hold was refused where it
was declared and raises no second fault.

**Also landed.** §7.5's file protocol (`OutputFileProtocol.Replace`: write to a GUID temporary, rename over the target with a
bounded retry, fail loudly naming the file) for the assembly, the deployed runtime and the runtimeconfig;
`CompilationInputs.ReadsRepository`, which keeps a repository-reading compilation out of the compiled-program cache (its
answer depends on a directory listing no recorded input replays) and is §17.6's zero-cost witness; the manual page
`docs/manual/separately-compiled-units.md`; CONFORMANCE DOC-A.1-66 / -67 and D-R3's repository arms.

**The corpus.** Nineteen cases named a program or function that nothing answered: a definition that FOLLOWS the REPOSITORY
paragraph, a program contained in the caller, or nothing at all — non-conforming by SR14 / SR10, and let through by the
deleted state. The portable cases gained a program or function prototype definition ahead of every unit (§10.6.2 SR1,
GR10 b)); the WiseOwl cases `pb3677_format1_sdidi_expression` / `_function_value` compile their callee as a build step (the
GR10 c) multi-unit shape of §17.1), and `pb2103_structure_compared_by_definition` is now the stale caller (P2103DR rebuilt
after its caller). Legs whose subject was a run-time check of a violation now known at compile time were dropped where a
compile-time twin exists (pb3162, pb3686, pb4107, pb165's two literal legs), OMITTED formals are OPTIONAL as §14.9.4.3 SR24
requires (pb615, pb2087), and pb989 / pb1100 expect REPO-1. `ExternalRepositoryResolutionTests` witnesses the layouts and
damaged files no manifest case states.

**Not yet witnessed by a case** (leads in the report): REPO-4, -5, -7 and -12 need a patched or older entry; the
cross-repository REPO-13 and word cases; the pb1115 split across two units.

**Group B (PB2104) finished group A's branch.** Group A ended at its turn cap with its gate RED on seven causes, all its
own; each was fixed at its root before slice 8 began. *The compiled-program cache:* the provider probed the DEFAULT
repository directory (the output directory, which the compilation itself creates) and `--repository-check` probed the
output path, both recorded, so every rebuild in place missed; and an unrecorded `?? Directory.Exists` fallback bypassed
the input record. Now `RepositorySearchSpace.Inputs` is non-nullable, typed as `Frontend.Common.IRepositoryInputs` (the
binder may read `frontend.common`, never `frontend.pipeline`); a named `--repository` directory and a `--reference` file
are probed through it; the default directory is not probed (missing is empty, and any question marks the record); the
previous-build comparison marks `ReadsRepository` only when it REPORTS (REPO-9 / REPO-12), and
`CompiledProgramCache.TryHit` replays over an existing output only when it is the stored output byte for byte
(`APreviousBuildWithAnotherSignature_IsNeverReplayedOver`, seen red with the rule disabled). *Layering:* the driver calls
`BinderDriver.Bind` itself, so CodeGen no longer names the search space. *The corpus and tests:* `NotLocatedReasonTests`
puts the function its REPOSITORY names first (§12.3.8.4 GR11 a) "specified previously"); the `repository-program-2002`
matrix sample states its prototype; `TypedPointerAcrossCompilationsTests` compiles P4107X's prototype as its own module
first, which exposed a crash — a compilation group of prototypes alone dereferenced the current unit emitter in
`ProgramEmitter.Emit`, which no program class had set; the sign encoding is now a compilation fact on `BoundCompilation`.
The manual's example is a complete program plus a fragment; DOC-A.1-66's witnesses are a `;` list; GR-12.3.8.4-10's
test-ref follows the renamed test (record_verdicts batch).

**PB2104 step 1, part 1: a run-time description names a class by its identity.** DESIGN-external-repository §6.4: a
word is per source element, so `ActivationDescriptions.Of` now spells the class or interface an object-reference
description names, and its class facts (`ObjectClasses` / `ObjectInterfaces`), by the externalized name (ISO §11.3.4
GR1), through `OoClassTable.DescribedOperand` — `SetOperand`'s one derivation, which keeps the words for the compile-time
SET table — under call ABI 5 (`scripts/abi_pin.py bump --call-abi`). A corpus case was written first and did not witness
it: two groups' classes are distinct CLR types until a class is imported, so a cross-group argument already fails its
carrier (`CallAbi.Slot`) whatever the description says; it was removed, and `ObjectReferenceIdentityDescriptionTests`
pins the spelling. Found: the design's `ObjectRefDescriptor.Target` cannot be the symbol (`Binding.Model` may not reference
`Compiler.Oo`), so §6.4 now says it is the resolved identity; that, the class import and the rest of step 1 are next
(PB2104 stays open). Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,987/20,987 · Unit 39,388/39,388 ·
Characterization 37/37 cases ran (skipped 0) … (2 class(es) first; run 000002-20261102T220627Z) ===`.

### PB2103 review finish: D-R3 step 3's shadow look reports nothing, and a group function prototype no longer fails on an unrelated repository entry

**The finding (review R1-1, blocking).** `RepositoryResolver.ResolveFunction`'s step 3 (is there a DIFFERENT function under
the same word? then warn REPO-13) asked `MetadataRepositoryProvider.ByWord`, which decoded every repository entry carrying
the word with its faults REPORTED. ISO §12.3.8.4 GR11 b) ("the details are taken from that function prototype definition
and the details in the external repository are ignored", checked with `cite.py --check 12.3.8.4`) says the repository's
details play no part once the group's prototype answers a specifier by its externalized name, so an entry that merely carries
the word had no business raising REPO-8, REPO-7, REPO-11, REPO-5 or REPO-4. Re-probe on the part 2 build: a library
`FUNCTION-ID. FDBL` compiled with `--repository-update off` into `bin/`, then a program that carries its own
`FUNCTION-ID. FDBL IS PROTOTYPE` and `REPOSITORY. FUNCTION FDBL.` compiled into the same `bin/`, failed with
`COBOL5968 … compiled with --repository-update off` (exit 65); origin/main compiled it. After the fix it compiles (exit 0).
A library built by the previous runtime major failed the same way (REPO-7), which after this branch's major bump was every
not-yet-rebuilt library of a shared output directory.

**The fix, at the one place the rule belongs.** `IRepositoryProvider.FunctionsByWord` gained `otherThan`. With it null it is
step 2: the first function is the specifier's answer, so an unreadable repository entry is a finding of the compilation.
With it set it is step 3: the provider leaves out a function externalized `otherThan` (the function GR11 selected, or a stale
copy of it, skipped BEFORE the entry is opened) and decodes the rest quietly, so a function that merely might shadow the
selected one is advice for REPO-13 and never a fault. The group provider applies the same filter, so the resolver no longer
filters by hand and the warning still never names a function the search itself ignores. The cache of the word lookup is keyed
by `(word, otherThan)`.

**Its sibling, found in the same method.** Step 2 reported the faults of EVERY entry carrying the word although only the
first, in search order, is the answer (the by-name search has always worked so: later entries are quiet). `ByWord` now
reports only the first entry's faults, and `MetadataRepositoryProvider.Refused` takes the word as well as the externalized
name, so a refused word-path answer draws no REPO-1 beside its own finding (`AWordAnsweredByTheFirstEntry_ReportsOnlyThatEntrysFault`).
The other `ByName` / `ByWord` callers were swept: `Program` and `Function` (D-R3 step 1 and GR10 c)) run only after the group
missed, so their reports are the specifier's own.

**Stale statements (review R1-2).** The "no compile-time signature" description of a GR10 c) prototype is gone from
`docs/CONFORMANCE.md` (the SET/BY CONTENT paragraph), `SetBinder` (SR22's remark on `PrototypeSignatures.Same`),
`BoundCall.cs` (`BoundCallArg.Formal` and `CalleeSignatureKnown`, which `CallBinder` now sets for a repository prototype) and
`DESIGN-external-repository.md` (the `PrototypeRecord.HasSignature` model, the attribute table, the identity paragraph's
`EXTERNAL TO` prefix, §8.0's "the group's alone until the metadata provider lands", §8.2's `CalleeSignature?`); the §8.0
interface line and landed paragraph state `otherThan`.

**Goldens.** `conformance-suite/wiseowl/cases/12.3.8-REPOSITORY/pb2103_function_prototype_ignores_entry` (a multi-unit case:
the library built with `--repository-update off` through `wiseowl/options/<id>.pb2103_fp_lib.cbl.json`, then the program that
holds the function prototype; the function activated is the library's, 21 * 2 = 0042; run at 2002, 2014 and 2023), and
`ExternalRepositoryResolutionTests`: the same program against an entry without signatures and against an entry an older
runtime major compiled (`MakeStale` rewrites the recorded runtime version), the same two entries REPORTED when the group has
no prototype (REPO-8, REPO-7, no REPO-1), the step 3 shadow (REPO-13 for a readable repository function, nothing for an
unsigned or stale one), the word path's first-entry rule, and REPO-12 (a previous build of an older major is not compared,
COBOL5972). `OutputFileProtocolTests` (review R1-3) pins the replace protocol's abnormal paths: a failed or throwing write
leaves the previous build and no temporary, and a target that cannot be replaced fails loudly after the bounded retry naming
the file. No refusal golden below the introducing edition was added: the refusal this fix removes is repository-only and the
registry has no per-step refusal key yet; the edition gate of function prototypes is the version matrix's.

**What is still open.** REPO-4 and REPO-5 need a damaged-record fixture and design 17.1's remaining multi-unit cases are
unwritten: kb/Work PB4455. PB2103 is landed with the part 2 branch; PB2104 stays open for step 1's remainder.

