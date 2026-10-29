# TODO

Skate-mod voor Andy's Rust-server, in Andy's volgorde. Eén regel per punt; details en bewijs horen in het issue of de PR, niet hier.
Klaar = in het spel gezien of gereden, met versie. Bron, CI of een build alleen is niet klaar.
Laatste native controle: client 0.18.62 / server 0.3.38 (26 okt): verbinden en 18 WAVs laden bevestigd; opstappen weigert een niet-beschikbare Unity-aanroep. Shadow is niet beschikbaar voor een nieuwe rit.
Eerst deze opstapblokkade oplossen. Kandidaat .64 bewaart de .63-foutstap en stack; Windows-diagnostiek toont de laatste poging en eventuele Unity-stackmethode, headless gecontroleerd maar nog niet native uitgevoerd. De ontbrekende aanroep en reparatie blijven onbekend. [Stand en volgende speltest](docs/RUST-SKATE-PLAYTEST-064.md).

1. Bouwplan-wiel: één knop wisselt tussen Skate- en Rust-bouwen, steeds één wiel zichtbaar. Menucompatibiliteit .62 en X/Back-bevestiging .63 bronmatig gecontroleerd; native wisselen, plaatsen en de eerdere ongevraagde `off` blijven open.
2. MEGA-halfpipe plaatsen op vrije grond en op funderingen. Open.
3. Echte ronde quarterpipes (mini, normaal, groot). Assets aanwezig; maten wijken af van de huidige native bouwdelen. Versiegebonden migratie en spelcontrole open; geen incompatibele modelwissel in .63.
4. Alle twaalf skatestukken plaatsen met het gewone bouwplan, ook vanuit inventory (Andy, opnieuw 25 okt). Open.
5. Parkstukken staan op grond of fundering, zweven niet. Bron klaar, ongetest.
6. Snappen en preview zoals Rust-bouwen. Bron klaar, ongetest.
7. Niet bouwen of items gebruiken tijdens rijden. Bron klaar, ongetest.
8. Kompas blijft stabiel tijdens rijden. Geblokkeerd: huidige Rust-wrapper mist de gebruikte view-methode; .63 meldt de ontbrekende route. Vervangende binding en spelcontrole open.
9. Camera na afstappen horizontaal in rijrichting. Zelfde ontbrekende view-binding; .63 verwerpt schijnsucces en reset de binding bij een nieuwe speler. Herstel en spelcontrole open.
10. Flip/shuv en grab sluiten elkaar uit. Bron aanwezig sinds 0.18.34 (`SkateRide.cs`: een flip of shove tijdens een grab of met de grabknop al vast vóór de pop wordt geweigerd, een grab tijdens een flip start pas als het board rond is); ongetest in het spel.
11. Handen leeg op het board, item terug na afstappen. .63 bewaart oorspronkelijke rendererstatus en herstelt onafhankelijke held-objecten met begrensde herpogingen. Native slotselectie en volledige spelcontrole open.
12. Skateboardgeluid klinkt echt. [18 gekozen geluiden](docs/RUST-SKATE-SOUND-BRIEF.md) in .62 native geladen (26 okt), één per soort. .63 bewaart de ingestelde private geluidsbron bij installatie; bron gecontroleerd, native installatie, hoorbare mix en spelcontrole open.
13. Bouwplan zichtbaar in de hand. Open.
14. MEGA volledig zichtbaar met beide ronde zijden. Open.
15. Altijd dag op de server. Serverinstelling staat; duurtest open.
16. Grote skateparken bouwen. Limiet 1024 groepen; prestatietest open.
17. Precies 10 hout per skatestuk. Bron klaar, ongetest.
18. Halfpipe vloeiend, geen terugtrekken (#487). Open.
19. Air boven de coping. Open.
20. Pompen door de knieën, niet met een voet. Bron klaar, ongetest.
21. Spel bevriest niet en invoer blijft werken. Open.
22. Vrije air: rider en board delen één basis, samen rechtop richting apex en terug naar de waargenomen ramp bij landing; trickpitch blijft relatief, grinduitlijning alleen bij grind. Steil display-datum op dezelfde capsulebasis; broncompile en gecontroleerde viewerroute aanwezig. Volledige greep en native contact/Shadow-uitvoering open.
23. Volledige, samenhangende rijdersanimatie. Bron klaar, ongetest.
24. Alle tricks die Skate 3 heeft: alle flips (kickflip-familie), alle grabs, alle grinds en slides, manuals, enzovoort, met een eigen animatie per trick; batch 3 samenstellen uit gedeelde bronclips per stance ([40 bronreferenties, #584](https://github.com/Dj-Shortcut/rust-skate-mod/issues/584)). [Volledige bibliotheek in zeven batches](docs/RUST-SKATE-ANIMATION-LIBRARY.md); eerste vijf visueel herwerkt voor de viewer, familiesamenstelling nog niet geïmplementeerd, gameplayacceptatie op Shadow open.
25. Controller werkt op het board en te voet. Open.
26. Uitzoeken hoe Skate trick-, rijder- en boardanimaties koppelt. Deels onderzocht (privé).
27. Herkenbare drop-inanimatie voor halfpipes: afzonderlijk bronvoorstel met gewichtsverplaatsing en knieabsorptie bij bevestigd rampcontact; tail-op-coping voorfase, native uitvoering en beeld blijven open (Andy, 26 okt).
28. Automatische rigging-showcase op platte grond: alle beschikbare trickposes met naam, pauze en volgende/vorige, zodat handen, voeten en board samen bekeken kunnen worden. Bron aanwezig in 0.18.60; [batchpipeline met volledige boardrotatie](docs/RUST-SKATE-MOTION-CAPTURE.md) en externe capturebibliotheek toegevoegd. Normale presentatiecode in 0.18.64 gekoppeld met exacte variantselectie en aparte goedgekeurde pack; eerste vijf Berrics-captures krijgen bron-, voetpad- en continuïteitsherstel, met echte deckgeometrie, zichtbare-schoenprofielen en expliciete gezamenlijke richtingsreconstructie. Vrije-voetclearance, bewegingsacceptatie, overige batches en native Shadow-beeldcontrole open (Andy, 27 okt).

Per rit controleren: controller, eerste/derde persoon, kompas, hand-item, pose, geluid, put-backs. Halfpipe: drie beklimmingen en fakie-terugkeren zonder put-back.

## Werkblokken bij punten 23–25 (Andy, 25 okt)

- **Grabs:** Weddle, Rocket Air, tweaks en one-foot/no-foot met eigen hand- en voetcontact, loslaten, catch en landing: bron aanwezig sinds 0.18.51; gedeeld deckframe in 0.18.53. Headless gecontroleerd; beeld en gevoel in game open.
- **Grinds:** FS/BS Overcrook, Salad, Willy en Over Willy met boardhoek, truckcontact, rijderpose en in-/uitstap: bron aanwezig sinds 0.18.52; uitdempen bij exit in 0.18.54. Headless gecontroleerd; native railcontact en gevoel open.
- **Speciale flips:** Impossible, Front Foot Impossible, nollie-voetvarianten en Underflip met eigen boardbaan, voetvrijgave en catch: bron aanwezig in 0.18.53. Voortgang, scoring en poses headless gecontroleerd; uitvoering in game open.
- **Overgangen en invoerherstel:** push/pomp/pop/grab/catch/landing gekoppeld, grabwissels en herstel na menu, readerwissel of afstappen: bron aanwezig in 0.18.54. Headless gecontroleerd; controller te voet, camera en volledige rit op Shadow open.

## Zes resterende trickblokken (Andy, 25 okt)

- **Grondgrabs en Coffin:** bron aanwezig in 0.18.55, inclusief invoerprioriteit, gemeten poses, loslaten en scoring; native beeld, contact en botsingsruimte open.
- **Handplantvarianten:** alle genoemde FS/BS-varianten met gemeten hand-/voetcontact, gedeeld boardframe en invoerherstel: bron aanwezig in 0.18.56; native contact, beeld en gevoel open.
- **Extra ollies en Finger Flip:** Boned en front/back-foot One-Foot Ollies/Nollies plus handgedreven Finger Flip met release/catch: bron aanwezig in 0.18.57; uitvoering en gevoel op Shadow open.
- **Oldschool:** Caveman en Hippie Jump met daadwerkelijk afzonderlijk deck, release-kick flips/shoves, bodyspins, pickup/catch en steunverlies: bron aanwezig in 0.18.58; volledige uitvoering op Shadow open.
- **Walltricks:** Wallie en FS/BS Wallride met verse muurwaarneming, gebonden beweging, gemeten pose en release: bron aanwezig in 0.18.59; native contact en gevoel open.
- **Transitiontricks:** expliciete Drop-in na bevestigd transitioncontact en gekoppelde Front/Back Flip met bereikte grab, ruimte en voltooide landing: bron aanwezig in 0.18.59; aparte halfpipe-instapanimatie bij punt 27 en uitvoering op Shadow open.

Deze blokken worden samen op Shadow getest. Bron gereed telt niet als gameplayacceptatie; de overige TODO-punten blijven afzonderlijk open. Claude bouwt het park in Unity; parkassets en parkbouw vallen buiten deze trickwijzigingen.

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
