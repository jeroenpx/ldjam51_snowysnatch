using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slope : MonoBehaviour
{
    public Transform slopeMesh;
    public Transform[] points;


    
    [ContextMenu("Sort points in Order")]
    private void Sort() {
        #if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Sorting points of Slope");
        #endif
        System.Array.Sort(points, new Comparison<Transform>((a, b) => a.localPosition.z - b.localPosition.z > 0?1:-1));
        float start = points[0].localPosition.z;
        float end = points[points.Length-1].localPosition.z;
        Debug.Log("Slope Length: "+(end-start));
    }

    /** Get the end point of this slope in global space */
    public Vector3 GetGlobalSpaceEndPoint() {
        return points[points.Length-1].position;
    }

    public Vector3 GetGlobalSpawnShift() {
        return -(points[0].position-transform.position);
    }

    public Vector3[] GetPoints() {
        Vector3[] positions = new Vector3[points.Length];
        for(int i=0;i<points.Length;i++) {
            positions[i] = points[i].position;
        }
        return positions;
    }
}
