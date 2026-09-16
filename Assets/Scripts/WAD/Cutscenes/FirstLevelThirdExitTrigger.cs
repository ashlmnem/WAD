using UnityEngine;
using UnityEngine.Playables;

public class SceneThirdEndTriggers : MonoBehaviour
{
    public PlayableDirector playable_director;

    public void Trigger()
    {
        playable_director.Play();
    }
}
