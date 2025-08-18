using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class Fight 
{
    //private GameObject playerWeapon;      // ÎäÆ÷
    //private int weaponIndex = 0;          // ÎäÆ÷Ë÷Òý

    public AnimationMixerPlayable fightPlayable;
    public void init(PlayableGraph playableGraph)
    {
        fightPlayable= AnimationMixerPlayable.Create(playableGraph, 1);
        var Fight01 = Resources.Load<AnimationClip>("Animator/Animations/Punch");
        var Fight001 = AnimationClipPlayable.Create(playableGraph, Fight01);
        playableGraph.Connect(Fight001, 0, fightPlayable, 0);
    }
    //void SwitchWeapon(Transform playerRightHandBone)
    //{
    //    Destroy(playerWeapon);

    //    weaponIndex++;
    //    if (weaponIndex > 10)
    //    {
    //        weaponIndex = 0;
    //    }
    //    GameObject weaponResource = Resources.Load<GameObject>("Weapons/Sword" + weaponIndex);
    //    if (weaponResource != null)
    //    {
    //        playerWeapon = Instantiate(weaponResource);

    //        playerWeapon.transform.parent = playerRightHandBone;
    //        playerWeapon.transform.localPosition = new Vector3(-0.07f, 0.1f, 0.0f);
    //        playerWeapon.transform.localRotation = Quaternion.Euler(168, 90, 0);
    //        playerWeapon.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    //    }
    //}
}
