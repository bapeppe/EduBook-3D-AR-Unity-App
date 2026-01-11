using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using TMPro;

public class ARQRScanner : MonoBehaviour
{
    [Header("Componenti")]
    public ARCameraManager cameraManager;
    public ARRaycastManager raycastManager;
    public AWSLoader loaderScript; // Riferimento al cervello che ha il "semaforo"
    public TextMeshProUGUI statusText;

    private bool isScanning = false; 
    private MultiFormatReader reader = new MultiFormatReader();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    IEnumerator Start()
    {
        if(statusText != null) statusText.text = "Avvio fotocamera...";
        
        // Aspetta 2 secondi reali prima di attivare il cervello
        yield return new WaitForSeconds(2.0f);
        
        isScanning = true;
        if(statusText != null) statusText.text = "Inquadra un QR Code...";
        
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDestroy()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // --- MODIFICA 1: IL BLOCCO PRINCIPALE ---
        // Se la variabile locale è spenta OPPURE il Loader ha già un modello caricato...
        // ...FERMATI SUBITO. Non sprecare risorse a analizzare l'immagine.
        if (!isScanning || (loaderScript != null && loaderScript.IsModelLoaded)) return;

        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        StartCoroutine(ProcessImage(image));
    }

    IEnumerator ProcessImage(XRCpuImage image)
    {
        // Parametri di conversione (ottimizzati per le performance)
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            outputFormat = TextureFormat.R8,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        ConvertImageSafe(image, conversionParams, buffer);
        image.Dispose();

        int width = conversionParams.outputDimensions.x;
        int height = conversionParams.outputDimensions.y;
        
        var luminanceSource = new RGBLuminanceSource(buffer.ToArray(), width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        var binarizer = new HybridBinarizer(luminanceSource);
        var binaryBitmap = new BinaryBitmap(binarizer);
        
        // Decodifica QR
        var result = reader.decode(binaryBitmap);
        
        buffer.Dispose();

        if (result != null)
        {
            // --- MODIFICA 2: CONTROLLO DI SICUREZZA FINALE ---
            // Se nel frattempo (mentre analizzavo) è stato caricato un modello, esci.
            if (loaderScript != null && loaderScript.IsModelLoaded) yield break;

            string scannedText = result.Text;
            
            if (!string.IsNullOrEmpty(scannedText) && scannedText.StartsWith("http"))
            {
                Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                
                if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
                {
                    Pose hitPose = hits[0].pose;
                    
                    if(statusText != null) statusText.text = "Trovato! Scarico...";
                    
                    isScanning = false; // Spegni la scansione locale
                    
                    // Avvia il download
                    loaderScript.DownloadModelAtPosition(scannedText, hitPose.position, hitPose.rotation);
                }
                else
                {
                     if(statusText != null) statusText.text = "QR Trovato! Inquadra un piano...";
                }
            }
        }
        yield return null;
    }

    private unsafe void ConvertImageSafe(XRCpuImage image, XRCpuImage.ConversionParams paramsData, NativeArray<byte> buffer)
    {
        image.Convert(paramsData, new System.IntPtr(buffer.GetUnsafePtr()), buffer.Length);
    }

    // Questa funzione viene chiamata dal tasto Reset (o dall'AppToolbar)
    public void RestartExperience()
    {
        // 1. Resetta il loader (che metterà IsModelLoaded = false)
        loaderScript.DestroyModel();
        
        // 2. Riavvia la scansione con un piccolo ritardo per dare tempo all'utente di spostarsi
        StartCoroutine(ResetDelay());
    }

    IEnumerator ResetDelay()
    {
        if(statusText != null) statusText.text = "Reset...";
        
        // Aspetta 1.5 secondi prima di riattivare gli occhi della camera
        // Questo evita che rilegga istantaneamente lo stesso QR code se è ancora davanti
        yield return new WaitForSeconds(1.5f);
        
        isScanning = true;
        hits.Clear();
        if(statusText != null) statusText.text = "Scansione attiva";
    }
}