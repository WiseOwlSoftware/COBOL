**2026-11-12, late morning PST: the owner lifted the hold on account 1 for this week and announced a reset.**

The owner's words to the Mythos session, in order (three messages within about half an hour; the clock times were not
recorded):

- After the session reported the hold (87 % used against a 71.5 % day line): "Can you usefully consume the
  remaining Mythos quota for this week *AND* assuming I reset, which returns 100% Mythos weekly quota and 50% all Model
  quota, then usefully consume all that additional quota prior to the Sunday 02:00 AM reset?"
- "THere is 7% Mythos quota remaining. Consuming it would raise all model quota to 91%. A reset would change
  Mythos quota to 0% and refund 50% of all model quota taking it to 41%"
- "Lift the hold. Run util you hit the Mythos quota lime. Notify me and I'll immediately reset"

What this changes, and what it does not:

- Account 1 works now although `budget.py` at borrow 0 still reads hold (88 % against 71.5 %). This is the owner's grant
  for this week on account 1 only. After his reset the all-model reading is about 41 %, under the line, and the rule of
  this note applies again as written.
- The session notifies him when the Mythos meter is nearly spent (a watcher on `budget.py`: weekly 90.4 % is about 98 %
  of the Mythos meter, by his figures of 7 Mythos points to 3 all-model points).
- A Mythos or Fable dispatch still needs his approval for that dispatch. He approved none here: the session spends the
  Mythos meter itself and sends workers on cheaper models.
- Account 2 is unchanged: no borrow, its own day line, its reset on Saturday 2026-11-14 at 09:00 PST.
