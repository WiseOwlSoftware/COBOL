**2026-11-21, review round 1 (R1-1, R1-2): the left-parenthesis exception ruled from the 1985 text.**

The filing read `(- M) + 9` as a 1985 violation. The determination `docs/conformance/3/D-85-OPERATOR-SPACING.md` overturns
that one position: the 1985 text itself writes a left parenthesis directly before NOT (`(NOT (a = b))`, §1985:VI.6.3.3,
checked on the rendered page 293) and before a unary minus (`(- argument-2)`, §1989:p.A-34, page 864), so "preceded by a
space" admits two readings there, and GnuCOBOL 3.2.0 at `-std=cobol85` accepts it (CLAUDE.md rule 1). `-(M)`, `NOT(`,
`)AND(` and a relational operator touching a parenthesis stay refused: the text writes a space in every such position,
and the glossary's Unary Operator (§1985:III.2) says "precedes", not "immediately precedes", so it names the next symbol
and says nothing about the spaces. The behavior of the branch is unchanged; the record moved from the corpus to the text.
