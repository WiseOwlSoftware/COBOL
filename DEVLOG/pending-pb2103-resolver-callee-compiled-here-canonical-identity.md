## PB2103 part 1: the repository resolver, a callee compiled elsewhere checked at run time, and the canonical signature identity

Slice 7 of the external repository (DESIGN-external-repository §21 item 7), part 1 of at least two. The slice's
estimate was one landing; its eleven REPO codes are reported only by compilations that READ a repository entry, so they
land with the metadata provider in part 2, and the design's §21 now says so (CLAUDE.md rule 5).

**Re-probe.** A caller with `REPOSITORY. PROGRAM P2103B.` and `CALL P2103B USING W-X` (PIC X(10)) against a P2103B
compiled apart with `LINKAGE PIC 9(4)` compiled and ran `CALLEE ABCD`: no provider answers GR10 c) yet, and
`--repository` does not exist. PB2104 re-probed too: `CLASS K2104` compiled apart is refused in the caller (COBOL0813,
COBOL0823), legal source under §12.3.8.3 SR6 once the repository holds it; it waits for this slice.

**The resolver (§8.0).** `GroupRepository` is renamed and moved to `GroupRepositoryProvider`, the group's
`IRepositoryProvider`: `Program` and `Function` by externalized name through the one ordered `Candidates` enumeration
(a definition before the position, then a prototype: GR10 / GR11 a) then b)), and `FunctionsByWord`, the word's
candidates in that order, which D-R3 steps 2 and 3 read. D-R3 moved into `RepositoryResolver.ResolveFunction`, written
once over the provider list; `ResolveProgram` is GR10's. `FunctionResolution.Shadowed` is a `FunctionAnswer`, so
COBOL2969 names its function through a `RepositoryOrigin`. `BindSession.Repository` is the resolver; every caller
changed, nothing kept.

**The split (§4.2).** `BoundCallProgram.CalleeCompiledHere` is true only for AS NESTED, a prototype CALL that
activates GR10 a)'s definition (or §8.4.6.8's containing program) and a GR11 a) / §8.4.6.6 function. Everywhere else
the activated element is compiled elsewhere and may have been rebuilt since the bind checked it, so the site
describes its operands (`CallBinder.Described`, both lanes) and §14.9.4.4 GR3 d) is asked against the program
actually activated. Sibling found and fixed in `ProgramTable.Activate`: an activated FUNCTION was judged by
§14.8.2.3.2 rule 1 (same length) unless the site listed it as a program-specifier; rule 2 lists "a function". Golden
`wiseowl/cases/14.9.4-CALL/pb2103_prototype_callee_compiled_elsewhere` (multi-unit: a conforming callee runs, a stale
formal and a stale RETURNING item are EXC); `FunctionCompiledElsewhereTests` pins the fatal function arm.

**The canonical identity (§6.4).** A restricted pointer's type was its signature class's founder name, comparable
only inside one group. `PrototypeSignatures.CanonicalIdentity` is `SIG <hex>`, the SHA-256 of the class's minimized
signature graph (classes numbered in first-visit order, each signature written by `SignatureKey` / `DescriptionKey`,
the relation `Same` reads as a key), so two separately compiled groups spell one signature's pointers alike.
`PrototypeSignatureClasses` refines with provisional block positions and stamps the canonical identities once the
partition is stable; the founder spelling and the decoded admission numbers are gone. `SignatureIdentityDriftTests`
holds identity equality to `Same` over every prototype of the corpus (most pairs cross groups), ten perturbations, and
renamed, regrouped and differently recursive twins in another group. A program-address-identifier's description phrase
now names the prototype as written, not the identity.

**The structure key.** A DYNAMIC LENGTH clause's structure is compared by `DynamicLengthStructure.DefinitionKey`
(PREFIXED, DELIMITED, the physical structure), never its name, in the §8.5.3.1 profile, the clause identity and the
pair relation; docs/CONFORMANCE.md D-DL3 records it. Golden
`wiseowl/cases/14.8.2.3.2-Elementary-items-passed-by-reference/pb2103_structure_compared_by_definition` (main's
compiler answers EXC to the same-layout case; this tree OK).

**The ABI.** Call ABI 3. `RuntimeAbi.CallAbi` became a property: as a `const` its value sat in `PublicAPI.Shipped.txt`,
so `abi_pin.py bump --call-abi` could not succeed without moving the runtime's public-API hash too; no module reads the
member. `abi_pin.py`'s pattern and the two self-test fixtures follow; the runtime-hash re-measure is that change alone.

