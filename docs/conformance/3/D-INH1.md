- **D-INH1 — interface multiple inheritance: what SR5 asks of the inheriting interface, and what "the same interface-name"
  means in SR6** (kb/Work PB1502; ISO §2023:11.6.3 SR5 and SR6, §2023:11.7.3 SR4 b), §2023:9.3.10; all cite-checked). (1) **SR5.** "If a
  given method-name is inherited from more than one interface, the method prototype in each inherited interface shall be
  such that this interface conforms to all inherited interfaces." The inheriting interface has ONE method of that name
  (SR4 b) leaves it no prototype of its own), so the rule holds exactly when ONE inherited prototype conforms to every
  other, by the §2023:9.3.8.2.3 rule set (`OoConformance.MethodConformanceMismatches`): identical prototypes inherited twice,
  and a covariant RETURNING pair (rule 5) through the more specific one. WiseOwl COBOL reads it that way, reports a
  violation on the INHERITING interface (COBOL2762) and records the conforming prototype as the interface's own
  (`OoInterfaceSymbol.ChooseInheritedPrototype`), which the IMPLEMENTS pass, `InterfaceConformsTo` and the emitted C#
  interface (a hiding `new` member, because C# member lookup over two base interfaces declaring one name is ambiguous)
  all read. Rejected reading: pairwise conformance in BOTH directions, which refuses the covariant pair that rule 5 makes
  legal. (2) **SR4 b).** A prototype whose roster key (the method-name; the v1 method resolution signature, kb/Work
  PB1519) matches ANY prototype of the INHERITS closure is refused (COBOL2761): SR2 forbids OVERRIDE in a prototype, so there is no
  legal way to redeclare. (3) **SR6.** The rule is about the WRITTEN interface-name (a case variant is the same
  word, §2023:8.1.3.2 GR3). Two different REPOSITORY names for one externalized interface (§2023:12.3.8.3: `INTERFACE
  ALX AS "EXT-ALI"` and `INTERFACE ALY AS "EXT-ALI"`) are two interface-names, no sentence of the standard forbids them,
  and the inheriting interface has ONE base — the emitted C# base list carries it once. This follows the PB946
  determination for IMPLEMENTS (ISO controls where it states the prohibition; SR6 names the NAME). Witnesses:
  `conformance:cases/11.6-INTERFACE-ID/pb1502_interface_inherits_legal`, `conformance:cases/11.6-INTERFACE-ID/pb1502-interface-inherits-conflicting-prototypes`,
  `conformance:cases/11.7-METHOD-ID/pb1502-interface-prototype-redeclares-inherited`,
  `conformance:cases/11.7-METHOD-ID/pb1502-interface-prototype-contradicts-inherited`,
  `conformance:cases/11.6-INTERFACE-ID/pb1502-interface-inherits-same-name-twice`.
