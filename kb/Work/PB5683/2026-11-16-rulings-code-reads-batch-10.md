# Rulings on the non-agreeing rows of the 1985 code reads, batch 10 (the Mythos lane, Monday 2026-11-16 04:46 PST)

**The batch.** Slices 1985-186 to 1985-215 of `E:/COBOL-coord/scratch/coderead-1985/`: 176 KEPT rows of 1985 that
were closed on a witness AFTER the first population was built and so had never been read
(`my-20261115/store-import/unread-kept.json`). Read on the code at `--std 85` at main acffeb97ab (the Mythos lane's
six runs of the operator's workflow: wf_30ad8780-d5f, wf_c19cedfb-502, wf_729025f2-331, wf_5796f951-4c2,
wf_fbd69145-7f8, wf_42d314e7-e6a). 159 agree with the inherited verdict. Seventeen do not
(`coderead-1985/non-agreeing-186-215.json`). The refuters also raised four leads outside their rows.

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) ran its own
fixed-form programs at `--std 85` and `--std 2023` on a compiler built at main acffeb97ab:
`my-20261115/contradictions-b10/rulings.json`, programs under `contradictions-b10/src/`. I ran two myself at `--std 85`
with the tracked probe runner. `grpv.cob` printed `G1=[ABCD]`, `G3=[    ]` and `G5=[0000]`: the group VALUE is lost
when a subordinate carries REDEFINES. `iclose.cob` printed `AFTER CANCEL GST=10` and `AFTER INITIAL GST=10`.

