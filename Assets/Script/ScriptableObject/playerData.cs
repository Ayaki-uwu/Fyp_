using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public class playerData : using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "playerDataFile")]
public class playerDataAsset : ScriptableObject
{
    public int currentHealth=10;
    public int maxHealth=10;
    public player.PlayerState playerState = player.PlayerState.Gun;
    public List<string> collectedChips = new List<string>();
    public string equippedChip = null;

}
