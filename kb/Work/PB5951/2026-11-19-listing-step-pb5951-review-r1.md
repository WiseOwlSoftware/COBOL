## Review round 1 of listing step pb5951: how each finding was fixed

- R1-1 (BLOCKING, a closing line at a stretch's end printed at the top of the NEXT statement's stretch): fixed at its root in
  `ExceptionRegionLayout.Place`, which now gives each line to the stretch whose code it bounds: an opening line at X to the stretch
  holding X (`Start <= X < End`), a closing line at X to the stretch holding the block's last instruction (`Start < X <= End`); a line
  no stretch owns that way still prints in the next stretch. The body-end special case falls out of the rule and is gone; each
  stretch's `Depth` is now the running depth the earlier stretches' lines leave (the blocks containing its start when the stretch
  before ends there), so `DepthBefore` and `FirstAtOrAfter` are deleted. The reviewer's probe (regions.cob: OPEN, ADD ON SIZE ERROR,
  CLOSE) now prints each statement's `finally` and nested catches opened and closed under that statement. Siblings swept: the
  instruction stretches (`ObjectCodeMapReader`, half-open by instruction offset) and the merge (`ObjectCodeText.CodeLines`, a line
  past the last instruction prints after it) already agree with the rule; no other placement exists. Docs corrected: DESIGN-listing.md
  section 6.9 "Placed in one stretch" (with the correction recorded) and its witnesses item, `ListingObjectRange.Regions`/`Depth` docs
  and the regenerated `listing-v1.schema.json`, user guide `listing.md`. Witnesses: Unit
  `ExceptionRegionLayoutTests.ABlockClosesInTheStretchHoldingItsLastInstruction` (a block inside one stretch, one ending at a stretch
  boundary, one spanning it) and Conformance `ListingExceptionRegionTests` (each line in the stretch whose code it bounds; each
  directly-following stretch's depth from the blocks containing its start); with the old rule restored both were red
  (`__Dispatch: the HandlerEnd line at IL_0064 prints in IL_0064-IL_00BA`), restored green.
