# COBOL Scope Termination Rules (from ISO/IEC 1989:2023)

This document extracts every rule about scope termination, sentences, periods, and
statement nesting from the ISO COBOL standard. It is intended as a precise reference
for parser implementers.

Page references: physical page = logical page + ~30 in the PDF.

---

## 1. The Paragraph / Section / Sentence Hierarchy (§14.2, §14.4)

### 1.1 Procedure Division Structure

The Procedure Division has two formats:

**Format 1 — with sections:**
```
PROCEDURE DIVISION [using-phrase] [RETURNING data-name-2] .
  [DECLARATIVES.
    {section-name SECTION. use-statement. [sentence]...
      [paragraph-name. [sentence]...]...}...
  END DECLARATIVES.]
  {section-name SECTION.
    [sentence]... [paragraph-name. [sentence]...]...}...
```

**Format 2 — without sections:**
```
PROCEDURE DIVISION [using-phrase] [RETURNING data-name-2] .
  [sentence]... [{paragraph-name. [sentence]...}...]
```

Key rules:
- A Procedure Division uses EITHER sections (Format 1) OR paragraphs-only (Format 2).
  You cannot mix: if any section header appears, ALL paragraphs must be inside sections.
- In Format 2 (no sections), sentences may appear before the first paragraph-name.
  These "orphan sentences" are executed when control falls through to the Procedure Division.

### 1.2 Sections (§14.4.2)

A **section** consists of:
1. A section header: `section-name SECTION [segment-number] .`
2. Zero or more paragraphs

A section ends immediately before the next section header, or at the end of the
Procedure Division. The section header is followed by a separator period.

### 1.3 Paragraphs (§14.4.3)

A **paragraph** consists of:
1. A paragraph header: `paragraph-name .`
2. Zero or more sentences

A paragraph ends immediately before the next paragraph-name, the next section header,
or the end of the Procedure Division. The paragraph-name is followed by a separator period.

### 1.4 The Containment Hierarchy

```
Procedure Division
  └── Section (optional level)
        └── Paragraph
              └── Sentence (one or more statements, terminated by period)
                    └── Statement
                          └── Nested statement (in conditional phrases)
```

---

## 2. Sentences (§14.5.1)

### 2.1 Definition

A **sentence** is a sequence of one or more statements, the last of which is terminated
by a separator period.

The separator period is the character `.` followed by a space (or end-of-line, which
is equivalent to a space). The period-space combination is the separator; the period
alone is not a separator.

### 2.2 Examples

Single-statement sentence:
```cobol
MOVE 1 TO WS-COUNT.
```

Multi-statement sentence:
```cobol
MOVE 1 TO WS-COUNT MOVE 2 TO WS-TOTAL ADD WS-COUNT TO WS-TOTAL.
```

The period at the end terminates ALL three statements and closes the sentence.

### 2.3 Critical Rule: Period Terminates ALL Open Scopes

From §14.5.3.3 (Implicit Scope Termination):

> A separator period terminates every open statement scope — every containing IF,
> PERFORM, EVALUATE, SEARCH, etc. It does not just terminate the innermost statement;
> it terminates ALL of them, all the way out.

This is the single most important rule for parser implementers. A period is not like
a closing brace in C — it is like closing ALL open braces at once.

---

## 3. Statements: Imperative vs. Conditional (§14.5.2)

### 3.1 Imperative Statements

An **imperative statement** is a statement that specifies an unconditional action.
A statement is imperative if:

1. It has NO conditional phrases (e.g., a plain `MOVE`, `DISPLAY`, `SET`), OR
2. It HAS conditional phrases but IS terminated by its explicit scope terminator
   (e.g., `ADD A TO B ON SIZE ERROR ... END-ADD`).

Case (2) is called a **delimited scope statement**. A delimited scope statement is
classified as imperative because its scope is explicitly bounded — it cannot "leak"
conditional behavior to enclosing constructs.

### 3.2 Conditional Statements

