# Editor-Anleitung: Visuelles Feedback für Barriere-Zustände

**Feature:** Lockable Barrier (Gesperrter Ausgang)
**Task:** `Docs/requirements/lockable-barrier/02-code-visual-feedback.md`

## Übersicht

- Das visuelle Feedback ist Teil der bestehenden `Barrier`-Komponente in `Barrier.cs`
- Zwei Material-Felder müssen im Inspector zugewiesen werden: `Locked Material` und `Unlocked Material`
- Ein `UnityEvent<BarrierState>` (`OnStateChanged`) ist im Inspector sichtbar und kann mit Sound-Events, Partikeln oder Animationen verdrahtet werden
- Die Notification-HUD zeigt automatisch "Ausgang gesperrt" / "Ausgang offen" an (keine Konfiguration nötig)

## Schritt-für-Schritt

### Schritt 1: Materialien erstellen

1. Im **Project**-Fenster: `Assets/FPS/Art/Materials/` öffnen (oder erstellen)
2. **Rechtsklick** → **Create** → **Material**
   - Nenne es `Barrier_Locked`
3. **Rechtsklick** → **Create** → **Material**
   - Nenne es `Barrier_Unlocked`
4. Konfiguriere die Materialien:

| Material           | Farbe / Albedo                    | Empfohlener Effekt                                  |
| ------------------ | --------------------------------- | --------------------------------------------------- |
| `Barrier_Locked`   | Rot (`#FF0000` oder ähnlich)      | Undurchsichtig, solide                              |
| `Barrier_Unlocked` | Grün (`#00FF00`) oder Transparent | Evtl. `Render Mode` auf `Fade` für Durchsichtigkeit |

### Schritt 2: Materialien in der Barrier-Komponente zuweisen

1. Wähle das Barriere-GameObject (`ExitBarrier`) im Hierarchy aus
2. Im **Inspector** → `Barrier`-Komponente:
   - **Locked Material:** Ziehe `Barrier_Locked` per Drag & Drop hinein
   - **Unlocked Material:** Ziehe `Barrier_Unlocked` per Drag & Drop hinein

3. Nach der Zuweisung zeigt der MeshRenderer sofort das `Locked Material`

### Schritt 3: UnityEvent (OnStateChanged) verdrahten (optional)

Der `OnStateChanged`-Event im Inspector der `Barrier`-Komponente kann mit beliebigen Effekten verdrahtet werden:

**Beispiel: Sound abspielen beim Entsperren**

1. Klicke auf das **+** unter `OnStateChanged ()` im Inspector
2. Ziehe ein GameObject mit `AudioSource` (z. B. `ExitBarrier` selbst) per Drag & Drop ins Feld
3. Wähle im Dropdown: **AudioSource → PlayOneShot (AudioClip)**
4. Weise ein `AudioClip` zu (z. B. Tür-öffnen-Sound)

**Beispiel: Partikel-Effekt abspielen**

1. Erstelle ein Particle System als Child von `ExitBarrier`
2. Verdrahte es via `OnStateChanged` → `ParticleSystem → Play()`

### Schritt 4: Visuelles Feedback testen

1. Drücke **Play**
2. Prüfe: Die Barriere zeigt das rote (`Locked`) Material
3. (Wenn bereits ein Unlock-Trigger existiert:) Besiege alle Gegner
4. Prüfe: Die Barriere wechselt zum grünen (`Unlocked`) Material
5. Prüfe: In der oberen linken Ecke erscheint die Notification "Ausgang offen"

## Prüfliste (Checkliste für den Entwickler)

- [ ] `Barrier_Locked.mat` existiert in `Assets/FPS/Art/Materials/`
- [ ] `Barrier_Unlocked.mat` existiert in `Assets/FPS/Art/Materials/`
- [ ] `Locked Material` ist im Barrier-Inspector zugewiesen (rotes Material)
- [ ] `Unlocked Material` ist im Barrier-Inspector zugewiesen (grünes/transparentes Material)
- [ ] Im Play Mode: Locked = rotes Material sichtbar
- [ ] Im Play Mode: Nach Unlock = grünes Material sichtbar
- [ ] Notification "Ausgang gesperrt" / "Ausgang offen" erscheint automatisch
- [ ] (Optional) Sound oder Partikel sind via `OnStateChanged` verdrahtet
