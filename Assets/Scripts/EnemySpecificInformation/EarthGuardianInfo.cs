public class EarthGuardianInfo : EnemySpecificInfo
{
    private void Awake(){
        _rightHandTransform = transform.Find("RiggedEarthGuardian/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/mixamorig:RightHandIndex1/mixamorig:RightHandIndex2/mixamorig:RightHandIndex3");
    }
    
}
