using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tubes : MonoBehaviour
{
    public int maxhealth;
    public int currenthealth;
    public TubeState tubeState = TubeState.Selectedable;
    private SpriteRenderer spriteRenderer;
    
    public enum TubeState{
        Selectedable,
        Broken,
        AbleToBreak,
        Absorbed,
    }

    public enum CrystalAni{
        Normal,
        Absorbing,
        Broken,
        Absorbed,
    }

    private CrystalAni animationState;
    private Animator animator; 
    // Start is called before the first frame update
    void Start()
    {
        tubeState = TubeState.Selectedable;
        animationState = CrystalAni.Normal;
        spriteRenderer = GetComponent<SpriteRenderer>();
        currenthealth = maxhealth;
        animator = GetComponent<Animator>();
        // UpdateColor();
    }

    // Update is called once per frame
    void Update()
    {
        if (currenthealth <=0)
        {
            tubeState = TubeState.Broken;
            animationState = CrystalAni.Broken;
        }
        UpdateColor();
    }

    public void SetState(TubeState newState)
    {
        tubeState = newState;
        // UpdateColor();
    }

    private void UpdateColor()
    {
        if (spriteRenderer == null) return;

        switch (tubeState)
        {
            case TubeState.Selectedable:
                animationState = CrystalAni.Normal;
                break;
            case TubeState.AbleToBreak:
                animationState = CrystalAni.Absorbing;
                animator.SetBool("Absorbing",true);
                break;
            case TubeState.Broken:
                animationState = CrystalAni.Broken;
                animator.SetBool("Broken",true);
                break;
            case TubeState.Absorbed:
                animationState = CrystalAni.Absorbed;
                animator.SetBool("Absorbed",true);
                break;
        }
    }

    public bool isBroken()
    {
        return tubeState == TubeState.Broken;
    }

    public bool isAbleToBreak()
    {
        return tubeState == TubeState.AbleToBreak;
    }
    
    public TubeState GetTubeState(){
        return tubeState;
    }
}
