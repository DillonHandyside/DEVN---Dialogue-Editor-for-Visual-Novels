﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

namespace DEVN
{

public class NodeManager
{
    // collection of all nodes in graph
    [SerializeField] private List<BaseNode> m_nodes;

    // clipboard used for copy and paste
    [SerializeField] private BaseNode m_clipboard;

    #region getters

    public List<BaseNode> GetNodes() { return m_nodes; }
    public BaseNode GetClipboard() { return m_clipboard; }

    #endregion

    #region setters

    //public void SetNodes(List<BaseNode> nodes) { m_nodes = nodes; }

    #endregion
            
    public void UpdateNodes()
    {
        Scene currentScene = NodeEditor.GetScene();
        m_nodes = currentScene.GetNodes(currentScene.GetCurrentPageID());
    }

    /// <summary>
    /// function which iterates through all nodes and calls draw
    /// </summary>
    public void DrawNodes()
    {
        for (int i = 0; i < m_nodes.Count; i++)
            m_nodes[i].Draw();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nodeID"></param>
    /// <returns></returns>
    public BaseNode GetNode(int nodeID)
    {
        for (int i = 0; i < m_nodes.Count; i++)
        {
            if (m_nodes[i].GetNodeID() == nodeID)
                return m_nodes[i];
        }

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nodeType"></param>
    /// <returns></returns>
    public BaseNode AddNode(System.Type type)
    {
        Scene currentScene = NodeEditor.GetScene();

        BaseNode node = ScriptableObject.CreateInstance(type) as BaseNode;
        node.Init(NodeEditor.GetMousePosition());

        if (type != typeof(StartNode))
        {
            Page currentPage = currentScene.GetCurrentPage();
            Undo.RecordObject(currentPage, "New Node");
            m_nodes.Add(node);

            Undo.RegisterCreatedObjectUndo(node, "New Node");
        }
        
        //node.hideFlags = HideFlags.HideInHierarchy;
        string path = AssetDatabase.GetAssetPath(currentScene);
        AssetDatabase.AddObjectToAsset(node, path);
        AssetDatabase.SaveAssets();

        GUI.changed = true;

        return node;
    }

    /// <summary>
    /// handles node copying
    /// </summary>
    /// <param name="node">the desired node to copy</param>
    public void CopyNode(BaseNode node)
    {
        m_clipboard = node;
        GUI.changed = true;
    }

    /// <summary>
    /// handles node pasting
    /// </summary>
    public void PasteNode()
    {
        BaseNode node = ScriptableObject.CreateInstance(m_clipboard.GetType()) as BaseNode;
        node.Copy(m_clipboard, NodeEditor.GetMousePosition());

        string path = AssetDatabase.GetAssetPath(NodeEditor.GetScene());
        AssetDatabase.AddObjectToAsset(node, path);

        m_nodes.Add(node);
        GUI.changed = true;
    }

    /// <summary>
    /// handles node removal
    /// </summary>
    /// <param name="node">the desired node to remove</param>
    public void RemoveNode(BaseNode node)
    {
        if (node is StartNode)
            return; // disallow removal of starting node

        // iterate through all connected input nodes
        for (int i = 0; i < node.m_inputs.Count; i++)
        {
            BaseNode connectedNode = NodeEditor.GetNodeManager().GetNode(node.m_inputs[i]);

            // find the index of the desired node to remove in the connected node's outputs
            int indexOfConnectedNode = connectedNode.m_outputs.IndexOf(node.GetNodeID());

            // disconnect connection
            connectedNode.m_outputs[indexOfConnectedNode] = -1;
			node.m_inputs.Remove(connectedNode.GetNodeID());
        }

        // iterate through all connected output nodes
        for (int i = 0; i < node.m_outputs.Count; i++)
        {
            if (node.m_outputs[i] == -1)
                continue; // skip empty outputs

			// disconnect connection
			NodeEditor.GetConnectionManager().RemoveConnection(node, i);
        }

        // record changes to the page
        Page currentPage = NodeEditor.GetScene().GetCurrentPage();
        Undo.RecordObject(currentPage, "Remove Node");
        m_nodes.Remove(node); // perform removal

        // destroy object and record
        Undo.DestroyObjectImmediate(node);
        AssetDatabase.SaveAssets();
    }
}

}

#endif