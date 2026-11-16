# Two earlier branches of this note's fix are superseded by the branch that landed (decided 2026-11-16 by the Mythos lane)

`worktree-wf_1d5ba7f2-7a4-13` ABANDONED: superseded. Its two commits (037588ee07, 10e5d5b6e8) are the first form of this note's fix, and the branch that landed the fix, pull request 1768, carries the same change.

`worktree-wf_1d5ba7f2-7a4-21` ABANDONED: superseded. It is the branch above with its review finding answered (3fe95f672b, the indexed RECORD KEY minimum-size rules read `RecordSizeRange.Min`), and pull request 1768 carries that too.

**The evidence, measured on 2026-11-16 at main d92742fe62** (`git diff --name-only origin/main...<branch>`, then each file's blob on the branch against the blob on main):
- Every file the second branch changed is byte-identical on main: the three conformance cases with their expected outputs under `conformance-suite/cases/IX.2.7@1985-RECORD-KEY-CLAUSE/` and `conformance-suite/cases/XI.4.4@1985-SORT-STATEMENT/`, the refusal `conformance-suite/wiseowl/diagnostics/pb5731_sort_giving_format3_odo_smaller_than_smallest.json`, this note and its landing file, `src/WiseOwl.Cobol.Compiler/Binding/FileControlKeyRules.cs` and `src/WiseOwl.Cobol.Compiler/Binding/Model/FileModel.cs`.
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SortBinder.cs` differs only because main has 17 later lines of other work: the two lines the branch changed (the comment that names `FileModel.RecordSizeRange` and the test on `file.RecordSizeRange.Min`) are both in main's file.
- The one file absent from main is the branch's pending DEVLOG entry, which the landing stamped under its number.
- The first branch changed a subset of the same files; its version of this note differs from main's because the note was edited again before it landed.

Neither branch holds a commit whose content is not on main. Both read DECISION in the branch survey for five hours and more because no wave plans a branch whose note has landed.
