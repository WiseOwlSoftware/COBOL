## PB2403 step 2: the backend reads the OO answers as bound data, and no emitter names WiseOwl.Cobol.Compiler.Oo

Wave 20261854 A carried the predecessor branches forward: the 13 unlanded commits of w20261852a (PB4319, PB2398, PB2401,
PB2402, PB2182, PB4517 and PB2403 step 1) were cherry-picked onto main c1f649345, and w20261852d's PB4512 branch was merged
with the merge drivers. The carried head gated GREEN (run 000001) before any new work.

Step 2 of PB2403 ends the backend's reads of the class table and of the binder's description rules. The emitters asked
`OoClassTable` (through `BoundCompilation.OoClasses` and `EcState.OoClasses`), `ActivationDescriptions` and
`OoConformance.SignatureDifference` at emit time, so a second backend would have needed the binder to describe a formal.
Now:
- `BoundCompilation` carries the defined interfaces (`Interfaces`) and the imported types (`Imported`, a new model record
  `ImportedTypes` whose `Assemblies` replaced `OoClassTable.ImportedAssemblies`) instead of the table.
- `BackendAnswers.Write` runs at the end of `BinderDriver.Bind`, beside the repository records, and writes each answer onto
  the node the backend already reads: a unit's formal and RETURNING descriptions (for a unit that checks
  EC-PROGRAM-ARG-MISMATCH or publishes a host entry), the host entry's pairs of formals with one description, a method's
  descriptions (for a method with a universal dispatch case), the RAISING phrase's classes and interfaces resolved to
  symbols (`RaisingObject`), and an EXTERNAL file's §12.4.5.3 GR1 identity.
- `OoEmitter.SelectFingerprint` moved to the binder as `FileEntryIdentity.Of`, and its result is `FileModel.SelectFingerprint`.
- A missing answer is a loud internal error in the emitter, never a silent default.

The behavior-neutrality oracle compared the new tree with the carried head: `ARCH-ORACLE: IDENTICAL — 16527 cases`. The
architecture walk now reads zero for `CodeGen → Compiler.Oo` and `Binding.Model → Compiler.Oo`, and the edges file is
ratcheted with a census recorded in this landing, so both tolerated rows are deleted and any new use is a layering red. The
`OoNameResolutionDriftTests` adjudication of the two RAISING re-lookups moved with them to `BackendAnswers`.
`ObjectReferenceIdentityDescriptionTests` gained a check that the bound descriptions are the builder's answer.
DESIGN-backend-abstraction §1.3 L6 and DESIGN-architecture-review §8 record the step as done.

PB2403 is landed with `closes_rows: []` (a behavior-neutral extract). PB2293, PB2361 and PB4273 remain open: the backend
input still carries `DataBinder` (PB2361's `DataBindResult`), and the assembly split (PB4273) waits for that.

Self-review (architecture, full code, performance, duplication): one writer of the answers and one reader per answer; every
answer the emitter needs and lacks throws an internal error; each method's descriptions are now built once at bind rather
than three times per formal at emit; `ImportedAssemblies` moved, not copied. A semgrep verify run under the gate's CPU load
reported `DiagnosticCatalog.cs` unreadable; rerun alone it passed (no rule count grew). Gate on a09065bca:
`=== BUILD-LOCAL GATE: GREEN — Conformance 21,463/21,463 · Unit 39,670/39,670 · Characterization 37/37 cases ran (skipped 0) ... (run 000002-20261104T021646Z) ===`.
