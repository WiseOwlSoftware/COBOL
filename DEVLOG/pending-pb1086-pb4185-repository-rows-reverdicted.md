## PB1086 discharged: eleven external-repository inventory rows re-verdicted against the landed slices; PB4185's Problem annotations

**What the re-probe showed.** The umbrella note PB1086 (no section 8.13 external repository; each consumer reading 12.3.8 its own way) had been
blocked on PB2099-PB2104 and stayed open after they landed. On this branch's build the note's own repros no longer reproduce: `sr10.cob` (six
unanswerable specifiers: FUNCTION x2, PROGRAM, CLASS, INTERFACE, PROPERTY) is refused COBOL5962 at each, naming SR10 / SR14 / SR6 / SR9 / SR16 and the
places searched; `sr6.cob` (a PROGRAM specifier plus `CALL ... ON EXCEPTION`) is refused at the specifier instead of printing NOT-FOUND; an EXCLS
class, an EXB base class inherited by EXD (`INHERITS FROM EXB`), and a program prototype compiled alone (`proto_only.cob`) are all reached from
another compilation group (HELLO FROM EXCLS, EXB-HELLO, and a CALL passing PIC X(3) for the prototype's PIC 9 formal refused COBOL1688). The note was
discharged, not fixed: `git log -S` names PB2103 (slice 7, the resolver and the metadata provider) and PB2104 (slice 8, OO across assemblies).

**Rows.** One `record_verdicts` batch moved GR-12.3.8.4-10, GR-12.3.8.4-11, SR-12.3.8.3-10, SR-12.3.8.3-14, GR-8.3.2.2-L2.1, SR-11.3.3-2,
GR-11.3.4-2, GR-10.6.3-1, GR-11.6.4-1, DOC-A.1-66 and DOC-A.1-67 to CONFORMS (GAP 12 to 1; 11 of 11 accepted). Each row's notes were restated from
the probes above, its sites restated, and the DOC rows' `Pinned by` lists in `docs/CONFORMANCE.md` section 7 extended to the witnesses the inventory
names (`audit_annex_a1.py --check` reports no findings). The multi-base INHERITS half of GR-11.3.4-2 is Annex A.4.10 item 1, an optional item
documented as not claimed (PB285), so it does not hold the row open.

**New witnesses.** Goldens: `pb1086-program-specifier-no-information` (SR 12.3.8.3 14, refused COBOL5962 at 2002, 2014, 2023),
`pb1086-inherits-class-not-in-repository` (SR 11.3.3 2, COBOL0821), `pb1086_inherited_factory_data_is_distinct` (9.3.9: a subclass's factory has its
own copy of inherited data; BUMP x2 on the base and x1 on the subclass print CNT=102 and CNT=101, derived from the spec). Unit-level:
`ExternalRepositoryResolutionTests.AProgramPrototypeCompiledAlone_...` and `AFunctionPrototypeCompiledAlone_...` (10.6.3 GR1: a prototype
compiled alone publishes its signature; with update off the entry is found and cannot answer, REPO-8).

**Spec and docs.** Rendering PDF pages 336-338 settled the note's two transcription defects: 12.3.8.4 GR11 c) was run into the b) paragraph of
`specs/ISO_COBOL.md` and is now its own paragraph (the rule catalog is unchanged); SR16 a)'s "the property literal-5" is the printed standard's own
typo and stays. `DESIGN-external-repository.md` no longer says there is no repository today nor that slices 7 and 8 are open, and the stale comment in
`ProgramEmitter` that cited 10.6.3 GR1 for the opposite of what GR1 says now names the host entry as the information GR1 requires.

**PB4185.** `repository_inputs()` and `generate()` in `conformance-suite/harness/generate.py` are annotated with `Problem`. The README half had landed
with PB4455; the sweep found a stray duplicated fragment at the end of the `build` row of `DESIGN-conformance-suite.md`, removed.

**Branch review round 1 (finding R1-1): a subclass's factory shared its base factory's file connector.** The reviewer showed that GR-11.3.4-2 was
re-verdicted with "file-definition inheritance is not probed", and that 9.3.9 ("The subclass inherits all the file definitions in the same way as the data
definitions"; "Each factory object has its own copy of inherited data, distinct from the copy belonging to a factory object of an inherited class") was
violated for files: `BinderDriver.QualifyClassFiles` keyed a FACTORY file by a static literal (`Class::FACT::name`), and a subclass factory inherits the
base factory's constructor, so its creation re-registered the SAME key and replaced the base's open connector (base OPEN 00, subclass OPEN 00 and a
WRITE that answered 00 lost; the subclass's CLOSE 42). Re-probed on this build: it reproduced. Fix at the root: a non-EXTERNAL, non-SD factory file now
takes the instance-file mechanism (a class-qualified base key plus a minted `__fkey_` field, one `FileModel.InstanceKeyField`, one `FileKeyExpr` funnel),
so each factory object mints its own connector key; the same probe now answers base OPEN 00, subclass OPEN 61 (the plain-program two-FD answer), subclass
WRITE 48, CLOSE 00 / 42. The sweep: the OBJECT half already minted per object; EXTERNAL files stay one connector per run unit (13.18.22.4 GR4a); an SD keeps
its static key (its store is one SORT statement's in-memory buffer, never a host connector); every connector-key site reads `FileKeyExpr` (grep showed no
other `CsLiteral(...CobolName)` consumer). New golden `pb1086_inherited_factory_file_is_distinct` (2002/2014/2023): two factories name different files
through their own FNAME, both OPEN/WRITE/CLOSE answer 00 and each reads back its own record. GR-11.3.4-2's notes and witnesses were restated by a
`record_verdicts` batch.
