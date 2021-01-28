using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using UnityEngine.AI;

public class VoronoiDemo : MonoBehaviour
{

    public Material land;
    public const int NPOINTS = 150;
    public const int WIDTH = 1000;
    public const int HEIGHT = 1000;
	public float freqx = 0.02f, freqy = 0.018f, offsetx = 0.43f, offsety = 0.22f;

    // buildings and road prefabs
    public GameObject road;
    public GameObject roadHighway;
    public GameObject roadPedestrian;
    public GameObject house;
    public GameObject skyscaper;
    public GameObject entertainment;
    public GameObject stadium;
    public GameObject tree;
    public GameObject lake;

    public NavMeshSurface navSurface;
    public NavMeshAgent agent;

    private List<Vector2> m_points;
	private List<LineSegment> m_edges = null;
    private List<Building> offices = new List<Building>();
    private List<Building> houses = new List<Building>();
    private List<Building> entertainments = new List<Building>();
    private List<Road> roads = new List<Road>();
    private Building stadiumInstance = null;
    private bool hasStadiumSpawned = false;

    private Texture2D tx;

	void Start ()
	{
        float [,] map = createMap();
        Color[] pixels = createPixelMap(map);
        List<uint> colors = new List<uint>();

        /* Create random points */
        createPoints(map, ref pixels, ref colors);

		/* Generate voronoi diagram */
		Delaunay.Voronoi v = new Delaunay.Voronoi(m_points, colors, new Rect(0, 0, WIDTH, HEIGHT));
		m_edges = v.VoronoiDiagram();

        /* Instanciate roads */
        Debug.Log("Generating roads and building...");
        generateRoads(m_edges, map, ref pixels);
        generateBuildings(roads, map);
        generateLandscapeFeatures(v, map);
        Debug.Log("Complete!");
        Debug.Log("Building nav mesh...");
        this.navSurface.BuildNavMesh();
        Debug.Log("Complete!");

        /* Apply pixels to texture */
        tx = new Texture2D(WIDTH, HEIGHT);
        land.SetTexture("_MainTex", tx);
		tx.SetPixels(pixels);
		tx.Apply();

        // add agents
        Debug.Log("Adding agents...");
        foreach (Building house in houses)
        {
            float r = Random.Range(0f, 1f);
            if (r <= 0.5f)
            {
                int office_idx = Random.Range(0, offices.Count);
                int entertainment_idx = Random.Range(0, entertainments.Count);
                NavMeshAgent a = (NavMeshAgent)Instantiate(agent, house.roadAnchor, Quaternion.identity);
                Controller cont = (Controller)a.GetComponent<Controller>();
                cont.house = house;
                cont.current = cont.house;
                cont.work = offices[office_idx];
                cont.shop = entertainments[entertainment_idx];
                cont.stadium = stadiumInstance;
            }
        }
        Debug.Log("Complete!");
    }

    void Update()
    {
        
    }

    // Density map creation
    private void createPoints(in float[,] map, ref Color[] pixels, ref List<uint> colors)
    {
        m_points = new List<Vector2>();
        int pi, pj; int iterMax = 10, iter = 0;
        float thresh = 0.75f; // default = 0.7 
        for (int i = 0; i < NPOINTS; i++)
        {
            pi = (int)Random.Range(0, WIDTH - 1);
            pj = (int)Random.Range(0, HEIGHT - 1);
            iter = 0;
            while (map[pi, pj] < thresh && iter < iterMax)
            {
                pi = (int)Random.Range(0, WIDTH - 1);
                pj = (int)Random.Range(0, HEIGHT - 1);
                iter++;
            }
            Vector2 vec = new Vector2(pi, pj);
            colors.Add((uint)0);
            //Vector2 vec = sampleWithDensityMap(map);
            DrawPoint(pixels, vec, Color.red);
            m_points.Add(vec);
        }
    }

