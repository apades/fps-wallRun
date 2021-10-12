using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public struct Palette
{

}

public class PaletteGenerator : EditorWindow
{
    public GameObject root;

    public Material mainMaterial;
    public Material accentMaterial;
    public Material floatingElementsMaterial;

    public Material newMainMaterial;
    public Material newAccentMaterial;
    public Material newFloatingElementsMaterial;

    float progress = -1;

    [MenuItem("WallRun/PaletteGenerator")]
    static void Init()
    {
        UnityEditor.EditorWindow window = GetWindow(typeof(PaletteGenerator));
        const int width = 500;
        const int height = 500;
        Vector2 size = new Vector2 (width, height);
        window.minSize = size;
        window.position = new Rect(0, 0, width, height);
        window.Show();
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    void OnGUI()
    {   
        root = (GameObject) EditorGUILayout.ObjectField("Level", root, typeof(GameObject), true);
        
        EditorGUILayout.LabelField("Current Materials");
        mainMaterial = (Material) EditorGUILayout.ObjectField("Main Material", mainMaterial, typeof(Material), true);
        accentMaterial = (Material) EditorGUILayout.ObjectField("Accent Material", accentMaterial, typeof(Material), true);
        floatingElementsMaterial = (Material) EditorGUILayout.ObjectField("Floating Elements Material", floatingElementsMaterial, typeof(Material), true);
        
        EditorGUILayout.LabelField("New Materials");
        newMainMaterial = (Material) EditorGUILayout.ObjectField("Main Material", newMainMaterial, typeof(Material), true);
        newAccentMaterial = (Material) EditorGUILayout.ObjectField("Accent Material", newAccentMaterial, typeof(Material), true);
        newFloatingElementsMaterial = (Material) EditorGUILayout.ObjectField("Floating Elements Material", newFloatingElementsMaterial, typeof(Material), true);

        if (GUILayout.Button("Apply Palette!"))
        {
            if (root == null)
            {
                ShowNotification(new GUIContent("No root selected for searching materials"));
            }
            else
            {
                List<Renderer> rends = root.GetComponentsInChildren<Renderer>(true).ToList();
                progress = 0;
                ChangeMats(rends, mainMaterial, newMainMaterial, 0.4f);
                ChangeMats(rends, accentMaterial, newAccentMaterial, 0.3f);
                ChangeMats(rends, floatingElementsMaterial, newFloatingElementsMaterial, 0.3f);
            }
        }

        if(progress >= 0 && progress < 1)
            EditorUtility.DisplayProgressBar("Palette", "Applying Palette...", progress);
        else
            EditorUtility.ClearProgressBar();
    }

    void OnDestroy()
    {
        EditorUtility.ClearProgressBar();
    }

    
    void ChangeMats(List<Renderer> r, Material oldM, Material newM, float weight)
    {
        if(r != null && oldM != null && newM != null)
        {
            List<Renderer> objs = r.Where(x => x.sharedMaterial == oldM).ToList();
            foreach(var o in objs)
            {
                o.sharedMaterial = newM;
                Debug.Log(o);
            }
        }
        progress += weight;
    }
}
