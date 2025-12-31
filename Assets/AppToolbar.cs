using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems; // FONDAMENTALE PER LA TORCIA
using TMPro;

public class AppToolbar : MonoBehaviour
{
    [Header("Riferimenti UI Gruppi")]
    public GameObject groupScanMode; 
    public GameObject groupViewMode; 

    [Header("Riferimenti Pulsanti")]
    public Button btnTorch;
    public TextMeshProUGUI txtPlayPause; 

    [Header("Logica AR")]
    public ARCameraManager arCameraManager; 
    public AWSLoader awsLoader;             
    public Camera arCamera;                 

    private bool isTorchOn = false;
    private bool isRotationPaused = false;
    
    // VARIABILI ZOOM (Usiamo la matrice per aggirare il blocco AR)
    private float defaultFov = 60f;     
    private float currentFov = 60f;     
    private float minFov = 20f; 
    private float maxFov = 60f; 

    void Start()
    {
        if (arCamera != null) 
        {
            defaultFov = arCamera.fieldOfView;
            if(defaultFov == 0) defaultFov = 60f;
            currentFov = defaultFov;
            maxFov = defaultFov;
        }
        SwitchToScanMode();
    }

    // --- ZOOM OTTICO DIGITALE (Funziona sovrascrivendo la matrice) ---
    void LateUpdate()
    {
        if (arCamera == null) return;

        // Questo trucco forza lo zoom anche se AR Foundation cerca di bloccarlo
        float aspect = arCamera.aspect;
        Matrix4x4 projectionMatrix = Matrix4x4.Perspective(currentFov, aspect, arCamera.nearClipPlane, arCamera.farClipPlane);
        arCamera.projectionMatrix = projectionMatrix;
    }

    public void SwitchToScanMode()
    {
        if(groupScanMode != null) groupScanMode.SetActive(true);
        if(groupViewMode != null) groupViewMode.SetActive(false);
    }

    public void SwitchToViewMode()
    {
        if(groupScanMode != null) groupScanMode.SetActive(false);
        if(groupViewMode != null) groupViewMode.SetActive(true);
        currentFov = defaultFov; // Reset Zoom quando appare il modello
        isRotationPaused = false;
        UpdatePlayButtonText();
    }

    // --- TORCIA (Versione che funzionava) ---
    public void ToggleTorch()
    {
        isTorchOn = !isTorchOn;
        if (arCameraManager != null && arCameraManager.subsystem != null)
        {
            // Usiamo il metodo moderno che ti funzionava prima
            arCameraManager.subsystem.requestedCameraTorchMode = isTorchOn ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
        }
    }

    // --- PULSANTI ZOOM ---
    public void ZoomIn()
    {
        currentFov = Mathf.Max(currentFov - 5f, minFov);
    }

    public void ZoomOut()
    {
        currentFov = Mathf.Min(currentFov + 5f, maxFov);
    }

    public void TogglePlayPause()
    {
        isRotationPaused = !isRotationPaused;
        if(awsLoader != null) awsLoader.SetRotationPaused(isRotationPaused);
        UpdatePlayButtonText();
    }

    public void TriggerReset()
    {
        if(awsLoader != null) awsLoader.DestroyModel();
        SwitchToScanMode();
        currentFov = defaultFov; 
    }

    void UpdatePlayButtonText()
    {
        if(txtPlayPause != null) txtPlayPause.text = isRotationPaused ? "Play" : "Pausa";
    }
}