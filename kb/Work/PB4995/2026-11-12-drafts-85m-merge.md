**Addendum (2026-11-12, branch witnesses-85m, merging origin/main): one of the 13 placed tests was already main's.**

`git merge origin/main` stopped on an add/add conflict in
`conformance-suite/cases/VI.6.3.1@1985-Simple-Conditions/1985-GR-VI_6_3_1_2-5.{cob,out}`. Both sides had added a witness
for `1985-GR-VI.6.3.1.2-5` under the name `place_witnesses.py` gives it (`test_id`). Main's test came from the PB5122
landing (the WIP checkpoint 2c76c029cd, merged with witnesses-85n, PR #1575). PB5122 placed the slice-04 draft as a
witness of its fix. This branch's test was the draft-85m draft.

**Both expected outputs follow the text.** I checked every line of each program against `§1985:VI.6.3.1.2 GR5`, checked
with `cite.py --check`. The rule says "An operand is in conformance to class-name-1, if it consists entirely of the
characters listed in the definition of class-name-1", "If identifier-1 is a function-identifier, it must reference an
alphanumeric function", "When the class condition does not include the word NOT and identifier-1 is a zero-length group
item, the result of the class test is always false" and, with NOT, "always true".
- Main's 28 lines cover NUMERIC with and without a sign (SIGN LEADING SEPARATE: `+12` and `-12` true, ` 12` and `012`
  false; an unsigned item holding `+` false), ALPHABETIC, -LOWER and -UPPER, the class-name VOWEL (a space outside the
  class makes it false), a numeric-edited operand tested by content, and the zero-length group (F T F T F).
- The branch's 15 lines cover the zero-length group (F T F T F), the same group at length 3 holding `ABC`, NUMERIC on
  `PIC 9(3)` and on a SIGN LEADING SEPARATE item, the THRU class-name HEXDIG (`1F` true, `1G` false), and an
  alphanumeric function operand (`FUNCTION UPPER-CASE (W3) ALPHABETIC-UPPER` true).
The zero-length results agree. Neither test is wrong. They witness different cases of the rule.

**Decided: main's test stays and the branch's draft is dropped.** `place_witnesses.py` places one test per rule. It has
no name for a second test of a rule. A second case with different text is refused, or replaces the first with
`--replace` (its `place`, "A rule is placed once"). The store's `test-ref` is a single path. So the branch cannot keep a
distinct second test under the tool's convention, and it does not overwrite a landed test. The merge takes main's
`.cob` and `.out` byte for byte.

**The record stands.** Main had placed the test but never recorded it: main's store file for `1985-GR-VI.6.3.1.2-5` has
no `test-ref`. The branch's witness-only record names the same path,
`conformance:cases/VI.6.3.1@1985-Simple-Conditions/1985-GR-VI_6_3_1_2-5`, and that path now holds main's test. The
record is consistent with the files, so no store file changed in the merge.

**Counts now.** This branch places 12 tests (10 output, 2 refusal) and records 13 witness-only test-refs. One of those
test-refs names main's test. The six mismatches and the notes filed for them are unchanged. The 1985 GAP is still 321.

**Left uncovered (a finding against main's test, not a defect).** No conformance test exercises GR5's function-identifier
sentence at 85: no alphanumeric function as identifier-1 of a class condition. The branch's dropped draft did, and it
ran MATCH on 09ce7dc601. Its text is in `E:/COBOL-coord/scratch/draft-85m/`. Adding it needs either a second-test name
in `place_witnesses.py` or an extension of main's test by its owner.
