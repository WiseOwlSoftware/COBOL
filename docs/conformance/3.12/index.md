---
heading: "### 3.12 A literal EVALUATE selection subject against a literal selection object, at COBOL-85 (kb/Work PB5481)"
---
| Item | Determination |
|---|---|
| `EVALUATE 5 WHEN 5`, `EVALUATE ZERO WHEN 0`, `EVALUATE "A" WHEN "A"`, `EVALUATE 5 WHEN NOT 4` and an ALSO position of the same, `--std 85` | Legal; the object is selected by the value and class of the two literals. §1985:VI.6.13.3 SR6 a asks that an object be "a valid operand for comparison to the corresponding operand in the set of selection subjects", an operand-pair property (class, usage, index rules); it is not the relation condition's "at least one reference to a variable" (§1985:VI.6.3.1.1), because EVALUATE builds no relation condition and §1985:VI.6.13.4 GR1 gives each literal "the value and class of the specified literal". The text keeps both readings alive, so it is implementor latitude and CLAUDE.md rule 1 sends it to GnuCOBOL: 3.2 with -std=cobol85 compiles and runs each of them |
| The same, `--std 2002` and later | COBOL1634. §2002:14.8.12.2 SR9 and §2023:14.9.13.3 SR10 print the combination table (Table 16, Table 15) and its [NOT] literal row is blank under the literal subject column |

Every other blank cell of the table is refused at every edition. The one mechanism is `EvaluateOperandCombinations.IsPermitted` with `DialectBehavior.EvaluateRefusesLiteralSubjectLiteralObject` (VCR row 7.73). The tests are `conformance-suite/cases/VI.6.13@1985-EVALUATE-STATEMENT/pb5481_literal_subject_literal_object_85` and `conformance-suite/cases/14.9.13-EVALUATE/pb5481_literal_subject_literal_object_2002`.
