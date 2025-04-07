using System.Collections.Generic;
using UnityEngine;

public class ChipBag
{
    private Dictionary<string, bool> chips = new Dictionary<string, bool>(); // Tracks collected chips
    private string equippedChip = null; // Tracks currently equipped chip

    public ChipBag()
    {
        // Initialize all chips as not collected
        chips.Add("SpiderChip", false);
        chips.Add("WolfChip", false);
        foreach (KeyValuePair<string, bool> chip in chips)
        {
            Debug.Log($"Key: {chip.Key}, Value: {chip.Value}");
        }
    }

    // Method to collect a chip
    public void CollectChip(string chipName)
    {
        if (chips.ContainsKey(chipName))
        {
            chips[chipName] = true;
            Debug.Log(chipName + " chip collected!");
        }
    }

    // Method to check if a chip is collected
    public bool HasChip(string chipName)
    {
        foreach (KeyValuePair<string, bool> chip in chips)
        {
            Debug.Log($"Key: {chip.Key}, Value: {chip.Value}");
        }
        return chips.ContainsKey(chipName) && chips[chipName];
    }

    // Method to equip a chip
    public void EquipChip(string chipName)
    {
        if (HasChip(chipName))
        {
            equippedChip = chipName;
            Debug.Log("Equipped chip: " + equippedChip);
        }
        else
        {
            Debug.Log("Chip " + chipName + " not collected.");
        }
    }

    // Method to unequip current chip
    public void UnequipChip()
    {
        Debug.Log("Unequipped chip: " + equippedChip);
        equippedChip = null;
    }

    // Method to get the currently equipped chip
    public string GetEquippedChip()
    {
        return equippedChip;
    }

    public List<string> GetCollectedChips()
    {
        List<string> collected = new List<string>();
        foreach (var chip in chips)
        {
            Debug.Log("GetCollectable Chip" + chip);
            if (chip.Value)
                collected.Add(chip.Key);
        }
        return collected;
    }

    public void RestoreCollectedChips(List<string> savedChips)
    {
        if (savedChips == null) return;
        List<string> keys = new List<string>(chips.Keys);
        foreach (var key in keys)
        {
            chips[key] = savedChips.Contains(key);
        }
    }
}
