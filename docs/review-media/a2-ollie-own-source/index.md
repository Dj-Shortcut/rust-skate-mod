# Normal rolling Ollie: rig and contact review

[Private full-motion viewer](http://127.0.0.1:63761/a2-ollie-own-source/) · [genuine rolling reference](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=60) · [A2 issue](https://github.com/Dj-Shortcut/rust-skate-mod/issues/689)

The original rig and full 13-part shaped skateboard are shown at Load, tail-pressure Pop, free Air, settling Catch, observed Land and ordinary End. Separate contact sheets show the front outer-upper guide. Source pixels remain private; the viewer contains the original, unmirrored bounded reference for comparison.

| Physical preview | Full motion phases | Contact detail |
| --- | --- | --- |
| Regular, normal | [phase sheet](normal-regular-phases.jpg) | [tail / upper guide / Air / Land](normal-regular-contacts.jpg) |
| Goofy, normal | [phase sheet](normal-goofy-phases.jpg) | [tail / upper guide / Air / Land](normal-goofy-contacts.jpg) |
| Regular, low | [phase sheet](low-regular-phases.jpg) | [tail / upper guide / Air / Land](low-regular-contacts.jpg) |
| Goofy, low | [phase sheet](low-goofy-phases.jpg) | [tail / upper guide / Air / Land](low-goofy-contacts.jpg) |

Observed visual result: the load bends before release; the tail foot stays beyond the bolts and the front foot starts near the middle. Tail pressure pitches the board up; the front outer upper guides toward the nose, then the level board stays near both shoes with compact knees. The catch settles over the trucks, landing bends the knees, and recovery becomes upright. The source's brief raised balance arm is retained; head gaze and neutral head roll are reconstructed toward board and landing. Camera/depth and exact contact remain authored, not measured capture.

The actual managed controller-to-32-Hz-Step/Land route then drives 60-Hz body sampling: normal36/36 active Air frames, low34/34, both input stances, no capture fallback, observed landing and ordinary recovery through4seconds. All four browser cases completed1× and¼× replays through4seconds, with actual decoded reference frames and zero browser errors. Seven phase screenshots per case and six-phase private source comparisons were inspected. Additional Side/Feet-close screenshots cover Pop, the outer-upper guide, Air and Land in both physical stances; no sole substitute or visible foot/deck pass-through was seen. The raw pack stays `reviewed=false`; Claude owns the offline verdict and library table. Unity/IL2CPP and Shadow are not verified here.

`review-manifest.json` pins the authored source, exact C# route, generated pack, four GLBs, physical foot-bone audit and these original images. The frozen review proxy swaps requested/native and visible stance: visibleRegular uses requestedGoofy, and vice versa. Labels follow the animated foot bones against board forward, not the request string. This is a proxy-specific audit, not a claim about native game handedness.

The bounded preview's integer frame PTS were inspected. Local decoder158..227 corresponds to approximately60.271933–62.574233seconds in the public video. Original global frame numbers and full source hash are unknown; the actor cut at229 is excluded. The independent source inputs and baker are in [the source directory](../../../assets/authored/animation/ollie-keyed/README.md).
