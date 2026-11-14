---
order: 11
---
> ⚖ **DETERMINATION — §2023:11.9.10.3 SR1's "one-byte hexadecimal-alphanumeric literal" is §2023:8.3.3.2.2 FORMAT 2, and
> nothing else** (2026-09-02; kb/Work PB152, COBOL1727). "Hexadecimal-alphanumeric literal" is a defined
> term: §2023:8.3.3.2.2 gives the alphanumeric literal exactly two general formats — format 1 (`"…"` / `'…'`) and
> format 2 (`X"…"` / `X'…'`) — and the term names format 2. `INITIALIZE ALL TO "Z"` is therefore **not** a
> conforming spelling however short the literal is; `X"5A"` is, and one byte is exactly two hexadecimal digits
> (§2023:8.3.3.2.3 SR5). This is recorded as a determination because the natural reading of SR1 diverges from its
> letter here, and because the previous decoder took the literal's first character for ANY shape — so
> `INITIALIZE ALL TO "AB"` silently filled with `A`.
