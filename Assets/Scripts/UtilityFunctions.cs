using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class UtilityFunctions{
    
    public static void ThrowWithRigidbody(GameObject obj, Vector3 start, Vector3 end, float speed, float arcHeight){
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        rb.useGravity = true;
        Vector3 direction = end - start;
        Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z);
        float horizontalDistance = horizontalDirection.magnitude;
        float verticalDistance = direction.y;
        float timeToTarget = horizontalDistance / speed;
        float horizontalSpeed = horizontalDistance / timeToTarget;
        float verticalSpeed = (verticalDistance / timeToTarget) + (0.5f * Mathf.Abs(Physics.gravity.y) * timeToTarget);
        Vector3 velocity = horizontalDirection.normalized * horizontalSpeed;
        velocity.y = verticalSpeed;
        rb.linearVelocity = velocity;
    }

    public static void SetParentOfTransform(Transform childTransform, Transform parentTransform, Vector3 positionOffset, Vector3 rotationOffset){
        childTransform.SetParent(parentTransform, true);
        childTransform.localPosition = positionOffset;
        childTransform.localEulerAngles = rotationOffset;
        childTransform.localScale *= parentTransform.root.localScale.x;
    }

    public static IEnumerator SmoothMoveBetweenTransforms(Transform item, Vector3 targetPosition, Vector3 targetRotation, float transitionSpeed){
        Vector3 initialPosition = item.localPosition;
        Quaternion initialRotation = item.localRotation; // Use Quaternion
        Quaternion targetQuaternion = Quaternion.Euler(targetRotation);
        float distance = Vector3.Distance(initialPosition, targetPosition); // Calculate distance to target
        float duration = distance / transitionSpeed; // Adjust for smooth transition
        float elapsedTime = 0f;
        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            item.localPosition = Vector3.Lerp(initialPosition, targetPosition, t);
            item.localRotation = Quaternion.Lerp(initialRotation, targetQuaternion, t); // Use Quaternion.Lerp
            yield return null; // Wait until the next frame
        }
        item.localPosition = targetPosition;
        item.localRotation = targetQuaternion;
    }

    public static IEnumerator SmoothMoveBetweenTransformsChangeParent(Transform item, Transform newParent, Vector3 targetPosition, Vector3 targetRotation, float transitionSpeed){
        item.SetParent(newParent, worldPositionStays: true);
        Vector3 initialPosition = item.localPosition;
        Quaternion initialRotation = item.localRotation; // Use Quaternion
        Quaternion targetQuaternion = Quaternion.Euler(targetRotation); // Convert targetRotation to Quaternion
        float distance = Vector3.Distance(initialPosition, targetPosition); // Calculate distance to target
        float duration = distance / transitionSpeed; // Adjust for smooth transition
        float elapsedTime = 0f;
        while (elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            item.localPosition = Vector3.Lerp(initialPosition, targetPosition, t);
            item.localRotation = Quaternion.Lerp(initialRotation, targetQuaternion, t); // Use Quaternion.Lerp
            yield return null; // Wait until the next frame
        }
        item.localPosition = targetPosition;
        item.localRotation = targetQuaternion;
    }

    public static IEnumerator LookAtCoroutine(Transform rotatingTransform, Transform targetTransform, System.Func<bool> stopCondition){
        while(!stopCondition()){
            Vector3 targetPosition = new Vector3(targetTransform.position.x, rotatingTransform.position.y, targetTransform.position.z);
            rotatingTransform.LookAt(targetPosition);
            yield return null;
        }
    }    

    public static IEnumerator MoveTowardsPositionNavMesh(Transform objectTransform, Transform targetTransform, Func<bool> stopCondition, float moveSpeed){
        NavMeshAgent agent = objectTransform.GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.acceleration = moveSpeed * 2f;
        agent.angularSpeed = Mathf.Max(120f, moveSpeed * 5f); // Adjust rotation speed
        while (!stopCondition()){
            Debug.Log("HEREHERE");
            agent.SetDestination(targetTransform.position);
            yield return null;
        }
        agent.ResetPath();
    }

    public static IEnumerator MoveTowardsPositionSimulated(Transform objectTransform, Vector3 targetPosition, float timeToGetThere, float moveSpeed, float stoppingDistance){
        Rigidbody rb = objectTransform.GetComponent<Rigidbody>();
        float elapsedTime = 0f;
        float targetDistanceSqr = stoppingDistance * stoppingDistance;
        while (elapsedTime < timeToGetThere){
            elapsedTime += Time.deltaTime;
            Vector3 direction = (targetPosition - objectTransform.position).normalized;
            float distanceToTargetSqr = (targetPosition - objectTransform.position).sqrMagnitude;
            if (distanceToTargetSqr <= targetDistanceSqr){break;}
            objectTransform.position += direction * moveSpeed * Time.deltaTime;
            yield return null;
        }
    }

    public static Vector3 GetRandomPositionInDirection(Transform objectTransform, Vector3 directionAwayFromTarget, float angle, float retreatDistance){
        float randomAngle = UnityEngine.Random.Range(-angle, angle); // Adjust within a 45-degree cone
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        Vector3 adjustedDirection = rotation * directionAwayFromTarget;
        Vector3 newPosition = objectTransform.position + adjustedDirection * retreatDistance;
        return newPosition;
    }


    public static Vector3 FindNavMeshPosition(Vector3 position, Vector3 defaultPosition){
        RaycastHit hit; //this is to determine the exact y coordinate of the xz coordinate determined by newpos
        if (Physics.Raycast(new Vector3(position.x, 1000f, position.z), Vector3.down, out hit, Mathf.Infinity)){   
            Debug.DrawRay(new Vector3(position.x, 1000f, position.z), Vector3.down, Color.red, 3f);
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 1000f, NavMesh.AllAreas)){
                return navHit.position;
            } // Return the valid NavMesh position
        }
        Debug.Log("RETURNING DEFAULT POSITION!");
        return defaultPosition;
    }

    public static Vector3 FindNavMeshPosition(Vector3 position, NavMeshAgent agent)
    {
        if (Physics.Raycast(new Vector3(position.x, 1000f, position.z), Vector3.down, out RaycastHit hit, Mathf.Infinity))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1000f, NavMesh.AllAreas))
            {
                // If this works, draw a green line to visualize the successful point
                Debug.DrawLine(hit.point, navHit.position, Color.green, 10f);
                return navHit.position;
            }
            else
            {
                // DEBUG: This part is failing. Let's draw a red sphere to see where we are searching from.
                // This sphere will be HUGE, but it confirms the start point.
                Debug.LogError("NavMesh.SamplePosition failed! Searching from: " + hit.point);
            }
        }
        
        Debug.Log("Returning ZERO!");
        return Vector3.zero;
    }
    
    public static IEnumerator MoveWithGravityRigidbody(Rigidbody rigidbody, Vector3 destination, float timeframe)
    {
        Vector3 startPosition = rigidbody.position;
        float elapsedTime = 0f;
        Vector3 horizontalDisplacement = new Vector3(destination.x - startPosition.x, 0, destination.z - startPosition.z);
        Vector3 horizontalVelocity = horizontalDisplacement / timeframe;
        float verticalDisplacement = destination.y - startPosition.y;
        float initialVerticalVelocity = (verticalDisplacement + 0.5f * Physics.gravity.y * timeframe * timeframe) / timeframe;

        while (elapsedTime < timeframe)
        {
            elapsedTime += Time.deltaTime;
            Vector3 newPosition = startPosition + horizontalVelocity * elapsedTime;
            float verticalPosition = startPosition.y + initialVerticalVelocity * elapsedTime + 0.5f * Physics.gravity.y * elapsedTime * elapsedTime;
            newPosition.y = verticalPosition;
            rigidbody.MovePosition(newPosition); // Use MovePosition
            yield return null;
        }
        rigidbody.MovePosition(destination); // Ensure final position is set
    }

    public static IEnumerator MoveRigidBodyWithAnimationCurve(Rigidbody rigidbody, Vector3 destination, AnimationCurve positionCurve, float duration){
        Vector3 startPosition = rigidbody.position; // Starting position of the Rigidbody

        float elapsedTime = 0;
        while(elapsedTime < duration){
            float t = elapsedTime/duration;
            Vector3 newPosition = Vector3.Lerp(startPosition, destination, t);
            rigidbody.MovePosition(newPosition);
            Debug.Log("Rigidbody position: " + rigidbody.position);
            Debug.Log("Transform position: " + rigidbody.gameObject.transform.position);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the Rigidbody reaches the destination
        rigidbody.MovePosition(destination);
    }
}