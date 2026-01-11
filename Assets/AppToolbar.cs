using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems; 
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
    
    // --- NUOVO: Riferimento allo Scanner per riavviarlo ---
    public ARQRScanner qrScanner; 

    private bool isTorchOn = false;
    private bool isRotationPaused = false;
    
    // VARIABILI ZOOM (Metodo Matrice - Sicuro per la Build)
    private float defaultFov = 60f;     
    private float currentFov = 60f;     
    private float minFov = 25f; 
    private float maxFov = 60f; 

    void Start()
    {
        if (arCamera != null) 
        {
            defaultFov = arCamera.fieldOfView;
            // Protezione se Unity parte con valori strani
            if(defaultFov <= 10 || defaultFov >= 120) defaultFov = 60f;
            
            currentFov = defaultFov;
            maxFov = defaultFov;
        }
        SwitchToScanMode();
    }

    // Questo LateUpdate è il segreto per lo zoom funzionante senza plugin nativi
    void LateUpdate()
    {
        if (groupScanMode.activeSelf && arCamera != null)
        {
            if (Mathf.Abs(currentFov - defaultFov) > 0.1f)
            {
                float aspect = arCamera.aspect;
                Matrix4x4 projectionMatrix = Matrix4x4.Perspective(currentFov, aspect, arCamera.nearClipPlane, arCamera.farClipPlane);
                arCamera.projectionMatrix = projectionMatrix;
            }
            else
            {
                arCamera.ResetProjectionMatrix();
            }
        }
    }

    public void SwitchToScanMode()
    {
        if(groupScanMode != null) groupScanMode.SetActive(true);
        if(groupViewMode != null) groupViewMode.SetActive(false);
        
        // Reset Zoom Camera
        currentFov = defaultFov;
        if(arCamera != null) arCamera.ResetProjectionMatrix();
    }

    public void SwitchToViewMode()
    {
        if(groupScanMode != null) groupScanMode.SetActive(false);
        if(groupViewMode != null) groupViewMode.SetActive(true);
        
        // Reset Zoom Camera immediato quando appare il modello
        currentFov = defaultFov; 
        if(arCamera != null) arCamera.ResetProjectionMatrix();

        isRotationPaused = false;
        UpdatePlayButtonText();
    }

    public void ToggleTorch()
    {
        isTorchOn = !isTorchOn;
        if (arCameraManager != null && arCameraManager.subsystem != null)
        {
            arCameraManager.subsystem.requestedCameraTorchMode = isTorchOn ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
        }
    }

    public void ZoomIn()
    {
        // Riduci FOV = Zoom In
        currentFov = Mathf.Max(currentFov - 5f, minFov);
    }

    public void ZoomOut()
    {
        currentFov = Mathf.Min(currentFov + 5f, maxFov);
    }

    // Zoom del MODELLO (chiamato dai tasti nella View Mode)
    public void ModelZoomIn()
    {
        if(awsLoader != null) awsLoader.ChangeScale(1.1f);
    }

    public void ModelZoomOut()
    {
        if(awsLoader != null) awsLoader.ChangeScale(0.9f);
    }

    public void TogglePlayPause()
    {
        isRotationPaused = !isRotationPaused;
        if(awsLoader != null) awsLoader.SetRotationPaused(isRotationPaused);
        UpdatePlayButtonText();
    }

    // --- RESET CORRETTO ---
    public void TriggerReset()
    {
        // 1. Spegni Torcia
        if (isTorchOn) ToggleTorch();

        // 2. Resetta Zoom Camera
        currentFov = defaultFov;
        if(arCamera != null) arCamera.ResetProjectionMatrix();

        // 3. Avvisa lo Scanner di ripartire (questo resetterà anche il loader)
        if (qrScanner != null)
        {
            qrScanner.RestartExperience();
        }
        else
        {
            // Fallback di sicurezza
            if(awsLoader != null) awsLoader.DestroyModel();
        }
        
        // 4. Torna alla UI di scansione
        SwitchToScanMode();
    }

    void UpdatePlayButtonText()
    {
        if(txtPlayPause != null) txtPlayPause.text = isRotationPaused ? "Play" : "Pausa";
    }
}