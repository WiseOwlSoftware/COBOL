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
describes its operands (`StatementBinder.Described`, both lanes) and §14.9.4.4 GR3 d) is asked against the program
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
