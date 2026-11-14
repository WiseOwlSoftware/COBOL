- **D-GLOBAL-EXIT — EC-FLOW-GLOBAL-EXIT is never set** (kb/Work PB3102). §2023:14.6.13.1.6 Table 13 names it ("EXIT PROGRAM
  in a global Declarative") as a Fatal condition, and §2023:14.6.13.1.3 6) mentions it beside EC-FLOW-GLOBAL-GOBACK, but the
  standard states no general rule that SETS it. The sibling is set by §2023:14.9.18.4 GR6 (a GOBACK executed within the range
  of a global declarative); §2023:14.9.14.4 GR3 gives an EXIT PROGRAM only GOBACK's GR3 and GR4, not GR6, and the one
  normative statement about EXIT PROGRAM in a global declarative is §2023:14.9.14.3 SR2, which the compiler enforces at compile
  time. The name is catalogued and accepted wherever an exception-name may be written, and the implementation never
  raises it: inventing a raise site for a condition the standard never sets would be behaviour with no rule behind it.
  Held by `unit:EcRaiseSiteRegistryDriftTests.EveryCataloguedLevel3Name_IsDeclaredAndRaised_OrDeclinedWithAReason`.
