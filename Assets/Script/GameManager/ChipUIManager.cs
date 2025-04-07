using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChipUIManager : MonoBehaviour
{
    public GameObject chipPanel;
    public GameObject spiderChipButton;
    public GameObject wolfChipButton;
    public Transform chipButtonParent;

    public TextMeshProUGUI bulletNum;

    public Image gunStateImage;
    public Image chipStateImage;
    public Sprite NullIcon;
    public Sprite spiderGunIcon;
    public Sprite fireGunIcon;
    public Sprite normalGunIcon;
    public Sprite SpiderChipIcon;
    public Sprite WolfChipIcon;

    private player playerScript;
    public enemy target;
    private player.PlayerState playerState;
    // Start is called before the first frame update
    void Start()
    {
        playerScript = FindObjectOfType<player>();
        chipPanel.SetActive(false); // Hide initially
        InitiateChipUI();
        UpdateGunStateImages();
        target = FindObjectOfType<enemy>();

    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            chipPanel.SetActive(!chipPanel.activeSelf);
            playerScript.SetControl();
            // RefreshChipUI();
            InitiateChipUI();
            target.setControl();
        }
        if(Input.GetKeyDown(KeyCode.T))
        {
            PostProcessingManager.Instance.HurtEffect();
        }
        UpdateGunStateImages();
        bulletNum.text = $"{playerScript.bulletCount} / {playerScript.maxbulletCount}";
    }

    void InitiateChipUI()
    {
        if( playerScript.chipBag.GetCollectedChips().Contains("SpiderChip") )
        {
            spiderChipButton.GetComponent<Image>().color = new Color(1,1,1,1);
        }else
        {
            spiderChipButton.GetComponent<Image>().color = new Color(1,1,1,0.5f);
        }
        if( playerScript.chipBag.GetCollectedChips().Contains("WolfChip") )
        {
            wolfChipButton.GetComponent<Image>().color = new Color(1,1,1,1);
        }
        else
        {
            wolfChipButton.GetComponent<Image>().color = new Color(1,1,1,0.5f);
        }
    }

    public void EquipChip(string chipName)
    {
        Debug.Log($"Equip Chip {chipName}");
        playerScript.chipBag.EquipChip(chipName);
        UpdateChipStateImages();
    }
    public void UnequipChip(string chipName)
    {
        Debug.Log($"Unequip Chip {chipName}");
        if(playerScript.chipBag.GetEquippedChip() != chipName)  return;
        playerScript.chipBag.UnequipChip();
        UpdateChipStateImages();
    }

    void UpdateGunStateImages()
    {
        string equippedChip = playerScript.chipBag.GetEquippedChip();

        if (playerScript.playerState == player.PlayerState.SpiderGun)
        {
            gunStateImage.sprite = spiderGunIcon;
            gunStateImage.enabled = true;
        }
        else if (playerScript.playerState == player.PlayerState.FireGun)
        {
            gunStateImage.sprite = fireGunIcon;
            gunStateImage.enabled = true;
        }
        else
        {
            gunStateImage.sprite = normalGunIcon;
            gunStateImage.enabled = true;
        }
    }

    void UpdateChipStateImages()
    {
        string equippedChip = playerScript.chipBag.GetEquippedChip();

        if (equippedChip == "SpiderChip")
        {
            chipStateImage.sprite = SpiderChipIcon;
            chipStateImage.enabled = true;
        }
        else if (equippedChip == "WolfChip")
        {
            chipStateImage.sprite = WolfChipIcon;
            chipStateImage.enabled = true;
        }
        else
        {
            chipStateImage.sprite = NullIcon;
            chipStateImage.enabled = true;
        }
    }
}
