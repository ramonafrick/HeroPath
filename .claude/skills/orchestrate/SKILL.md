---
name: orchestrate
description: >
  Full development workflow for a feature, run end-to-end in 9 gated phases:
  Clarify (grill-me) → Plan (vertical slices) → Implement (per slice,
  code+tests) → Build (loop to green) → Verify (Playwright E2E + visual
  check of screenshots) → Review (quality+security, severity-picked fixes)
  → Document → Update relevant project guidance → Audit changed guidance
  for size/SRP. Human approval gates after Clarify and after Plan. Invoke
  with /orchestrate [feature description].
disable-model-invocation: true
---

# Orchestrator: Full Development Workflow

Aufgabe/Feature: $ARGUMENTS

Führe die 9 Phasen in Reihenfolge aus. Delegierte Phasen dürfen den Kontext isolieren. Verwende nur verfügbare Agenten und bestätige keine Modell- oder Tool-Fähigkeiten, die in der aktuellen Umgebung nicht existieren. Phasen, die nicht delegiert werden (1, 8, 9, alle HITL-Gates), laufen im aktuellen Sitzungsmodell.

| Phase | Name | Modell | Wie |
|---|---|---|---|
| 1 | Clarify | Sitzungsmodell | `grill-me` Skill, inline (braucht AskUserQuestion) |
| — | **HITL-Gate** | — | AskUserQuestion: requirements.md freigeben? |
| 2 | Plan | Sitzungsmodell oder verfügbarer Plan-Agent | Module und vertikale Slices |
| — | **HITL-Gate** | — | AskUserQuestion: plan.md freigeben, welcher Slice zuerst? |
| 3+4 | Implement + Build (pro Slice, Loop) | Sonnet / Haiku | Agent `implementer` → Agent `build-fixer` |
| 5 | Verify (E2E + Visual) | Sonnet | Agent `visual-checker` (nur wenn sichtbare UI geändert wurde) |
| 6 | Review | Sonnet | Agent `code-reviewer` + Agent `security-reviewer` parallel |
| 7 | Documentation | Sonnet | Agent `docs-writer` |
| 8 | Long-Term Memory | Sitzungsmodell | inline, aktualisiert Projekt-`.claude/` |
| 9 | Size/SRP-Audit | Sitzungsmodell | inline |

Vor dem Start: Ermittle aus dem `architecture`-Skill und dem Repository, welche Projekte betroffen sind (`Shared`, `Modules.Levels`, `Modules.Game`, jeweils mit `.UI`, `Hosts/HeroPath.Web`). Frage nur nach Host- oder Scope-Entscheidungen, die sich nicht aus der Anfrage ableiten lassen. Eine MAUI-App ist kein bestehender Host und darf nicht stillschweigend als Voraussetzung angenommen werden.

