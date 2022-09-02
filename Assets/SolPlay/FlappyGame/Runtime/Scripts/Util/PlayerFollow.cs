using UnityEngine;

[ExecuteAlways]
public class PlayerFollow : MonoBehaviour
{
    [SerializeField] float _xOffset;
    [SerializeField] Transform _objectToFollow;

    private void Start() 
    {
        if(_objectToFollow == null)
            enabled = false;    
    }

    void LateUpdate()
    {
        Vector3 target = transform.position;
        target.x = _objectToFollow.position.x + _xOffset;
        transform.position = target;
    }
}
