# 02-code-visual-feedback – Visuelles Feedback fuer Zustaende

**Status:** offen

## Ziel

Die Barriere soll dem Spieler ihren aktuellen Zustand (`locked`/`unlocked`) klar kommunizieren – sowohl statisch als auch beim Zustandswechsel.

## Kurzbeschreibung

Erweiterung der Barrier-Komponente um visuelles Feedback:

- Unterschiedliche Materialien/Farben fuer `locked` (z. B. rot/undurchsichtig) und `unlocked` (z. B. gruen/transparent oder deaktiviert).
- Ein Event-Hook fuer den Zustandswechsel, sodass Designer eigene Effekte (Partikel, Sound, Animation) im Inspector verdrahten koennen.
- Optional: Eine UI-Text-Anzeige ("Ausgang gesperrt" → "Ausgang offen") in der Naehe der Barriere.

## Abhaengigkeiten

- **01-code-barrier-component** – Die Basis-Komponente mit Zustandslogik muss vorhanden sein.

## Akzeptanzkriterien

- [ ] Die Barriere zeigt im `locked`-Zustand ein klar erkennbares visuelles Signal (z. B. rotes Material).
- [ ] Die Barriere zeigt im `unlocked`-Zustand ein klar anderes visuelles Signal (z. B. gruenes Material oder deaktiviertes Mesh).
- [ ] Der Zustandswechsel loest ein im Inspector konfigurierbares Event aus.
- [ ] Visuelle Assets (Materialien, Farben) sind im Inspector austauschbar.
- [ ] Die Aenderungen sind im Play Mode sofort sichtbar.

## Definition of Done

- [ ] Visuelles Feedback fuer beide Zustaende funktioniert.
- [ ] Event-Hook ist vorhanden und im Inspector verdrahtbar.
- [ ] Feature wurde im Play Mode geprueft (Zustandswechsel sichtbar).
- [ ] Keine bekannten Regressionen.
