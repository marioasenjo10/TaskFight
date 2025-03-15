using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public GameObject mainScreenPanel;
    public GameObject secondScreenPanel;

    public void ShowMainScreen()
    {
        mainScreenPanel.SetActive(true);
        secondScreenPanel.SetActive(false);
    }

    public void ShowSecondScreen()
    {
        mainScreenPanel.SetActive(false);
        secondScreenPanel.SetActive(true);
    }
}