using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public playerDataAsset playerData;

    public void StartNewGame()
    {
        // Reset player data
        playerData.currentHealth = playerData.maxHealth;
        playerData.playerState = player.PlayerState.Gun;
        playerData.collectedChips.Clear();
        playerData.equippedChip = null;

        // Then load the first scene
        SceneManager.LoadScene("GameStartScene");
    }
}

