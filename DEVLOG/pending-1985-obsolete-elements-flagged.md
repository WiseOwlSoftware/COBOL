## The 1985 obsolete elements are flagged and documented (PB5986), and two leads ruled (PB5992)

§1985:I.1.5.2.3 requires an implementation to support the obsolete elements of §1985:C.1, to identify them in its documentation and
to provide a warning for their use. At `--std 85` none of the 18 rows that stand for them carried `ObsoleteIn`, so COBOL0903 never
fired and no user document listed them (re-probe at 8244f1aa4b: AUTHOR, MEMORY SIZE and ALTER compiled silent).

The fix is structural. Setting `ObsoleteIn` to 85 alone would have broken the sites that ask whether a 1985 element exists,
because `ConstructRegistry.IsAvailable` read `StatusAt == Available` and an obsolete element is not that. `ConstructDialectStatus.IsInForceAt`
(Available or Obsolete) is now the one spelling of "in force", used by `IsAvailable` and by the two directive-catalogue readers that
had spelled its complement by hand. The 18 rows carry `obsoleteIn: 85` and cite their item; the two debugging rows move from 2002 to 85,
as item 18 states (the owner decision R61 had followed GnuCOBOL's edge). A USE FOR DEBUGGING section that a unit without WITH DEBUGGING
MODE makes comment never reached a parse, so `DebuggingLineRewriter.Rewrite` now returns the hidden sections' headers and the frontend
reports the first through the registry's verdict. ENTER stays refused at every edition (COBOL7240) and has no warning row.

`docs/manual/user-guide/editions.md` gains the table of the 18 items; the doc audit `scripts/docchecks/manual.py` holds it to the registry in both
directions, and the version matrix's obsolete cells now cover each row at 85 (the full Conformance population is the gate's run).

PB5992: the contained program named like its container conforms (§1985:X.1.3.8.1 reads the same as §2023:8.4.6.3); a SEGMENT-LIMIT
in a program with no sections is accepted by the text's silence on what makes segmentation "used", recorded in docs/conformance/3.8.
Neither is a defect.

No conformance-suite golden: the 1985 catalog has no rule id for I.1.5.2.3 yet (PB5924).