A **conditional statement** is a statement that has a conditional phrase and is NOT
terminated by its explicit scope terminator.

Examples of conditional phrases:
- `ON SIZE ERROR` / `NOT ON SIZE ERROR` (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE)
- `ON EXCEPTION` / `NOT ON EXCEPTION` (ACCEPT, CALL, DISPLAY, RECEIVE, SEND)
- `INVALID KEY` / `NOT INVALID KEY` (DELETE, READ, REWRITE, START, WRITE)
- `AT END` / `NOT AT END` (READ, RETURN, SEARCH)
- `AT END-OF-PAGE` / `NOT AT END-OF-PAGE` (WRITE)
- `WHEN` (EVALUATE, SEARCH)
- `THEN` / `ELSE` (IF)

A conditional statement without its explicit scope terminator can only be terminated by:
- A separator period (which terminates ALL open scopes), or
- The termination of its containing statement.

### 3.3 Why This Distinction Matters

The spec requires that certain positions accept only imperative statements. For example:
- The body of an inline PERFORM: `PERFORM ... imperative-statement-1 END-PERFORM`
- The ON SIZE ERROR handler: `ON SIZE ERROR imperative-statement-1`
- WHEN clauses in EVALUATE: `WHEN ... imperative-statement-1`

In these positions, a conditional statement is NOT permitted (unless it is delimited,
making it imperative). This means:

```cobol
*> LEGAL: ADD with END-ADD is a delimited scope statement (imperative)
PERFORM
    ADD A TO B ON SIZE ERROR DISPLAY "ERR" END-ADD
END-PERFORM

*> ILLEGAL: ADD without END-ADD is conditional, not allowed in imperative position
PERFORM
    ADD A TO B ON SIZE ERROR DISPLAY "ERR"
END-PERFORM
```

In practice, most compilers are lenient about this. But the spec is clear.

### 3.4 The Complete Statement Classification Table (Table 12, §14.5)

| Statement   | Conditional Phrase(s)                        | Scope Terminator |
|-------------|----------------------------------------------|------------------|
| ACCEPT      | [NOT] ON EXCEPTION                           | END-ACCEPT       |
| ADD         | [NOT] ON SIZE ERROR                          | END-ADD          |
| CALL        | [NOT] ON EXCEPTION                           | END-CALL         |
| COMPUTE     | [NOT] ON SIZE ERROR                          | END-COMPUTE      |
| DELETE      | [NOT] INVALID KEY                            | END-DELETE        |
| DISPLAY     | [NOT] ON EXCEPTION                           | END-DISPLAY      |
| DIVIDE      | [NOT] ON SIZE ERROR                          | END-DIVIDE       |
| EVALUATE    | WHEN                                         | END-EVALUATE     |
| IF          | THEN / ELSE                                  | END-IF           |
| MULTIPLY    | [NOT] ON SIZE ERROR                          | END-MULTIPLY     |
| PERFORM     | (inline: body) / WHEN (Format 3)             | END-PERFORM      |
| READ        | [NOT] AT END, [NOT] INVALID KEY              | END-READ         |
| RECEIVE     | [NOT] ON EXCEPTION                           | END-RECEIVE      |
| RETURN      | [NOT] AT END                                 | END-RETURN       |
| REWRITE     | [NOT] INVALID KEY                            | END-REWRITE      |
| SEARCH      | WHEN, AT END                                 | END-SEARCH       |
| SEND        | [NOT] ON EXCEPTION                           | END-SEND         |
| START       | [NOT] INVALID KEY                            | END-START        |
| STRING      | [NOT] ON OVERFLOW                            | END-STRING       |
| SUBTRACT    | [NOT] ON SIZE ERROR                          | END-SUBTRACT     |
| UNSTRING    | [NOT] ON OVERFLOW                            | END-UNSTRING     |
| WRITE       | [NOT] INVALID KEY, [NOT] AT EOP              | END-WRITE        |

