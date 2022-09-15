using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/MedalParameters")]
public class MedalParameters : ScriptableObject
{
    [field: SerializeField] public List<Medal> Medals { get; private set; }
    private void OnValidate() => Medals.Sort();
}
