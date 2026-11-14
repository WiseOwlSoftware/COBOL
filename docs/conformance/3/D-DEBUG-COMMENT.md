- **D-DEBUG-COMMENT — a debugging section without WITH DEBUGGING MODE, and a SORT or MERGE in one** (kb/Work PB5291, PB5275; the text decides, no
  latitude). §1985:XV.2.1.3 GR2: without the mode "any USE FOR DEBUGGING statements and all associated debugging sections are compiled as if they were
  comment lines". The text is comment text BEFORE the parse, by the switch that already makes a debugging line a comment: `DebuggingLineRewriter` moves
  the tokens of a declarative section whose first sentence is `USE FOR DEBUGGING` to the absent channel when the unit declares no mode (COBOL-85 only: a later edition, `--permissive` included, recognizes the section as the removed
  construct and reports COBOL0902), so a body that is not COBOL syntax compiles and its paragraph names declare
  nothing; a DECLARATIVES portion holding only such sections leaves with them, since the format admits no empty portion. With the mode, a debugging
  section is a declarative section (§1985:XV.3.2.3 SR1: it follows "the DECLARATIVES header"), so the placement rules that ask whether a statement is in the
  declaratives ask it of a debugging section too (`EnclosingContext.InDeclarative`): a SORT may appear "except in the declarative portion" (§1985:XI.4.4.3
  SR1), a MERGE "except in the declaratives portion" (§1985:XI.4.1.3 SR1), COBOL2700. Witnesses:
  `conformance:cases/XV.2.1@1985-WITH-DEBUGGING-MODE-CLAUSE/pb5291_debug_section_is_comment_without_mode_85`,
  `conformance:wiseowl/diagnostics/pb5275-sort-merge-in-debugging-section`.