Statements NOT in this table (MOVE, SET, GO TO, STOP, EXIT, INITIALIZE, INSPECT,
OPEN, CLOSE, CANCEL, etc.) have no conditional phrases and no scope terminator.
They are always imperative.

---

## 4. Scope Termination Rules (§14.5.3)

### 4.1 Explicit Scope Termination (§14.5.3.2)

A statement is explicitly terminated by its matching scope terminator keyword:
- `END-IF` terminates `IF`
- `END-PERFORM` terminates inline `PERFORM`
- `END-EVALUATE` terminates `EVALUATE`
- `END-SEARCH` terminates `SEARCH`
- `END-ADD` terminates `ADD` (when conditional phrases are present)
- etc.

A statement terminated by its explicit scope terminator is a **delimited scope
statement** and is classified as imperative (even though it contains conditional
phrases).

Explicit scope terminators are matched innermost-first (nearest preceding unmatched
statement of the matching type).

### 4.2 Implicit Scope Termination (§14.5.3.3)

A statement NOT explicitly terminated is implicitly terminated. The rules differ
based on the statement's classification and position:

#### Rule 1: Imperative statement NOT contained within another statement

Terminated by:
- (a) Any syntax element that follows the exhaustion of the statement's own syntax
- (b) The next-encountered statement keyword (e.g., seeing `MOVE` terminates a
  preceding `DISPLAY`)
- (c) A separator period

#### Rule 2: Imperative statement CONTAINED within another statement

Terminated by:
- (a) Anything that would terminate the containing statement (including a period,
  which terminates ALL enclosing scopes)
- (b) The next phrase of the containing statement (e.g., `ELSE` terminates the
  THEN-branch of IF; `NOT ON SIZE ERROR` terminates the `ON SIZE ERROR` handler)

#### Rule 3: Conditional statement NOT contained within another statement

Terminated by:
- A separator period ONLY

This is important: a conditional statement at the top level can ONLY be ended by a period.

#### Rule 4: Conditional statement CONTAINED within another statement

Terminated by:
- (a) The termination of the containing statement
- (b) The next phrase of any containing statement

### 4.3 The Period Rule — Summary

**A separator period terminates ALL open statement scopes simultaneously.**

It does not matter how deeply nested the scopes are. The period closes them all:

```cobol
IF A > B
    IF C > D
        IF E > F
            PERFORM SOME-PARA
            ADD 1 TO WS-COUNT
                ON SIZE ERROR
                    DISPLAY "OVERFLOW".
```

The period after `"OVERFLOW"` simultaneously terminates:
1. The DISPLAY statement
2. The ON SIZE ERROR phrase (and thus the ADD)
3. The innermost IF (E > F)
4. The middle IF (C > D)
5. The outermost IF (A > B)
6. The sentence

---

## 5. IF Statement Scope Rules (§14.9.19)

### 5.1 General Format

```
IF condition-1 THEN
    {statement-1}
    [ELSE {statement-2}]
END-IF
```

Archaic format (with NEXT SENTENCE):
```
IF condition-1 THEN
    {statement-1 | NEXT SENTENCE}
    [ELSE {statement-2 | NEXT SENTENCE}]
```

### 5.2 Statement-1 and Statement-2

Statement-1 and statement-2 each represent:
- One or more imperative statements, OR
- A conditional statement optionally preceded by imperative statements

They may themselves contain IF statements (nesting).

### 5.3 ELSE and END-IF Matching (Left-to-Right Rule)

Processing from left to right:

1. Any `ELSE` encountered is matched with the **nearest preceding IF** that:
   - Has not already been matched with an ELSE, AND
   - Has not been implicitly or explicitly terminated

2. Any `END-IF` encountered is matched with the **nearest preceding IF** that:
   - Has not been implicitly or explicitly terminated

### 5.4 Period Termination of Nested IFs

From the spec:
> A nested IF statement is terminated by the separator period of the sentence
> that contains it.

A period terminates the ENTIRE containing IF and ALL nested IFs within it.

