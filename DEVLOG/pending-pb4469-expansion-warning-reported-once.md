## A placement-(a) expansion's warning is reported once, and both placements position it at the EXPANDS entry (PB4469)

**Re-probe.** A library whose skeleton holds `IF 1 = 1 DISPLAY "A".` (COBOL3146, a relation of two literals, ISO §8.8.4.2.1) and a client
that expands it over a class of another assembly (placement (a)): the client's build printed the warning at `CLIENT1.cob(8,438)` and again at
`CLIENT1.cob(8,5) ... [in the expansion 'HOLD-BOX']`. The same text expanded over a class of the client's own group (placement (b)) printed it
once, at the column of the recorded text on the entry line (the 438), where the docs said arm (b) positions "exactly" as arm (a) does, at the
entry's column.

**Root cause.** Two owners of one text, not the string form of `CompilerDriver.Result`. The group binds every expansion's text to type its
callers; an expansion placed in an assembly of its own is also compiled apart, under the state its skeleton was published with, and that
compilation reports the same warnings (PB4457). A structured diagnostic list would only have let the driver recognise the pair after the fact,
and would still have kept the group's report of the text under the group's state.

**Fix.** The compilation that compiles the text answers for it.
- `ExpansionToken` (a `CommonToken` that names its expansion's `DiagnosticRegion`) is every token of an expansion's text
  (`OoExpansion.SubstitutedTokens`); `DiagnosticCursor` gained `Region`, read off the token by `DiagnosticCursorAt.At`/`CursorOf` and kept by
  `DiagnosticCursorExtensions.At(DiagnosticCursor)`, so a post-build pass that replays a captured cursor attributes as the bind did and no binder
  pass had to be touched.
- `EditionContext.Warning` remembers which regions raised each warning line (a line the user's own text, or a second expansion still standing, also
  raised is not withdrawable); `ExpansionPlacementPass` calls `WithdrawWarningsOf` for each `OwnAssembly` expansion. Errors are never withdrawn.
- One column rule: the tokens of an expansion of a skeleton read from the repository take the EXPANDS entry's line and column, the position
  `ExpansionAssemblies.AtTheEntry` relabels the sub-compilation to. `ExpansionAssemblies.Write`, `ExpansionToWrite.RequestedAt` and
  DESIGN-external-repository §10.5 now say what the code does.

**Goldens.** None in the conformance suite: a diagnostic's position is implementor-defined output, not an ISO rule, so the witnesses are Unit tests:
`ExternalRepositoryExpansionTests` (arm (a): the warning once, at `(8,5)`, naming the expansion; arm (b): once, at the entry's column) and
`DiagnosticRegionTests` (token to region, replayed cursor keeps it, an unset cursor leaves it, a warning raised by user text or a second expansion
stays, errors stay, and no `new CommonToken(` in `OoExpansion.cs`). No inventory row is owned; `closes_rows: []` with its reason.

**Sibling sweep.** The only caller of `CompilerDriver.Compile` for expansion text is `ExpansionAssemblies.Write`; every writer of
`EditionContext.Warnings` is `Warning` (grep pasted in the report). An IMPORTED expansion's text is reported by the group alone (nothing else compiles
it in that run): left as it was, and registered as a lead.