    private float[,] createMap()
    {
        float[,] map = new float[WIDTH, HEIGHT];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
                map[i, j] = Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety);
        return map;
    }

    private void normalizeDensityMap(float[,] map)
    {
        float sum = 0;
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
                sum += map[i, j];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
                map[i, j] /= sum;
    }

    private Vector2 sampleWithDensityMap(float[,] map)
    {
        float thresh = Random.value;
        float cumsum = 0;
        for (int i = 0; i < WIDTH; i++)
        {
            for (int j = 0; j < HEIGHT; j++)
            {
                cumsum += map[i, j];
                if (cumsum >= thresh)
                    return new Vector2(i, j);
            }
        }
        return new Vector2(WIDTH - 1, HEIGHT - 1);
    }

    // Roads
    private void generateRoads(in List<LineSegment> edges, in float[,] densityMap, ref Color[] pixels)
    {
        Color color = Color.blue;
        for (int i = 0; i < m_edges.Count; i++)
        {
            LineSegment seg = m_edges[i];
            Vector2 left = (Vector2)seg.p0;
            Vector2 right = (Vector2)seg.p1;
            Vector2 middle = (left + right) / 2;
            Vector3 segment = (right - left) / WIDTH * 200;
            float a = Vector2.SignedAngle(Vector2.right, right - left);
            float density = densityMap[(int)middle.x, (int)middle.y];
            Vector3 start = new Vector3(left.y / WIDTH * 400 - 200, 0, left.x / HEIGHT * 400 - 200);
            GameObject roadObject = null;
            float segLength = segment.magnitude * 2.0f;
            if (density >= 0.74f)
                roadObject = roadPedestrian;
            else
            {
                if (segLength >= 40f)
                    roadObject = roadHighway;
                else
                    roadObject = road;
            }
            GameObject go = Instantiate(roadObject, start, Quaternion.Euler(0, a + 90, 0));
            go.transform.localScale = new Vector3(segment.magnitude, 1, 1);
            roads.Add(new Road(go, seg, segLength, true, density, start));
            DrawLine(pixels, left, right, color);
        }
    }

    private void generateBuildings(in List<Road> roads, in float[,] densityMap)
    {
        foreach (Road road in roads)
        {
            LineSegment seg = road.segment;
            Vector2 left = (Vector2)seg.p0;
            Vector2 right = (Vector2)seg.p1;
            Vector3 dir = (right - left); dir.Normalize();             // normalize segment direction
            Vector3 segment = (right - left) / WIDTH * 200;
            float segLength = segment.magnitude * 2.0f;                // segment length in world space
            if (segLength < 10f) continue;
            int nbBuildings = (int)Mathf.Min(10.0f, Mathf.Max(0f, Mathf.Floor(segLength / 7f)));
            bool noSpawnLeft = false;
            if (!hasStadiumSpawned && segLength >= 30f && segLength <= 40f)
                noSpawnLeft = true;
            for (int j = 0; j < nbBuildings; j++)
            {
                if (!noSpawnLeft)
                    createBuilding(segLength, nbBuildings, road.density, j, left, right, road, true);
                else if (noSpawnLeft && !hasStadiumSpawned)
                    createStadium(segLength, left, right, road);
                createBuilding(segLength, nbBuildings, road.density, j, left, right, road, false);
            }
        }
    }

    private void createBuilding(float segLength, int nbBuildings, float density, int step, in Vector2 left, in Vector2 right, in Road road, bool side)
    {
        float rand = Random.Range(0f, 1f);
        BuildingType buildingType;
        if (density <= 0.74f)
        {
            if (rand <= 0.8f) buildingType = BuildingType.HOUSE;
            else buildingType = BuildingType.ENTERTAINMENT;
        }
        else
        {
            if (rand <= 0.8f) buildingType = BuildingType.SKYSCRAPER;
            else buildingType = BuildingType.ENTERTAINMENT;
        }
        GameObject buildingGo = null;
        float translation = 0f; float verticalScale = 1f;
        switch (buildingType)
        {
            case BuildingType.HOUSE:
                translation = 2.1f;
                buildingGo = house;
                break;
            case BuildingType.ENTERTAINMENT:
                translation = 2.1f;
                buildingGo = entertainment;
                break;
            case BuildingType.SKYSCRAPER:
                translation = 2.1f;
                buildingGo = skyscaper;
                verticalScale = Random.Range(0.5f, 1.4f);
                break;
        }
        float dirStepSize = (segLength / (nbBuildings + 1)) * (step + 1);
        float a = Vector2.SignedAngle(Vector2.right, right - left);
        GameObject go = Instantiate(buildingGo, road.start, Quaternion.Euler(0, a + 90, 0));
        if (!side) translation = -translation;
        go.transform.Translate(new Vector3(-dirStepSize, 0, 0), go.transform);
        Vector3 roadAnchor = go.transform.position;
        go.transform.Translate(new Vector3(0, buildingGo.transform.position.y * verticalScale, translation), go.transform);
        Vector3 insideAnchor = go.transform.position;
        go.transform.localScale = new Vector3(go.transform.localScale.x, go.transform.localScale.y * verticalScale, go.transform.localScale.z);
        go.transform.parent = this.transform;
        switch (buildingType)
        {
            case BuildingType.HOUSE:
                houses.Add(new Building(buildingType, go, roadAnchor, insideAnchor));
                break;
            case BuildingType.ENTERTAINMENT:
                entertainments.Add(new Building(buildingType, go, roadAnchor, insideAnchor));
                break;
            case BuildingType.SKYSCRAPER:
                offices.Add(new Building(buildingType, go, roadAnchor, insideAnchor));
                break;
        }
    }

    public void createStadium(float segLength, in Vector2 left, in Vector2 right, in Road road)
    {
        hasStadiumSpawned = true;
        float a = Vector2.SignedAngle(Vector2.right, right - left);
        GameObject go = Instantiate(stadium, road.start, Quaternion.Euler(0, a + 90, 0));
        go.transform.Translate(new Vector3(-segLength / 2, 0, 0), go.transform);
        Vector3 roadAnchor = go.transform.position;
        go.transform.Translate(new Vector3(0, 0, 6), go.transform);
        Vector3 insideAnchor = go.transform.position;
        go.transform.parent = this.transform;
        stadiumInstance = new Building(BuildingType.STADIUM, go, roadAnchor, insideAnchor);
    }

    public void generateLandscapeFeatures(in Delaunay.Voronoi v, in float[,] densityMap)
    {
        int count = Random.Range(1, 3);
        foreach(Vector2 site in v.SiteCoords())
        {
            if (densityMap[(int)site.x, (int)site.y] <= 0.2f)
            {
                if (count > 0)
                {
                    GameObject go = Instantiate(lake, new Vector3(site.y / WIDTH * 400 - 200, 0, site.x / HEIGHT * 400 - 200), Quaternion.identity);
                    count--;
                }
                int nbTrees = Random.Range(3, 7);
                for (int i = 0; i < nbTrees; ++i)
                {
                    float rot = Random.Range(0, 360f);
                    float dist = Random.Range(7f, 8f);
                    GameObject go = Instantiate(tree, new Vector3(site.y / WIDTH * 400 - 200, 0, site.x / HEIGHT * 400 - 200), Quaternion.Euler(0, rot, 0));
                    go.transform.Translate(new Vector3(dist, 0, 0), go.transform);
                }
            }
        }
    }

    /* Functions to create and draw on a pixel array */
    private Color[] createPixelMap(float[,] map)
    {
        Color[] pixels = new Color[WIDTH * HEIGHT];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
            {
                pixels[i * HEIGHT + j] = Color.Lerp(Color.black, Color.white, map[i, j]);
            }
        return pixels;
    }

    private void DrawPoint (Color [] pixels, Vector2 p, Color c) {
		if (p.x<WIDTH&&p.x>=0&&p.y<HEIGHT&&p.y>=0) 
		    pixels[(int)p.x*HEIGHT+(int)p.y]=c;
	}

    // Bresenham line algorithm
	private void DrawLine(Color [] pixels, Vector2 p0, Vector2 p1, Color c) {
		int x0 = (int)p0.x;
		int y0 = (int)p0.y;
		int x1 = (int)p1.x;
		int y1 = (int)p1.y;

		int dx = Mathf.Abs(x1-x0);
		int dy = Mathf.Abs(y1-y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx-dy;
		while (true) {
            if (x0 >= 0 && x0 < WIDTH && y0 >=0 && y0 < HEIGHT)
    			pixels[x0*HEIGHT+y0]=c;
			if (x0 == x1 && y0 == y1) break;
			int e2 = 2*err;
			if (e2 > -dy) {
				err -= dy;
				x0 += sx;
			}
			if (e2 < dx) {
				err += dx;
				y0 += sy;
			}
		}
	}
}
