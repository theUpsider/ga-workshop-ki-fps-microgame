---
req-id: SWR-300
status: draft
trace: optional
title: Epic: Interaktionssystem mit Schaltern oder Terminals
---

# Epic: Interaktionssystem mit Schaltern oder Terminals

**GitHub-Epic:** [#13 – Feature 2: Interaktionssystem mit Schaltern oder Terminals](https://github.com/theUpsider/ga-workshop-ki-fps-microgame/issues/13)

## Ziel

Der Spieler kann gezielt mit Objekten in der Welt interagieren. Eine Interaktion wird bewusst durch eine aktive Eingabe ausgelöst und verändert einen Spielzustand.

## Feature-Beschreibung

Im Level gibt es mindestens ein interaktives Objekt, beispielsweise einen Schalter, ein Terminal, einen Generator oder eine Konsole. Der Spieler kann eindeutig erkennen, ob ein Objekt interaktiv ist und ob es sich in Interaktionsreichweite befindet.

Die Interaktion wird ausschließlich durch eine aktive Spielereingabe ausgelöst. Darauf reagiert mindestens ein anderes Spielsystem, beispielsweise eine Barriere, ein Alarm, ein Exit oder ein Buff. Die erfolgreiche oder abgelehnte Interaktion wird sichtbar oder hörbar kommuniziert.

Die Lösung soll mit den vorhandenen Eingabe-, UI- und Gameplay-Systemen arbeiten und so entworfen sein, dass weitere interagierbare Objekttypen und Reaktionen ergänzt werden können, ohne die zentrale Interaktionslogik zu duplizieren.

## Umfang

Dieses Epic umfasst:

- die Erkennung eines interaktiven Objekts in Spielerreichweite,
- die Kommunikation der Interaktionsmöglichkeit an den Spieler,
- die bewusste Auslösung über eine Spielereingabe,
- mindestens ein konkretes interaktives Objekt im Level,
- die Reaktion mindestens eines anderen Spielsystems,
- sichtbares oder hörbares Feedback zur Interaktion,
- eine gemeinsame, erweiterbare Abstraktion für interagierbare Objekte.

Nicht Bestandteil dieses Epics sind ein Dialogsystem, ein Inventar- oder Schlüsselgegenstandssystem sowie eine persistente Speicherung von Interaktionszuständen über Level- oder Spielstarts hinweg, sofern dies nicht für das gewählte Beispielobjekt zwingend erforderlich ist.

## Funktionale Anforderungen

### Erkennung und Reichweite

- Das System ermittelt, ob sich ein geeignetes interaktives Objekt in der definierten Reichweite des Spielers befindet.
- Außerhalb der Reichweite darf das Objekt nicht über die reguläre Interaktionseingabe ausgelöst werden.
- Wenn mehrere interaktive Objekte erreichbar sind, wird genau ein nachvollziehbares Ziel ausgewählt.
- Das aktuelle Interaktionsziel wird entfernt, sobald es nicht mehr gültig, nicht mehr erreichbar oder deaktiviert ist.

### Spielerführung

- Ein erreichbares interaktives Objekt wird durch einen UI-Hinweis, eine Hervorhebung am Objekt oder ein vergleichbar eindeutiges Feedback kenntlich gemacht.
- Der Hinweis benennt die erforderliche Eingabe oder macht die mögliche Aktion anderweitig verständlich.
- Der Hinweis verschwindet, sobald keine gültige Interaktion mehr möglich ist.

### Auslösung

- Eine Interaktion findet nur statt, wenn der Spieler die konfigurierte Interaktionseingabe aktiv betätigt.
- Reines Annähern, Anschauen oder Betreten der Reichweite darf die eigentliche Aktion nicht automatisch auslösen.
- Eine ungültige oder gesperrte Interaktion verändert keinen Spielzustand.
- Mehrfachauslösungen innerhalb eines einzelnen Eingabeereignisses werden verhindert.

### Reaktion und Feedback

- Mindestens ein konkretes interaktives Objekt ist in einem spielbaren Level oder einer geeigneten Testszene eingerichtet.
- Die Interaktion verändert nachweisbar den Zustand eines anderen Systems, beispielsweise einer Barriere, eines Alarms, eines Exits oder eines Buffs.
- Eine erfolgreiche Interaktion erzeugt sichtbares oder hörbares Feedback.
- Falls das Beispielobjekt nur einmal verwendet werden darf, ignoriert es weitere Eingaben oder kommuniziert seinen bereits aktivierten Zustand nachvollziehbar.

### Erweiterbarkeit

- Gemeinsames Verhalten wird über eine klar definierte Schnittstelle, Basiskomponente oder gleichwertige Abstraktion bereitgestellt.
- Spielererkennung und Eingabeverarbeitung hängen nicht von einem einzelnen konkreten Schalter- oder Terminaltyp ab.
- Neue interagierbare Typen können ergänzt werden, ohne vorhandene Typprüfungen oder eine zentrale Fallunterscheidung für jeden neuen Typ erweitern zu müssen.
- Konfigurierbare Werte wie Reichweite, Hinweise und Feedbackreferenzen sind im Unity Inspector zugänglich, soweit dies für Designer sinnvoll ist.

## Nicht-funktionale Anforderungen

- Die Implementierung integriert sich in die bestehenden Unity- und FPS-Microgame-Systeme und vermeidet unnötige doppelte Logik.
- Fehlende optionale Feedbackreferenzen führen nicht zu unbehandelten Exceptions.
- Zustandswechsel sind eindeutig und erzeugen keine wiederholten Seiteneffekte durch ein einzelnes Eingabeereignis.
- Öffentliche und serialisierte Felder erhalten verständliche Namen und Inspector-Hinweise.

## Akzeptanzkriterien

- [ ] Der Spieler kann ein interaktives Objekt in Reichweite erkennen.
- [ ] Die Interaktion wird nur durch eine aktive Eingabe des Spielers ausgelöst.
- [ ] Außerhalb der zulässigen Reichweite kann keine Interaktion ausgelöst werden.
- [ ] Mindestens ein anderes System reagiert auf die Interaktion.
- [ ] Die Interaktion erzeugt sichtbares oder hörbares Feedback.
- [ ] Ungültige oder gesperrte Interaktionen verändern keinen Spielzustand.
- [ ] Ein einzelnes Eingabeereignis löst die Aktion höchstens einmal aus.
- [ ] Das System ist für weitere interagierbare Typen erweiterbar, ohne die zentrale Interaktionslogik typspezifisch umzubauen.

## Verifikation

- Automatisierte Edit-Mode-Tests prüfen die vom konkreten Level unabhängige Interaktions- und Zustandslogik, soweit technisch sinnvoll.
- Ein manueller Play-Mode-Test prüft Erkennung, Reichweite, Eingabe, Feedback und die Reaktion des angebundenen Systems im vollständigen Spielfluss.
- Der Test umfasst mindestens einen negativen Fall, beispielsweise eine Eingabe außerhalb der Reichweite oder an einem deaktivierten Objekt.
- Das Testergebnis und verbleibende manuelle Prüfungen werden nachvollziehbar dokumentiert.

## Definition of Done

- [ ] Alle Akzeptanzkriterien sind erfüllt oder verbleibende Einschränkungen sind ausdrücklich dokumentiert.
- [ ] Das Feature wurde im relevanten Level oder in einer geeigneten Testszene getestet.
- [ ] Relevante automatisierte Tests wurden ausgeführt und bestanden.
- [ ] Der finale Diff wurde auf unbeabsichtigte Änderungen und Regressionen geprüft.
- [ ] Erforderliche Unity-Editor-, Inspector- und Level-Setup-Schritte sind dokumentiert.
- [ ] Die Umsetzung wurde in einem nachvollziehbaren Git-Commit festgehalten.