Projekt-Skills (in Phase 2 im Plan pro Slice nennen, die Agents laden sie über ihr `skills:`-Frontmatter): `architecture` (Module, Abhängigkeiten), `dotnet10` (C#/Solution), `game-rules` (Spielregeln), `blazor-ui` (Komponenten), `localization` (Texte), `level-design` (Levels), `svg-assets` (Grafiken), `e2e-playwright` (E2E/Screenshots), `release` (CI/Deployment).

---

## PHASE 1: Clarify

Folge dem `grill-me`-Skill für `$ARGUMENTS` (7 Bereiche, `AskUserQuestion`, max. 2 Fragen pro Runde). Ergebnis: `docs/requirements.md`.

**STOPP**: Zeige `docs/requirements.md` und frage per `AskUserQuestion` explizit: freigeben und weiter zu Phase 2, oder Änderungen? Ohne explizite Freigabe nicht weitermachen.

---

## PHASE 2: Plan (Vertical Slices)

Erstelle einen Plan im aktuellen Kontext oder verwende einen verfügbaren Plan-Agenten. Gib `docs/requirements.md`, die Projektunterlagen und den `architecture`-Skill mit.

Was ein guter Slice ist: Er schneidet durch die tatsächlich betroffenen Projekte (Domain → Application → UI → Host-Registrierung), ist ohne den nächsten Slice testbar und bleibt klein genug, um überprüfbar zu sein. Er respektiert die Abhängigkeitsregeln (`Shared ← Levels ← Game`). Ein neues Modul nur nach den Kriterien im `architecture`-Skill.

Ergebnis: `docs/plan.md` mit Modul-Überblick und pro Slice: Name, Ziel, Projekte/Dateien, Tests (xUnit/bUnit/**E2E-Flow mit Screenshot-Namen**, falls sichtbar), Akzeptanz, Abhängigkeiten. Dazu die Build- und Testbefehle aus dem Repository.

**STOPP**: Zeige die Slice-Tabelle und frage per `AskUserQuestion`: freigeben, und welcher Slice zuerst?

---

## PHASE 3+4: Implement → Build (Loop pro Slice)

Für jeden Slice, in der freigegebenen Reihenfolge:

1. **Implement**: Agent-Tool, `subagent_type: "implementer"`. Übergib die Slice-Definition aus plan.md, den relevanten Abschnitt von requirements.md und die betroffenen Dateien. Bei sichtbaren Änderungen gehören `data-testid`-Attribute und der E2E-Test für den Flow dazu.
2. **Build**: Agent-Tool, `subagent_type: "build-fixer"`, mit den geänderten Dateien. Er loopt intern bis grün oder 3 Versuche. Dazu gehören die ArchitectureTests. Die E2E-Tests laufen hier nicht, sie bleiben ohne `HEROPATH_E2E` übersprungen.
3. Wenn build-fixer nach 3 Versuchen immer noch rot meldet: **STOPP**, zeige den Fehler und frage die Nutzerin. Nicht selbst weiterraten.
4. Erstelle nur dann einen Commit, wenn die Nutzerin dies angefordert oder freigegeben hat. Sonst lasse Änderungen uncommitted.
5. Kurzer Kontext-Reset: notiere "Slice X abgeschlossen, nächster: Y", dann weiter.

Erst wenn alle Slices durch sind: weiter zu Phase 5.

---

## PHASE 5: Verify (E2E + Visual Check)

Nur wenn ein Slice sichtbare UI geändert hat (Razor, CSS, SVG, resx). Sonst überspringen und das im Abschluss erwähnen.

1. Agent-Tool, `subagent_type: "visual-checker"`. Übergib die Akzeptanzkriterien und die geänderten Komponenten. Er führt `HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E` aus und liest alle Screenshots, normal und in Graustufen, jeweils auf Deutsch und Englisch.
2. Schlägt ein E2E-Test fehl, ist das ein [MUST]-Finding. Wenn die Ursache eindeutig im Code liegt, gib es zuerst an `build-fixer` mit der Fehlermeldung.
3. Die Findings des visuellen Checks werden **nicht** sofort umgesetzt, sondern gehen gesammelt in die Auswahl von Phase 6.
4. Meldet der Checker Lücken (Komponente ohne Screenshot), nimm sie als [SHOULD]-Finding auf.

---

## PHASE 6: Review (Quality + Security + Visual, max. 3 Runden)

Runde 1 von maximal 3:

1. Dispatch parallel: Agent `code-reviewer` (Vereinfachung, Konsistenz, Modulgrenzen) und Agent `security-reviewer` (Security/SAST, inkl. Datenschutz Minderjähriger) auf alle in Phase 3+4 geänderten Dateien.
2. Führe die Findings von code-reviewer, security-reviewer und visual-checker (Phase 5) in einer Liste zusammen, sortiert nach Schwere ([MUST]/🔴 zuerst). Bei jedem Finding steht die Quelle.
3. `AskUserQuestion` (multiSelect): liste jedes Finding als Option (Schwere, Quelle, Kurzbeschreibung). Die Nutzerin wählt aus, welche jetzt umgesetzt werden.
4. Setze die ausgewählten Findings um.
5. Zurück zu **Phase 4** (build-fixer) auf die geänderten Dateien. Wurden visuelle Findings umgesetzt: danach **Phase 5** erneut, damit die Korrektur auf den Screenshots belegt ist.
6. Wenn nach dieser Runde noch offene Findings da sind: weiter zu Phase 7 und sie als bekannte Einschränkung notieren.
7. Weitere Runden sind möglich, aber **nie mehr als 3 insgesamt**.

---

## PHASE 7: Documentation

Verwende `docs-writer` oder dokumentiere direkt: übergib den Plan (falls vorhanden), die implementierten Dateien und nicht adressierte Findings. Dokumentiere nur tatsächlich implementiertes Verhalten. Ändere README nur bei Auswirkungen auf Setup, Nutzung oder öffentliche Befehle. Kommt ein neues Modul dazu, wird die Modultabelle in `docs/Architektur C4.md` nachgeführt.

---

## PHASE 8: Long-Term Memory (Projekt, nicht persönliche Cross-Session-Memory)

Aktualisiere Projektanweisungen nur dann, wenn sich während der Arbeit eine dauerhafte Konvention oder ein belegter Gotcha ergeben hat. Das betrifft vorhandene `.claude/`-Dateien oder `CLAUDE.md`. Lege keine Langzeitnotizen ausserhalb des Repositories an.

1. Vergleiche neue, belegte Erkenntnisse mit vorhandenen Projektanweisungen und Skills.
2. Aktualisiere nur, was sich tatsächlich geändert oder erweitert hat: neue Gotchas, neue Konventionen, neue Module/Komponenten/Dateien, geänderte Commands, neue `data-testid`-Konventionen.
3. Nichts hinzufügen, was schon aus Code oder Git-History ableitbar ist.

---

## PHASE 9: Size/SRP-Audit

Für jede in Phase 8 berührte oder neu erstellte Datei unter `.claude/`:

- **SKILL.md-Body**: Ziel < 500 Zeilen. Über dem Limit in `references/*.md` aufteilen und aus SKILL.md eine Ebene tief verlinken. Referenzdateien mit mehr als 100 Zeilen bekommen ein Inhaltsverzeichnis.
- **Projektanweisungen**: knapp und thematisch fokussiert halten. Nur bei tatsächlichem Umfangsbedarf aufteilen.
- SRP-Check: Enthält eine Datei mehrere unabhängige Themen? Dann nach Domäne aufteilen.

---

## Regeln

- Niemals einen Slice beginnen, wenn der Build vorheriger Arbeit noch rot ist.
- Review (Phase 6) und Tests (Teil von Phase 3) nie überspringen. Verify (Phase 5) nur überspringen, wenn keine sichtbare UI geändert wurde.
- HITL-Gates sind turn-basiert: Nach dem Stopp endet die Antwort, es geht erst in einer neuen Nutzer-Nachricht weiter. Schweigen oder Themenwechsel zählt nicht als Freigabe.
- Nach 2 gescheiterten Korrekturversuchen am selben Problem: nicht weiter raten, Nutzerin einbeziehen.
- Verwende Delegation nur, wenn passende Agenten tatsächlich verfügbar und für die Aufgabe sinnvoll sind.
- Behandle vorgeschlagene oder zukünftige Architektur (z. B. MAUI) nicht als implementierten Projektbestand.
