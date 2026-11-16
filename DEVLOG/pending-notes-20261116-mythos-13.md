## Four notes and four finding files from the Mythos lane's batch-11 rulings of 2026-11-16

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed, and a Sites block.

- PB5856 (defect, MAJOR): ACCEPT from the device into a binary or packed numeric item converts the whole 80-character record, so characters the rule says are ignored decide the value (`12345678` into `PIC 9(4) COMP` stores 5678); every edition; decided: the code is wrong, and the sentence of A.1 item 1 that gives such an item one whole record with it.
- PB5857 (defect, MINOR): the RELATIVE KEY agreement of an external file connector identifies the key item by its name path, so two programs may put it at different character positions of the external record (every edition); decided: the same external data item is the same storage, checked with the identity the RECORD KEY agreement uses.
- PB5858 (defect, MAJOR): at `--std 85` UNSTRING with no POINTER phrase from a zero-size sender runs ON OVERFLOW, where the 1985 text gives NOT ON OVERFLOW; the fix of PB5816 carried its ruling to the form with no POINTER, and leg C of its witness expects the wrong answer.
- PB5859 (defect, MINOR): INSPECT TALLYING with a reference-modified numeric counter passes the binder and fails in the C# compile (CS1503, exit 70), at every edition; the fix of PB5734 covered STRING and UNSTRING only.

Four dated finding files beside existing notes: `kb/Work/PB5731/2026-11-16-two-superseded-branches.md` (two branches abandoned as superseded), `kb/Work/PB5816/2026-11-16-the-no-pointer-form.md`, `kb/Work/PB5734/2026-11-16-inspect-is-a-note-of-its-own.md`, and the ruling copied unchanged as `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-11.md`.
