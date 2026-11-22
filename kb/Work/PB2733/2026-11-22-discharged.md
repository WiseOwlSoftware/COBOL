## Discharged (2026-11-22, wave 20262135 group A)

Re-probed on 2061bdb7bb: `grep -rn "WrittenText" src --include=*.cs` finds no `DataBinder.WrittenText` (only the unrelated
`OoExpansion.ClientWrittenTexts`), `WrittenSource.Of` has 485 callers, and `src/WiseOwl.Cobol.Frontend/Parsing/WrittenSource.cs`
no longer names DataBinder. `git log -S"static string WrittenText" -- src` names the landing that did the wave's whole target:
868f612503 "PB5979: delete the DataBinder.WrittenText forwarder; every binder calls WrittenSource.Of". Nothing is left to do,
so the note is retired with no change of its own.
