## PB4971: the edition audit routes each 1985 clause to its 2023 clauses before mapping, and the 1985 map is re-run

The first 1985 map pass chose each rule's 2023 candidates by wording alone (15 per rule). 464 rules came back NONE, and
about 200 of those blamed the candidates, not a deleted feature. The owner asked for the strongest appropriate signals
and a re-run (2026-11-08).

A clause heading alone was not enough: it matches no 2023 clause for 598 of the 2,253 rules, because 1985 repeats a clause
in each I-O module and names clauses differently. So `audit_batch.py` gains a `route` pass, one request per 1985 clause
(247), which names the 2023 clauses that state the same feature. A rule's candidates are now every 2023 rule of the same
kind in its routed or heading-matched clauses, then the wording's top 15. The map plan refuses to run before the routes
are in.

The re-run (route $3.62, map $37.90, verify $8.02, 39 new drafts) gives 759 rules that carry their 2023 verdict, 564
disputed and 930 rows of their own, against 747, 504 and 1,002 before. The NONE answers that blame the candidates fell
from 204 to 131, and a sample of the rest reads as genuine. 891 of the 930 drafts were reused from the first run, whose
items were identical.
