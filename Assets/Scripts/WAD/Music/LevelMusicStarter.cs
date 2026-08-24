using UnityEngine;
using WAD.Audio;

namespace WAD.Levels
{
    /// <summary>
    /// In jede Level-Szene legen. Startet beim Laden automatisch den
    /// Level-Track ueber den persistenten MusicManager - der Crossfade
    /// blendet dabei automatisch die vorherige Musik (z.B. Main-Menu-Track) aus.
    /// </summary>
    public class LevelMusicStarter : MonoBehaviour
    {
        public AudioClip levelTrack;
        public bool loop = true;

        private void Start()
        {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayTrack(levelTrack, loop);
            }
            else
            {
                Debug.LogWarning("[LevelMusicStarter] Kein MusicManager gefunden - lief die Main-Menu-Szene vorher nicht?");
            }
        }
    }
}