Rows: none re-verdicted (the rows stay claimed by PB1086; GR-14.9.4.4-3 and GR-14.8.2.3.2-L2.1 were already CONFORMS
and gain witnesses).

**Wave 20261816 (group A) carried part 1 onto main** (cherry-picked, re-gated GREEN at run 000001) and landed the group's
smaller mechanisms on the same branch.

**PB4012 — `--permissive` accepts OMITTED passed BY REFERENCE in a Format 1 CALL** (owner decision 2026-10-28, "Yes,
permissive accepts"). The re-probe drew COBOL0899 errors under `--permissive` for `USING W OMITTED` and `USING W BY
REFERENCE OMITTED`. `CallBinder.Format2OnlySpellingOf` now reads the mode §14.9.4.4 GR5 carries: OMITTED passed BY
REFERENCE is the sixth vendor spelling (a COBOL0899 warning, bound as the GR11 omitted argument); an OMITTED a BY
CONTENT phrase carries over keeps the error under both modes, as cobc 3.2 does. The survey and the decision are
CONFORMANCE D-CALLV; `FormatOneVendorCallPermissiveTests` pins strict, permissive (run output
`A=ABCD B OMITTED` twice, then `A=ABCD B=ABCD`) and the BY CONTENT refusal.

**PB4340 — `cobol check-batch --help` named the compile's `<source>`.** System.CommandLine writes a parent command's
arguments before a subcommand, in the usage line and in the grammar (so `cobol x.cob check-batch m.tsv` ran the batch
and ignored `x.cob`). The two verbs are now two command trees, `Program.BuildParser` and
`Program.BuildCheckBatchParser`, chosen by the first word in `Program.Parse`; `CliParserTests` pins the help and the
usage error.

**PB4047 — an emitted assembly is compiled against the framework's reference assemblies.** The re-probe's AssemblyRef
table named `System.Private.CoreLib`, so an ordinary MSBuild host failed CS0012. The compiler's build now lays the
Microsoft.NETCore.App targeting pack it resolved at `reference-assemblies/` beside every referencing project's output and
in the CLI's publish output (the ReadyToRun images' content mechanism); `AssemblyPackager.ReferenceAssemblyPaths` is the
one reader and `RoslynBackend` compiles against it, so the same program references `System.Runtime`, and a plain
net10.0 host project with two HintPath references built against a compiled callee and ran it. The three test-side
copies of the reference set (one of which hand-added `System.Private.CoreLib`, which is how the defect hid from the host
tests) read the same property, the compiled-program cache keys on the files, and `EmittedReferenceIdentityTests` pins the
metadata. The manual's "Known limitation" section became "Building a host project".

**PB4219 — the non-data tier of a name's nearest declaration has a depth.** APPLY COMMIT ON RELF drew COBOL1639
"index-name" where a nearer container's GLOBAL file made SR11 the rule (COBOL4709), and SR11 about a container's file
where the program's own condition-name RELF was the referenced item. `NonDataDeclaration` now carries the depth of its
declaring element (index-name by its table, constant by its inherited distance, condition-name by its variable, file by
the new `DataBinder.FileDepth`, which replaces `InheritedFileDepth`), `NonDataDeclarationOf` ranks a file-name against a
condition-name by it, and `ScreenEnclosingFileOperand` lets a non-file name hide the GLOBAL file only when no farther.
Two goldens pin the repros.

**PB4226 arm (1) — SEARCH ALL no longer words a condition-name reference twice.** `CheckConditionNameSide` added COBOL1965
to the condition binder's own verdict (COBOL5092 for a nearer report entry, which it called a condition-name; COBOL1639
for a misqualified one); it now defers to `ConditionBinder.ReportMisqualifiedCondition`, the one wording. Arm (2), an RD
OCCURS DEPENDING ON whose name a nearer report entry hides, reproduces, but the text may make that report entry a legal
DEPENDING ON object, so the note stays open and the question is a lead.

**PB4306 — a screen-section constant entry parses.** §13.9.2 prints constant-entry beside screen-description-entry;
`screenSection` now has both, and the binder binds the constant as the report host does, so the section draws its named
COBOL1560 (not COBOL0001) and a reference to the constant resolves.

**PB4267 — the EVALUATE, OO, MOVE, CALL and SET binders quote nodes as written.** Forty message sites in six binders read
`GetText()` (glued qualifiers: "SET 'AOFG'"); each now reads `DataBinder.WrittenText`.
