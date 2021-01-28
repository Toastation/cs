using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingType
{
    HOUSE,
    SKYSCRAPER,
    ENTERTAINMENT,
    STADIUM
}

public class Building
{

    public BuildingType type { get; }
    public GameObject go { get; }
    public Light light;
    public int capacity { get; set;  }
    public int current { get; set; }
    public Vector3 roadAnchor;
    public Vector3 insideAnchor;

    public Building(BuildingType type, GameObject go, Vector3 roadAnchor, Vector3 insideAnchor)
    {
        this.type = type;
        this.go = go;
        this.light = go.GetComponentInChildren<Light>();
        this.light.intensity = 0f;
        this.roadAnchor = roadAnchor;
        this.insideAnchor = insideAnchor;
        switch (this.type)
        {
            case BuildingType.HOUSE:
                capacity = current = 1;
                break;
            case BuildingType.ENTERTAINMENT:
                capacity = current = 4;
                break;
            case BuildingType.SKYSCRAPER:
                capacity = 100;
                current = 0;
                break;
            case BuildingType.STADIUM:
                capacity = 300;
                current = 0;
                break;
        }
    }

    public void addResident()
    {
        capacity += 1;
    }

    public void arrived()
    {
        current += 1;
        if (this.light.intensity == 0f)
        {
            switch (this.type)
            {
                case BuildingType.HOUSE:
                    this.light.intensity = 5f;
                    break;
                case BuildingType.ENTERTAINMENT:  case BuildingType.SKYSCRAPER: case BuildingType.STADIUM:
                    this.light.intensity = 7f;
                    break;
            }
        }
    }

    public void leave()
    {
        current -= 1;
        if (current <= 0)
        {
            current = 0;
        }
    }

}
