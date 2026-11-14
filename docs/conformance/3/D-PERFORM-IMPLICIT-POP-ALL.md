- **D-PERFORM-IMPLICIT-POP-ALL — Exception-checking PERFORM — the implicit POP ALL and the END-PERFORM line's reference format (§2023:14.9.28.4 GR14, the
  spec states the order, not the line)**: "Immediately preceding the END PERFORM phrase, there is an implicit POP ALL",
  and ALL includes SOURCE FORMAT (§2023:7.3.22.4 GR2). A reference format is a property of a physical LINE (§2023:6.5), so the
  op is a boundary between lines: **pinned choice: the restored format governs the line that holds END-PERFORM itself**
  (the phrase is text following the POP, and §2023:7.3.24.3 1) makes a format govern "the source text … following the
  directive"), and the implicit PUSH takes effect after the line that ends imperative-statement-1. A handler may thus
  switch format and write END-PERFORM in the layout of the format it started in. Where the unrestored format makes that
  line unreadable, the front end finds the phrase in the physical text and verifies the guess by re-parsing (kb/Work
  PB1066; `DESIGN-frontend-grammar.md`).
