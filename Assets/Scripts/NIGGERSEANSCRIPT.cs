using UnityEngine;

public class NIGGERSEANSCRIPT : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform pfFireball;
    [SerializeField] private Transform rightHandTransform;
    private Transform currFireBall;

    private bool fireballPresent;

    private void Start(){
        fireballPresent = false;
    }

    private void Update(){
        Debug.Log("SEAN IS A GIANT FAGGOT!");
        transform.LookAt(targetTransform);
        if(!fireballPresent){
            Instantiate(pfFireball, transform);
            fireballPresent = true;
        }
    }

    private void SpawnFireBall(){
        if(currFireBall){
            Destroy(currFireBall.gameObject);
        }
        currFireBall = Instantiate(pfFireball, rightHandTransform);
        currFireBall.SetParent(null);
        Rigidbody rb = currFireBall.GetComponent<Rigidbody>();
        Vector3 dirForce = targetTransform.position - rightHandTransform.position;
        rb.AddForce(dirForce * 100f, ForceMode.Impulse);
    }
}