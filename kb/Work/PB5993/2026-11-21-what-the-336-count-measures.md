# PB5993, 2026-11-21: what the 336-code count measures, and the measurement still owed (review R1-1 of wave 20262118)

The finisher's re-measure (addendum `2026-11-21-split-codes-left.md`) compiled at `--std 85` every suite case accepted only from
2002 and every refusal not refused at 85, and counted 336 diagnostic codes that cite only a later edition. That population is
mostly programs 1985 cannot express (object orientation, exception checking, directives, CONSTANT entries, TYPEDEF, the 2002 and
later intrinsic functions). A later-edition citation on such a program is not a wrong-edition rule on a 1985 program, so **336 is an
upper bound on this note's class, not its size.** The class this note owns is: a diagnostic, at `--std 85`, on a program whose
constructs all exist in 1985, that cites only a later edition's rule.

**Owed next (the note's next step, before any further code is changed):** split the 336 by reachability from a legal-at-85 program.
For each code, either (a) a program of 1985 constructs only reaches it at `--std 85` (it belongs to this note; fix its citation
from the 1985 text), or (b) it is reachable only through a construct the edition gate refuses first at 85 (COBOL0900 family; record
`No1985Rule` with that reason, no citation change). Move the measuring script (`cite85.py`, now in the coordination scratch) into
the repository as a drift check that fails on any class (a) code, so the measurement is reproducible from the tree.

Also from the review, not fixed here: the pb742 golden's loosened expectation no longer pins either edition's wording (lead R1-2);
COBOL1858 at 85 says "ISO" followed by a 1985 locator where 1985 is ANSI X3.23-1985 (FileModel.cs:328; the pb5993-access-random golden pins it);
COBOL2198's message now prints the usage word at every edition, a cosmetic change beyond the 1985 arm.
