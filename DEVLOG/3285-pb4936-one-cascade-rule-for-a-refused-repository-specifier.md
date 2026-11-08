## Entry 3285 — 2026-11-07 17:35 PST — PB4936 - one cascade rule for a refused REPOSITORY specifier, and a backstop that still checks

Two leads from the PB4822 review. R1-1: a reference through a function specifier whose AS literal-5 was refused was silent (PB4822), but a CALL, CANCEL, CALL AS or SET ... ADDRESS OF PROGRAM through a program specifier whose AS literal-3 was refused drew a second COBOL1760 or COBOL2157 repeating SR2 (PB4708). R1-3: `NoteCauseReportedEarlier` raised `ErrorsRecorded` with no Error call and no check that one existed, so a wrong caller turned off the PB1029 backstop in `NoteRefusal`.

Re-probe: both reproduced from the code paths and from the PB4708 tests, which asserted the second diagnostic.

Fix: `EditionContext` holds a ledger of refusals reported at the specifier (`NoteSpecifierRefusalReported(RefusedSpecifierKind, word)`, recorded by the two REPOSITORY binding sites after `BindSpecifierExternalizedName` returns null). `NoteCauseReportedEarlier(kind, word)` now returns whether the ledger holds one and raises the counter only then; a false answer sends the caller on to report. `ConfigurationSlice.IsRefusedProgramReference` and `IsRefusedFunctionReference` are the one question per kind; `UdfBinder`, `CallBinder.ResolvePrototype` and the ADDRESS OF PROGRAM operand ask it. The literal-3 sentence of `WhyNoPrototype` is deleted, since nothing reaches it. The REPO-1 and repository-finding routes still draw COBOL1760 with their route sentence.

Tests: a theory over kinds in `ExternalRepositoryResolutionTests` (the next kind is a row), the PB4708 literal-3 facts rewritten to expect only COBOL1761, and a unit test of the ledger. No inventory row is claimed or closed.

