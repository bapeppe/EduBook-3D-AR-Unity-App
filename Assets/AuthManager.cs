using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

// --- QUESTA È LA PARTE CHE TI MANCAVA ---
[System.Serializable]
public class LoginDTO
{
    public string email;
    public string password;
}
// ----------------------------------------

public class AuthManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI errorText;
    public Button loginButton;

    [Header("Network Settings")]
    // Qui incollerai il link di Ngrok dall'Inspector (senza toccare il codice)
    public string serverUrl = "https://laurice-pseudoenthusiastic-palmer.ngrok-free.dev"; 
    public string loginEndpoint = "/api/auth/login";

    public void OnLoginPressed()
    {
        StartCoroutine(LoginRoutine());
    }

    IEnumerator LoginRoutine()
    {
        loginButton.interactable = false;
        
        // Reset messaggio errore
        if(errorText != null) errorText.gameObject.SetActive(false);

        // Costruzione URL
        string fullUrl = serverUrl + loginEndpoint;
        // Rimuovi eventuali doppi slash se presenti per errore
        if (serverUrl.EndsWith("/") && loginEndpoint.StartsWith("/"))
            fullUrl = serverUrl + loginEndpoint.Substring(1);

        // Creazione JSON usando la classe LoginDTO
        LoginDTO userData = new LoginDTO();
        userData.email = emailField.text;
        userData.password = passwordField.text;

        string jsonBody = JsonUtility.ToJson(userData);

        Debug.Log($"Tentativo Login su: {fullUrl}");
        Debug.Log($"Body inviato: {jsonBody}");

        // Preparazione richiesta
        var request = new UnityWebRequest(fullUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Headers essenziali
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        // Invio
        yield return request.SendWebRequest();

        loginButton.interactable = true;

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login Riuscito! Risposta: " + request.downloadHandler.text);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError($"Errore Login: {request.error} (Codice: {request.responseCode})");
            Debug.LogError($"Risposta Server: {request.downloadHandler.text}");
            
            if(errorText != null)
            {
                if(request.responseCode == 401)
                    errorText.text = "Email o Password errati!";
                else
                    errorText.text = "Errore di connessione (Controlla Ngrok)";
                
                errorText.gameObject.SetActive(true);
            }
        }
    }
}