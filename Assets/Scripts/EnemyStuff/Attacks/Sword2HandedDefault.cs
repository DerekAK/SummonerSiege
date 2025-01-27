public class Sword2HandedDefault : BaseAttackScript{   
    private bool endRotate;
    
    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){ 
        base.ExecuteAttack(sender, e);
        endRotate = false;
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += EndRotate;
        StartCoroutine(UtilityFunctions.LookAtCoroutine(_enemyGameObject.transform, e.TargetTransform, ()=>endRotate));
    }
    protected void EndRotate(object sender, EnemyAI4.AttackEvent e){ 
        _enemyScript.AnimationAttackEvent -= EndRotate;
        endRotate = true;
    }
}
