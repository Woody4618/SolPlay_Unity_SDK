using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XpWidget : MonoBehaviour
{
    public Slider XPSlider;
    public TextMeshProUGUI XpText;
    public TextMeshProUGUI LevelText;

    public static string PlayerPrefsXpKey = "TestXp4";

    public void AnimateXp(int xp, int newXp)
    {
        StartCoroutine(AnimateXpRoutine(xp, newXp));
    }

    private IEnumerator AnimateXpRoutine(int xp, int newXp)
    {
        int shownXp = xp - newXp;
        while (shownXp <= xp)
        {
            SetData(shownXp);
            yield return new WaitForSeconds(0.1f);
            shownXp++;
        }
    }

    public void SetData(int xp)
    {
        int playerLevel = Mathf.FloorToInt(Mathf.Log(xp / 5 + 1, 2));

        var nextLevelXp = GetNextLevelXp(playerLevel);
        var lastLevelXp = GetNextLevelXp(playerLevel - 1);

        XPSlider.minValue = lastLevelXp;
        XPSlider.maxValue = nextLevelXp;
        XPSlider.value = xp;

        XpText.text = $"{xp - lastLevelXp} / {nextLevelXp - lastLevelXp}";
        LevelText.text = (playerLevel + 1).ToString();
    }

    private int GetNextLevelXp(int currentLevel)
    {
        var totalXpNextLevel = 5 * (Mathf.Pow(2, currentLevel + 1) - 1);
        return (int) totalXpNextLevel;
    }
}