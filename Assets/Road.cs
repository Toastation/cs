using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delaunay.Geo;
using UnityEngine.AI;

public enum RoadType
{
    PEDESTRIAN,
    CARNORMAL,
    CARHIGHWAY
}

public class Road
{
    public GameObject go { get; }
    public LineSegment segment { get; }
    public bool pedestrian { get; }
    public float density { get; }
    public float segLength { get; }
    public Vector3 start;
    public RoadType roadType;

    public Road(GameObject go, LineSegment segment, float segLength, bool pedestrian, float density, Vector3 start)
    {
        this.go = go;
        this.segment = segment;
        this.pedestrian = pedestrian;
        this.density = density;
        this.start = start;
        this.segLength = segLength;
        NavMeshModifier modifier = go.GetComponent<NavMeshModifier>();
        if (this.density >= 0.74f)
        {
            roadType = RoadType.PEDESTRIAN;
            modifier.area = 4;
        }
        else
        {
            if (this.segLength >= 40)
            {
                modifier.area = 3;
                roadType = RoadType.CARHIGHWAY;
            }
            else
            {
                roadType = RoadType.CARNORMAL;
            }
        }
    }


}
