## Lead: "§<edition>:<clause> rule N" is the commonest wrong form left: 1544 sites (found by listing PB5951's lead sweep)

Listing step 7b's review lead R1-1 (`review-listing-step-7b.jsonl`) asked for the last `§2023:14.7.8 rule 1 / rule 2` siblings;
branch `listing-step-pb5951` (commit db54297c3f) turned the ten left in `CollatingModel.cs`, `DataBinder.cs` and `EvaluateBinder.cs`
into `GR1` / `GR2`. The same form is written for many other clauses, and `cite.py` refuses every one ("'rule 2' is not a rule
designator: write SR n, GR n, AR n or RV n"):

    grep -rnoE "§(1985|2002|2014|2023):[0-9A-Z.]+( [A-Za-z]+ phrase)? rule [0-9]+" src tests docs conformance-suite | grep -v /evidence/ | wc -l
    1544   (on listing-step-pb5951 at db54297c3f; origin/main 8dd07d2eab differs only by the ten fixed)

The most frequent clauses: §2023:8.8.2 (87), §2023:14.6.13.2 (84), §2023:13.18.40.5 (76), §2023:8.3.2.1 (69), §2023:9.3.8.2.3 (64),
§2023:14.8.2.3.3 (63), §2023:14.8.2.3.2 (58), §2023:14.7.4.3 (55), §2023:14.8.2.2 (54), §2023:14.7.7 (48). Some of those clauses
number their rules with no SR/GR heading (a plain numbered list), so the right designator is not always `GRn`: each needs
`cite.py --check` with its quoted text, which is this note's sweep. `audit_code_citations.py` passes them because it reads the clause
and not the words after it, the gap PB5911 also names.
