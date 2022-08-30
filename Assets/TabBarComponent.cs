using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class TabBarComponent : MonoBehaviour
{
    public HorizontalScrollSnap HorizontalScrollSnap;
    
    private void Awake()
    {
        int counter = 0;
        foreach (var toggle in GetComponentsInChildren<Button>())
        {
            var counter1 = counter;
            toggle.onClick.AddListener(delegate
            {
                HorizontalScrollSnap.ChangePage(counter1);
            });
            counter++;
        }
    }
}
