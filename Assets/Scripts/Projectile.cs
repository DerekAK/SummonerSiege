using UnityEngine;

public class Projectile : MonoBehaviour
{
    private void OnCollisionEnter(Collision other){
        if (other.collider.CompareTag("Player")){
            Debug.Log("HIT PLAYER WITH BOULDER!");
            Rigidbody rbPlayer = other.collider.GetComponent<Rigidbody>();
            Vector3 direction = (other.transform.position - transform.position).normalized;
            Vector3 forceDir = new Vector3(direction.x, 0.1f, direction.z);
            //rbPlayer.AddForce(forceDir*50f, ForceMode.Impulse);
        }
    }
}
