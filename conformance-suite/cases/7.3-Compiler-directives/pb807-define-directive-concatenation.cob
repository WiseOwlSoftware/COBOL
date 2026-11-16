      *>--- conformance-case
      *> id: pb807-define-directive-concatenation
      *> rules: [SR-7.3.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> 7.3.3 SR10 (2002: 7.2.2 SR10, the same words): "A literal in a
      *> compiler directive shall not be specified as a concatenation
      *> expression" (kb/Work PB807). The DEFINE directive exists in
      *> 2002, so the refusal witnesses the rule at 2002 as at 2023; the
      *> DISPLAY directive form (pb807-display-directive-concatenation)
      *> needs the 2023 >>DISPLAY.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB807D.
000300 PROCEDURE DIVISION.
000400 >>DEFINE V AS "A" & "B"
000500     STOP RUN.
