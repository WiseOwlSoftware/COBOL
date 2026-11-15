- **D-PARAMETERIZED-UNEXPANDED — at `--std 2002`, a reference to a parameterized interface or class that is not expanded is
  refused** (kb/Work PB5625 reading A, adjudicated 2026-11-15; a defect in the 2002 text, recorded here and not coded
  around). At 2014 and 2023 the refusal is the standard's own rule, not this determination: §2014:12.3.7.3 GR7 "If
  interface-name-1 is an interface described with the USING phrase, interface-name-1 may be specified only in the
  REPOSITORY paragraph" and GR1 the same for a class ("class-name-1 may be specified only in the REPOSITORY paragraph");
  §2023:12.3.8.4 GR7 in the same words, and GR1 for a class ("object-class-name-1 may be specified only in the REPOSITORY
  paragraph"). The 2002 text grants the name in general words and gives the unexpanded
  name no meaning. §2002:12.2.7.3 GR7: "Interface-name-2 is the name of an interface that may be used throughout the
  scope of the containing environment division", and §2002:14.8.45.2 SR17 asks of a USE AFTER EXCEPTION OBJECT operand
  only that it be "the name of an interface specified in the REPOSITORY paragraph". But §2002:9.3.13 calls a
  parameterized interface a skeleton that becomes an interface "When it is expanded by substituting specific
  class-names or interface-names as actual parameters, an interface is created that functions as a non-parameterized
  interface"; the only rule that supplies actual parameters is §2002:12.2.7.3 GR8 ("If the EXPANDS phrase is specified
  in an interface-specifier, an interface interface-name-2 is created from the parameterized interface
  interface-name-3"); and §2002:9.3.7.1.3 defines the use of a parameterized interface "as if the actual parameter
  classes or interfaces were substituted for the parameters", of which an unexpanded reference has none. The class case
  has the same gap in the same words (§2002:9.3.12, §2002:12.2.7.3 GR1 and GR5). **Determination, at `--std 2002`:**
  WiseOwl COBOL refuses, outside the REPOSITORY paragraph, any reference to a parameterized interface or class that the
  REPOSITORY lists without the EXPANDS phrase, because the alternative is to invent semantics no edition states (what
  an object reference typed by a skeleton conforms to, which class implements it). The 2014 and 2023 rules are a
  pointer to what the committee later wrote, never the 2002 rule. **The reading not taken:** §2002:9.3.13's third
  paragraph ("When a parameterized interface is specified in the REPOSITORY paragraph, a new interface (an instance of
  a parameterized interface) is created based on the specification of the parameterized interface") could be read to
  create an instance without EXPANDS; it is not, because the next paragraph identifies an instance by "the same actual
  parameters", so an instance without actual parameters is not a thing the clause describes. GnuCOBOL and IBM
  Enterprise COBOL have no parameterized interfaces or classes, so CLAUDE.md rule 1's precedence offers nothing below the
  ISO text. Refusals: COBOL0859 (USE AFTER EXCEPTION OBJECT), COBOL0813 (USAGE OBJECT REFERENCE) and COBOL0823 (INVOKE,
  whose wording does not yet state this ground: kb/Work PB5676); their text cites 2023 rules at every edition, a PB4950
  instance at 2002 and 2014. The owner may overturn this determination; overturning it means implementing the
  unexpanded reference at `--std 2002` only.
