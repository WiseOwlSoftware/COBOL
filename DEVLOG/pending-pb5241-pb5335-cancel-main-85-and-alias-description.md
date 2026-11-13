## PB5241, PB5335: a COBOL-85 CANCEL of the main program takes no action, and the code generator asks an operand's description

**PB5241.** Re-probed: `K2.cob` at `--std 85` (a main program that CALLs a contained one and then CANCELs itself) printed
`SUBX 1` and ended the run unit abnormally with EC-PROGRAM-CANCEL-ACTIVE. §2023:14.9.5.4 GR5 and §2002:14.8.5.3 GR5 refuse a
program "in the active state"; §1985:X.5.3.4 GR5 forbids only a program "that has been called and has not yet executed an EXIT
PROGRAM statement", and the main program was never called, so §1985:X.5.3.4 GR8's "No action is taken" governs at 85. It is a
behaviour change, so it is a `DialectBehavior` member, `CancelRefusesEveryActiveProgram` (from 2002, VCR row 7.66). The binder
folds it into `BoundCancel.RefusesEveryActiveProgram`; `CallEmitter.EmitCancel` passes it to `ProgramTable.Cancel`; `CancelNode`
asks `Active > 0` from 2002 and, at 85, the activations a CALL entered (`Node.MainActive` marks the main one). The main
activation no longer sets the GR7 "called" mark, since no CALL entered it. A host's `Cancel()` and the INITIAL program's implicit
cancel pass the 2002 test. The runtime's public surface changed, so the runtime major is 27. Witnesses: the 1985 X.5.3 CANCEL
folder's `pb5241_cancel_main_program_85` (`SUB 1`, `AFTER-SELF-CANCEL M=1`, `SUB 2`: no action, the contained program not
cancelled either), `14.9.5-CANCEL/pb5241_cancel_main_program_active` (2002 2014 2023: the raise, taken by a declarative) and
`CancelCascadeOrderTests` (the main program's CANCEL under each rule through `RunMain`'s exit status; a called program not
exited refused under both).

**PB5335.** The probe the analysis owed: an elementary level-66 alias answers every attribute read right (its entry gets
data-name-2's PICTURE), but a GROUP alias's own entry has no members, and the backend asked `Place.DenotedItem`, the identity,
for attributes. A CALL by data-name stated no length, §2023:8.5.1.12 atoms or run-time description for `GT-ALIAS`, so under
EC-PROGRAM-ARG-MISMATCH checking an eight-byte group formal raised the condition for `GT` and not for `GT-ALIAS`, and the callee
read past the six-byte argument (measured with the old backend restored). `Place.DescribedItem` is now the one reader of an
operand's description (`Item`, or null for a reference-modified view); `MoveTable16.OperandItem(Place)`, the binder's private
copy, is deleted; every `DenotedItem` read in `CodeGen/` and `Oo/ActivationDescriptions.cs` reads it, and
`PlaceDenotedItemDriftTests.TheBackendAsksTheDescriptionNotTheIdentity` keeps it so. Witness:
`14.8.2.2-Group-items/pb5335_alias_group_argument_length` (2002 2014 2023).

Found on the way, not fixed (outside the group): an INVOKE's BY CONTENT phrase is not transitive, so `INVOKE C "M" USING BY
CONTENT G N` passes N by reference and the method's store reaches the caller.

Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance up to date (run 000002-20261113T005546Z ...) · Unit 40,821/40,821 ran ·
Characterization 37/37 ran (skipped 0) ... (run 000003-20261113T010745Z) ===`, after two reds fixed at their roots: the citation
audit (ProgramTable's CANCEL and GOBACK citations now name §2023:14.9.5.4 and §2023:14.9.18.4) and the 14 emitted-C# snapshots,
whose only difference is the host entry's `Cancel()` passing `refusesEveryActiveProgram: true`.
