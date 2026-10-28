# Joint lookup fallback for boarding

This development change is prepared for the 4 November Shadow test. It is not
proof that the historical `AcquireRootProxy` failure came from Joint enumeration.
That run did not record the exact failing method.

Both root and physical-frame Joint checks try, in order:

1. `GetComponentsInChildren<Joint>(true)`.
2. The non-generic overload with the native Joint type.
3. `GetComponentsInChildren<Component>(true)` with a native `TryCast<Joint>`
   check, including inactive children.

A Joint found by any route still refuses boarding. Only `NotSupportedException`
advances to another route. If all routes are unsupported, boarding continues
with a `ROOTSYNC Joint check ...: SKIPPED; Unity enumeration unavailable` log.
In that case the absence of Joints has **not** been verified. This is the owner's
explicit fallback policy for unavailable Unity enumeration, not an equivalent
safety result. Other exceptions still refuse boarding.

Player identity, independent Rigidbody ownership, body mode, collision flags,
distance, collision restoration and unchanged physical-body readbacks retain
their existing checks. No player motion, view binding or game startup changes.

Source compilation against the available cached references and a temporary
exception-path probe passed. The probe covers direct results, both fallbacks,
native type checking, all-unsupported logging and unexpected-error propagation.
It does not execute Unity. The cached compile references include an older
PhysicsModule; verify again against the complete current laptop interop set.

Metadata inspection for the 28 October 2026 Windows client, Unity 6000.3.15, confirms
both Component enumeration overloads and `Joint : Component` in newly generated
diagnostic wrappers. The inspected generic wrapper and its initialization helpers
are not direct method-unstripping failure stubs. This confirms API shape, not
native callability or the cause of the historical exception.

That diagnostic generation completed with its cross-reference cache disabled
and some unrestored method bodies. It is not a client-installable interop set;
its input identity, output inventory and reports remain private. No source
compile against a complete production interop set or native mount is claimed.

On Shadow, preserve one real mount attempt's failure step and stack, or its
successful `MOUNT` line, plus any Joint fallback/skip lines. Then test riding,
dismounting and collision behavior. Compass and dismount camera remain a separate
view-binding task.
