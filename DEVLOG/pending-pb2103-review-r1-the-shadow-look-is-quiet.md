## PB2103 review finish: D-R3 step 3's shadow look reports nothing, and a group function prototype no longer fails on an unrelated repository entry

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
