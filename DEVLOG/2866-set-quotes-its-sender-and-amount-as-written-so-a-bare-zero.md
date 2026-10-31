## Entry 2866 — 2026-10-31 12:00 PDT — SET quotes its sender and amount as written, so a bare ZERO prints as ZERO (PB4209)

**Mechanism (PB4209, the SET sibling of PB3961).** `ZeroTokenRewriter` gives a bare ZERO in an arithmetic slot the token text "0" on
purpose, and `GetText()` glues tokens with no separators. The SET diagnostics built their sender from `GetText()`: `SET N TO ZERO` was
refused with `SET 'N' TO '0' violates ISO §14.9.39.3 SR4`, `SET DP TO ZERO + 1` with `TO 0+1: identifier-6 …`, and `SET N UP BY ZERO`
with COBOL2112 `UP/DOWN BY 0`.

**Re-probe.** All three reproduced on base 84cdb410f (probes in the scratch dir `p/`). The note named four sites; the root is one
producer: `SetSenders.Classify` and `SetSender.OfReference` made `SetSender.Text`, which the pointer, program-pointer,
function-pointer and object-reference arms (`SetBinder`, `OoBinder`) all print, and `BindSetTo` and `BindSetUpDown` each read
`amount.GetText()` besides.

**Fix.** All four producers read `DataBinder.WrittenText` (PB3961's one "as the programmer wrote it" reader). No printer of
`sender.Text` changed. The note's listed lines 554, 571, 661, 810, 1365 and 1416 are printers of that one field.
`SetSenderWrittenTextDriftTests` (tree sweep, proven to fire on the carried text) keeps `SetBinder.cs` and `SetSender.cs` from
returning to `amount.GetText()` / `dref.GetText()` as a sender.

**Goldens** (`cases/14.9.39-SET/`, each refused at all four editions, `expect` = the message text): `pb4209-set-to-zero-numeric-receiver`
(SR4, `TO 'ZERO + 1'`), `pb4209-set-up-by-zero-integer-receiver` (COBOL2112, `UP/DOWN BY ZERO`), `pb4209-set-pointer-to-zero-expression`
(SR17, `TO ZERO + 1`). Citations run through `cite.py --check` (§14.9.39.3 4), 17)).

**Sibling sweep.** The other `{x.GetText()}` message sites of the Compiler assembly were swept by PB3961; the SET family's own are all
receiver names, keywords or locale words. Left unfixed and registered as leads: EVALUATE `WHEN '{item.GetText()}'`, OoBinder BY CONTENT
`{bx.GetText()}`, MoveBinder `{move.GetText()}` (all quote an operand node by token text).

