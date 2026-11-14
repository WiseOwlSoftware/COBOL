- **D-RETURN-TO-NON-COBOL — Return to a NON-COBOL activating runtime element (§2023:14.9.18.4 GR1 a) — "If the activating runtime element is a
  non-COBOL element, execution continues in the activating element in an implementor-defined fashion"; the
  §2023:14.9.14.4 GR3 EXIT PROGRAM twin reads through to the same rule)**: **an ordinary managed return, and no
  exception condition is raised in the activator.** WiseOwl COBOL defines no §2023:7.3.9 call-convention-name (only `>>CALL-CONVENTION COBOL`), so a
  non-COBOL element cannot be the TARGET of a CALL (see the A.1 item 19 determination in §7); the only non-COBOL
  ACTIVATOR it can have is a .NET host that activates a program through the program's generated host entry,
  `Cobol.<assembly>.<NAME>.Call(...)` (the program classes themselves are internal) — A.1 items 65 and 141 in §7. For
  that host the activation is a plain method call: the activation catches the `ProgramReturn` the GOBACK throws and
  returns, the PROCEDURE DIVISION header RETURNING item's value becomes `Call`'s return value exactly as it is delivered
  to a COBOL activator (GR2), and the host resumes at its own call site. A RAISING phrase stages its condition and nothing consumes it: a non-COBOL element enables
  checking for nothing, so GR1 b)'s test — "if checking for that exception condition is enabled in the activating
  runtime element" — is false, and the host runs no pickup, so the staged condition is never raised, fatal or
  not (a COBOL pickup takes a staging only in the activation the staging names — kb/Work PB892). No COBOL exception condition, and no .NET exception, crosses into a non-COBOL
  activator. (kb/Work PB408.)
