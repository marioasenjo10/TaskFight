using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class TaskManager : MonoBehaviour
{
    public GameObject taskPanel;
    public TMP_InputField taskNameInput;
    public TMP_Dropdown assigneeDropdown;
    public Toggle setDueDateToggle;
    public TMP_InputField dueDateInput;
    public TMP_Dropdown effortPointsDropdown;
    public Button saveTaskButton;
    public Button cancelTaskButton;
    public Button newTaskButton; // Añadir referencia al botón "New Task"
    public Button deleteTaskButton; // Añadir referencia al botón "Delete Task"

    public Transform taskListContainer;
    public GameObject taskPrefab;
    public ScreenManager screenManager; // Añadir referencia a ScreenManager

    public TMP_Text dateLabel; // Nueva referencia a un solo Label
    public TMP_Text taskTitleLabel;
    public TMP_Text leagueIDLabel;

    // Referencias al pop-up de confirmación
    public GameObject confirmDeletePanel;
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;

    private FirebaseAuth auth;
    private FirebaseUser user;
    private DatabaseReference reference;
    private TaskData currentTask = null; // Variable para saber si estamos editando o creando una tarea nueva

    private string currentLeagueId; // Añadir una variable para almacenar el ID de la liga actual

    async void Start()
    {
        LeagueDetailsManager.OnMembersLoaded += OnMembersLoaded; // Suscribirse al evento

        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                user = auth.CurrentUser;
                reference = FirebaseDatabase.GetInstance(app, "https://taskfight-c4a80-default-rtdb.europe-west1.firebasedatabase.app/").RootReference.Child("tasks");

                saveTaskButton.onClick.AddListener(SaveTask);
                cancelTaskButton.onClick.AddListener(CloseTaskPanel);
                setDueDateToggle.onValueChanged.AddListener(ToggleDueDateInput);
                newTaskButton.onClick.AddListener(() => OpenTaskPanel(null)); // Añadir listener al botón "New Task"
                deleteTaskButton.onClick.AddListener(ShowConfirmDeletePanel); // Añadir listener al botón "Delete Task"

                confirmDeleteButton.onClick.AddListener(() => DeleteTask(currentTask.id)); // Añadir listener al botón de confirmar eliminación
                cancelDeleteButton.onClick.AddListener(HideConfirmDeletePanel); // Añadir listener al botón de cancelar eliminación

                InitializeEffortPointsDropdown();

                Debug.Log("EffortPointsDropdown inicializado");

                // Esperar a que los datos de los miembros de la liga se hayan cargado
                Debug.Log("Buscando LeagueDetailsManager...");
                var leagueDetailsManager = FindFirstObjectByType<LeagueDetailsManager>();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
            }
        });

        // Asegurarse de que el Toggle esté desactivado por defecto
        setDueDateToggle.isOn = false;
        ToggleDueDateInput(false);

        // Asegurarse de que el panel de confirmación de eliminación esté oculto por defecto
        confirmDeletePanel.SetActive(false);
    }

    void OnDestroy()
    {
        LeagueDetailsManager.OnMembersLoaded -= OnMembersLoaded; // Desuscribirse del evento
    }

    private void OnMembersLoaded()
    {
        LoadAssignees();
        Debug.Log("Llamando a LoadTasks desde OnMembersLoaded");
        LoadTasks();
    }

    public void InitializeEffortPointsDropdown()
    {
        Debug.Log("Inicializando el Dropdown de effortPoints");
        effortPointsDropdown.ClearOptions();
        List<string> options = new List<string> { "Very Low", "Low", "Medium", "Hard", "Extreme" };
        effortPointsDropdown.AddOptions(options);
        Debug.Log("Opciones añadidas al Dropdown de effortPoints");
    }

    public void LoadAssignees()
    {
        Debug.Log("Cargando los integrantes de la liga");
        List<string> assignees = LeagueData.Members;

        Debug.Log("LeaguesData.Members: " + string.Join(", ", assignees));

        assigneeDropdown.ClearOptions();
        assigneeDropdown.AddOptions(assignees);
        Debug.Log("Integrantes de la liga añadidos al Dropdown de assignees");
    }

    void ToggleDueDateInput(bool isOn)
    {        
        dateLabel.text = isOn ? "BEFORE" : "WHEN";
    }

    public void OpenTaskPanel(TaskData task = null)
    {
        currentTask = task; // Guardamos la tarea si es una edición

        if (task == null)
        {
            // Nueva tarea: limpiar inputs
            taskTitleLabel.text = "NEW TASK";
            taskNameInput.text = "";
            assigneeDropdown.value = 0;
            setDueDateToggle.isOn = false;
            dueDateInput.text = "";
            effortPointsDropdown.value = 0;
        }
        else
        {
            // Editar tarea: llenar inputs
            taskTitleLabel.text = "EDIT TASK";
            taskNameInput.text = task.name;
            assigneeDropdown.value = GetAssigneeIndex(task.assignee);

            setDueDateToggle.isOn = !string.IsNullOrEmpty(task.dueDate);

            if(setDueDateToggle.isOn){
                dueDateInput.text = task.dueDate; // Asegurar que el campo no se borre
            } else {
                dueDateInput.text = task.exactDate; // Asegurar que el campo no se borre
            }

            effortPointsDropdown.value = task.effortPoints - 1;
        }

        taskPanel.SetActive(true);
    }

    void UpdateDueDateInput()
    {
        dueDateInput.text = currentTask.dueDate;
    }

    public void SaveTask()
    {
        string name = taskNameInput.text;
        string assignee = assigneeDropdown.options[assigneeDropdown.value].text;
        string dueDate = setDueDateToggle.isOn ? dueDateInput.text : "";
        string exactDate = setDueDateToggle.isOn ? "" : dueDateInput.text;
        int effortPoints = effortPointsDropdown.value + 1;
        currentLeagueId = leagueIDLabel.text;

        Debug.Log("League ID in SaveTask: " + currentLeagueId);

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("El nombre de la tarea no puede estar vacío.");
            return;
        }

        if (currentTask == null)
        {
            // Crear nueva tarea
            string taskId = Guid.NewGuid().ToString();
            TaskData newTask = new TaskData(taskId, name, assignee, dueDate, effortPoints, exactDate, currentLeagueId);
            reference.Child(taskId).SetRawJsonValueAsync(JsonUtility.ToJson(newTask));
        }
        else
        {
            // Editar tarea existente
            currentTask.name = name;
            currentTask.assignee = assignee;
            currentTask.dueDate = dueDate;
            currentTask.effortPoints = effortPoints;
            currentTask.exactDate = exactDate;
            currentTask.leagueId = currentLeagueId;
            reference.Child(currentTask.id).SetRawJsonValueAsync(JsonUtility.ToJson(currentTask));
        }
        
        CloseTaskPanel();
        screenManager.ShowMainScreen();
        Debug.Log("Llamando a LoadTasks desde SaveTask");
        LoadTasks();
    }

    public void DeleteTask(string taskId)
    {
        reference.Child(taskId).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Tarea eliminada de Firebase: " + taskId);
                // Eliminar la tarea de la interfaz de usuario
                Transform taskToDelete = null;
                foreach (Transform child in taskListContainer)
                {
                    var textComponent = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null && textComponent.text.Contains(taskId))
                    {
                        taskToDelete = child;
                        break;
                    }
                }
                if (taskToDelete != null)
                {
                    Destroy(taskToDelete.gameObject);
                    Debug.Log("Tarea eliminada de la interfaz de usuario: " + taskId);
                }
                else
                {
                    Debug.LogError("No se encontró la tarea en la interfaz de usuario: " + taskId);
                }
                HideConfirmDeletePanel(); // Ocultar el panel de confirmación de eliminación
                CloseTaskPanel(); // Ocultar el panel de la tarea
                LoadTasks(); // Recargar las tareas
            }
            else
            {
                Debug.LogError("Error al eliminar la tarea de Firebase: " + task.Exception);
            }
        });
    }

    void ShowConfirmDeletePanel()
    {
        confirmDeletePanel.SetActive(true);
    }

    void HideConfirmDeletePanel()
    {
        confirmDeletePanel.SetActive(false);
    }

    void CloseTaskPanel()
    {
        taskPanel.SetActive(false);
        currentTask = null;
    }

    void LoadTasks()
    {
        Debug.Log("Cargando tareas...");
        foreach (Transform child in taskListContainer)
        {
            if (child.gameObject.name != "TasksLabelTitle") // Asegúrate de que el título tenga este nombre en Unity
            {
                Destroy(child.gameObject);
            }
        }
        
        reference.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                if(currentLeagueId == null || currentLeagueId == "")
                {
                    currentLeagueId = leagueIDLabel.text;
                }
                DataSnapshot snapshot = task.Result;
                Debug.Log("Tareas obtenidas: " + snapshot.ChildrenCount + ". League ID: " + leagueIDLabel.text);
                foreach (var child in snapshot.Children)
                {
                    TaskData taskData = JsonUtility.FromJson<TaskData>(child.GetRawJsonValue());
                    if (taskData.leagueId == currentLeagueId) // Filtrar por leagueId
                    {
                        Debug.Log("Tarea cargada: " + taskData.name);
                        GameObject taskItem = Instantiate(taskPrefab, taskListContainer);
                        taskItem.GetComponentInChildren<TextMeshProUGUI>().text = taskData.name;
                        taskItem.GetComponent<Button>().onClick.AddListener(() => OpenTaskPanel(taskData));
                    }
                }
            }
            else
            {
                Debug.LogError("Error al cargar tareas: " + task.Exception);
            }
        });
    }

    int GetAssigneeIndex(string assignee)
    {
        for (int i = 0; i < assigneeDropdown.options.Count; i++)
        {
            if (assigneeDropdown.options[i].text == assignee)
                return i;
        }
        return 0;
    }

    [System.Serializable]
    public class TaskData
    {
        public string id;
        public string name;
        public string assignee;
        public string dueDate;
        public int effortPoints;
        public string exactDate; // Nuevo campo
        public string leagueId; // Nuevo campo

        public TaskData(string id, string name, string assignee, string dueDate, int effortPoints, string exactDate, string leagueId)
        {
            this.id = id;
            this.name = name;
            this.assignee = assignee;
            this.dueDate = dueDate;
            this.effortPoints = effortPoints;
            this.exactDate = exactDate;
            this.leagueId = leagueId;
        }
    }
}