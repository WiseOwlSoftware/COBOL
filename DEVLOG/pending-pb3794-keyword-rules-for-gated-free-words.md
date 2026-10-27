## PB3794: a word a program declares keeps its keyword where a format still spells it (MESSAGE, MULTIPLE, ORDER, METHOD)

**Mechanism.** The §8.9 reservation gate works on tokens: when a program declares a word that §8.9 leaves free at
the compile edition, `ReservationGateRewriter` retypes EVERY occurrence of that word to `IDENTIFIER` and parses
again. A format that still spells the word as a keyword at that edition then meets an `IDENTIFIER` where its rule
wants the token. kb/Work PB3794 found it with `01 MESSAGE PIC 9.` and `RECEIVE … CONTINUE AFTER MESSAGE RECEIVED`
at COBOL-2023: 2023 §8.9 lists MESSAGE-TAG but not MESSAGE, and §14.9.31.2 prints `MESSAGE RECEIVED`.

**Re-probe** on this build: `recv4.cob` gave `COBOL2269: unexpected 'RECEIVED'`. The sweep (every gated token
against every hand-grammar rule that spells it, read against the word's reservation pattern) found three more:
`01 MULTIPLE PIC 9.` with `LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS` was `unexpected 'MULTIPLE'` at 2002,
2014 and 2023 (§8.9 reserves MULTIPLE at 85 only; 2023 §8.10 lists it as the LOCK ON phrase's context-sensitive
word), `01 ORDER PIC 9.` with `SORT … WITH DUPLICATES IN ORDER` was `unexpected 'ORDER'` at 85, and METHOD at 2023
was already handled by a hand rule (kb/Work PB2623). The design doc called METHOD "the one live case". It was one of four.

**Fix shape.** One generated mechanism. The design doc's earlier claim is corrected in the same change. A
`cobol-words.json` row now carries the determination `keywordWhereFree` (cited text naming the format and edition), and
`gen-cobol-words.ps1` step 5c emits `<word>Keyword : TOKEN | {wordAhead("WORD")}? IDENTIFIER` into `CobolWords.g4`
for each row. `mcsReceiveStatement`, `lockOnPhrase`, `multipleFileClause`, `sortDuplicatesPhrase` and
`orderTableClause` spell their word through it; the hand `methodKeyword` in `CobolOO.g4` is deleted (the generated
rule has the same name, so its callers are unchanged). `DataBinder.MapLockMode` reads `multipleKeyword()`. The
`mcsReceiveStatement` comment that claimed the parser's lookahead already told the arms apart is replaced by the true
account.

**Drift tests** (`CobolWordsDriftTests`): the generated rules are exactly the rows, each in the one shape and on a
gated word; no hand-written rule spells a row's bare token (a synthetic grammar shows that guard failing); and the
determinations are re-derived from the standard where the repository has it. Every gated word that 2023 §8.9 leaves
free but a 2023 general format underlines, and every gated §8.10 word, must carry a determination. That oracle finds
exactly METHOD, MESSAGE and MULTIPLE, and fails naming MESSAGE when its row is removed. The repository has no text for
the earlier editions, so ORDER at 85 stands on its row's citation.

**Goldens.** `wiseowl/cases/14.9.31-RECEIVE/pb3794_message_data_item_continue_phrase` (2023: four CONTINUE spellings,
the data item also used as arithmetic-expression-1), `cases/12.4.5.9-LOCK-MODE/pb3794_multiple_data_item_lock_on_multiple`
(2002, 2014, 2023), `cases/14.9.40-SORT/pb3794_order_data_item_duplicates_in_order` (85; the §14.9.40.4 GR3 b) release
order). Three COBOL0901 refusals in `cases/8.3.2-COBOL-words/` cover the editions that reserve each word. One
expectation changed: `pb755-lock-on-omitted` (`WITH LOCK MULTIPLE RECORDS`) is still refused at the same line and
column, but the error now reads `missing token before 'MULTIPLE'` instead of `unexpected 'MULTIPLE'`.

**Rows.** None re-verdicted. FMT-14.9.31.2 (DOCUMENTED-NON-SUPPORT), FMT-12.4.5.9.2, FMT-14.9.40.2, GR-14.9.40.4-3,
SR-8.3.2.1-1 and SR-8.3.2.1-3 gain the new witnesses (batch w20261720a-PB3794, GAP 31 → 31).
