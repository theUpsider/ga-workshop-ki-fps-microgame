# Editor-Anleitung: Integration & End-to-End-Test im Level

**Feature:** Lockable Barrier (Gesperrter Ausgang)
**Task:** `Docs/requirements/lockable-barrier/04-code-integration-test.md`

## Übersicht

- Vollständige Integration aller Komponenten im Level
- Platzierung der Barriere an einer strategischen Stelle (vor dem Levelausgang)
- Konfiguration des Unlock-Triggers mit "Alle Gegner besiegt"
- End-to-End-Test des Spielflusses: Start → Blockade → Kämpfen → Freischaltung → Durchgang

## Schritt-für-Schritt

### Schritt 1: Level-Szene öffnen

1. Öffne die Hauptspielszene (z. B. `Assets/FPS/Scenes/` → Doppelklick auf die `.unity`-Datei)
2. Warte, bis die Szene vollständig geladen ist

### Schritt 2: Position für die Barriere wählen

1. Suche im Level nach einem strategischen Punkt:
   - **Ideale Position:** Vor dem Ausgang / Level-Ende / Zielbereich
   - Der Spieler muss gezwungen sein, die Barriere zu passieren
2. Platziere ein neues GameObject an dieser Stelle:
   - **Rechtsklick** im Hierarchy → **Create Empty**
   - Nenne es `ExitBarrier`
   - Setze Position mit dem **Move-Tool** an die gewünschte Stelle

### Schritt 3: Barriere-Komponenten hinzufügen

1. Wähle `ExitBarrier` aus
2. **Add Component** → `Box Collider`
   - Stelle sicher, dass **`Is Trigger` deaktiviert** ist
   - Passe `Size` so an, dass der gesamte Durchgang blockiert wird
3. **Add Component** → `Mesh Filter` (optional, für Sichtbarkeit)
   - Weise z. B. `Cube` als Mesh zu
4. **Add Component** → `Mesh Renderer`
5. **Add Component** → `Barrier`
6. Konfiguriere die Barrier-Komponente:
   - `Initial State`: `Locked`
   - `Barrier Renderer`: Ziehe den MeshRenderer von `ExitBarrier` per Drag & Drop hinein
   - `Locked Material`: Ziehe `Barrier_Locked` hinein
   - `Unlocked Material`: Ziehe `Barrier_Unlocked` hinein

### Schritt 4: Unlock-Trigger hinzufügen

1. Erstelle ein weiteres GameObject:
   - **Rechtsklick** → **Create Empty**
   - Nenne es `UnlockManager`
   - Position ist egal (die Komponente arbeitet event-basiert, nicht positionsabhängig)
2. Wähle `UnlockManager` aus
3. **Add Component** → `Unlock Trigger Barrier`
4. Konfiguriere:
   - `Condition`: `All Enemies Killed`
   - `Barriers`: Size = `1`, Element 0 = `ExitBarrier` (Drag & Drop)

### Schritt 5: Gegner im Level prüfen

1. Stelle sicher, dass mindestens ein Gegner im Level vorhanden ist:
   - Suche im Hierarchy nach `Enemy`-Prefabs (z. B. `Grunt`, `Soldier`, `Joe`)
   - Falls keine Gegner existieren: Platziere einen über **Drag & Drop** aus dem Project-Fenster q
2. Notiere die Anzahl der Gegner für den Test

### Schritt 6: End-to-End-Test durchführen

1. Drücke **Play**
2. **Phase 1 – Barrier locked (Start):**
   - ✅ Barriere ist sichtbar (rotes Material)
   - ✅ Spieler kann nicht durch die Barriere laufen (Collider blockiert)
   - ✅ Notification "Ausgang gesperrt" erscheint (oben links)

3. **Phase 2 – Kampf:**
   - ✅ Gegner sind aktiv und greifen an
   - ✅ Spieler kann Gegner beschießen und besiegen

4. **Phase 3 – Alle Gegner besiegt:**
   - ✅ Barriere wechselt zu grünem (Unlocked) Material
   - ✅ Notification "Ausgang offen" erscheint
   - ✅ Console zeigt:
     - `[UnlockTriggerBarrier] Unlocking barrier 'ExitBarrier'.`
     - `[Barrier] ExitBarrier unlocked.`

5. **Phase 4 – Durchgang nutzen:**
   - ✅ Spieler kann durch die Barriere laufen (Collider deaktiviert)
   - ✅ Der Ausgang / Durchgang ist erreichbar

6. **Phase 5 – Persistenz prüfen:**
   - ✅ Entferne den Spieler vom Ausgang und komme zurück
   - ✅ Barriere bleibt geöffnet (kein Zurückfallen in Locked)

### Schritt 7: Fehlerbehebung

| Problem                     | Mögliche Ursache                            | Lösung                                                         |
| --------------------------- | ------------------------------------------- | -------------------------------------------------------------- |
| Barriere blockiert nicht    | `Is Trigger` aktiviert                      | Deaktiviere `Is Trigger` am Collider                           |
| Material wechselt nicht     | `Barrier Renderer` nicht zugewiesen         | Ziehe den MeshRenderer per Drag & Drop ins Feld                |
| Unlock löst nicht aus       | `Barriers`-Liste leer                       | Füge `ExitBarrier` zur Liste hinzu                             |
| Unlock löst nicht aus       | Gegner nicht korrekt registriert            | Prüfe, ob Gegner das `Enemy`-Script haben                      |
| Keine Notification          | NotificationHUDManager fehlt in Szene       | NotificationHUDManager ist automatisch in FPS-Szenen vorhanden |
| Console-Warning zu Material | `Locked/Unlocked Material` nicht zugewiesen | Weise Materialien im Inspector zu                              |

## Prüfliste (Checkliste für den Entwickler)

- [ ] Level-Szene ist geöffnet und geladen
- [ ] `ExitBarrier` ist an strategischer Stelle platziert
- [ ] Barriere blockiert den Durchgang vollständig (Collider passt)
- [ ] `Barrier`-Komponente ist vollständig konfiguriert (Renderer + Materialien)
- [ ] `UnlockManager` mit `Unlock Trigger Barrier` existiert
- [ ] Barrier-Referenz in `UnlockManager` ist verdrahtet
- [ ] Mindestens ein Gegner ist im Level vorhanden
- [ ] **Play Mode Test:**
  - [ ] Start → Barriere locked (rot) + Notification
  - [ ] Alle Gegner besiegen → Barriere unlocked (grün) + Notification
  - [ ] Durchgang ist frei passierbar
  - [ ] Persistenz: Barriere bleibt unlocked
- [ ] Keine Fehler in der Console
- [ ] Alle Akzeptanzkriterien aus der README sind erfüllt
