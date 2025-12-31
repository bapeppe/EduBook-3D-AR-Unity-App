using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoutManager : MonoBehaviour
{
    public void DoLogout()
    {
        // 1. Cancella la memoria
        PlayerPrefs.DeleteKey("IsLoggedIn");
        // PlayerPrefs.DeleteAll(); // Usa questo se vuoi cancellare PROPRIO TUTTO (impostazioni, ecc)
        
        // 2. Torna alla scena di Login
        SceneManager.LoadScene("LoginScene"); // Assicurati che si chiami così la tua scena iniziale
    }
}