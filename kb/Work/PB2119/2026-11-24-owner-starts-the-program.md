## 2026-11-24 - the owner starts the Delete program now; status `open`

**The owner, 2026-11-24 (account 1 session).** The owner said "I release the 'no known compiler defects' hold"
(`kb/Work/PB2911/2026-11-24-owner-releases-the-defect-hold.md`). Claude then asked one question: "Does your release lift
that hold too?" The question meant this program's own hold of 2026-11-15, which waited until the 2002 and 1985 inventories
read zero GAP. The answer was "Yes, start Delete too".

So both holds on this program are lifted. `2026-11-15-owner-wait.md` no longer applies, and `status` returns to `open`.

**What still holds:**

- **Every Delete note is re-measured first.** `2026-11-15-owner-wait.md` set this condition for the restart, and the restart
  keeps it. The census behind the 126 open member notes was taken weeks of landings ago. So each note's measured-dead code is
  confirmed dead again against the main of the day it is worked, before anything is removed: a re-run of `census.py`
  reachability, or the note's own measurement repeated.
  - Code the new census finds still dead is deleted by the note's wave.
  - Code that is now reached retires the note, and the note records the new measurement.
- **The 2026-11-23 umbrella finding stands.** This note names no site. Its work is its member notes
  (`cluster: ["PB2119"]`), each one its own Delete wave. A group made of this note alone has nothing to delete.