### 5.5 Example: Period vs END-IF

```cobol
*> Using END-IF (modern, explicit):
IF A > B
    MOVE 1 TO X
    IF C > D
        MOVE 2 TO Y
    ELSE
        MOVE 3 TO Y
    END-IF
    MOVE 4 TO Z
END-IF
MOVE 5 TO W.
```
Here, `MOVE 4 TO Z` executes when A > B, regardless of the inner IF result.
`MOVE 5 TO W` always executes.

```cobol
*> Using period (archaic, implicit):
IF A > B
    MOVE 1 TO X
    IF C > D
        MOVE 2 TO Y
    ELSE
        MOVE 3 TO Y.
MOVE 5 TO W.
```
Here, the first period terminates BOTH IFs. There is no way to insert `MOVE 4 TO Z`
after the inner IF but still inside the outer IF without using END-IF.

### 5.6 The Dangling ELSE Problem

```cobol
IF A > B
    IF C > D
        MOVE 1 TO X
ELSE
    MOVE 2 TO Y.
```

Which IF does the ELSE belong to? Per the left-to-right matching rule, the ELSE
matches the **nearest preceding unmatched IF** — i.e., `IF C > D`. So:
- If A > B and C > D: MOVE 1 TO X
- If A > B and C <= D: MOVE 2 TO Y
- If A <= B: nothing

To make the ELSE match the outer IF, you must explicitly terminate the inner IF:
```cobol
IF A > B
    IF C > D
        MOVE 1 TO X
    END-IF
ELSE
    MOVE 2 TO Y
END-IF.
```

---

## 6. EVALUATE Statement Scope Rules (§14.9.13)

### 6.1 General Format

```
EVALUATE selection-subject-1 [ALSO selection-subject-2]...
    {WHEN selection-object-1 [ALSO selection-object-2]...}...
        imperative-statement-1
    ...
    [WHEN OTHER imperative-statement-2]
[END-EVALUATE]
```

### 6.2 Scope Rules

- Each WHEN clause group (one or more consecutive WHEN lines) is followed by an
  imperative-statement. The imperative-statement is terminated by the next WHEN
  keyword, by WHEN OTHER, or by END-EVALUATE.
- WHEN OTHER is the default/fallthrough case.
- END-EVALUATE explicitly terminates the entire EVALUATE.
- Without END-EVALUATE, a period implicitly terminates it.

### 6.3 WHEN Clause Body

The body after a WHEN clause group must be an **imperative statement** (which may be
a sequence of imperative statements, including delimited-scope statements). A
conditional statement is not permitted here unless it is delimited.

### 6.4 Selection Subjects and Objects

Selection subjects can be: identifier, literal, arithmetic-expression, condition, TRUE, FALSE.
Selection objects can be: [NOT] identifier/literal/expression, condition, TRUE, FALSE, ANY,
or a range (value-1 THRU value-2).

The ALSO keyword creates multi-dimensional evaluation (like a multi-column truth table).

---

## 7. PERFORM Statement Scope Rules (§14.9.28)

### 7.1 Out-of-Line PERFORM (Format 1)

```
PERFORM procedure-name-1 [THRU procedure-name-2]
    [times-phrase | until-phrase | varying-phrase]
```

