- **D-OOCIRC — the reach of "directly or indirectly references" in §2023:9.3.8.2.3's closing sentence** (kb/Work PB1498;
  owner decision R63). §2023:9.3.8.2.3: "If the description of the returning item of a method in interface-1 directly or
  indirectly references interface-2, the description of the returning item of the corresponding method in interface-2
  shall not directly or indirectly reference interface-1" (cite-checked). The standard defines "indirectly" no further,
  so it carries its ordinary TRANSITIVE meaning. The reference graph: nodes are classes and interfaces, keyed by name;
  a RETURNING item described as an object reference to Y (an object-class-name, an interface-name, or ACTIVE-CLASS —
  the containing class) is an edge to Y, from the class whose methods (instance or factory) or the interface whose
  prototypes (inherited ones included) declare it; a class has edges to the class it INHERITS and to every interface
  in either IMPLEMENTS list; an interface to the interfaces it INHERITS. A universal reference is no edge. The walk is
  breadth-first with a visited set. The sentence is asked only of two DISTINCT interfaces ("If two interfaces are of
  the same interface, they conform to each other"), so a returning item that references its own class or interface is
  legal. It is one of `OoConformance.MethodConformanceMismatches`' rules (`ReturningCircularity`), so every asker
  applies it — IMPLEMENTS (COBOL0841), an override (COBOL0829) and the interface relation GOBACK / EXIT RAISING
  asks. Witnesses: `conformance:cases/9.3.8.2.3-Conformance-between-interfaces/pb1498_self_referencing_returning`,
  `conformance:cases/9.3.8.2.3-Conformance-between-interfaces/pb1498-returning-circularity-direct`, `conformance:cases/9.3.8.2.3-Conformance-between-interfaces/pb1498-returning-circularity-indirect`.
