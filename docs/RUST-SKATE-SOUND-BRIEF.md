# Geluidenbrief voor het skateboard

De huidige geluiden in de game zijn door Andy beluisterd en afgekeurd als
grotendeels ruis. Andy heeft vervanging via ElevenLabs Sound Effects gevraagd. Het doel is herkenbaar,
droog skateboardgeluid: wielen, hout, schoenen en metaal moeten te onderscheiden
zijn. De bestaande gegenereerde WAV-set geldt niet als goedgekeurd eindresultaat.

Maak **18 soorten, elk met één gekozen variant**: 18 WAV-bestanden. Gebruik de onderstaande
stam met `_01.wav`, bijvoorbeeld `skate_ollie_01.wav`. Dit vervangt op verzoek van
Andy de eerdere opdracht voor drie varianten per soort en beperkt de lokale opslag.
Kies uit de gegenereerde alternatieven op basis van de waveform; bewaar alleen de
gekozen bron en het benodigde spelbestand. Tussentijdse luisterproeven zijn niet nodig.

| Bestandsstam | Type | Benodigd geluid |
| --- | --- | --- |
| `skate_rolling_smooth` | Loop | Urethaanwielen op glad beton, licht lagergeluid; middelmatige snelheid rond 4 m/s. |
| `skate_rolling_rough` | Loop | Wielen op ruw asfalt, met duidelijk korrelig wegcontact. |
| `skate_rolling_wood` | Loop | Rollen op een houten ramp, met holle resonantie van de panelen. |
| `skate_grit` | Loop | Kleine steentjes en zand onder de wielen; een zachte extra laag. |
| `skate_whirr` | Loop | Zacht zoemen van vrij draaiende wielen/lagers wanneer het board in de lucht is. |
| `skate_push` | Eenmalig | Eén schoen die neerkomt, afzet en over de ondergrond schuurt. |
| `skate_ollie` | Eenmalig | Scherpe houten tail-pop, korte griptape-/schoenschraap en klein hardwaretikje. |
| `skate_landing` | Eenmalig | Vier wielen landen op beton, het deck veert en de trucks rammelen kort. |
| `skate_drop` | Eenmalig | Zwaardere landing na een flinke drop of halfpipe-air. |
| `skate_seam` | Eenmalig | De twee assen tikken kort na elkaar over een voeg of rampnaad. |
| `skate_grind_in` | Eenmalig | Trucks haken met een metalen klank op een rail of coping. |
| `skate_grind` | Loop | Metalen trucks schuren continu over staal, met fijne onregelmatige korrel. |
| `skate_grind_out` | Eenmalig | Trucks verlaten de rail, met een korte droge metaalring. |
| `skate_slide_in` | Eenmalig | De houten onderkant van het deck komt op een ledge terecht. |
| `skate_deckslide` | Loop | Hout glijdt over een gewaxte betonnen ledge of stalen rand. |
| `skate_scrape` | Eenmalig | De tail schuurt over de grond bij afremmen of het begin van een powerslide. |
| `skate_slam` | Eenmalig | Een val: kleding, schoenen, handen en lichaam raken de grond; zonder stem. |
| `skate_bail` | Eenmalig | Het losse board rolt weg, kantelt en klettert op zijn zijkant. |

Aanlevering: één map `skate_sounds/`, WAV mono, 44.100 Hz, 16-bit PCM.
Maximale piek −1 dBFS, geen clipping of DC-offset. Droog opnemen, zonder
ingebakken galm, muziek of achtergrondstemmen.

Loops duren 2–4 seconden en sluiten zonder tik aan. Houd kleine tikjes
onregelmatig, zodat herhaling niet opvalt. Ze moeten natuurlijk blijven klinken
wanneer de game de afspeelsnelheid tot ongeveer 40% verhoogt. Eenmalige geluiden
beginnen binnen 20 ms, hebben een natuurlijke uitklank en eindigen met een fade.

Richtniveau: een normale pop ongeveer −12 LUFS-I; loops ongeveer −20 LUFS-I.
Pop en gewone landing vormen de referentie, zware drop circa 3 dB boven landing,
lichaamsval iets eronder, push circa 10 dB onder pop. Rollen blijft zacht en
lagerzoem zeer zacht. Vermeld gemeten niveau en piek per bestand; behoud de
relatieve dynamiek. Het uiteindelijke volume wordt in de game afgesteld.

Graag ook `demo.wav` van 30 seconden: push → rollen → ollie → landing → grind →
val. Lever een `manifest.json`
mee met per bestand type, lengte, LUFS, piek, looppunten en herkomst.

Leg de werkelijke herkomst en rechten vast. ElevenLabs-uitvoer krijgt geen
CC0-licentie: de [ElevenCreative-voorwaarden](https://elevenlabs.io/creative-terms)
blijven gelden. Houd deze audiobestanden buiten de openbare GitHub-repository;
gebruik ze binnen het private spelpakket. Bewaar brongegevens en `RIGHTS.txt`,
inclusief toepasselijke [attributie- en gebruiksvoorwaarden](https://elevenlabs.io/docs/help-center/legal/can-i-publish-the-content-i-generate-on-the-platform).
Andy accepteert een onhoorbaar digitaal watermerk; een hoorbare merkstem hoort
niet in de clips. Pas geen watermerkverwijdering toe.

Een latere eigen opname of aantoonbaar CC0-alternatief kan wel met de bijbehorende
bronnenlijst en licentie worden gepubliceerd. Gebruik geen geluid uit andere games.

## Inbouwen en acceptatie

Clientbron `0.18.61` heeft een WAV-loader voor deze 18 gekozen `_01.wav`-bestanden
in `BepInEx/plugins/skate_sounds/`, met koppelingen aan de bestaande events en
oppervlakken. Ontbreekt een vereist bestand of gaat laden mis, dan blijft rijden
mogelijk zonder skateboardgeluid. De [audiodocumentatie](RUST-SKATE-AUDIO.md)
beschrijft die bronintegratie; de historische synthesizers zijn niet het actieve
afspeelpad.

De standaard launcher en het gewone spelpakket plaatsen de geluidsmap nog niet.
Een private staging-adapter is voorbereid, maar niet op Windows/PowerShell
uitgevoerd. Alle 18 gekozen bestanden zijn privé voorbereid: 44,1 kHz mono PCM16,
veilige pieken, gecontroleerde onsets en loopnaden, en volledige decode met de
werkelijke clientlezer. De absolute LUFS-doelen en de gevraagde drop/landing-verhouding
worden niet allemaal gehaald. Daadwerkelijke installatie, volumebalans, timing en
gezamenlijk luisteren op Shadow blijven open; het pakket is niet in-game goedgekeurd.