- No END-PERFORM.
- The PERFORM statement itself is a single imperative statement.
- Control transfers to procedure-name-1, executes through procedure-name-2 (or to
  end of procedure-name-1's paragraph), then returns.
- The procedure-name arguments are paragraph-names or section-names.

### 7.2 Inline PERFORM (Format 2)

```
PERFORM [times-phrase | until-phrase | varying-phrase]
    imperative-statement-1
END-PERFORM
```

- MUST have END-PERFORM.
- The body (imperative-statement-1) is executed inline.
- imperative-statement-1 may be a sequence of imperative statements.
- imperative-statement-1 must be imperative (not conditional without explicit scope
  terminator).

### 7.3 Parser Disambiguation

The key challenge: after `PERFORM`, how does the parser know whether it's Format 1
(out-of-line) or Format 2 (inline)?

Decision tree:
1. If the first token after PERFORM (and any times/until/varying phrase) is a
   **procedure-name** (a user-defined word followed by `.` or used as a target),
   it's out-of-line.
2. If the first token is a **statement keyword** (MOVE, IF, ADD, etc.), it's inline.
3. The presence of END-PERFORM confirms inline format.

In practice, this requires lookahead or backtracking. A common approach:
- If PERFORM is followed by a data-name-like token and then THRU or TIMES or UNTIL
  or VARYING or a period, treat it as out-of-line.
- Otherwise, treat the body as inline statements until END-PERFORM.

### 7.4 EXIT PERFORM

Inside an inline PERFORM, `EXIT PERFORM` immediately exits the loop.
`EXIT PERFORM CYCLE` immediately begins the next iteration.

These are only valid inside inline PERFORM blocks.

---

## 8. SEARCH Statement Scope Rules (§14.9.37)

### 8.1 Format 1 (Serial Search)

```
SEARCH identifier-1 [VARYING {identifier-2 | index-name-1}]
    [AT END imperative-statement-1]
    {WHEN condition-1 {imperative-statement-2 | NEXT SENTENCE}}...
[END-SEARCH]
```

### 8.2 Format 2 (Binary Search)

```
SEARCH ALL identifier-1
    [AT END imperative-statement-1]
    WHEN condition-1
    {imperative-statement-2 | NEXT SENTENCE}
[END-SEARCH]
```

### 8.3 Scope Rules

- AT END handler: executed if the search reaches the end of the table without
  satisfying any WHEN condition.
- WHEN clauses: each followed by imperative-statement or NEXT SENTENCE.
- Multiple WHEN clauses in Format 1: each is evaluated in order; the first matching
  WHEN's imperative-statement is executed, then control passes to the end of the
  SEARCH.
- END-SEARCH explicitly terminates.
- Without END-SEARCH, a period terminates.
- NEXT SENTENCE in WHEN is an archaic feature (see §8 below).

---

## 9. NEXT SENTENCE vs. CONTINUE (§14.9.9, §14.9.19)

### 9.1 NEXT SENTENCE (Archaic Feature)

`NEXT SENTENCE` transfers control to the first statement following the next separator
period. It is NOT the same as "skip to the end of the current IF/SEARCH."

Critical distinction:
```cobol
IF A > B
    NEXT SENTENCE
ELSE
    MOVE 1 TO X
END-IF
MOVE 2 TO Y.
MOVE 3 TO Z.
```

`NEXT SENTENCE` transfers control to `MOVE 3 TO Z` — past the period after `MOVE 2 TO Y`.
It does NOT transfer to `MOVE 2 TO Y` (which is what END-IF would do).

### 9.2 CONTINUE

`CONTINUE` is a no-operation statement. It does nothing and control falls through to
the next statement in sequence. It serves as a placeholder where a statement is
syntactically required.

```cobol
IF A > B
    CONTINUE
ELSE
    MOVE 1 TO X
END-IF
MOVE 2 TO Y.
```

After CONTINUE, control falls through to END-IF, then to `MOVE 2 TO Y`.

### 9.3 Key Difference

| Feature        | NEXT SENTENCE                | CONTINUE              |
|----------------|------------------------------|-----------------------|
| Effect         | Jump past next period        | No operation          |
| Scope-aware?   | NO — ignores END-IF etc.     | YES — respects scope  |
| With END-IF    | Dangerous: jumps PAST it     | Safe: falls through   |
| Classification | Archaic feature              | Standard feature      |

### 9.4 NEXT SENTENCE with END-IF is Dangerous

The spec explicitly notes that NEXT SENTENCE shall NOT appear in a statement that is
delimited by an explicit scope terminator. This is because NEXT SENTENCE jumps to
after the next period, which may be well beyond the END-IF, producing surprising
behavior.

```cobol
*> DANGEROUS: NEXT SENTENCE inside END-IF delimited scope
IF A > B
    IF C > D
        NEXT SENTENCE          *> Jumps past the period below!
    END-IF
    DISPLAY "AFTER INNER IF"   *> SKIPPED by NEXT SENTENCE
END-IF
DISPLAY "AFTER OUTER IF".     *> NEXT SENTENCE jumps to HERE
DISPLAY "NEXT LINE".          *> Control continues here
```

---

## 10. Explicit Scope Terminators — Complete List

The following END-xxx keywords are explicit scope terminators:

| Terminator     | Terminates   |
|----------------|-------------|
| END-ACCEPT     | ACCEPT      |
| END-ADD        | ADD         |
| END-CALL       | CALL        |
| END-COMPUTE    | COMPUTE     |
| END-DELETE     | DELETE      |
| END-DISPLAY    | DISPLAY     |
| END-DIVIDE     | DIVIDE      |
| END-EVALUATE   | EVALUATE    |
| END-IF         | IF          |
| END-MULTIPLY   | MULTIPLY    |
| END-PERFORM    | PERFORM     |
| END-READ       | READ        |
| END-RECEIVE    | RECEIVE     |
| END-RETURN     | RETURN      |
| END-REWRITE    | REWRITE     |
| END-SEARCH     | SEARCH      |
| END-SEND       | SEND        |
| END-START      | START       |
| END-STRING     | STRING      |
| END-SUBTRACT   | SUBTRACT    |
| END-UNSTRING   | UNSTRING    |
| END-WRITE      | WRITE       |

Each END-xxx keyword matches the nearest preceding unmatched xxx statement.

---

## 11. Conditional Phrases That Create Nesting

When a statement has conditional phrases, those phrases create nested statement
positions. Each conditional phrase body accepts imperative-statement(s).

### 11.1 Paired Conditional Phrases

Most conditional phrases come in positive/negative pairs:

```
ON SIZE ERROR imperative-statement-1
NOT ON SIZE ERROR imperative-statement-2
```

The positive phrase (ON SIZE ERROR) is the error/exception path.
The negative phrase (NOT ON SIZE ERROR) is the success path.

The keyword `NOT` signals the transition from error-path to success-path. The parser
must recognize NOT ON SIZE ERROR as a single phrase keyword, not as a negation.

### 11.2 How Phrases Terminate Nested Statements

Within a conditional statement, each phrase's imperative-statement is terminated by:
1. The next phrase keyword of the same statement (e.g., NOT ON SIZE ERROR ends
   the ON SIZE ERROR handler)
2. The explicit scope terminator (END-ADD, etc.)
3. A separator period (terminates everything)

Example:
```cobol
ADD A TO B
    ON SIZE ERROR
        DISPLAY "ERROR"
        MOVE 0 TO B
    NOT ON SIZE ERROR
        DISPLAY "OK"
END-ADD
```

Here:
- `DISPLAY "ERROR"` and `MOVE 0 TO B` are the ON SIZE ERROR handler
- The `NOT ON SIZE ERROR` keyword terminates that handler and begins the success handler
- `END-ADD` terminates the NOT ON SIZE ERROR handler and the entire ADD statement

### 11.3 IF Statement Phrases

IF is unique — its phrases are `THEN` (optional keyword) and `ELSE`:

```
IF condition-1 [THEN]
    statement-1          *> THEN branch (may be imperative or conditional)
[ELSE
    statement-2]         *> ELSE branch (may be imperative or conditional)
[END-IF]
```

Unlike other conditional phrases, the THEN/ELSE branches may contain conditional
statements (not just imperative ones). This allows nested IFs without END-IF.

---

## 12. Deep Nesting Example and Analysis

```cobol
PROCEDURE DIVISION.
MAIN-PARA.
    IF WS-TYPE = "A"
        PERFORM PROCESS-A
        IF WS-COUNT > 10
            ADD 1 TO WS-TOTAL
                ON SIZE ERROR
                    DISPLAY "OVERFLOW IN A"
                NOT ON SIZE ERROR
                    IF WS-TOTAL > 100
                        DISPLAY "LIMIT REACHED"
                    END-IF
            END-ADD
        ELSE
            DISPLAY "COUNT OK FOR A"
        END-IF
    ELSE
        IF WS-TYPE = "B"
            EVALUATE TRUE
                WHEN WS-COUNT > 20
                    DISPLAY "HIGH B"
                WHEN WS-COUNT > 10
                    DISPLAY "MED B"
                WHEN OTHER
                    DISPLAY "LOW B"
            END-EVALUATE
        ELSE
            DISPLAY "UNKNOWN TYPE"
        END-IF
    END-IF
    DISPLAY "DONE".
    STOP RUN.
```

Scope analysis:
- Outermost IF (WS-TYPE = "A") is explicitly terminated by its END-IF
- Inner IF (WS-COUNT > 10) is explicitly terminated by its END-IF
- ADD is explicitly terminated by END-ADD (making it a delimited scope / imperative)
- Innermost IF (WS-TOTAL > 100) is explicitly terminated by END-IF
- EVALUATE is explicitly terminated by END-EVALUATE
- Inner IF (WS-TYPE = "B") is explicitly terminated by END-IF
- The period after "DONE" terminates the DISPLAY statement and the sentence
- The period after `STOP RUN` terminates that statement and its sentence

### 12.1 Same Example with Period Termination (Archaic Style)

```cobol
PROCEDURE DIVISION.
MAIN-PARA.
    IF WS-TYPE = "A"
        PERFORM PROCESS-A
        IF WS-COUNT > 10
            ADD 1 TO WS-TOTAL
                ON SIZE ERROR
                    DISPLAY "OVERFLOW IN A".
```

The period after "OVERFLOW IN A" terminates:
1. The DISPLAY
2. The ON SIZE ERROR phrase (and the ADD becomes conditional)
3. The inner IF (WS-COUNT > 10)
4. The outer IF (WS-TYPE = "A")
5. The sentence

There is NO ELSE branch. There is no NOT ON SIZE ERROR path. The period ends everything.

---

## 13. Parser Implementation Guidance

### 13.1 Scope Stack

The parser should maintain a scope stack. Each entry tracks:
- The statement type (IF, PERFORM, EVALUATE, ADD, etc.)
- Whether we're in a specific phrase (THEN, ELSE, ON SIZE ERROR, WHEN, etc.)
- Whether the statement has been explicitly terminated

### 13.2 Period Handling Algorithm

When a separator period is encountered:
1. Pop ALL entries from the scope stack
2. Each popped statement is implicitly terminated
3. The sentence is complete

### 13.3 END-xxx Handling Algorithm

When an END-xxx token is encountered:
1. Search the scope stack top-down for the nearest matching xxx statement
2. Pop everything above it (those are implicitly terminated by rule 4(a))
3. Pop the matched statement (explicitly terminated)
4. The delimited scope statement is now classified as imperative

### 13.4 Phrase Keyword Handling

When a phrase keyword is encountered (ELSE, NOT ON SIZE ERROR, etc.):
1. Search the scope stack top-down for the matching statement
2. Intermediate entries between the stack top and the matched statement must be
   implicitly terminated (rule 4(b))
3. Transition the matched statement to its next phrase

### 13.5 Statement Keyword as Terminator

When a new statement keyword is encountered (MOVE, ADD, IF, etc.):
- It does NOT terminate containing scopes
- It begins a new statement, which is pushed onto the scope stack
- The new statement is nested within whatever scope is currently open

The only things that terminate scopes are:
1. Separator period (terminates ALL)
2. Explicit scope terminator END-xxx (terminates one matching scope)
3. A phrase keyword of a containing statement (terminates inner scopes up to the
   containing statement)

### 13.6 Common Pitfalls

1. **Treating period like a single-scope terminator**: Period terminates ALL scopes,
   not just the innermost.

2. **Not recognizing delimited scope as imperative**: `ADD A TO B ON SIZE ERROR
   DISPLAY "ERR" END-ADD` is imperative, not conditional.

3. **ELSE matching the wrong IF**: ELSE always matches the nearest preceding
   unmatched, unterminated IF.

4. **NEXT SENTENCE inside END-IF**: NEXT SENTENCE ignores scope terminators and
   jumps to after the next period. This is a semantic issue, not a parsing issue —
   the parser should parse it normally but may want to emit a warning.

5. **Inline vs out-of-line PERFORM**: The parser must disambiguate based on what
   follows PERFORM (and its optional phrases). A procedure-name means out-of-line;
   a statement keyword means inline.

6. **EVALUATE WHEN is not SEARCH WHEN**: In EVALUATE, WHEN introduces a selection-
   object (value matching). In SEARCH, WHEN introduces a condition. The parser
   must track which containing statement it's in.

---

## 14. Special Cases

### 14.1 Empty Paragraphs

A paragraph may contain zero sentences:
```cobol
EMPTY-PARA.
NEXT-PARA.
    DISPLAY "HELLO".
```
`EMPTY-PARA` is a valid paragraph with no statements.

### 14.2 Multiple Statements per Sentence

```cobol
MOVE 1 TO A MOVE 2 TO B MOVE 3 TO C.
```
This is ONE sentence with THREE statements. All three execute sequentially.

### 14.3 Declaratives

Declaratives sections use USE statements and have special scoping:
```
DECLARATIVES.
section-name SECTION. USE AFTER STANDARD EXCEPTION PROCEDURE ON file-name.
    paragraph-name.
        [sentences]...
END DECLARATIVES.
```

The `END DECLARATIVES` marker (two words, no hyphen) terminates the declaratives
region. This is NOT an explicit scope terminator in the same sense as END-IF — it
is a structural delimiter.

### 14.4 GO TO as Last Statement

From §14.9.17: A GO TO statement appearing in a consecutive sequence of imperative
statements within a sentence shall appear as the last statement in that sequence.

This means:
```cobol
*> LEGAL:
IF A > B
    MOVE 1 TO X
    GO TO SOME-PARA
END-IF.

*> ILLEGAL (per spec): statement after GO TO in same sequence
IF A > B
    GO TO SOME-PARA
    MOVE 1 TO X        *> Unreachable, spec forbids this
END-IF.
```

### 14.5 STOP RUN Terminates Execution

`STOP RUN` terminates the entire run unit. It does not interact with scope
termination rules — it is simply an imperative statement that happens to end
the program. The scope termination rules still apply to the syntax containing it.

---

## 15. Summary of Key Rules for Parser Implementers

1. **Sentence = statements + period.** Every sentence ends with a separator period.

2. **Period terminates ALL open scopes.** Not just the innermost — all of them.

3. **Explicit scope terminators (END-xxx) terminate exactly one matching scope.**
   They make that statement a delimited scope statement (imperative).

4. **ELSE matches nearest unmatched IF.** END-IF matches nearest unterminated IF.

5. **Conditional phrases create nested imperative-statement positions.**
   The handler body must be imperative (or a sequence of imperatives).

6. **IF is special**: its THEN/ELSE branches can contain conditional statements.
   Other conditional phrase handlers (ON SIZE ERROR, WHEN, etc.) require imperative
   statements only.

7. **Inline PERFORM requires END-PERFORM.** Out-of-line PERFORM must not have it.

8. **NEXT SENTENCE jumps past the next period**, ignoring scope terminators.
   CONTINUE does nothing. They are NOT interchangeable.

9. **A statement keyword does NOT terminate containing scopes.** Only period,
   END-xxx, and phrase keywords of containing statements do.

10. **The paragraph/section hierarchy is structural, not scope-related.**
    Scope termination is about statements and sentences, not paragraphs.
