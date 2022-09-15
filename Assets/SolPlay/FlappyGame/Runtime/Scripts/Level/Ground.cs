using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Ground : MonoBehaviour
{
    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.GetComponent<PlayerDeathController>() is PlayerDeathController player)
        {
            player.Controller.OnHitGround();
        }
    }
}