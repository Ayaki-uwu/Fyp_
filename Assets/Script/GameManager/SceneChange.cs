using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public string sceneToload;
    
    [SerializeField]
    public playerDataAsset playerData;
    // Start is called before the first frame update
    public void loadScence(string sceneToload){
        player playerScript = FindObjectOfType<player>();
        if (playerScript != null)
        {
            playerScript.DestroyEntity();
            playerScript.SavePlayerData(); //before scene change
        }
        else
        {
            Debug.LogWarning("Player not found when trying to save data before scene switch.");
        }
        // player.instance.DestroyEntity();
        SceneManager.LoadScene(sceneToload);
        }

    public void quitGame(){
        Application.Quit();
    }

    
    private void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.CompareTag("Player"))
        {
            loadScence(sceneToload);
        }
    }
}
