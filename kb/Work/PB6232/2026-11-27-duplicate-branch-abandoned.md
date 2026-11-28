# PB6232: the duplicate fix branch is abandoned

PB6232 landed through `worktree-wf_19b8f6c4-960-11` (e1b2f3c4cf: the `EvaluateSubjectBinder` first-phrase rule and the
golden `pb6232_evaluate_later_subject_evaluated_first`). Branch `worktree-agent-a79c92744280df84a` ABANDONED: its one
commit 4fee595e1d is a second implementation of the same rule (`ReachedWheneverPhraseIsAnalyzed`, the same loop as
main's `AllAnyBefore`) with its own golden of the same shape; it carries no behavior main lacks (resume unit 2026-11-27).
