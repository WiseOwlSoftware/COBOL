## R2 batch 1f (pin 9e65164eb3a9): 1 finding on this note

### `procedure-binding-3--code--f1#3` (defect, defect-for-fix-lane, votes upheld/upheld/upheld)

**Finding:** PERFORM VARYING SR5 b)/c) (an index-name in the FROM phrase) cites ISO/IEC 1989:2023 14.9.28.3 at --std 1985 under a comment claiming the 1985 text has no such rule; ANSI X3.23-1985 VI.6.21.3 SR8 states it

**Rule:** CLAUDE.md rule 1 and the project edition rule (each edition stands on its own text): a diagnostic names the rule of the edition being compiled; ANSI X3.23-1985 VI.6.21.3 SR8 a-c is the 1985 statement of ISO/IEC 1989:2023 14.9.28.3 SR5 a-c (both checked with cite.py --check in this review). Lying comment: CheckVaryingOperandRules.Sr says a rule the 1985 text does not have (SR5, an index-name FROM) cites the 2023 one

**Scenario:** `cobol --std 1985` on `PERFORM VARYING N FROM IX BY 1.5 UNTIL N > 5` (IX an index-name): the SR5 c) arm calls Sr(by.Text, "BY", "SR5 c)", ...) with rule1985 null, so CiteRule(later, later) prints `ISO/IEC 1989:2023 14.9.28.3 SR5 c)` to a COBOL-85 user, whose standard states the rule as ANSI X3.23-1985 VI.6.21.3 SR8c ("The literal in the associated BY phrase must be an integer"); the SR5 b) arm likewise. Every sibling arm in the same method (SR4 a-c, SR6) passes its 1985 rule (SR7a-c, SR9), so only this pair cites the wrong edition, and the comment keeps the next editor from fixing it.

**Target:** Pass "SR8b"/"SR8c" and their 1985 wording to the two SR5 calls and delete the false comment, making rule1985 a required parameter of Sr so a new arm cannot omit it; lands in whichever DESIGN-architecture-review §8.3 piece takes the PERFORM VARYING partial

**Finding sites (the pin's):** src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.PerformVarying.cs:338-339 (ControlFlowBinder.CheckVaryingOperandRules SR5 b); src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.PerformVarying.cs:353-354 (ControlFlowBinder.CheckVaryingOperandRules SR5 c); src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.PerformVarying.cs:357-365 (ControlFlowBinder.CheckVaryingOperandRules.Sr)

The finding is a further site of this note's rule (an edition-pinned citation left at another edition, the PB4950 rule): ControlFlowBinder.CheckVaryingOperandRules passes no 1985 rule to Sr for its SR5 b) and SR5 c) arms, so a --std 1985 compile cites the 2023 clause under a comment that claims the 1985 text has no such rule. PB6219's named sites do not include src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.PerformVarying.cs; the fix lane sweeps the rule's siblings and takes this one with them. Recorded here and not filed apart; the severity is Suggestion and the harm misleads-diagnosis. `ControlFlowBinder.CheckVaryingOperandRules` is still declared in that file on the tree after the pin (the file differs from the pin by one line). The sites are the pin's.
