- **D-85-SPACE-CLASS — the class of the figurative constant SPACE outside MOVE at COBOL-85 (kb/Work PB5683).** This entry
  speaks for COBOL-85. In a context that requires a class and is not a MOVE, the figurative constant SPACE is of class
  alphanumeric, as a nonnumeric literal is: `SPACE THRU "Z"` is one range of an EVALUATE and SPACE THRU an alphabetic item
  is refused (`§1985:VI.6.13.3 SR4`). The 1985 text gives SPACE the category alphabetic in the MOVE statement only
  (`§1985:VI.6.19.4 GR3`); GnuCOBOL makes no class test there; IBM Enterprise COBOL treats SPACE as an alphanumeric literal.
  Decided by the precedence of CLAUDE.md rule 1: the text does not decide it, GnuCOBOL has no rule here, and the vendor
  manual decides. Source: `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-16.md`, mechanism 4. Witnesses:
  `conformance:cases/VI.6.13@1985-EVALUATE-STATEMENT/pb5796_range_figurative_class_accepted` and
  `conformance:cases/VI.6.13@1985-EVALUATE-STATEMENT/pb5796_range_figurative_class_refused`.
