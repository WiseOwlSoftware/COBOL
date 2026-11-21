- **D-85-OPERATOR-SPACING — where a left parenthesis stands in for the space beside an operator at COBOL-85 (kb/Work
  PB5989).** This entry speaks for COBOL-85. The 1985 operator sentences ask for a space on both sides of every arithmetic
  operator, unary ones included (`§1985:VI.6.2.2`: "must be preceded by a space and followed by a space"), of every word
  of a relational operator (`§1985:VI.6.3.1.1`) and of every logical operator (`§1985:VI.6.3.2`). Where the 1985 standard
  lets a left parenthesis replace a space it says so (`§1985:IV.4.2.1 SR5`: "immediately preceded by a space, left
  parenthesis"), so these sentences, read alone, admit no parenthesis. Yet the standard's own text writes a left
  parenthesis directly before the logical operator NOT: its expanded equivalents read `(NOT (a = b)) OR (a = c)` and
  `AND (NOT (a NOT > d))` (`§1985:VI.6.3.3`), and its precedence example reads `(condition-1 OR (NOT condition-2))`
  (`§1985:VI.6.3.2.3`). It writes one directly before a unary minus in the ANNUITY formula, `(- argument-2)`
  (`§1985:p.A-34`, Amendment 1; both pages checked against the rendered PDF, pages 293 and 864). It also names the left
  parenthesis as the unary operator's required neighbor on the left (`§1985:VI.6.2.3 SR4`: "it must be immediately
  preceded by a left parenthesis"), and a parenthesis is itself a separator (`§1985:IV.4.2.1 SR4`). So for that one
  position, a left parenthesis directly before a unary operator or NOT, the text admits two readings. GnuCOBOL 3.2.0 at
  `-std=cobol85` accepts `(-M)` and `(NOT (M = 1))`, and by the precedence of CLAUDE.md rule 1 the position is
  accepted. Every other position keeps the sentence's plain reading, because no 1985 text writes an operator touching a
  parenthesis there and its examples put a space there (`NOT (`, `) AND (`, `(1 - (1 + argument-1)`). So `-(M)`,
  `NOT(A = 1)`, `(A = 1)AND(B = 2)` and `N GREATER(M)` are refused at COBOL-85 (COBOL2993, COBOL7672). From 2002 the
  arithmetic exception covers both sides of a unary operator by its own text (`§2023:8.7.1`), and no later edition
  states the relational or logical sentences. The glossary's Unary Operator, a sign "which precedes a variable or a left
  parenthesis in an arithmetic expression" (`§1985:III.2`), does not admit `-(M)`. Like the table of `§1985:VI.6.2.3 SR3`
  (a unary operator followed by `(` is a permissible pair), it names the next symbol, not the characters between them. It
  says "precedes", where the standard writes "immediately preceded" for adjacency (`§1985:VI.6.2.3 SR4`,
  `§1985:IV.4.2.1 SR5`), and `- (M)` satisfies it. GnuCOBOL also accepts `-(M)`, but its acceptance does not decide
  here: the precedence applies only where the text admits two readings, and GnuCOBOL makes no operator-spacing check at
  `-std=cobol85` at all (it accepts `IF N=M`, which no reading admits). Witnesses:
  `conformance:cases/_unassigned/pb5989-unary-spaced-runs-85`,
  `conformance:cases/_unassigned/pb5989-logop-spaced-runs-85`,
  `conformance:cases/_unassigned/pb5989-unary-paren-refused-85`,
  `conformance:cases/_unassigned/pb5989-logop-not-before-paren-refused-85`,
  `conformance:cases/_unassigned/pb5989-relop-word-paren-refused-85` and
  `conformance:cases/_unassigned/pb2017_arithmetic_operator_spacing`.
