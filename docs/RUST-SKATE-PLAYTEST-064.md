# Combined development candidate 0.18.64

This separate source candidate combines capture work from `f88518f` with the
preserved recovery branch at `d4691e0`. It does not replace the focused capture
PR or promote `release.txt`. Client version remains 0.18.64; the original branches
and their evidence remain available.

The candidate includes the guarded capture catalogue and reviewed-pack normal
presentation, mount-failure diagnostics, building-wheel controller handling and
authored icons, held-item restoration, unavailable-view reporting, interrupted-log
preservation and the pinned-candidate installer. The eighteen private WAVs and
optional private capture/approval pair remain separate local inputs.

Installer checks retain compatibility with the older pinned release, whose own
scripts predate the shared diagnostics helper. Newer scripts still require that
helper. A legacy install preserves local WAV settings while explicitly reporting
that it cannot use the source pointer; an explicit WAV-folder override is refused
before install changes. Earlier scripts that already support default-folder WAVs
and capture pairs retain those capabilities independently of the helper.
Previous capture files are preserved outside `play` before staging, so
an absent or incomplete source cannot silently reuse an older approved pair.

The supported-entry drop-in proposal is included. It adjusts rider presentation
only after fresh confirmed ramp support. It does not implement a full drop-in
starting with the tail on the coping or move the physical player. Competing
actions keep their pose ownership; captured flat-ground tricks remain ineligible
on ramps.

The [recovery findings and focused native procedure](RUST-SKATE-PLAYTEST-063.md)
still apply: the exact unavailable mounting call needs fresh native evidence,
the named native view route is missing, and controller, compass, camera, held
items, placement and riding have not been accepted in Shadow. Capture source
review, compatibility compilation, installation and native gameplay are separate
gates. A successful local build does not close them.

Install only a reviewed combined-candidate commit through the explicit full-SHA
`pcandidate` route described in the [client tools](../mods/rust/client/tools/README.md).
No combined candidate is installed or released by this source integration.