**Ruling: fourteen mechanisms. Nine UPHELD (seven new notes, two existing), three rows REJECTED, two NOT
DECIDED.** Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-SR-VII.2.8.3-1`, `1985-FMT-VII.2.8.2`, `1985-GR-VII.2.8.4-3`, `1985-GR-VII.2.8.4-5` | UPHELD (b) | under_rejects | RECORD DELIMITER. The clause on a fixed-length file compiles with a warning only, and ANY word is taken as its implementor-name (`RECORD DELIMITER IS F1`, the file's own name, compiles) though the implementation defines none. See below: decided. | PB5844 |
| 2 | `1985-GR-VII.2.8.4-2`, `1985-SR-VII.2.8.3-2` | REJECTED | none | The STANDARD-1 arm is a hardware element the implementation does not claim, which 1985 lets it decline. The register states the absence of reel devices citing the 2023 text only: a 1985 sentence is owed (carried by PB5844). | (PB5844) |
| 3 | `1985-GR-VII.1.3.5-5`, `1985-GR-VII.1.3.5-L2.5` | UPHELD (a) | wrong_answer | An OPEN refused with status 90 (LINAGE of zero) leaves the file open: a second OPEN gives 41, a WRITE 90, the CLOSE 00. At 1985 a 9x statement is unsuccessful, so the WRITE is 48 and the CLOSE 42. The 2023 continuation (kb/Work PB526) is applied at every edition. Checker's confidence: medium-high. | PB5845 |
| 4 | `1985-GR-X.5.1-4` | UPHELD (b) | under_rejects, wrong_answer | A Linkage Section item that is not in the USING phrase is accepted as a subscript or a reference-modification operand. Sibling of the landed kb/Work PB3287. | PB5846 |
| 5 | `1985-GR-X.5.3.4-10`, `1985-GR-X.5.4.4-3` | UPHELD (b) | wrong_answer | The implicit CLOSE of a CANCEL, and of an INITIAL program's EXIT PROGRAM, never stores an I-O status: the FILE STATUS item keeps 10 where 00 is due. I ran it. Sibling of kb/Work PB5685 (the same emitted block). | PB5847 |
| 6 | `1985-SR-IV.4.2.2.1-2` | UPHELD (b) | under_rejects | `COPY LENGTH.` and `COPY ADD.` compile: a text-name is never checked against the reserved words. Sibling of kb/Work PB5482. The checker's confidence in the 2023 half is medium. | PB5848 |
| 7 | `1985-SR-VI.5.14.3-2` | NOT DECIDED | under_rejects under one reading | A group declared USAGE BINARY accepts a COMP subordinate. Is "the same usage" of SR2 the alternative as written, or the manner of representation? The data-model design chose the second on purpose. An open reading. | PB5849 |
| 8 | `1985-SR-VI.5.6.3-1` | UPHELD (a) | under_rejects | JUSTIFIED, and BLANK WHEN ZERO, on a report group entry that has subordinates compiles at 85; 2023 refuses it. | PB5850 |
| 9 | `1989-FMT-A.2.37.2` | UPHELD (b) | rejects_legal_source | `FUNCTION REM (A (- B))` is refused: the mechanism of the open kb/Work PB5781. | joins PB5781 |
| 10 | `1985-GR-XIII.3.17.4-3` | REJECTED | none for this row | The sign representation the rule states is right for every entry the compiler admits. SIGN SEPARATE with BLANK WHEN ZERO is the open kb/Work PB5790's question. | (none) |
| 11 | `1989-RV-A.2.41.4-2` | REJECTED | none | The 1989 text asks for an approximation and leaves its precision to the implementor; the register documents the returned value. | (none) |
| 12 | `1985-GR-VI.5.15.6-3` (a lead outside its slice) | UPHELD (b) | wrong_answer | A group VALUE is silently dropped when a subordinate carries REDEFINES: `01 G3 VALUE "ABCD". 05 A3 PIC X(4). 05 A4 REDEFINES A3 PIC X(4).` displays spaces. `§1985:VI.5.15.6 GR3` says "the group area is initialized without consideration for the individual elementary or group items" (cite.py OK). I ran it. The row's read in slice 1985-013 agreed without trying it. | PB5851 |
| 13 | `1985-GR-VI.6.19.4-4` (a lead) | UPHELD (a) | wrong_answer | `MOVE -100` to `S99 SIGN LEADING SEPARATE` stores `+00`. The 1985 rule places the SENDING operand's sign, so `-00` is due; the compiler makes a zero result positive. Checker's confidence: medium (the 2023 wording ties the sign to the value). | PB5852 |
| 14 | `1985-GR-XIII.3.10.8-4`, `1985-GR-XIII.3.10.8-6` (a lead) | NOT DECIDED | wrong_answer under one reading | In a report with no PAGE clause a first group that begins LINE PLUS 0 ends with LINE-COUNTER 3 where one reading gives 2. Does an unpaged report have a line 0? Checker's confidence: low-medium. (`-4` is already contradicted by kb/Work PB5823 on another mechanism.) | PB5853 |

A fifteenth item, a lead with no row: `1 = A OR 2` and `IF 1 = 2` only warn at 85. It JOINS kb/Work PB5826 (the same
1985 sentence, the same site).

**Mechanism 1, decided.** Three things are true of the 1985 text. `§1985:VII.2.8.3 SR1` says the clause "may be
specified only for variable length records" (cite.py OK): a clause on a fixed-length file is a refusal, whatever
else is decided. The format admits STANDARD-1 or an implementor-name, and `§1985:VII.2.8.4 GR3` makes the method
"that associated with implementor-name-1 by the implementor" (cite.py OK): a word the implementor associates with
no method is not an implementor-name, and accepting it with a warning is accepting source the format does not
print. And STANDARD-1 is a hardware element: `§1985:I.1.5.2.5.1` says "Language elements that pertain to specific
hardware components for which support is not claimed need not be implemented" (cite.py OK), which covers the
STANDARD-1 arm and nothing else of the clause.
The checker left open whether the set of implementor-names may be empty. I decide the shape that is right under
either reading, and it is small: the implementation DEFINES ONE implementor-name for the framing its variable-length
record sequential files already use, the clause with it selects that method (it is the only one, so the file
behaves as it does today), EVERY OTHER WORD is refused, and the clause on a fixed-length file is refused. STANDARD-1
stays a documented decline. Which word names the method is the implementor's: by CLAUDE.md rule 1 the fix looks at
the implementor-names GnuCOBOL defines for this clause first (not read here) and takes one that means this framing,
or defines its own. A 1985 entry in the register states all of it.

**What follows.**
- Rows of mechanisms 1, 3, 4, 5, 6, 8, 9, 12 and 13 do not close on their inherited verdict (fourteen rows). The
  rows of mechanisms 2, 10 and 11 are confirmed. The rows of mechanisms 7 and 14 wait.
- The 2023 rows of mechanisms 1, 4, 5, 6, 9 and 12 are re-recorded non-conforming by a registrar (the checker's
  `rulings.json` names them, with a remark on some: the registrar takes only an id that names a row).
- The readable KEPT rows are read. Counted from the rulings files by script (batches 1 to 9: 83 rows
  contradicted) and this batch's table (14): 97 of 1,282 rows contradicted, 7.6 %. Still owed a read: two rows
  whose first read was withdrawn, the rows open today (read after their fixes), and the six rows the harness
  branch closed.
