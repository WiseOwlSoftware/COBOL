## Entry 2655 — 2026-10-27 10:06 PDT — PB3851: the ANTLR output is ignored by its shape, so a project move leaves no visible generated file

PB3837 (#663) renamed `src/Cobol.Net.Frontend` to `src/WiseOwl.Cobol.Frontend`, and `.gitignore`'s rule for the
generated lexer and parser moved with it, because it named the project's path. Every checkout that had built before
the rename then showed eight untracked files under `src/Cobol.Net.Frontend/Generated/`, and a checkpoint's
`git add -A` would have committed them under the retired name. The operator found them on the main checkout and in
worktree-wf_426be72c-f33-17 right after #663 merged.

The rule is now `src/*/Generated/`, the output's shape, the way `bin/` and `obj/` were always ignored; no
`Generated/` directory under `src/` is tracked, so it hides nothing. The main checkout's stale `src/Cobol.Net.*` and
`tests/Cobol.Net.*` build output (about 590 MB, nothing tracked) was deleted by hand.

