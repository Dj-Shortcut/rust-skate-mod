# TODO

Skate-mod voor Andy's Rust-server, in Andy's volgorde. Eén regel per punt; details en bewijs horen in het issue of de PR, niet hier.
Klaar = in het spel gezien of gereden, met versie. Bron, CI of een build alleen is niet klaar.
Laatste speltest: client 0.18.45 / server 0.3.38 (21 okt). Geen testomgeving sinds Shadow weg is.

1. Bouwplan-wiel: één knop wisselt tussen Skate- en Rust-bouwen, steeds één wiel zichtbaar. Actief: na Low Ledge kiezen volgt ongevraagd `off` (fix in .46, ongetest).
2. MEGA-halfpipe plaatsen op vrije grond en op funderingen. Open.
3. Echte ronde quarterpipes (mini, normaal, groot). Open.
4. Alle twaalf skatestukken plaatsen met het gewone bouwplan, ook vanuit inventory (Andy, opnieuw 25 okt). Open.
5. Parkstukken staan op grond of fundering, zweven niet. Bron klaar, ongetest.
6. Snappen en preview zoals Rust-bouwen. Bron klaar, ongetest.
7. Niet bouwen of items gebruiken tijdens rijden. Bron klaar, ongetest.
8. Kompas blijft stabiel tijdens rijden. Open.
9. Camera na afstappen horizontaal in rijrichting. Open.
10. Flip/shuv en grab sluiten elkaar uit. Open.
11. Handen leeg op het board, item terug na afstappen. Bron klaar, ongetest.
12. Skateboardgeluid klinkt echt. Bron klaar, niet beluisterd.
13. Bouwplan zichtbaar in de hand. Open.
14. MEGA volledig zichtbaar met beide ronde zijden. Open.
15. Altijd dag op de server. Serverinstelling staat; duurtest open.
16. Grote skateparken bouwen. Limiet 1024 groepen; prestatietest open.
17. Precies 10 hout per skatestuk. Bron klaar, ongetest.
18. Halfpipe vloeiend, geen terugtrekken (#487). Open.
19. Air boven de coping. Open.
20. Pompen door de knieën, niet met een voet. Bron klaar, ongetest.
21. Spel bevriest niet en invoer blijft werken. Open.
22. Lichaam trekt in de lucht niet horizontaal recht. Bron klaar, ongetest.
23. Volledige, samenhangende rijdersanimatie. Bron klaar, ongetest.
24. Alle tricks die Skate 3 heeft: alle flips (kickflip-familie), alle grabs, alle grinds en slides, manuals, enzovoort, elk met eigen animatie (Andy, 25 okt). Open.
25. Controller werkt op het board en te voet. Open.
26. Uitzoeken hoe Skate trick-, rijder- en boardanimaties koppelt. Deels onderzocht (privé).

Per rit controleren: controller, eerste/derde persoon, kompas, hand-item, pose, geluid, put-backs. Halfpipe: drie beklimmingen en fakie-terugkeren zonder put-back.

## Werkblokken bij punten 23–25 (Andy, 25 okt)

- **Grabs:** Weddle, Rocket Air, tweaks en one-foot/no-foot met eigen hand- en voetcontact, loslaten, catch en landing: bron aanwezig sinds 0.18.51; gedeeld deckframe in 0.18.53. Headless gecontroleerd; beeld en gevoel in game open.
- **Grinds:** FS/BS Overcrook, Salad, Willy en Over Willy met boardhoek, truckcontact, rijderpose en in-/uitstap: bron aanwezig sinds 0.18.52; uitdempen bij exit in 0.18.54. Headless gecontroleerd; native railcontact en gevoel open.
- **Speciale flips:** Impossible, Front Foot Impossible, nollie-voetvarianten en Underflip met eigen boardbaan, voetvrijgave en catch: bron aanwezig in 0.18.53. Voortgang, scoring en poses headless gecontroleerd; uitvoering in game open.
- **Overgangen en invoerherstel:** push/pomp/pop/grab/catch/landing gekoppeld, grabwissels en herstel na menu, readerwissel of afstappen: bron aanwezig in 0.18.54. Headless gecontroleerd; controller te voet, camera en volledige rit op Shadow open.

## Skate 3-tricks (bij punt 24)

Doellijst voor punt 24: elke trick krijgt eigen board- en lichaamsbeweging, catch en landing. Namen zoals in de Skate-serie; controleer de uiteindelijke set tegen de Trick Guide in Skate 3 zelf.

- **Ollie-flips:** Ollie, Kickflip, Heelflip, Double/Triple Kickflip, Double/Triple Heelflip, FS Pop Shove-it, BS Pop Shove-it, FS 360 Shove-it, BS 360 Shove-it, Varial Kickflip, Varial Heelflip, 360 Flip, Laser Flip, Hardflip, Inward Heelflip, 360 Hardflip, 360 Inward Heelflip, Impossible, Front Foot Impossible.
- **Nollie-flips:** Nollie, Nollie Kickflip, Nollie Heelflip, Nollie FS/BS Pop Shove-it, Nollie FS/BS 360 Shove-it, Nollie Varial Kickflip, Nollie Varial Heelflip, Nollie 360 Flip, Nollie Laser Flip, Nollie Hardflip, Nollie Inward Heelflip, Nollie 360 Hardflip, Nollie 360 Inward Heelflip, Nollie Impossible, Nollie Back Foot Impossible.
- **Standvarianten:** alle ollie-flips ook Fakie en alle nollie-flips ook Switch, in regular en goofy.
- **Flipvarianten:** late flips (in de lucht), held flips (extra rotaties), Boned Ollie, front/back foot One-Foot Ollie, body varials, FS/BS 180 en 360 body spins, Finger Flip.
- **Underflips en dark:** Underflip, Dark Catch (board ondersteboven vangen), Darkslide.
- **Grinds:** FS/BS 50-50, FS/BS 5-0, FS/BS Nosegrind, FS/BS Crooked, FS/BS Overcrook, FS/BS Smith, FS/BS Feeble, FS/BS Salad, FS/BS Willy, FS/BS Over Willy.
- **Slides:** FS/BS Boardslide, FS/BS Lipslide, FS/BS Noseslide, FS/BS Tailslide, FS/BS Bluntslide, FS/BS Nosebluntslide; grind/slide exit links en rechts.
- **Basisgrabs:** FS Grab (Indy), BS Grab, Double Grab, Tail Grab, Nose Grab, Stale(fish), Weddle, Crail, Seatbelt, Rocket Air.
- **Getweakte grabs:** Nosebone, Tailbone, Tuck Knee, Stiffy, BS Tweak, Melon, Crossbone, Method, Tail Nosebone, FS/BS Shifty Tail Grab, Nose Tailbone, FS/BS Shifty Nose Grab, Stale FS/BS Tuck Knee, Stale Tailbone, Stale Nosebone, Japan Air, Weddle Tailbone, Weddle Nosebone, FS/BS Shifty Crail, FS/BS Shifty Seatbelt, Double Grab mod (nose, tail, links, rechts).
- **One-foot en no-foot grabs:** Frigid Air, FS One-Foot Air, Judo Air, BS One-Foot Air, Dog Piss, Judo Nose Grab, Benihana, One-Foot Tail Grab, Airwalk, Tailwalk, Christ Air, No-Foot Air, Superdude.
- **Grondgrabs:** FS/BS Ground Grab, Double Ground Grab, Coffin.
- **Handplants:** FS/BS Handplant, FS Tailbone, FS Tuck Knee, FS Stiff, FS Boned, BS Tuck Knee, BS Crossbone, BS Method, BS Tweak, BS One-Foot, BS Judo, FS Frigid, FS One-Foot, FS/BS Gymnast Plant.
- **Footplants en oldschool:** Boneless (FS), Beanplant, FS/BS Fastplant, No Comply, Caveman, Hippie Jump (ook met flip en spin).
- **Manuals en rijden:** Manual, Nose Manual, manual revert, FS/BS Revert, FS/BS Powerslide, Speed Check, Kick Turn, Wallie, FS/BS Wallride.
- **Transition:** Drop-in, Air-to-Flat, Air-to-Lip, lip trick-instap (grind of stall op de coping), FS/BS 180/360 airs met grab, Back Flip en Front Flip met grab.
