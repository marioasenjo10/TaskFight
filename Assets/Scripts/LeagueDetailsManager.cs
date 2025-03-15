using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class LeagueDetailsManager : MonoBehaviour
{
    public TMP_Text leagueNameText;
    public TMP_Text leagueIDText;
    public TMP_Text adminText;
    public Transform membersContentPanel;
    public GameObject memberItemPrefab;

    private DatabaseReference reference;
    private FirebaseAuth auth;
    private FirebaseUser user;
    private string leagueID;

    public static event Action OnMembersLoaded;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        user = auth.CurrentUser;
        leagueID = PlayerPrefs.GetString("SelectedLeagueID");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                reference = FirebaseDatabase.GetInstance(app, "https://taskfight-c4a80-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
                Debug.Log("✅ Firebase Database URL configurada correctamente.");
                LoadLeagueDetails();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
            }
        });
    }

    public Task LoadLeagueDetails()
    {
        var tcs = new TaskCompletionSource<bool>();

        reference.Child("leagues").Child(leagueID).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("Datos de la liga obtenidos de Firebase");
                League league = JsonUtility.FromJson<League>(task.Result.GetRawJsonValue());
                leagueNameText.text = league.name;
                leagueIDText.text = league.id;
                adminText.text = "Admin: " + league.admin;

                foreach (Transform child in membersContentPanel)
                {
                    Destroy(child.gameObject);
                }

                List<string> members = new List<string>();
                foreach (var member in league.members)
                {
                    members.Add(member);
                    GameObject newMemberItem = Instantiate(memberItemPrefab, membersContentPanel);
                    newMemberItem.GetComponentInChildren<TextMeshProUGUI>().text = member;
                }

                LeagueData.SetMembers(members); // Almacenar los miembros en LeagueData
                Debug.Log("ℹ️ Integrantes de la liga seteados correctamente.");

                // FORZAR ACTUALIZACIÓN DEL LAYOUT
                Canvas.ForceUpdateCanvases();

                Debug.Log("Invocando evento OnMembersLoaded...");
                OnMembersLoaded?.Invoke(); // Disparar el evento

                tcs.SetResult(true);
            }
            else
            {
                Debug.LogError("❌ Failed to load league details: " + task.Exception);
                tcs.SetResult(false);
            }
        });

        return tcs.Task;
    }

    public string GetCurrentLeagueId()
    {
        return leagueID;
    }

    [System.Serializable]
    public class League
    {
        public string id;
        public string name;
        public string admin;
        public List<string> members;
    }
}

public static class LeagueData
{
    public static List<string> Members { get; private set; } = new List<string>();

    public static void SetMembers(List<string> members)
    {
        Members = members;
    }
}