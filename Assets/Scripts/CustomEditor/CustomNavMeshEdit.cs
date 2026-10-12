/*
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections;

public class CustomNavMeshEdit : MonoBehaviour
{
	public NavMeshData NavMesh_Data;

	
    private void Awake()
    {
		MakeAutoOffMeshLinksBidirectional();
	}
	
    private void MakeAutoOffMeshLinksBidirectional()
	{
		SerializedObject so = new SerializedObject(NavMesh_Data);

		SerializedProperty offMeshLinksProp = so.FindProperty("m_OffMeshLinks");
		if (offMeshLinksProp == null || !offMeshLinksProp.isArray)
		{
			Debug.Log("No m_OffMeshLinks array found in NavMeshData.");
			return;
		}

		for (int i = 0; i < offMeshLinksProp.arraySize; i++)
		{
			SerializedProperty linkElement = offMeshLinksProp.GetArrayElementAtIndex(i);

			SerializedProperty linkDirection = linkElement.FindPropertyRelative("m_LinkDirection");
			if (linkDirection != null)
			{
				linkDirection.intValue = 1; // Force bidirectional
			}

			SerializedProperty linkTypeProp = linkElement.FindPropertyRelative("m_LinkType");
			if (linkTypeProp != null)
			{
				linkTypeProp.intValue = 3;
			}
		}

		so.ApplyModifiedPropertiesWithoutUndo();		
	}
}
*/