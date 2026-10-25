# Rust PC skate mod

## Current direction: skating in the existing Rust PC game

Op 14 oktober 2026 schrapte de eigenaar MW2 uit het actieve doel. We werken aan
de skateboardmod voor **Rust op Steam/pc**, met het gevoel en de bediening van
het nieuwe **skate.** als eerste prioriteit; Skate 3 is een tweede referentie.
Rust levert de wereld, survival, bouwen en bestaande TC-regels. Alle spelerstekst
blijft Engels. De zelfstandige Rust/Bevy-rewrite blijft geparkeerd sinds 5 oktober.

Client- en serverbron voor rijden, tricks, parkstukken, presentatie en geluid
zijn aanwezig. **De volledige herhaalbare controllerdemo en productacceptatie
blijven open.** Een compile of afzonderlijke rijproef bewijst die volledige flow niet.
Het [opleverdoel](docs/SKATE-MOD.md), de [eerste skateparkdemo](docs/archive/RUST-SKATE-DEMO.md)
en [TODO.md](TODO.md) onderscheiden bron, verificatie en speelbaarheid.
De [client](mods/rust/client/README.md) en [servercomponenten](mods/rust/plugins/README.md)
hebben ieder hun eigen integratie- en verificatiegrenzen; een Oxide-plugin alleen
bewijst geen clientanimaties of skateboardphysics.
Servervoorbereiding: [Windows](mods/rust/server/README.md) en [Linux](mods/rust/server/LINUX.md).
Voor een vriend zonder ontwikkeltools: [FRIEND.md](mods/rust/client/FRIEND.md) (pakket en
installer als bron aanwezig, nog op geen pc gemaakt of geïnstalleerd).
Shadow wordt als speelclient gebruikt. Hostingkeuze en kosten vereisen het akkoord
van de eigenaar; de bestaande wereld, plugins en gegevens blijven beschermd.

Eerdere MW2-gunplay-, operator- en loadoutplannen zijn historisch werk en geen
actieve oplevercriteria. Hun bron en credits blijven bewaard.

Ontwerp- en onderzoeksreferenties voor de huidige skate-mod:
[ReSkate](https://github.com/Dingo-Shenanigans/ReSkate) (GPL-3.0) voor
plaatsingsbeheer, besturing en runtimecompatibiliteit;
[REA — Reverse Engineer Anything](https://github.com/morluto/rea) (MIT) voor
statische inspectie van managed code. Deze projecten zijn bestudeerd; hun code
en assets worden niet meegeleverd. Zie [NOTICE](NOTICE) voor credits en
[de eerste demo-acceptatie](docs/archive/RUST-SKATE-DEMO.md) voor de toepassing en grenzen.

De oude MW2/Bevy-rewrite is uit main gehaald en staat nog in de git-branch
`archive/mw2-rewrite`. Credits en licenties: [NOTICE](NOTICE) en [LICENSE](LICENSE).
