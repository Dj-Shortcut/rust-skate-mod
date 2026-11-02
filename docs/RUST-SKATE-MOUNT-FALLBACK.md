# Joint lookup fallback for boarding

Prepared for the 4 November Shadow test. The historical `AcquireRootProxy`
failure did not record its exact method; Joint enumeration is not the proven cause.

Both root and physical-frame checks try, including inactive children:

1. `GetComponentsInChildren<Joint>(true)`. A `NotSupportedException` logs its
   bounded method message before falling back.
2. The non-generic overload with the native Joint type. Unavailable binding/type
   failures advance to the last route.
3. `GetComponentsInChildren<Component>(true)`, walking each native type's
   `FullName`/`BaseType` ancestry for `UnityEngine.Joint`. This route needs neither
   `Il2CppType.Of<Joint>()` nor `TryCast<Joint>()`; a 32-level/cycle bound prevents
   an invalid ancestry walk from hanging the mount attempt.

Any found Joint still refuses boarding. An unavailable component ancestry does
not stop the remaining scan: a later Joint still refuses. If the scan remains
incomplete, boarding continues with `ROOTSYNC Joint check ...: SKIPPED; absence
of Joints unverified`. This is the owner's explicit unavailable-guard fallback,
not a verified absence of Joints. Unknown errors still refuse boarding.
The lookup runs only during acquisition, never in the per-frame proxy check.

`MountFailure` now records the method message for unsupported/missing methods,
and refusal/fallback messages are single-line and bounded to 240 characters.
Every new failed press retains its HUD notice; changed failures retain a bounded
stack. No objects, player state or native payloads are dumped.

Player identity, independent Rigidbody ownership, body mode, collision flags,
distance, collision restoration and physical-body readbacks retain their checks.
The main #660 injected collision access is preserved. No player motion, view
binding, game startup or release-pin change.

The repaired overlay's full 81-source C# 7.3 compile passed against all 191
corrected production game/Unity references from the 28 October client (Rust
build 26140533, Unity 6000.3.15), retaining only the 165 existing framework,
BepInEx and Interop dependencies. No older game/Unity references remain.
A temporary controlled probe passed 20 helper/error paths, including partial
ancestry failure followed by a real Joint, explicit skip, bounded diagnostics
and unknown-error propagation. It does not execute Unity. Both new managed-only
Exception helpers are hidden from IL2CPP registration.

The earlier diagnostic-only API inspection is superseded for source compilation
by that corrected set. Input/output inventories, SDK binaries and receipts stay
private. A successful source compile does not prove native callability, the
historical failure's cause, successful mounting or a client-installable package.

On Shadow, preserve one real mount attempt's failure step/message/stack or
successful `MOUNT` line, plus any fallback/skip lines. Then ride, dismount and
check collision restoration. Compass/dismount camera remain a separate binding
and gameplay gate; neither generation nor this patch establishes the replacement.
