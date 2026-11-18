## Step 4a built (branch `listing-step-4a`, w20262104d)

Step 4a of `docs/rearchitecture/DESIGN-listing.md` (the directive hooks, witnessed at 2023) is built:

- The conditional-compilation driver holds the LISTING state (ON by default, §2023:7.3.18.3 GR2), carried by its
  `DirectiveStateStack` so a PUSH saves it and a POP restores it, restored at the end of each library text (§2023:7.2.3.4 GR5),
  and records every change into the trace (`TextListingSwitch`: a LISTING directive, a written POP, the implicit POP ALL of an
  exception-checking PERFORM, an inclusion's end). `DirectiveStateRegistry`'s LISTING row names the driver.
- The trace also records each PAGE directive, each DISPLAY transfer to the listing, each COPY statement's SUPPRESS phrase and
  every line a replacing action matched (COPY REPLACING by inclusion, REPLACE by resultant line). `CompileOutputStream.Listing`
  is the DISPLAY directive's LISTING destination; with a listing the transfer goes into the listing, without one to the
  compiler's standard output.
- The model gains `ListingDocument.Displays` (`ListingDisplay`), `ListingInclusion.Suppress` and `ListingReplacement.Replacing`;
  the schema was regenerated (still 1.0, design section 7.3). `DirectiveEffectsBuilder` places every recorded fact through the
  one index; `ListingVisibility` replays the driver's state. The text listing marks a changed line 'R' with its compiled text
  under it, and prints `DISPLAY: text` at its directive.
- Records: the seven 2023 rows that rested on "no listing" (GR-7.3.18.3-2 to -5, GR-7.3.19.4-2, GR-7.2.3.4-4, -5) are
  witnessed by `Listing2023Tests` and their signed derivations withdrawn; DOC-A.1-200 is CONFORMS on a "Provided"
  determination; DOC-A.1-53, -54, -117, GR-7.3.12.4-1, -2, -5, -6, GR-7.3.18.3-1, GR-7.3.19.4-3 and FMT-7.3.18.2 restated.

The design was corrected where the code proved it wrong, each correction marked "(step 4a)": the LISTING state has one holder
(the driver), because PUSH / POP and the GR14 implicit ops change it and the GR5 restore must be the driver's too; every fact
carries the inclusion open when the stage met it; `CompileOutputLine` gains no origin (the trace carries the transfer); the 'R'
text is the resultant text placed by the one index.

The lead `2026-11-18-text-stage-diagnostic-placement.md` stands: step 4a carries the inclusion on the trace's own facts, not on
a text stage's diagnostics, whose location still names no inclusion. Step 4b (2002 and 2014) is next, after this step lands.
