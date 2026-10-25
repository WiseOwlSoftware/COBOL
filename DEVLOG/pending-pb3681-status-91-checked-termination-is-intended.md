## PB3681 retired: a WRITE refused with I-O status 91 ends the run unit only under EC-I-O checking, which is the rule, and now has witnesses

The note reported that `pb547a.cob` (a WRITE of a record holding a character above U+00FF) ends with `EC-I-O-IMP (fatal):
I-O status 91` although the program declares a FILE STATUS item. Re-probed on this worktree's own build: the program is
right to end, because its source carries `>>TURN EC-ALL CHECKING ON`. Without the directive the same WRITE stores `91` in
the FILE STATUS item and the run unit continues (`FS=91`); a control program with a plain `'48'` WRITE under the same
directive terminates identically. So the behavior is the checking-enabled arm of Annex A.1 item 103, already
determined in `docs/CONFORMANCE.md` DOC-A.1-103: `'91'` is fatal (a '9' first digit the implementor defines as fatal,
ISO §9.1.13.1, DOC-A.1-110), its condition is EC-I-O-IMP, and with checking enabled and no PERFORM WHEN phrase, USE
declarative or PROPAGATE directive applicable §14.6.13.1.3 rule 7 ends the run unit abnormally, "even when the file has a
FILE STATUS clause". Rule 3 of that clause lets the implementor specify otherwise; DOC-A.1-103 declines to, and follows
GnuCOBOL. Citations re-run through `cite.py --check`: 9.1.13.1, 9.1.13.11 1), 14.6.13.1.3 7), all OK.

No product code changes: the status 91 arms in `SequentialConnector.cs` only set the status; the disposition is the
generated after-hook (`SequentialIoEmitter.EmitUseHook`) and `FileRegistry.IoConditionName`, shared with every other
organization and every other fatal status. The gap the note exposed was a missing witness: nothing pinned `'91'` under
both checking settings. Added `DocA1Item103WitnessTests.CharacterWithoutByteImage_Status91_WithFileStatusClause_Continues`
(unchecked, `FS=91`, continues) and `..._CheckingEnabled_TerminatesAbnormally` (`>>TURN EC-I-O-IMP CHECKING ON`,
terminates), and listed both in DOC-A.1-103's witness column. The note is `retired` (no inventory row claimed or closed).
