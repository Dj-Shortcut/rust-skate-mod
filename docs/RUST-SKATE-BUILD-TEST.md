# Building source prepared for the Shadow test

The existing source contains the Rust/Skate wheel switch and twelve distinct
marked inventory build items. This change reports the accepted wheel mode and
held-item reader state, and refreshes the reader binding when the player wrapper
type changes. It does not establish successful native placement.

With an ordinary building plan equipped, open Rust's wheel once so its identity
can be verified. In the Skate wheel, X on the controller or Shift+P switches to
Rust building. Release the opener, then hold Back/right click for the native
wheel. X or Shift+P switches back to Skate building. At most one wheel may own
the cursor. Cancel, inventory, focus loss and mounting must release that ownership.

Expected transition logs are `PIECES wheel mode=Skate` after native-wheel hiding
and cursor acquisition, and `PIECES wheel mode=Rust` while awaiting native open.
These logs report state transitions; inspect actual visibility separately.

After selecting a Skate piece, B, Escape, F1 or Tab cancels the selection before
any place input in that frame. The client requests the existing server `off`
acknowledgement and retains pending placement/payment state until that reply.
If sending fails, it keeps the unresolved selection and sends no placement in that frame.
Check cancellation with RT/left click held, delayed or mismatched replies,
then re-entry. Ordinary Rust building keeps its own cancel handling.

The administrator's `skate.items all` command supplies the inventory catalogue.
Equip and place each marked item through the native square-foundation route:

| Sector | Item |
| --- | --- |
| 0 | Bank |
| 1 | Kicker |
| 2 | Quarterpipe |
| 3 | Low Ledge |
| 4 | High Ledge |
| 5 | Long Ledge |
| 6 | Funbox |
| 7 | Halfpipe |
| 8 | Mini Quarterpipe |
| 9 | Large Quarterpipe |
| 10 | Half-wall Kicker |
| 11 | MEGA Halfpipe |

Use the half-wall kicker's wall support; verify the other pieces on free terrain
and suitable foundations. Check preview, rotation, native affordability, server
confirmation, one successful debit, failure retention/refund and removal. Existing
server permission, support, occupancy and payment checks remain authoritative.

`PIECES inventory reader: GetActiveItem unavailable` explicitly means the client
cannot verify the inventory preview on that wrapper. It is not a successful
twelve-item test. The reader never substitutes an arbitrary Item-returning method.
Recheck this route against the current laptop interop, then run the native test
on Shadow. Source compilation, catalogue checks and CI remain separate gates.
