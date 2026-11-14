- **D-SORT-USE-ABNORMAL-EXIT — A USE procedure invoked while a SORT is active that does not complete normally — from the SORT's INPUT or OUTPUT
  PROCEDURE, or from a program CALLed or a method INVOKEd in their range (§2023:14.6.13.1.2 1); §2023:14.9.33.4 GR2 a), GR3;
  §2023:14.9.40.4 GR10, GR13, GR17), spec silent on where control lands**: GR17 — "If a USE procedure invoked while a format
  1 SORT statement is active does not complete normally, the SORT statement is terminated" — names every such USE
  procedure, whichever runtime element declares it: GR10/GR13 put in the procedure's range "all statements that are
  executed as the result of a transfer of control in the range" (so a CALLed program's or INVOKEd method's statements)
  and "all statements in declarative procedures that are executed as a result of the execution of statements in the
  range" (so their USE procedures, and a containing program's GLOBAL one, §2023:14.9.49.4 GR4 b)). The rejected narrower
  reading — only the USE procedures of the program instance executing the SORT — has no text behind it. §2023:14.9.33.4
  GR2 a) 1. alone would put a RESUME AT NEXT STATEMENT after the raising statement, inside the procedure (or inside the
  CALLed program). **Pinned choice: the SORT ends where it is terminated, and its whole range with it** — nothing more
  of the procedure, of an element the range activated, of the sequence phase or of the output procedure runs, and a
  RESUME AT NEXT STATEMENT (or a not-normal fall-off) lands after the SORT statement, as it does for the implicit
  transfers: GR2 a) yields "unless general rules associated with the applicable statement specify otherwise", and the
  next statement of a terminated SORT's range is no longer part of an executing statement (the rejected reading —
  continue the procedure with the SORT ended — runs RELEASE/RETURN statements that GR1 of each then refuses, and the
  procedure's inserted return would hand control back into a statement that has ended). A RESUME AT procedure-name of
  the SORT's OWN program instance is a GO TO (GR3) taken after the SORT has ended, never from inside the procedure with
  the SORT still active; one of ANOTHER element names a procedure of an activation the termination has ended, so it is
  not taken and control lands after the SORT. An activation the range entered ends as the range does: what it stored
  through a BY REFERENCE formal is the activator's (§2023:14.2.3 GR8, "the same storage area as the argument"), and an
  INITIAL program's activation ends with its implicit CANCEL (§2023:14.9.18.4 GR2). An EXIT PROGRAM or GOBACK in another
  element's USE procedure is that element's return (§2023:14.9.14.4, §2023:14.9.18.4): it returns to its activator, and the SORT is
  terminated there once the return is complete, so a CALL's or an INVOKE's RETURNING item (§2023:14.9.4.4 GR4, §2023:14.9.23.4
  GR8) holds the result, and a condition the same USE procedure staged with GOBACK … RAISING has been raised in the
  activator first (§2023:14.9.18.4 GR1 b): "after the result, if any, of the activated element is returned"), so the activator's
  own USE procedure for it runs inside the range before the SORT ends (kb/Work PB3246). A USE procedure invoked by another while the SORT is active is itself one GR17 reaches, so its
  own RESUME terminates the SORT there. **MERGE has no such rule** (§2023:14.9.24.4): a statement of a MERGE output
  procedure is its own applicable statement, and its USE procedure's RESUME AT NEXT STATEMENT continues the procedure
  and the MERGE. The ISO text controls; no GnuCOBOL latitude is taken. (kb/Work PB1177; goldens
  `2002/pb1177_sort_procedure_use_not_normal`, `2002/pb1177_sort_called_use_not_normal`,
  `2002/pb1177_invoke_sort_use_not_normal`, `2002/pb1177_sort_called_goback_ec_free`,
  `2002/pb1177_invoke_self_new_raising`, `2002/pb3246_sort_terminated_after_raising_pickup`.)
