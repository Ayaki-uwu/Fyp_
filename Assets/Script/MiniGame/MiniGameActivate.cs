using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGameActivate : MonoBehaviour
{
    public GameObject minigameCanvas;
    private Tubes tubeScript;
    private bool minigameCompleted = false;

    void Start()
    {
        tubeScript = GetComponent<Tubes>();
        if (minigameCanvas != null)
        {
            minigameCanvas.SetActive(false);
        }
    }

    public void StartMinigame()
    {
        if (tubeScript != null && !minigameCompleted)
        {
            minigameCanvas.SetActive(true);
        }
    }

    public void CompleteMinigame()
    {
        minigameCompleted = true;
        if (tubeScript != null)
        {
            tubeScript.SetState(Tubes.TubeState.Broken);
        }

        if (minigameCanvas != null)
        {
            minigameCanvas.SetActive(false);
        }

        // Notify the boss controller
        WolfBoss.Instance.OnCrystalBroken();
    }

    public void FailedMinigame()
    {
        minigameCompleted = true;
        if (minigameCanvas != null)
        {
            minigameCanvas.SetActive(false);
        }

        // Notify the boss controller
        WolfBoss.Instance.OnCrystalBroken();
        WolfBoss.Instance.IceRain();
    }
}
