using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class SceneStartTrigger : MonoBehaviour
{
    public bool start_on_scene_loaded = false;
    public PlayableDirector cutscene_director;
    public UnityEvent[] additional_events_before_cutscene;

    public void PlayStartCutscene()
    {
        PlayCutscene();
    }

    void Start()
    {
        if (start_on_scene_loaded)
        {
            PlayCutscene();
        }
        else
        {
            Debug.Log($"{name} is disabled, no cutscene will be played");
        }
    }

    void PlayCutscene()
    {
        foreach (UnityEvent ev in additional_events_before_cutscene)
        {
            ev.Invoke();
        }
        cutscene_director.Play();
    }
}
