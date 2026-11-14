---
heading: "### 3.9 The language level at COBOL-85: level 2, so no level 1 restriction is enforced (kb/Work PB5142, PB4885)"
---
X3.23-1985 divides nine of its modules into level 1 and level 2 elements (§1985:I.1.2), and a conforming implementation (§1985:I.1.5.2) must support one of the three
subsets of §1985:I.1.5.1 in full. `--std 85` claims the HIGH subset, the highest level of every required module (owner decision, kb/Work PB4885), so it is a LEVEL 2
processor and has no level axis: a level 1 restriction is a restriction on the programs of a LOWER level, and a level 2 processor accepts the program that breaks it.

| Item | Determination |
|---|---|
| INSPECT's one-character literals and data items (§1985:VI.6.18.3 SR5: "In level 1, literal-1, literal-2, and literal-3, and the data items referenced by identifier-3, identifier-4, and identifier-5 must be one character in length. Except as specifically noted in syntax and general rules, this restriction on length does not apply to level 2.") | Not enforced, at any edition. The rule lifts itself at level 2, so refusing it at `--std 85` would reject legal level 2 source. The "noted in syntax and general rules" exceptions are enforced where they are stated: SR7 and SR9 (the pair sizes), SR8 (CHARACTERS BY one character) and SR10 (the repeated character of literal-4), COBOL0846 and COBOL6859 (kb/Work PB5089, PB5103) |
| A level 1 only mode | None. No option selects the intermediate or the minimum subset, so no level 1 rule has a place to be enforced; the compiler states its claim and a program written for level 1 is a program of level 2 |
