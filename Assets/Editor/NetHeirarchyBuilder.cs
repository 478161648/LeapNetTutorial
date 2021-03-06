﻿using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class NetworkSekeltalComposer : EditorWindow
{
    [MenuItem("GameObject/Network/Skeltal Composer")]
    public static void Show()
    {
        EditorWindow.GetWindow<NetworkSekeltalComposer>();
    }

    GameObject selection;
    NetworkTransform netTransform;
    bool multiSelectFail = false;

    [SerializeField] Transform heirarchyRoot;

    void OnGUI()
    {
        if (selection != null)
        {
            if (multiSelectFail)
            {
                EditorGUILayout.LabelField("Not compatible with multi-selection!");
            }
            else
            {
                if (netTransform == null)
                {
                    EditorGUILayout.LabelField(selection.name);
                    EditorGUILayout.LabelField("Object must have a NetworkTransform.");
                    if (GUILayout.Button("Try finding one further up the tree."))
                    {
                        NetworkTransform transformCandidate = selection.GetComponentInParent<NetworkTransform>();
                    }

                    if (GUILayout.Button("Add one to this object."))
                    {
                        netTransform = selection.AddComponent<NetworkTransform>();
                    }
                }
                else
                {
                    heirarchyRoot = (Transform)EditorGUILayout.ObjectField("skeltal root", heirarchyRoot, typeof(Transform), true);

                    if (heirarchyRoot == null)
                    {
                        EditorGUILayout.LabelField("Assign a skeltal root object before assigning network transform children");
                    }
                    else
                    {
                        if (GUILayout.Button("Build tree"))
                        {
                            BuildTree(heirarchyRoot);
                        }

                        if(GUILayout.Button("Ensure Path To Parent"))
                        {
                            List<NetworkTransformChild> comprehensiveList = new List<NetworkTransformChild>();
                            comprehensiveList.AddRange(selection.GetComponentsInChildren<NetworkTransformChild>());

                            EnsurePathToParent(heirarchyRoot, netTransform.transform, ref comprehensiveList);
                        }
                    }

                    if (GUILayout.Button("Debug: delete all NetworkTransformChildren"))
                    {
                        NetworkTransformChild[] children = selection.GetComponents<NetworkTransformChild>();

                        for (int i = 0; i < children.Length; i++) DestroyImmediate(children[i]);
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("To use this script, you must have one GameObject selected with a NetworkTransform component.");
            if(GUILayout.Button("Refresh"))
            {
                GetSelectionComponents();
            }                    
        }
    }

    void BuildTree(Transform skeletalRoot)
    {
        List<NetworkTransformChild> comprehensiveList = new List<NetworkTransformChild>();
        comprehensiveList.AddRange(selection.GetComponentsInChildren<NetworkTransformChild>());

        EnsurePathToParent(skeletalRoot, netTransform.transform, ref comprehensiveList);
        ApplyNetTransChild(skeletalRoot, netTransform.transform, ref comprehensiveList);
    }

    void EnsurePathToParent(Transform child, Transform root, ref List<NetworkTransformChild> childList)
    {
        if (child.GetInstanceID() == root.GetInstanceID()) return;

        if (!ChildListContains(child, childList))
        {
            NetworkTransformChild newChild = root.gameObject.AddComponent<NetworkTransformChild>();
            newChild.target = child;
            childList.Add(newChild);
        }

        EnsurePathToParent(child.parent, root, ref childList);
    }

    private static bool ChildListContains(Transform child, List<NetworkTransformChild> childList)
    {
        bool childListContainsTarget = false;

        for (int i = 0; i < childList.Count; i++)
        {
            if (childList[i].target == null)
            {
                Debug.Log("Network Transform Child had no target!");
                Debug.Break();
            }
            else
            {
                if (childList[i].target.GetInstanceID() == child.GetInstanceID())
                {
                    childListContainsTarget = true;
                    break;
                }
            }
        }

        return childListContainsTarget;
    }

    void ApplyNetTransChild(Transform child, Transform root, ref List<NetworkTransformChild> childList)
    {
        if (!ChildListContains(child, childList))
        {
            NetworkTransformChild newChild = root.gameObject.AddComponent<NetworkTransformChild>();
            newChild.target = child;
            childList.Add(newChild);
        }

        if(child.childCount > 0)
        {
            for(int i=0; i < child.childCount; i++)
            {
                ApplyNetTransChild(child.GetChild(i), root, ref childList);
            }
        }
    }

    void GetSelectionComponents()
    {
        selection = null;
        multiSelectFail = false;

        if (Selection.gameObjects.Length == 0) return;
        if (Selection.gameObjects.Length > 1)
        {
            multiSelectFail = true;
            return;
        }

        selection = Selection.gameObjects[0];

        netTransform = selection.GetComponent<NetworkTransform>();
    }

    void OnSelectionChange()
    {
        GetSelectionComponents();
    }
}
