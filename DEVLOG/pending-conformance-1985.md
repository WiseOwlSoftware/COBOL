## The COBOL-85 conformance statement: section 9 of CONFORMANCE.md, computed from the 1985 rows (PB4997, PB4885)

**What.** `--std 85` now has the conformance statement §1985:I.1.5 asks for: `docs/conformance/9/`. The claim is the owner's
decision PB4885: the high subset of ANSI X3.23-1985 with the highest level of every optional module. On the amended text that
means five modules, so the claim includes Intrinsic Function. Everything the statement says the compiler meets is computed
from the 1985 view on every render. `scripts/spec/conformance_1985.py` builds the module table, every GAP row (none) and
the 18 obsolete elements of §1985:C.1 with the rules each one marks. At this head the 1985 view is 2,275 rules: 2,275 OK,
0 GAP, 902 closed by their own verdict, 1,373 closed by a KEPT chain to 2023, and 14 the claim does not support. Section
9.1 names those 14 rules with their grounds: ENTER is not included (§1985:I.1.5.2.5.5); the reel and STANDARD-1 rules need
hardware WiseOwl COBOL does not support (§1985:I.1.5.2.5.1); SYNCHRONIZED GR10 is implementor latitude; and the
CURRENT-DATE and WHEN-COMPILED rows carry D-UTC-OFFSET, the one stated departure. Section 9.2 specifies the hardware
components item by item from §1985:C.3, and section 9.3 covers the character set.

**How it stays true.** The computed text is not a second rendered file. A new reader mechanism,
`conformance_source.GENERATORS` (an index's `generated:` frontmatter), appends it to section 9's preamble, so the rendered
view, the package's copy and every reader see one document. The generator's findings are the sources' findings:
`conformance_doc.py --check` (the RENDERED VIEWS and DOC AUDITS audits) fails when section 9.1 and the rules the claim
does not support differ in either direction, or when a 9.1 row's citation or subject is not its rule's.

**The object time switch (§1985:XV.1.3.4).** "Recompilation of the source program is not required to provide or take away
this facility", yet only a .NET host could turn `RunUnit.DebugMode` off: a user running the program `cobol` built could
not. The switch now reads `WISEOWL_COBOL_DEBUG` at its first use in the run unit (ON or unset is on, OFF is off, any other
value stops the run and names the variable), unless a host has set it. It is registered in `RuntimeConfig` and documented
in D-DEBUG-SWITCH-85. `DebugSwitchTests` runs one compiled program with the switch unset, on, off and unreadable; the
expected values come from §1985:XV.1.3.4 and §1985:XV.3.2.4 GR3a.

**The seven 1985 sentences the prose campaign left UNDECIDED** for want of a claim now have verdicts (kb/Work
PB4997's addendum). §1985:I.1.5.2 s1 and s2, §1985:I.1.5.2.5.1 s1 and §1985:IV.4.1 s2 conform. §1985:I.1.5.2 s3 and
§1985:I.1.5.2.2 s1 conform with the one stated D-UTC-OFFSET departure. §1985:XV.1.3.4 s2 conforms after the fix above.
One requirement is still unmet: the obsolete-element warning (§1985:I.1.5.2.3) is not on main, and the statement says so.
That sentence is computed from the construct catalogue, so it becomes the list of warned constructs when PB5986 lands.
