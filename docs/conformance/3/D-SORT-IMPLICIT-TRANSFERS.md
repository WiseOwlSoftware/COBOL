- **D-SORT-IMPLICIT-TRANSFERS — The SORT/MERGE implicit transfers after an unsuccessful as-if OPEN, READ, WRITE or CLOSE (§2023:14.9.40.4 GR12,
  GR15, GR17; §2023:14.9.24.4 GR7, GR12; §2023:9.1.13.1; §2023:14.6.13.1.3 2))**: the USE procedure runs first, then the verb's
  rule disposes of the status — **terminate the statement, bypass the file, or continue** — from ONE table
  (`SortEmitter.RuleFor`). Four points the standard leaves to a reading: **(a) a FATAL status the verb's rules do
  not name terminates the statement.** §2023:9.1.13.1: if the implementor continues the run unit (WiseOwl COBOL does, with
  checking off, when the file has a FILE STATUS clause or a USE procedure applies — A.1 item 103 in §7; with neither,
  the as-if statement ends the run unit exactly as the explicit one does, before this table is read, kb/Work PB2700), "control is transferred to the end of the statement that produced the fatal exception condition
  unless the rules for that statement define other behavior" — and the statement is the SORT/MERGE, since the
  as-if statement is not one the program wrote (§2023:14.9.33.4 GR2 a) 1.). With EC-I-O checking ON the same holds:
  §2023:14.6.13.1.3 2) ("If the executed statement is a MERGE or SORT statement, then the rules for those statements
  apply") precedes the run-unit termination of 5)/7). So a SORT whose USING file is missing ('35') or whose GIVING
  write fails '48'/'71' ends there, where it used to run every remaining implicit function against a connector that
  was not open. **(b) SORT GR12 b)'s "If a fatal exception condition exists for file-name-1"** reads as the USING
  file (file-name-2): the sort file has no I-O status, and the same paragraph's "at end condition exists for
  file-name-1" has the same slip; the reading agrees with (a) either way. **(c) "an exception condition exists"
  means an UNSUCCESSFUL status** (first character not '0'): a successful '0x' — an OPTIONAL file's '05' — runs no USE
  procedure (§2023:14.9.49.4 GR6) and leaves the connector usable, so it never terminates a transfer. **(d) a terminated
  statement performs NONE of its remaining implicit functions** — "control is transferred to the end of the
  statement" — so a GIVING file whose write terminated the statement is still OPEN afterwards and the program may
  CLOSE it; a file whose OPEN failed was never opened. (Rejected: closing every file the transfer opened at the
  landing — the standard asks for that only for MERGE GR6's sequence error, "all files associated with the MERGE
  statement are closed", which shows it says so when it means it.) The specific rules that override (a) are MERGE
  GR7 a) (a nonfatal USING OPEN terminates unless a USE procedure completes normally), GR12 a) (a fatal GIVING OPEN
  followed by a USE that completes normally BYPASSES that file — the other GIVING files are still written), GR12 b)
  (any WRITE exception continues only after a USE that completes normally) and both verbs' write-boundary paragraph
  (the file is closed and the statement goes on). (kb/Work PB993; golden `2002/pb993_sort_merge_transfer_termination`.)
