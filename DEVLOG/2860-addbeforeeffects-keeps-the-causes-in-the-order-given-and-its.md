## Entry 2860 — 2026-10-31 10:40 PDT — AddBeforeEffects keeps the causes in the order given, and its cross-file arm is pinned (PB4200)

**Re-probe.** On the build at 1625eab0f, `CAFÉ€` at COBOL-85 in a program whose later statement does not parse printed COBOL2773, COBOL0900, COBOL0001: the two screen diagnoses at the word's position reversed (the screen makes COBOL0900 first, and the binder arm, which reports in the order made, prints it first). The cause was `DiagnosticBag.AddBeforeEffects`, which scanned the live list from `from` with a `>=` tie test, so the second cause met the first cause it had just inserted, tied with it, and went in front of it.

**Fix.** A cause is never an effect of another cause of the same call. Each cause's place is now decided against the diagnostics the bag held on entry, and the causes are merged in one stable pass, so causes that share an effect keep the order given and the list is rebuilt once instead of insert-shifted per cause. `Frontend.LexAndParseOnce` is the only caller; no other site inserts into a bag (grep `AddBeforeEffects`, `_diagnostics.Insert`). The doc comment states the rule.

**Tests.** `DiagnosticBagOrderTests` (Unit, eight facts) pins the contract on a bag: ties, three-way ties, append, different positions, out-of-order input, another file never compared, `from` respected, no causes. Six of the eight fail on the old method. `WordCharacterScreenTests` gains the tie fact on both arms (the failed-parse arm fails on the old code) and the two cross-file facts: a COPY member's parse error at line 9 is not compared with a cause at line 6 of the program file, and a cause with no later effect in its own file is appended after the member's error. No golden: the order a compilation prints its diagnostics in is a presentation choice of this compiler, not a rule of the standard, and the facts are in-process tests of that order.

**Register.** PB4200 landed; it closes no inventory row (reason on the note).

