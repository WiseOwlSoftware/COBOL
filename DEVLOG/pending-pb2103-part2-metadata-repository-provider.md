## PB2103 part 2: the external repository is read — the metadata provider, REPO-1 … -12 and the four options

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
