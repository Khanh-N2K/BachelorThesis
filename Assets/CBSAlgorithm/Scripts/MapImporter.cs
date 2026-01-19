using UnityEngine;
using UnityEngine.UI;
using SFB; // StandaloneFileBrowser

public class MapImporter : MonoBehaviour
{
    [Header("References")]
    public MapLoader mapLoader;

    [Header("Buttons")]
    public Button importMapButton;
    public Button importScenButton;

    void Start()
    {
        if (importMapButton != null)
            importMapButton.onClick.AddListener(OpenMapDialog);

        if (importScenButton != null)
            importScenButton.onClick.AddListener(OpenScenDialog);
    }

    void OpenMapDialog()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Map File", "", "map", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string mapText = System.IO.File.ReadAllText(paths[0]);
            mapLoader.LoadFromString(mapText);
            mapLoader.RenderMap();
            Debug.Log($"Loaded map: {paths[0]}");
        }
    }

    void OpenScenDialog()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Scenario File", "", "scen", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string scenText = System.IO.File.ReadAllText(paths[0]);
            mapLoader.LoadScenariosFromString(scenText);
            mapLoader.SpawnEnemies();
            Debug.Log($"Loaded scenario: {paths[0]} with {mapLoader.Scenarios.Count} entries");
        }
    }
}
