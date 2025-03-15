using UnityEngine;
using UnityEngine.UI;

public class HomeScreenManager : MonoBehaviour
{
    public Button logoutButton;

    void Start()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(() =>
            {
                if (AuthManager.Instance != null)
                {
                    AuthManager.Instance.SignOut();
                }
                else
                {
                    Debug.LogError("❌ AuthManager no encontrado.");
                }
            });
        }
        else
        {
            Debug.LogError("❌ Botón de Logout no asignado.");
        }
    }
}
