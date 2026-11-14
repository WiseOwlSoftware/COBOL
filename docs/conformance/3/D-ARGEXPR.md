- **D-ARGEXPR — a keyword-less `N + 1` in a CALL, INVOKE or inline-invocation argument list is ONE argument
  (kb/Work PB1135 / PB130, decision R59).** A USING list is juxtaposition, so `USING N + 1` has two grammatical
  readings: the ONE arithmetic-expression-1 argument, or the two arguments `N` and a unary `+ 1`. The format answers
  it: CALL Format 2 (§2023:14.9.4.2) and INVOKE (§2023:14.9.23.2) print `arithmetic-expression-1` and `boolean-expression-1`
  under an OPTIONAL `[ BY CONTENT ]`, and CALL SR20 forbids omitting BY CONTENT only "when identifier-4 is an
  identifier that is permitted as a receiving operand" (`cite.py --check 14.9.4.3 "BY CONTENT shall not be omitted
  when identifier-4 is an identifier that is permitted as a receiving operand"` → OK 20); SR17 makes every identifier
  in it a sending operand. The token-level tie is broken by the text too: a sign "shall appear as the leftmost
  character of the literal" (§2023:8.3.3.3.2 SR2, OK) and an arithmetic operator is "preceded by a space and followed by
  a space" (§2023:8.7.1, OK), so a `+` or `-` after an operand is the BINARY operator. **Determination (the lexer skips
  whitespace, so the parse does not consult spacing; §2023:8.7.1's spacing is then asked of the parsed operator, COBOL2993,
  kb/Work PB2017 — `USING N -1` is the expression `N - 1` and is refused for its missing space):** an argument is the
  LONGEST arithmetic expression that starts at it — `USING N + 1`, `USING N - 1`, `USING 5 + 1`, `USING BY CONTENT N + 1` and
  `O :: "M" (N + 1)` are each one argument, evaluated before the call and passed BY CONTENT (§2023:14.9.4.4 GR9 a) 2,
  §2023:14.9.23.4 GR6 a) 2); an argument that is a SOLE identifier or literal stays an identifier or literal
  (§2023:14.9.4.4 GR8). A second argument that is a signed literal is written first or after a BY phrase — `USING +1 N`,
  `USING N BY CONTENT +1` (`N (+ 1)` is N subscripted). This replaces PB130's earlier "the list reading wins".
  Implemented in the grammar (no bare `dataReference` arm in `callArgument` / `callByContent` / `invokeArgument`, and
  `{!numericLiteralIsLeftOperand()}?` on the literal arms), with the binders recovering the sole identifier;
  pinned by `conformance:cases/14.9.4-CALL/pb130_call_format2_bare` and `conformance:cases/14.9.23-INVOKE/pb1135_invoke_keywordless_expression_argument`.
