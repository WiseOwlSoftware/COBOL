## 2026-11-17: the owner confirms the compiler keeps declining ENTER

The owner, in the Mythos session on 2026-11-17, after an account of what ENTER is and what providing it would take:
"Agreed, keep declining ENTER".

The account the owner agreed with:
- §1985:I.1.5.2.5.5 makes ENTER optional: "An implementation of Standard COBOL may include the ENTER statement or not,
  at the option of the implementor." Declining it conforms, and COBOL7240 says so at --std 85.
- §1985:VI.6.12 leaves everything that defines the feature to the implementor (SR1 the language-names, GR2 "all
  details on how the other language(s) are to be written"). 1985 marks ENTER obsolete, and 2002 deletes it.
- Providing it would mean inventing a whole subsystem: a lexer mode for foreign text up to ENTER COBOL, a public
  contract for how C# sees the generated COBOL items (which would freeze the emitted field names), Roslyn errors mapped
  back to COBOL source lines, control-flow rules for PERFORM ranges and declaratives, and a trust policy for inline
  code. It would bring no conformance gain, and every test oracle would be our own definition. CALL already reaches
  other languages.

Consequence: the four DOCUMENTED-NON-SUPPORT ENTER rows of the 1985 inventory are final. They are not 1985 work left
to do.
