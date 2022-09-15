using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputs : MonoBehaviour
{
    private Vector2 _inputs;

    public bool IsJumping => _inputs.y > 0;
    private bool _disableInputs;

    public void EnableInputs(bool value) => _disableInputs = !value;

    public bool TapUp()
    {
        if(_disableInputs)
            return false;

        // Mouse
        if(Input.GetMouseButtonDown(0))
            return true;

        // Touch
        for(int i = 0; i < Input.touchCount; i++)
        {
            if(Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }
}
