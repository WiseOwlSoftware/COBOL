## Five filed notes named sites that do not exist, and why `work.py check` passed them (PB4654)

The enqueue unit's clerk filed PB4616 to PB4653 from the reports of 33 landed branches. Looking up every path and member
in those notes' Sites paragraphs found five wrong, each corrected here:

- PB4630: the function is `guard_files_apply`. The name `guard_files_verdict` came from the review lead and appears
  nowhere else in the tree. The lead itself stands: the comment at `scripts/guard-verdict.sh:233` still says the basis
  drops CR and the spaces before it.
- PB4640: `ArithmeticEmitter.cs` is under `CodeGen/Verbs/`, not `CodeGen/Emit/`.
- PB4649: `WordCharacterScreen.cs` is under `Frontend/Parsing/`, not `Frontend/Validation/`.
- PB4650: `ExpansionPlacementPass.cs` is under `Binding/Repository/`.
- PB4652: the SET arm's member is `OoBinder.OoBindSetObjectRef`; `SetObjectReference` does not exist.

`work.py check` passed all five for two reasons, recorded in PB4654: `sites_problems` holds only a note with a cluster or
in a campaign, and these have `cluster: []`; and a path it cannot find reads as a file the change creates. The check that
found them was a throwaway script in the unit's scratch directory, so nothing in the tree runs it yet.

The other 33 notes passed that script, whose member test is a substring search. The notes' repros were not re-run.
