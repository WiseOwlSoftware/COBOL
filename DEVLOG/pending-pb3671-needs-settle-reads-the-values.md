## PB3671: abi_pin.needs_settle reads the pin's values, so a landing builds for the pin only when two branches changed it

PB3664's landing stopped at 18:59 PDT with "CONFLICT in src/Cobol.Net.Runtime/RuntimeAbi.pin", though neither side had
changed the pin. After every clean merge of main, `merge_drivers.merge` asks `abi_pin.needs_settle`, which searched the
file's text for UNMEASURED. The pin's own header says "the hashes read UNMEASURED", so the answer was yes on every tree.
Every landing since PB3610 has run a solution build and the measuring test in the branch's worktree for nothing. This
build failed because the operator session's C# language server held `Cobol.Net.Compiler.SourceGen.dll`, and the failed
settle came back as a conflict.

`needs_settle` now reads the pin through `parse` and answers yes only when a hash line reads UNMEASURED, which
`merge_text` writes when both sides changed the pin. The self-test checks the shipped pin, whose header names the word,
so the old text search fails it. PB3672 (open, PB1957) files the rest: a settle that fails for an environment reason is
classified as exit 4 CONFLICT, and the dispatching session's language server locks analyzer DLLs in its agents'
worktrees. The operator stopped the language server and re-armed PB3664's landing.
