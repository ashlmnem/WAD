using UnityEngine;

namespace WAD.Vehicles
{
    /// <summary>
    /// Flugverhalten eines Jets - alle Werte im Inspector einstellbar.
    /// Erstelle im Editor via: Assets > Create > WAD > Jet Flight Profile
    /// </summary>
    [CreateAssetMenu(fileName = "JetFlight_", menuName = "WAD/Jet Flight Profile")]
    public class JetFlightProfile : ScriptableObject
    {
        [Header("Triebwerk")]
        [Tooltip("Schubkraft auf der Stelle (m/s^2)")]
        public float baseThrust = 22f;
        [Tooltip("Maximale Vorwaertsgeschwindigkeit (m/s)")]
        public float maxForwardSpeed = 160f;
        [Tooltip("Rueckwaerts-/Bremsbeschleunigung")]
        public float brakeAcceleration = 40f;
        [Tooltip("Nachbrenner-Multiplikator (Shift)")]
        public float afterburnerMultiplier = 1.8f;
        [Tooltip("Wie schnell der Schub auf Aenderungen reagiert (Traegheit)")]
        public float throttleResponse = 1.2f;

        [Header("Steuerung - Gier/Nick/Roll")]
        [Tooltip("Gierrate (Grad/s) bei voller Seitenruder-Eingabe")]
        public float yawRate = 45f;
        [Tooltip("Nickrate (Grad/s) bei voller Hoehenruder-Eingabe")]
        public float pitchRate = 60f;
        [Tooltip("Rollrate (Grad/s) bei voller Querruder-Eingabe")]
        public float rollRate = 120f;
        [Tooltip("Wie traege die Steuerflaechen reagieren - hoeher = direkter")]
        public float controlResponse = 3.5f;
        [Tooltip("Automatische Roll-Stabilisierung (0 = keine, 1 = stark)")]
        [Range(0f, 1f)] public float rollAutoLevel = 0.35f;

        [Header("Auftrieb & Schweben")]
        [Tooltip("Wie stark der Jet ohne Vorwaertsfahrt absinkt (0 = schwebt, 1 = faellt sofort)")]
        [Range(0f, 1f)] public float gravityInfluence = 0.25f;
        [Tooltip("Vertikaler Schub fuer Senkrechtstart/-landung (VTOL). 0 = aus.")]
        public float verticalThrust = 14f;

        [Header("Boden & Kollision")]
        [Tooltip("Hoehe ueber Grund, ab der der Fahrwerks-Kontakt gilt")]
        public float groundProximity = 2.5f;
        [Tooltip("Radius fuer die Kollisionspruefung der Flugzelle")]
        public float collisionRadius = 3f;
        [Tooltip("Aufprallgeschwindigkeit, ab der der Jet beschaedigt wird (m/s)")]
        public float crashSpeedThreshold = 45f;

        [Header("Startsequenz")]
        [Tooltip("Dauer des Hochfahrens der Triebwerke, bevor der Spieler steuern kann")]
        public float engineStartupSeconds = 4f;
        [Tooltip("Dauer des Herunterfahrens beim Aussteigen")]
        public float engineShutdownSeconds = 2f;
        [Tooltip("Darf der Jet senkrecht starten (VTOL)?")]
        public bool canVerticalTakeOff = true;
        [Tooltip("Mindestgeschwindigkeit fuer einen klassischen Start (wenn kein VTOL)")]
        public float takeoffSpeed = 45f;

        [Header("Kamera")]
        [Tooltip("Position der Cockpit-/Verfolgerkamera relativ zur Flugzelle")]
        public Vector3 cameraLocalOffset = new Vector3(0f, 1.6f, 0.6f);
        [Tooltip("Sichtfeld im Jet")]
        public float cameraFOV = 70f;

        [Header("Treibstoff")]
        [Tooltip("Verbrauch pro Sekunde bei Vollschub. 0 = unbegrenzt.")]
        public float fuelBurnPerSecond = 0.35f;
        [Tooltip("Nachbrenner verbraucht entsprechend mehr")]
        public bool afterburnerBurnsExtra = true;
    }
}
