- **D-DECLARATIVE-NORMAL-COMPLETION — Normal completion of a declarative — "a fatal exception occurs within the scope of the declarative" (§2023:14.6.13.1.2 1),
  spec silent on three points)**: the rule makes a declarative that falls off its end "not complete normally" when a
  fatal exception occurs within its scope. **Pinned choices: (a) "occurs" is read as "is RAISED"** — checking for it is
  enabled (§2023:14.6.13.1.1: with checking off nothing is set to exist, so nothing occurs); **(b) the scope is DYNAMIC and
  run-unit wide** — a fatal raised in a procedure the declarative PERFORMs or a program it CALLs is within it, and the
  mark reaches every open declarative; **(c) a RESUME that §2023:14.9.33.4 GR1 made a CONTINUE marks only the open
  declaratives of ITS OWN program instance** ("specified in this … program"). The disposition after a declarative falls
  off its end is by FATALITY, not by how it completed, wherever a rule does not key on normal completion (§2023:14.9.49.4
  GR13, GR7 b)/c), GR12 b)/c)), so only a USE procedure invoked while a format 1 SORT is active (§2023:14.9.40.4 GR17,
  kb/Work PB1177) and the MERGE (§2023:14.9.24.4 GR7, GR12) implicit transfers tell a not-normal fall-off from a normal one. **(d) A Format 4 (EXCEPTION OBJECT) declarative's not-normal fall-off is
  treated as normal**: §2023:14.6.13.1.5 names only the normal case ("completes normally, execution continues as specified
  in the activating statement for normal execution"). Witnesses `conformance:cases/14.6.13.1.2-Normal-completion-of-a-declarative-procedure/pb1122_declarative_fatal_in_scope_merge`
  (a MERGE GIVING file whose implicit OPEN fails: a declarative that completes normally BYPASSES the file, one that
  RESUMEs, fell off after a handled fatal, or PERFORMed a procedure whose RESUME §2023:14.9.33.4 GR1 made a CONTINUE
  TERMINATES the MERGE) and `unit:DeclarativeActivationRecordTests`.
  (kb/Work PB1122.)
