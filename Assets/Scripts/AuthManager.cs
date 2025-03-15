using Firebase.Auth;
using Firebase.Extensions;
using Firebase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{

    public static AuthManager Instance { get; private set; } // Singleton
    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;
    public Button signUpButton;
    public Button signInButton;
    public TMP_Text statusText;

    private FirebaseAuth auth;
    private FirebaseUser user;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // No destruir al cambiar de escena
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        auth = FirebaseAuth.DefaultInstance;
    }

    void Start()
    {
        
        if (signUpButton != null) signUpButton.onClick.AddListener(SignUp);
        if (signInButton != null) signInButton.onClick.AddListener(SignIn);

        if (auth.CurrentUser != null)
        {
            Debug.Log("✅ Usuario ya autenticado: " + auth.CurrentUser.Email);
            GoToHomeScreen();
        }
        else
        {
            ShowLoginPage();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LoginPage")
        {
            ReassignUIElements();
        }
    }

    void ReassignUIElements()
    {
        emailInputField = GameObject.Find("EmailInputField").GetComponent<TMP_InputField>();
        passwordInputField = GameObject.Find("PasswordInputField").GetComponent<TMP_InputField>();
        signUpButton = GameObject.Find("SignUpButton").GetComponent<Button>();
        signInButton = GameObject.Find("SignInButton").GetComponent<Button>();
        statusText = GameObject.Find("StatusText").GetComponent<TMP_Text>();

        if (signUpButton != null) signUpButton.onClick.AddListener(SignUp);
        if (signInButton != null) signInButton.onClick.AddListener(SignIn);
    }

    public void SignUp()
    {
        string email = emailInputField.text;
        string password = passwordInputField.text;

        if (password.Length < 6)
        {
            statusText.text = "❌ La contraseña debe tener al menos 6 caracteres.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
            {
                user = task.Result.User;
                statusText.color = Color.green; // Cambiar el color del texto a verde para mensajes de éxito
                statusText.text = "✅ Registro exitoso: " + user.Email;
                SignIn(); // Iniciar sesión automáticamente después de registrarse
            }
            else
            {
                HandleError(task.Exception);
            }
        });
    }

    void SignIn()
    {
        string email = emailInputField.text;
        string password = passwordInputField.text;

        Debug.Log("Login: " + email);

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
            {
                user = task.Result.User;
                statusText.color = Color.green; // Cambiar el color del texto a verde para mensajes de éxito
                statusText.text = "✅ Inicio de sesión exitoso: " + user.Email;
                GoToHomeScreen();
            }
            else
            {
                HandleError(task.Exception);
            }
        });
    }

    public void SignOut()
    {
        auth.SignOut();
        Debug.Log("Usuario desconectado.");
        ShowLoginPage();
    }

    void GoToHomeScreen()
    {
        Debug.Log("Redirigiendo a la pantalla principal...");
        SceneManager.LoadScene("HomeScreen");  // Asegúrate de que esta escena está en Build Settings
    }

    void ShowLoginPage()
    {
        Debug.Log("Mostrando la página de inicio de sesión...");
        SceneManager.LoadScene("LoginPage");  // Asegúrate de que esta escena está en Build Settings
    }

    void HandleError(System.AggregateException exception)
    {
        FirebaseException firebaseEx = exception.InnerExceptions[0] as FirebaseException;
        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

        statusText.color = Color.red; // Cambiar el color del texto a rojo para mensajes de error

        switch (errorCode)
        {
            case AuthError.MissingEmail:
                statusText.text = "❌ Introduce un email.";
                break;
            case AuthError.MissingPassword:
                statusText.text = "❌ Introduce una contraseña.";
                break;
            case AuthError.InvalidEmail:
                statusText.text = "❌ Email inválido.";
                break;
            case AuthError.WeakPassword:
                statusText.text = "❌ La contraseña es demasiado débil.";
                break;
            case AuthError.EmailAlreadyInUse:
                statusText.text = "❌ Este email ya está registrado.";
                break;
            case AuthError.WrongPassword:
                statusText.text = "❌ Contraseña incorrecta.";
                break;
            case AuthError.UserNotFound:
                statusText.text = "❌ No existe una cuenta con este email.";
                break;
            default:
                statusText.text = "❌ Error: " + firebaseEx.Message;
                break;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}