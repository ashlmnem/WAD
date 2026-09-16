using UnityEngine;
using UnityEngine.Playables;

public class FirstLevelFirstExitTrigger : MonoBehaviour
{
    public PlayableDirector playable_director;

    public Transform target;
    public int distance;

    void FixedUpdate()
    {
        if (target.position.z > distance) playable_director.Play();
    }
}
