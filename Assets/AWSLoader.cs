using UnityEngine;
using GLTFast;
using System.Threading.Tasks;

public class AWSLoader : MonoBehaviour
{
    [Header("Interfaccia UI")]
    public GameObject loadingPanel; 
    public GameObject scanFrame; 
    public AppToolbar toolbarManager; 
       
    [Header("Impostazioni")]
    public float autoRotationSpeed = 30f;
    public float sensitivity = 10f; 
    public float targetSize = 0.2f;

    private GameObject currentModel;
    private bool isDragging = false; 
    private float lastMouseX;
    private bool isPaused = false; 

    void Awake()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (scanFrame != null) scanFrame.SetActive(true);
    }

    public async void DownloadModelAtPosition(string url, Vector3 position, Quaternion rotation)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (scanFrame != null) scanFrame.SetActive(false);
        
        if (currentModel != null) Destroy(currentModel);

        currentModel = new GameObject("Scaricato_da_AWS");
        currentModel.transform.position = position;
        currentModel.transform.rotation = rotation;

        var gltf = currentModel.AddComponent<GltfAsset>();
        gltf.Url = url;
        gltf.LoadOnStartup = false;

        bool success = await gltf.Load(url);

        if (success)
        {
            if (gltf.SceneInstance != null)
            {
                await Task.Yield();
                await FixMaterialsDirectly(currentModel, gltf);
                RecenterModel(currentModel);
            }
            if (toolbarManager != null) toolbarManager.SwitchToViewMode();
        }

        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    void Update()
    {
        if (currentModel == null) return;

        if (Input.GetMouseButtonDown(0)) 
        {
            isDragging = true; 
            lastMouseX = Input.mousePosition.x; 
        }
        if (Input.GetMouseButtonUp(0)) isDragging = false; 

        if (isDragging)
        {
            float deltaX = Input.mousePosition.x - lastMouseX;
            lastMouseX = Input.mousePosition.x; 
            float rotationAmount = -deltaX * sensitivity * Time.deltaTime;
            currentModel.transform.Rotate(0, rotationAmount, 0, Space.World);
        }
        else
        {
            if (!isPaused) currentModel.transform.Rotate(Vector3.up, autoRotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    // --- ZOOM FIX (Logica Moltiplicativa) ---
    // Questa funzione aggira il problema del blocco fotocamera di AR Foundation
    public void ChangeScale(float factor)
    {
        if (currentModel != null)
        {
            // Invece di sommare (+), moltiplichiamo (*)
            // Se factor è 1.1, ingrandisce del 10%. Se è 0.9, rimpicciolisce del 10%.
            Vector3 newScale = currentModel.transform.localScale * factor;

            // Limiti di sicurezza (Minimo 0.05, Massimo 3 volte la grandezza originale)
            // Clamp serve a non far sparire l'oggetto se rimpicciolisci troppo
            float clampedX = Mathf.Clamp(newScale.x, 0.01f, 5.0f);
            float clampedY = Mathf.Clamp(newScale.y, 0.01f, 5.0f);
            float clampedZ = Mathf.Clamp(newScale.z, 0.01f, 5.0f);

            currentModel.transform.localScale = new Vector3(clampedX, clampedY, clampedZ);
            
            Debug.Log($"Nuova Scala: {currentModel.transform.localScale}");
        }
        else
        {
            Debug.LogWarning("Nessun modello caricato da zoomare!");
        }
    }

    public void SetRotationPaused(bool paused) { isPaused = paused; }

    public void DestroyModel()
    {
        if (currentModel != null) { Destroy(currentModel); currentModel = null; }
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (scanFrame != null) scanFrame.SetActive(true);
        if (toolbarManager != null) toolbarManager.SwitchToScanMode();
    }

    async Task FixMaterialsDirectly(GameObject model, GltfAsset gltfAsset)
    {
        // ... (Codice Materiali uguale a prima) ...
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");
        Texture2D textureDiretta = null;
        if (gltfAsset.Importer != null && gltfAsset.Importer.TextureCount > 0)
            textureDiretta = gltfAsset.Importer.GetTexture(0);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers) {
            foreach (Material mat in ren.materials) {
                mat.shader = standardShader;
                if (textureDiretta != null) { mat.SetTexture("_BaseMap", textureDiretta); mat.SetTexture("_MainTex", textureDiretta); mat.color = Color.white; }
                else { mat.color = Color.white; }
            }
        }
        await Task.Yield();
    }

    void RecenterModel(GameObject parentObject)
    {
        // ... (Codice Recenter uguale a prima) ...
        Bounds bounds = new Bounds(parentObject.transform.position, Vector3.zero);
        Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        foreach (Renderer ren in renderers) bounds.Encapsulate(ren.bounds);
        Vector3 centerOffset = bounds.center - parentObject.transform.position;
        foreach (Transform child in parentObject.transform) child.position -= centerOffset;
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension > 0) parentObject.transform.localScale = Vector3.one * (targetSize / maxDimension);
    }
}