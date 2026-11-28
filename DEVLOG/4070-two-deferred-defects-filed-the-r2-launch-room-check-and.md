## Entry 4070 — 2026-11-27 16:17 PST — Two deferred defects filed: the R2 launch room check and stale SET code-read symbols

Filed PB6211 (the R2 launch room check, `scripts/arch/r2_collect.py --launch` via `r2_cost.room`, ignores the orchestrator loop's in-flight wave reservation; account 1 reached 61.3 % weekly against the 57.2 % day line on 2026-11-25) and PB6212 (two 1985 code reads and SET design/construct rows still name `SetBinder` members PB2314 moved; `code_location.symbol_resolves` accepts a call-site mention, so no check caught it).

Checked and not filed: the COBOL0858 citation in `RaisingPhrase.cs` passes `cite.py --check "§2023:14.2.2 SR7"` against "Exception-name-1 shall be a level-3 exception-name for EC-USER."; the interface-name half of that phrase is already PB814. No code changed.

