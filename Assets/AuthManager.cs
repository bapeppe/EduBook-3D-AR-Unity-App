using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class AuthManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI errorText;
    public Button loginButton;

    [Header("Settings")]
    // IMPORTANTE: Se usi il simulatore PC usa "127.0.0.1".
    // Se fai la build sul telefono, devi mettere l'IP LOCALE del tuo PC (es. 192.168.1.X)
    public string serverIP = "127.0.0.1"; 
    public string serverPort = "8090";

    public void OnLoginPressed()
    {
        StartCoroutine(LoginRoutine());
    }

   IEnumerator LoginRoutine()
    {
        loginButton.interactable = false;
        
        // 1. RESET: Appena clicco, nascondo eventuali errori vecchi
        // Accedo al .gameObject per spegnere proprio l'oggetto nella scena
        if(errorText != null) 
            errorText.gameObject.SetActive(false); // <--- SI SPEGNE

        string url = $"http://{serverIP}:{serverPort}/api/collections/users/auth-with-password";
        string jsonBody = $"{{\"identity\":\"{emailField.text}\", \"password\":\"{passwordField.text}\"}}";

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        loginButton.interactable = true;

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login Riuscito!");
            // Se va bene, non mostriamo nulla e cambiamo scena
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError("Errore Login: " + request.error);
            
            // 2. ERRORE: Solo adesso accendiamo il testo
            if(errorText != null)
            {
                errorText.text = "Email o Password errati!";
                errorText.gameObject.SetActive(true); // <--- SI ACCENDE ORA
            }
        }
    }
}