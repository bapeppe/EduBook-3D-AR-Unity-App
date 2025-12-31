using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using UnityEngine.SceneManagement; // Importante per cambiare scena

[System.Serializable]
public class LoginDTO
{
    public string email;
    public string password;
}

public class AuthManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI errorText;
    public Button loginButton;

    [Header("Network Settings")]
    // Ricordati di aggiornare questo link se riavvii Ngrok!
    public string serverUrl = "https://tuo-link-ngrok-qui.ngrok-free.dev"; 
    public string loginEndpoint = "/api/auth/login";

    void Start()
    {
        // --- CONTROLLO LOGIN AUTOMATICO ---
        // Controlliamo se nella memoria del telefono c'è salvato che siamo già entrati
        if (PlayerPrefs.HasKey("IsLoggedIn") && PlayerPrefs.GetInt("IsLoggedIn") == 1)
        {
            Debug.Log("Utente già loggato. Salto al Menu.");
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void OnLoginPressed()
    {
        StartCoroutine(LoginRoutine());
    }

    IEnumerator LoginRoutine()
    {
        if(loginButton != null) loginButton.interactable = false;
        if(errorText != null) errorText.gameObject.SetActive(false);

        // Pulizia URL
        string fullUrl = serverUrl + loginEndpoint;
        if (serverUrl.EndsWith("/") && loginEndpoint.StartsWith("/"))
            fullUrl = serverUrl + loginEndpoint.Substring(1);

        // Creazione JSON
        LoginDTO userData = new LoginDTO();
        userData.email = emailField.text;
        userData.password = passwordField.text;

        string jsonBody = JsonUtility.ToJson(userData);

        var request = new UnityWebRequest(fullUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        if(loginButton != null) loginButton.interactable = true;

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login Riuscito!");

            // --- SALVATAGGIO STATO LOGIN ---
            // Salviamo "1" nella memoria del telefono per dire "Siamo dentro"
            PlayerPrefs.SetInt("IsLoggedIn", 1);
            
            // Opzionale: Se il server ti manda un token, potresti salvare anche quello
            // PlayerPrefs.SetString("AuthToken", "token_ricevuto...");
            
            PlayerPrefs.Save(); // Conferma il salvataggio

            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError($"Errore Login: {request.error}");
            
            if(errorText != null)
            {
                if(request.responseCode == 401)
                    errorText.text = "Email o Password errati!";
                else
                    errorText.text = "Errore di connessione.";
                
                errorText.gameObject.SetActive(true);
            }
        }
    }
}