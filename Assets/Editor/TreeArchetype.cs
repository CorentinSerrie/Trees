using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new_archetype", menuName = "ScriptableObjects/TreeArchetype", order = 1)]
public class TreeArchetype : ScriptableObject
{
    public int LoopCount;
    public int sides;
    public float truncHeight;
    public float truncWidth;

    public Vector2 MainBranchHeightScaleInterval;
    public Vector2 MainBranchWidthScaleInterval;
    public Vector2 MainBranchRotationInterval;
    public Vector2 MainBranchBendingInterval;

    public Vector2 SecondaryBranchHeightScaleInterval;
    public Vector2 SecondaryBranchWidthScaleInterval;
    public Vector2 SecondaryBranchRotationInterval;
    public Vector2 SecondaryBranchBendingInterval;
}
