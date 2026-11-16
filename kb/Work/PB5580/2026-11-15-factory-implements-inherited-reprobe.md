**2026-11-15, golden-2002-6 integrator: a factory that IMPLEMENTS an interface through a method inherited from the first of two bases (row 2002-GR-11.2.3-2, with 2002-FMT-11.2.1 and 2002-SR-11.3.2-2).**
Repro `E:\COBOL-coord\scratch\gl2002\integrate-6\repro\mi_factory_impl.cob` (the g2002u-11-3-factory fixer's,
`lane1\out\g2002u-11-3-factory\defects\`): `CLASS-ID. R6FC INHERITS FROM R6FA R6FB.` whose `FACTORY. IMPLEMENTS R6IF.`
declares no method, R6FA's factory defining SPEAK; the main program sets an `OBJECT REFERENCE R6IF` to R6FC and runs
`INVOKE S "SPEAK"`. Expected at `--std 2002` (§2002:9.3.9, a subclass "inherits from one or more classes"; §2002:3.1 makes
only Annex A optional): it compiles and displays `FA-FACTORY-SPEAKS`. Observed on a build of main 5e49d46678,
`cobol.exe mi_factory_impl.cob --std 2002 --run`: exit 65, `(55,42): error COBOL0849: class 'R6FC': INHERITS FROM 2 base
classes (R6FA, R6FB): Multiple inheritance ... is an OPTIONAL element (§2023:4.2.7; Annex A.4.10 item 1) whose support
WiseOwl COBOL does not claim`. The same mechanism as this note (the 2023 optionality applied at 2002); no new note.
