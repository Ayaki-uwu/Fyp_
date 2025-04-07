using System.Collections;
using System.Collections.Generic;
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
            playerScript.SavePlayerData(); //before scene change
        }
        else
        {
            Debug.LogWarning("Player not found when trying to save data before scene switch.");
        }
        SceneManager.LoadScene(sceneToload);
    }
    private void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.CompareTag("Player"))
        {
            loadScence(sceneToload);
        }
    }
}
