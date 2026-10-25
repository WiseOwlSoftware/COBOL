## Entry 2568 — 2026-10-25 14:01 PDT — PB935: the vendor SET ... TO ENTRY form obeys the content rule of a restricted program-pointer (ISO 13.18.60.4 GR25)

**Re-probe.** On a fresh Debug build, with `PPT IS TYPEDEF USAGE PROGRAM-POINTER TO PBT935` and `PP1`, `PP2 TYPE PPT`,
`SET PP1 TO ENTRY "PBT935"`, `SET PP2 TO ENTRY NM` and `SET PP1 PP2 TO ENTRY "PBT935"` all compiled clean, while
`SET PP1 TO ADDRESS OF PROGRAM "PBT935"` and `SET PP1 TO PU` (an unrestricted program-pointer) drew COBOLNET1959. So
the Micro Focus / IBM spelling could load an unrestricted address into an item that §13.18.60.4 GR25 binds to NULL or
the address of a program with the prototype's signature.

**Decision applied.** The note was decided by the ISO text on 2026-09-30: GR25 is an obligation on the ITEM, whatever
statement loads it, so the extension may not weaken it. The ENTRY operand (a literal or an identifier whose content is
the program name) names a program by value, which makes it an unrestricted program-address, associated with no
program-prototype, exactly the literal-1 and identifier-1 arms of `ADDRESS OF PROGRAM` that §14.9.39.3 SR22 refuses
for a restricted receiver (§8.4.3.13.4 GR3 restricts only the program-prototype-name-1 arm). All citations were run
through `cite.py --check`.

**Fix.** `SetBinder.BindSetEntry` asks the one helper the ISO arms ask. `RestrictedReceiverNeedsPrototype` now takes
the statement text and the rule sentence instead of two ISO-only strings, so one body serves both spellings; the new
`RestrictedReceiverRefusesEntry` supplies the ENTRY wording (GR25 and SR22, pointing to `ADDRESS OF PROGRAM
program-prototype-name-1`, GR3). Both the literal and the identifier return are covered, and every receiver of the
list is asked, so `SET UPP RP TO ENTRY NM` is refused whatever the order. The doc comment that called the ENTRY
asymmetry "the standard's" is replaced by the shared reading. The diagnostic is the existing COBOLNET1959; no new code
was needed.

**Goldens.** Five shipped witnesses loaded a restricted receiver through ENTRY and pinned the permissiveness
(pb817_restricted_program_pointer, pb848_pointer_usage_to_optional, pb609_call_restricted_program_pointer,
pb609-call-restricted-program-pointer-signature, pb1063_restricted_pointer_by_content), as did a test source in
`OptionalWordSubsetDriftTests`. They now load it with `SET pp TO ADDRESS OF PROGRAM program-prototype-name-1`, the
conforming 2002 spelling; their expected output is unchanged. New negatives under `14.9.39-SET`:
`pb935-set-entry-restricted-literal` and `pb935-set-entry-restricted-identifier-list` (COBOLNET1959 at 2002, 2014,
2023). ENTRY into an unrestricted receiver is still pinned by pb415 and pb418.

**Docs.** `docs/CONFORMANCE.md` (extension register) and `docs/COBOLNET_INTERPROGRAM_DESIGN.md` state the discipline.
Rows: none claimed; GR-13.18.60.4-25 stays CONFORMS and now has the ENTRY lane under it.

**Siblings.** `SET pp TO pp` and `ADDRESS OF PROGRAM` were already screened. Two other lanes still accept an
unrestricted address into a restricted program-pointer: `INITIALIZE pp REPLACING PROGRAM-POINTER DATA BY unrestricted`
(measured, compiles clean; outside this group, reported as a lead) and `CALL ... RETURNING pp` (PB3686).

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` on the head, Linux half (WSL clone of HEAD) GREEN;
the new negatives ran in leg 1 (`pb935-set-entry-restricted-literal` and `pb935-set-entry-restricted-identifier-list`, each at
2002, 2014 and 2023, all Passed). Self-review of the diff found nothing further.

