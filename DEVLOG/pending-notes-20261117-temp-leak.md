## kb/Work PB5921: the C# tests still leak into %TEMP%

Filed from the owner-requested cleanup of E:\Temp on 2026-11-17: 128,291 entries before, 9,731 after. In the 24 hours before the
cleanup the tests left 3,758 `file-*.dat` data files and 2,291 random-named scratch directories, so the leak is live. A sibling of
PB3194, which fixed the same leak for scratch directories. Notes only; no code changes.
