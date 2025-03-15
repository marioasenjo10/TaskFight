using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using Firebase;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using Firebase.Database;
using Firebase.Extensions;

public class LeagueManager : MonoBehaviour
{
    public GameObject leagueButtonPrefab;
    public Transform contentPanel;
    public Button addLeagueButton;
    public Button joinLeagueButton;
    public TMP_InputField leagueIDInputField;
    public ScreenManager screenManager;

    private List<League> leagues = new List<League>();
    private string savePath;
    private DatabaseReference reference;
    private FirebaseAuth auth;
    private FirebaseUser user;

    void Start()
    {
        savePath = Application.dataPath + "/Data/leagues.json";

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                user = auth.CurrentUser;
                reference = FirebaseDatabase.GetInstance(app, "https://taskfight-c4a80-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
                Debug.Log("✅ Firebase Database URL configurada correctamente.");
                LoadLeagues();
                addLeagueButton.onClick.AddListener(AddNewLeague);
                joinLeagueButton.onClick.AddListener(() => JoinExistingLeague(leagueIDInputField.text));
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
                // Load from local file if Firebase initialization fails
                LoadLeaguesFromLocal();
            }
        });
    }

    void LoadLeagues()
    {
        Debug.Log("Cargando ligas...");
        reference.Child("leagues").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                leagues.Clear(); // Limpiar lista antes de cargar

                DataSnapshot snapshot = task.Result;
                foreach (DataSnapshot leagueSnapshot in snapshot.Children)
                {
                    League league = JsonUtility.FromJson<League>(leagueSnapshot.GetRawJsonValue());
                    leagues.Add(league);
                }

                Debug.Log("✅ Leagues loaded successfully. Count: " + leagues.Count);
                UpdateLeagueButtons();
            }
            else
            {
                Debug.LogError("❌ Failed to load leagues: " + task.Exception);
            }
        });
    }

    void LoadLeaguesFromLocal()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("El archivo leagues.json está vacío. Se creará uno nuevo.");
                leagues = new List<League>();
                return;
            }

            LeagueList data = JsonUtility.FromJson<LeagueList>(json);
            leagues = data.leagues;
            Debug.Log("Leagues loaded from local file. Count: " + leagues.Count);
            UpdateLeagueButtons();
        }
        else
        {
            Debug.LogWarning("No se encontró el archivo leagues.json. Se iniciará una nueva lista de ligas.");
            leagues = new List<League>();
        }
    }

    void UpdateLeagueButtons()
    {
        // Limpiar los botones existentes
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // Crear botones solo para las ligas en las que el usuario actual es administrador o miembro
        foreach (var league in leagues)
        {
            if (league.admin == user.UserId || league.members.Contains(user.UserId))
            {
                GameObject newButton = Instantiate(leagueButtonPrefab, contentPanel);
                newButton.GetComponentInChildren<TextMeshProUGUI>().text = league.name + " (ID: " + league.id + ")";
                newButton.GetComponent<Button>().onClick.AddListener(() => OpenLeague(league.id));
            }
        }

        // FORZAR ACTUALIZACIÓN DEL LAYOUT
        Canvas.ForceUpdateCanvases();
    }

    public void AddNewLeague()
    {
        string newLeagueID = System.Guid.NewGuid().ToString();
        League newLeague = new League
        {
            id = newLeagueID,
            name = "League " + (leagues.Count + 1),
            admin = user.UserId,
            members = new List<string> { user.UserId }
        };

        // Convertimos la liga a JSON
        string json = JsonUtility.ToJson(newLeague);

        // Guardamos en Firebase en su propio nodo
        reference.Child("leagues").Child(newLeagueID).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("✅ Nueva liga creada: " + newLeague.name);
                leagues.Add(newLeague);
                UpdateLeagueButtons(); // Refrescar UI
            }
            else
            {
                Debug.LogError("❌ Error al guardar la liga: " + task.Exception);
            }
        });
    }

    public void JoinExistingLeague(string leagueID)
    {
        DatabaseReference leagueRef = reference.Child("leagues").Child(leagueID);

        leagueRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                League league = JsonUtility.FromJson<League>(task.Result.GetRawJsonValue());

                if (!league.members.Contains(user.UserId))
                {
                    league.members.Add(user.UserId);
                    
                    // Guardamos solo la lista de miembros sin tocar otros datos
                    leagueRef.Child("members").SetValueAsync(league.members).ContinueWithOnMainThread(saveTask =>
                    {
                        if (saveTask.IsCompleted)
                        {
                            Debug.Log("✅ Unido a la liga: " + league.name);
                            LoadLeagues(); // Recargar la lista de ligas
                            screenManager.ShowMainScreen();
                            leagueIDInputField.text = ""; // Limpiar el campo de texto
                        }
                        else
                        {
                            Debug.LogError("❌ Error al unirse a la liga: " + saveTask.Exception);
                        }
                    });
                }
                else
                {
                    Debug.LogWarning("⚠️ Ya eres miembro de esta liga.");
                }
            }
            else
            {
                Debug.LogError("❌ Liga con ID " + leagueID + " no encontrada.");
            }
        });
    }

    void SaveLeagues()
    {
        LeagueList data = new LeagueList { leagues = leagues };
        string json = JsonUtility.ToJson(data, true);

        Debug.Log("Intentando guardar en Firebase: " + json); // Ver qué se está guardando

        reference.Child("leagues").SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Leagues saved successfully.");
                File.WriteAllText(savePath, json); // También guardamos en local
            }
            else
            {
                Debug.LogError("Failed to save leagues: " + task.Exception);
            }
        });
    }

    void OpenLeague(string leagueID)
    {
        Debug.Log("Abriendo liga con ID: " + leagueID);
        PlayerPrefs.SetString("SelectedLeagueID", leagueID);
        SceneManager.LoadScene("LeagueDetails");
    }

    [System.Serializable]
    public class League
    {
        public string id;
        public string name;
        public string admin;
        public List<string> members;
    }

    [System.Serializable]
    public class LeagueList
    {
        public List<League> leagues;
    }
}