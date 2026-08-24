using System.Collections.Generic;
using UnityEngine;

namespace WAD.Core
{
    /// <summary>
    /// Liegt einmal pro Level-Szene in der Welt. Kennt alle Exit-Bedingungen
    /// dieses Levels (aus LevelSO) und prueft sie laufend. Sobald eine erfuellt
    /// ist, wird automatisch der Levelwechsel ausgeloest.
    ///
    /// Fuer Level 1 konkret: 2 aktiv geprüfte Exits (Distanz, Höhe) + 1 durch
    /// Entity-001srp ausgeloester Exit (siehe EntityUseExit + Entity001srp.cs).
    /// </summary>
    public class LevelExitController : MonoBehaviour
    {
        [Header("Referenzen")]
        public LevelSO levelData;
        public Transform player;

        private void Update()
        {
            if (levelData == null || player == null) return;

            foreach (var exit in levelData.exits)
            {
                CheckExit(exit);
            }
        }

        private void CheckExit(ExitCondition exit)
        {
            switch (exit)
            {
                case DistanceFromSpawnExit distanceExit:
                    if (distanceExit.CheckDistance(player.position))
                        TriggerExit(exit);
                    break;

                case AltitudeExit altitudeExit:
                    if (altitudeExit.CheckAltitude(player.position))
                        TriggerExit(exit);
                    break;

                case TimeBasedExit timeExit:
                    timeExit.Tick(Time.deltaTime);
                    if (timeExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                case FlagCheckExit flagExit:
                    flagExit.Tick(Time.deltaTime);
                    if (flagExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                case EntityUseExit entityExit:
                    if (entityExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                case KillCountExit killExit:
                    if (killExit.IsSatisfied(RunStateManager.Instance))
                        TriggerExit(exit);
                    break;

                // ChanceBasedExit wird NICHT hier automatisch geprueft - der wird
                // manuell von der Interaktion ausgeloest (z.B. "Russisches Roulette"-Trigger-Skript).
            }
        }

        private bool alreadyTriggered;

        private void TriggerExit(ExitCondition exit)
        {
            if (alreadyTriggered) return; // verhindert Doppel-Trigger im selben Frame
            alreadyTriggered = true;

            Debug.Log($"[LevelExit] Exit ausgeloest: {exit.exitLabel}");
            exit.OnTriggered(RunStateManager.Instance);

            // TODO: Szenenwechsel zur naechsten Level-Szene ueber SceneManager,
            // sobald die Level-Szenen als eigene Unity-Scenes existieren.
        }
    }
}