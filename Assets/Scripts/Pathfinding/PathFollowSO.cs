using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/PathFollowSO")]
public class PathFollowSO : ScriptableObject
{
    [Header("Movement")]
    public float movementSpeed;
    public float rotationSpeed;
    public float yValue;
    public float checkDistanceEndDestination;
}
