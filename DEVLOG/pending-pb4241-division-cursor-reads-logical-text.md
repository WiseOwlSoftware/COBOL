## PB4241: the division cursor reads logical text, skips COPY and REPLACE statements, and follows conditional groups

The obsolete COBOL-85 comment-entry paragraphs (AUTHOR ... SECURITY, REMARKS) exist only in an IDENTIFICATION DIVISION, so the
§6.5 converter keeps a `DivisionCursor` (one rule, PB2739) and reads each line's words to know where it stands. The Mythos
front-end review (N12) found that the cursor was fed the wrong TEXT, in three ways, each of which left a legal program with a
comment-entry or a REMARKS paragraph rejected.

**Re-probe on this build (all three reproduced).** `PROCEDURE DIVI` / `-SION.` at 85, 2002 and 2014: COBOL0001 on the REMARKS
paragraph after it. `COPY k2a REPLACING ==PROCEDURE DIVISION== BY ==X==.` then `INSTALLATION. "UNCLOSED HERE.` at 85: COBOL5563
(the comment-entry read as program text). `>>IF NEVER IS DEFINED / IDENTIFICATION DIVISION. / >>END-IF` in a PROCEDURE DIVISION
followed by a paragraph named REMARKS at 2002-2023: COBOL0001. Their COPY twins (library text, read by the COPY driver) printed
correctly, which is why the PB2739 rule looked shared when its text was not.

**The fix, one mechanism.**
1. *Logical lines, not physical ones.* The converters skipped fixed-form continuation lines and read each physical line's own
   program-text area, so half a continued word was never a word. `LogicalLineBuilder.ReadInto` now hands the cursor the
   resultant lines it has not read (comments gone, continuations joined), asked where a new source line is about to be
   classified, in both converters; the end of a segment reads the rest, so the next segment starts in the division this one left.
   Debugging lines are not read (source or commentary is the unit's WITH DEBUGGING MODE clause's to say, later). A line that
   continues a floating-indicator literal is no longer asked whether it opens a comment-entry in fixed form either, as in free
   form (the sibling arm).
2. *A COPY or REPLACE statement is not text that stands in a division.* The cursor passes over the statement from its keyword
   to its separator period outside pseudo-text (§8.3.5 3) and 6)), across lines. `CopyProcessor` had its own `Skip` for the
   statement it parsed, which left REPLACE operands read; the cursor's rule covers both and `Skip` is deleted (no shim).
3. *Conditional groups.* §6.5 converts "before the application of replacing and conditional compilation actions", so the
   converter cannot see which branch of a `>>IF` / `>>EVALUATE` survives. The cursor follows the group structure
   (`ConditionalGroup`) and counts a division word in it only when every branch ends in the same division (the branch no
   condition selects counted, absent `>>ELSE` / `>>WHEN OTHER`), else the text stays where it was before the group. No edition
   has both a comment-entry (removed by 2002) and a `>>IF`, so this is the implementor's determination (docs/CONFORMANCE.md
   D-CE2). Its residue is stated there: a selected branch that alone changes the division is not followed by the converter. The
   exact reading needs the comment-entry decision made after conditional compilation, a stage-order redesign this change does
   not take; it is registered in the branch report for the registrar.

**Goldens** (`cases/6.5-Logical-conversion/`): `pb4241_continued_division_header` (accept 85-2014; 2023 removed the continuation
of a word, Annex E.2), `pb4241_copy_operand_names_division` (85; a COPY on one line, one across two, and a REPLACE, all naming
a division, then comment-entry paragraphs holding an unclosed quotation mark), `pb4241_omitted_branch_division_header`
(2002-2023) and its 85 refusal `pb4241_omitted_branch_division_header_at_85` (`>>IF` is a 2002 introduction, COBOL0900). Each
positive was run against the pre-fix source and failed there (COBOL0001, COBOL5563, COBOL0001). Unit: `DivisionCursorTests` gains
the statement-skip, line-by-line, continued-header and eight conditional-group cases.

**Register.** PB4241 landed; it claimed and closes no inventory row (the rule, §6.5, was already verdicted).

**Self-review.** Architecture: one rule, one place (the converters and the COPY driver still share `Feed`; `Skip` and the
per-physical-line read are gone). Full code: the cited rules are §6.5 6) b) (join), §8.3.5 3) and 6) (period, pseudo-text),
§7.3.16.2 / §7.3.13.2 (group phrases), each run through `cite.py --check`. Performance: each logical line is scanned once, as
the physical line was before. Duplication: no second statement-skip rule.
