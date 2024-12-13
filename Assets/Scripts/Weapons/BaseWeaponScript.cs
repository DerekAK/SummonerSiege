using UnityEngine;

public class BaseWeaponScript : MonoBehaviour
{
    [Tooltip("1-one-handed, 2-double-wielding")]
    [SerializeField] private int weaponType; 
    public int GetWeaponType(){return weaponType;}
    [SerializeField] private int weaponQuality;
    public int GetWeaponQuality(){return weaponQuality;}
    
    // WeaponAnimations

    //sheath animationclip and offsets
    [SerializeField] private AnimationClip sheatheClip;
    public AnimationClip GetSheatheClip(){return sheatheClip;}

    //first weapon
    [SerializeField] private Vector3 positionOffsetFirstWeaponSheath;
    public Vector3 GetPositionOffsetFirstWeaponSheath(){return positionOffsetFirstWeaponSheath;}

    [SerializeField] private Vector3 rotationOffsetFirstWeaponSheath;
    public Vector3 GetRotationOffsetFirstWeaponSheath(){return rotationOffsetFirstWeaponSheath;}

    //second weapon
    [SerializeField] private Vector3 positionOffsetSecondWeaponSheath;
    public Vector3 GetPositionOffsetSecondWeaponSheath(){return positionOffsetSecondWeaponSheath;}

    [SerializeField] private Vector3 rotationOffsetSecondWeaponSheath;
    public Vector3 GetRotationOffsetSecondWeaponSheath(){return rotationOffsetSecondWeaponSheath;}



    //unsheath animationclip and offsets
    [SerializeField] private AnimationClip unsheatheClip;
    public AnimationClip GetUnsheatheClip(){return unsheatheClip;}

    //first weapon
    [SerializeField] private Vector3 positionOffsetFirstWeaponUnsheath;
    public Vector3 GetPositionOffsetFirstWeaponUnsheath(){return positionOffsetFirstWeaponUnsheath;}

    [SerializeField] private Vector3 rotationOffsetFirstWeaponUnsheath;
    public Vector3 GetRotationOffsetFirstWeaponUnsheath(){return rotationOffsetFirstWeaponUnsheath;}

    //second weapon
    [SerializeField] private Vector3 positionOffsetSecondWeaponUnsheath;
    public Vector3 GetPositionOffsetSecondWeaponUnsheath(){return positionOffsetSecondWeaponUnsheath;}

    [SerializeField] private Vector3 rotationOffsetSecondWeaponUnsheath;
    public Vector3 GetRotationOffsetSecondWeaponUnsheath(){return rotationOffsetSecondWeaponUnsheath;}
    
    
    //armed idle animationclip and offsets
    [SerializeField] private AnimationClip idleWeaponClip;
    public AnimationClip GetIdleWeaponClip(){return idleWeaponClip;}
    //first weapon
    [SerializeField] private Vector3 positionOffsetFirstWeaponIdle;
    public Vector3 GetPositionOffsetFirstWeaponIdle(){return positionOffsetFirstWeaponIdle;}

    [SerializeField] private Vector3 rotationOffsetFirstWeaponIdle;
    public Vector3 GetRotationOffsetFirstWeaponIdle(){return rotationOffsetFirstWeaponIdle;}

    //second weapon
    [SerializeField] private Vector3 positionOffsetSecondWeaponIdle;
    public Vector3 GetPositionOffsetSecondWeaponIdle(){return positionOffsetSecondWeaponIdle;}

    [SerializeField] private Vector3 rotationOffsetSecondWeaponIdle;
    public Vector3 GetRotationOffsetSecondWeaponIdle(){return rotationOffsetSecondWeaponIdle;}

    
    
    //armed roam animationclip and offsets
    [SerializeField] private AnimationClip roamingWeaponClip;
    public AnimationClip GetRoamingWeaponClip(){return roamingWeaponClip;}
    //first weapon
    [SerializeField] private Vector3 positionOffsetFirstWeaponRoaming;
    public Vector3 GetPositionOffsetFirstWeaponRoaming(){return positionOffsetFirstWeaponRoaming;}

    [SerializeField] private Vector3 rotationOffsetFirstWeaponRoaming;
    public Vector3 GetRotationOffsetFirstWeaponRoaming(){return rotationOffsetFirstWeaponRoaming;}

    //second weapon
    [SerializeField] private Vector3 positionOffsetSecondWeaponRoaming;
    public Vector3 GetPositionOffsetSecondWeaponRoaming(){return positionOffsetSecondWeaponRoaming;}

    [SerializeField] private Vector3 rotationOffsetSecondWeaponRoaming;
    public Vector3 GetRotationOffsetSecondWeaponRoaming(){return rotationOffsetSecondWeaponRoaming;}



    //armed chase animationclip and offsets
    [SerializeField] private AnimationClip chasingWeaponClip;
    public AnimationClip GetChasingWeaponClip(){return chasingWeaponClip;}
    //first weapon
    [SerializeField] private Vector3 positionOffsetFirstWeaponChasing;
    public Vector3 GetPositionOffsetFirstWeaponChasing(){return positionOffsetFirstWeaponChasing;}

    [SerializeField] private Vector3 rotationOffsetFirstWeaponChasing;
    public Vector3 GetRotationOffsetFirstWeaponChasing(){return rotationOffsetFirstWeaponChasing;}

    //second weapon
    [SerializeField] private Vector3 positionOffsetSecondWeaponChasing;
    public Vector3 GetPositionOffsetSecondWeaponChasing(){return positionOffsetSecondWeaponChasing;}

    [SerializeField] private Vector3 rotationOffsetSecondWeaponChasing;
    public Vector3 GetRotationOffsetSecondWeaponChasing(){return rotationOffsetSecondWeaponChasing;}


    
    //armed alert animationclip and offsets
    [SerializeField] private AnimationClip alertWeaponClip;
    public AnimationClip GetAlertWeaponClip(){return alertWeaponClip;}
    //first weapon
    [SerializeField] private Vector3 positionOffsetFirstWeaponAlert;
    public Vector3 GetPositionOffsetFirstWeaponAlert(){return positionOffsetFirstWeaponAlert;}

    [SerializeField] private Vector3 rotationOffsetFirstWeaponAlert;
    public Vector3 GetRotationOffsetFirstWeaponAlert(){return rotationOffsetFirstWeaponAlert;}

    //second weapon
    [SerializeField] private Vector3 positionOffsetSecondWeaponAlert;
    public Vector3 GetPositionOffsetSecondWeaponAlert(){return positionOffsetSecondWeaponAlert;}

    [SerializeField] private Vector3 rotationOffsetSecondWeaponAlert;
    public Vector3 GetRotationOffsetSecondWeaponAlert(){return rotationOffsetSecondWeaponAlert;}
}
