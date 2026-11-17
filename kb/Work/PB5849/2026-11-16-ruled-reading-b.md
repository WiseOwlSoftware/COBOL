Ruled 2026-11-16: reading B. The row `1985-SR-VI.5.14.3-2` is confirmed under a determination; the register sentence and the witness are owed by the next records branch, and this note stays open until they are on main.

## The ruling: reading B. The row `1985-SR-VI.5.14.3-2` is confirmed under a recorded determination
The text admits both readings (the note's checker says so, and I agree: "specified" leans to A, GR2's definition of
the clause by its manner leans to B, and no 1985 sentence chooses). CLAUDE.md rule 1 then gives the order: the
standard if it controls, otherwise GnuCOBOL, otherwise IBM or Micro Focus.

- **GnuCOBOL** (measured by this session, GnuCOBOL 3.2.0 in WSL, programs and outputs in
  `E:/COBOL-coord/scratch/my-20261115/pb5849-gc/`): `usg2.cob` (a COMPUTATIONAL item under a USAGE BINARY group)
  compiles and prints `+0001` at `-std=cobol85`, `-std=cobol2014` and `-std=default`. Its parser gives BINARY and
  COMP ONE usage: both alternatives of the `usage:` rule call `check_and_set_usage (CB_USAGE_BINARY)`
  (`gnucobol-3.3-dev/cobc/parser.y`, lines 8225-8244 of the copy at `my-20261115/gc-cobc/`). So in GnuCOBOL the two
  words name the same usage. (GnuCOBOL enforces nothing of SR2 at all: the control `usg.cob`, BINARY over
  PACKED-DECIMAL, also compiles; its group-usage check is inside `#if 0` in `cobc/field.c`. That silence is not an
  answer to SR2 and is not followed: the refusal COBOL1927 for two different manners stays.)
- **IBM Enterprise COBOL** documents BINARY, COMPUTATIONAL (COMP) and COMP-4 as equivalent usages. NOT re-read by
  this session: the registrar's register sentence cites it only after reading the Language Reference's USAGE
  clause section, or leaves it out.

So the answer GnuCOBOL gives where the text is open is that BINARY and COMP are one usage, which is what the
data-model design chose (`DESIGN-data-model.md` 2.7.1 item 3) and what the compiler does. No code changes.

## What a registrar records (records branch 4)
1. `code_reads.py adjudicate` for `1985-SR-VI.5.14.3-2`: confirmed, under this ruling (the tracked copy of this
   file is the deciding file).
2. The register: `docs/conformance/7/DOC-A.1-208.md` cites the 2023 text alone. Add that the determination holds
   at every edition, naming `§1985:VI.5.14.4 GR4` and `§1985:C.2` item 24 (each through cite.py), and ONE sentence:
   for the rule that a group's usage and a subordinate's be the same (`§1985:VI.5.14.3 SR2`, and the 2023 rule the
   note names, `SR-13.18.60.3-2`, by its own words), COMPUTATIONAL and BINARY are one usage because they are one
   manner of representation here; GnuCOBOL gives both words one usage. Then
   `python scripts/doc_audits.py --write-register`.
3. A witness case at `--std 85` under the 1985 USAGE clause's directory: the group of `usg2.cob` compiles and its
   item holds the value moved (expected `+0001`: the rule that makes it legal is the determination, named in the
   case's comment), and the control of `usg.cob` is refused COBOL1927 (expected from SR2: BINARY and PACKED-DECIMAL
   are two manners). Sources: `E:/COBOL-coord/scratch/my-20261115/contradictions-b10/src/usg2.cob`, `usg.cob`.
4. `kb/Work/PB5849.md`: `status: landed`; a dated file beside it with this ruling; `DESIGN-data-model.md` 2.7.1
   item 3 gains the pointer to the register sentence (it says today that no text is cited for the fold).
