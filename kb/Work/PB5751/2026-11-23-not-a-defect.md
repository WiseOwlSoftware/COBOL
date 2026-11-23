# PB5751, 2026-11-23: not a defect; the normative text makes the condition exist with the phrase, and EC-SIZE-TRUNCATION is the last exception status

Wave 20262150 group E re-probed the note's program on its branch build (`>>TURN EC-SIZE CHECKING ON`, `ADD 1 TO A ON SIZE ERROR DISPLAY "S [" FUNCTION
EXCEPTION-STATUS "]"`, `A PIC 9 VALUE 9`): `--std 2002` prints `S [EC-SIZE-TRUNCATION` and `A=9`, and the default edition prints the same. The note
expected spaces from the no-phrase branch of §2002:14.6.4 and from the informative §2002:E.15. Neither supports it.

- **The normative text states the condition exists when the phrase is written.** §2002:14.6.4: "If an EC-SIZE exception condition exists during the
  execution of the arithmetic statement other than during item identification and a SIZE ERROR phrase is specified for the statement, processing of the
  size error condition occurs as described below for the SIZE ERROR phrase, and no EC-SIZE exception declaratives are performed" (`cite.py --check
  "§2002:14.6.4"` OK, and the same sentence in §2014:14.7.4 and §2023:14.7.5, `cite.py --check "§2023:14.7.5"` OK). A condition that "exists" is the one
  §2002:14.5.12.1 (§2023:14.6.13.1.1) raises when checking is enabled, and "if checking for an exception condition is enabled and an exception status
  indicator is set ... the associated exception condition is raised, the last exception status is set to indicate that exception condition". The phrase
  enables the checking itself ("When the SIZE ERROR phrase is specified for an arithmetic statement, checking for the size error condition is enabled for
  the arithmetic operations"). What the phrase changes is the PROCESSING: the receiver is left unchanged, the phrase's imperative statement runs, and no
  EC-SIZE declarative is performed. That is exactly what `EcEmitter.EmitSizeHandling` emits: the last exception status is set, the selection and the
  fatal default are skipped. The no-phrase branch lists which EC-SIZE name is set when there is no phrase; it is not the only place the condition
  exists.
- **The informative annex disagrees with the normative text, and the normative text wins.** §2002:E.15 says the raising "is disabled for that
  statement, except during item identification". The sentence it summarizes is the one quoted above, which says the condition exists. Reading E.15 as
  "no last exception status" would also leave `FUNCTION EXCEPTION-STATUS` blank inside the very phrase that handles the condition, which no rule states.
- **The suite already pins this reading at 2023** by golden: `conformance:cases/14.7.5-SIZE-ERROR-phrase-and-size-error/pb74_sdidi_store_size_error`
  prints `PROH=EC-SIZE-TRUNCATION` and `ECOV=EC-SIZE-TRUNCATION` from `FUNCTION EXCEPTION-STATUS` inside an ON SIZE ERROR phrase, with
  `>>TURN EC-SIZE CHECKING ON`.

The note's own admission that its SIZE legs were "removed from the goldens, never edited toward the compiler" stands; no rule row depends on them.
Retired with no change. Should the owner read E.15 as controlling, the fix is one arm in `EcEmitter.EmitSizeHandling` (skip the `ExceptionState.Set`
when `hasPhrase`) plus the 2023 goldens that pin the opposite, and it would be an owner decision on an informative annex against the normative text.
