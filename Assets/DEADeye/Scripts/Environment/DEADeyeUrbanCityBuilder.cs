using System.Collections.Generic;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Constructs the high-rise urban sniper environment:
    /// - High-rise rooftop sniper nest with parapet walls, weapon rests, and safety collision boundaries.
    /// - City streets below with asphalt roadways, road markings, crosswalks, raised sidewalks, and streetlights.
    /// - Multi-story buildings with architectural setbacks, window grids, and varied heights.
    /// - Escape corridors (Highway, Metro, Bridge) with trigger zones.
    /// - Automatically hooks spawn points and waypoints into DEADeyeMissionDirector.
    /// </summary>
    public class DEADeyeUrbanCityBuilder : MonoBehaviour
    {
        [Header("City Layout Settings")]
        [SerializeField] private float rooftopHeight = 26.0f;
        [SerializeField] private bool generateOnAwake = true;

        [Header("Materials (Auto-created if null)")]
        [SerializeField] private Material asphaltMaterial;
        [SerializeField] private Material sidewalkMaterial;
        [SerializeField] private Material buildingConcreteMat;
        [SerializeField] private Material buildingBrickMat;
        [SerializeField] private Material windowGlassMat;
        [SerializeField] private Material roadMarkingMat;
        [SerializeField] private Material metalRooftopMat;

        private List<Transform> spawnPoints = new List<Transform>();
        private List<Transform> waypoints = new List<Transform>();
        private List<Transform> escapePoints = new List<Transform>();

        public float RooftopHeight => rooftopHeight;
        public List<Transform> SpawnPoints => spawnPoints;
        public List<Transform> Waypoints => waypoints;
        public List<Transform> EscapePoints => escapePoints;

        private void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void Awake()
        {
            Transform cityRoot = transform.Find("CityEnvironment_Generated");
            if (cityRoot != null)
            {
                CollectExistingNodes();
            }
            else if (generateOnAwake)
            {
                BuildCityEnvironment();
            }
        }

        public void CollectExistingNodes()
        {
            spawnPoints.Clear();
            waypoints.Clear();
            escapePoints.Clear();

            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Spawn_"))
                    spawnPoints.Add(child);
                else if (child.name.StartsWith("Waypoint_"))
                    waypoints.Add(child);
            }

            Transform escapeRoot = transform.Find("CityEnvironment_Generated/Escape_Corridors");
            if (escapeRoot == null) escapeRoot = transform.Find("Escape_Corridors");
            if (escapeRoot != null)
            {
                foreach (Transform child in escapeRoot)
                {
                    if (child.name.StartsWith("EscapeZone_"))
                        escapePoints.Add(child);
                }
            }

            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.SetSpawnPoints(spawnPoints, waypoints, escapePoints);
            }
        }

        public void BuildCityEnvironment()
        {
            InitializeMaterials();

            Transform existing = transform.Find("CityEnvironment_Generated");
            if (existing != null)
            {
                SafeDestroy(existing.gameObject);
            }

            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Spawn_") || child.name.StartsWith("Waypoint_"))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                SafeDestroy(go);
            }

            spawnPoints.Clear();
            waypoints.Clear();
            escapePoints.Clear();

            GameObject cityRoot = new GameObject("CityEnvironment_Generated");
            cityRoot.transform.SetParent(transform, false);

            BuildStreetsAndSidewalks(cityRoot.transform);
            BuildUrbanBuildings(cityRoot.transform);
            BuildStreetDetails(cityRoot.transform);
            BuildEscapeCorridors(cityRoot.transform);
            BuildRooftopSniperNest(cityRoot.transform);

            // Register nodes with Mission Director if present
            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.SetSpawnPoints(spawnPoints, waypoints, escapePoints);
            }
        }

        private void InitializeMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

            if (asphaltMaterial == null)
            {
                asphaltMaterial = new Material(litShader) { name = "Mat_Asphalt" };
                asphaltMaterial.color = new Color(0.12f, 0.12f, 0.13f);
            }
            if (sidewalkMaterial == null)
            {
                sidewalkMaterial = new Material(litShader) { name = "Mat_Sidewalk" };
                sidewalkMaterial.color = new Color(0.65f, 0.65f, 0.63f);
            }
            if (buildingConcreteMat == null)
            {
                buildingConcreteMat = new Material(litShader) { name = "Mat_BuildingConcrete" };
                buildingConcreteMat.color = new Color(0.28f, 0.30f, 0.33f);
            }
            if (buildingBrickMat == null)
            {
                buildingBrickMat = new Material(litShader) { name = "Mat_BuildingBrick" };
                buildingBrickMat.color = new Color(0.38f, 0.22f, 0.20f);
            }
            if (windowGlassMat == null)
            {
                windowGlassMat = new Material(litShader) { name = "Mat_WindowGlass" };
                windowGlassMat.color = new Color(0.15f, 0.25f, 0.35f);
            }
            if (roadMarkingMat == null)
            {
                roadMarkingMat = new Material(unlitShader) { name = "Mat_RoadMarking" };
                roadMarkingMat.color = new Color(0.95f, 0.85f, 0.25f); // Highway Yellow
            }
            if (metalRooftopMat == null)
            {
                metalRooftopMat = new Material(litShader) { name = "Mat_RooftopMetal" };
                metalRooftopMat.color = new Color(0.25f, 0.26f, 0.28f);
            }
        }

        private void BuildStreetsAndSidewalks(Transform parent)
        {
            GameObject streetsRoot = new GameObject("Streets_And_Sidewalks");
            streetsRoot.transform.SetParent(parent, false);

            // Ground base plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundAsphalt";
            ground.transform.SetParent(streetsRoot.transform, false);
            ground.transform.position = new Vector3(0f, 0f, 40f);
            ground.transform.localScale = new Vector3(14f, 1f, 14f); // 140m x 140m
            ground.GetComponent<Renderer>().material = asphaltMaterial;

            // Main Boulevard (North-South, 14m wide)
            CreateRoadMarkings(streetsRoot.transform, new Vector3(0f, 0.02f, 40f), 100f, isVertical: true);

            // Cross Avenue (East-West at Z = 38m, 12m wide)
            CreateRoadMarkings(streetsRoot.transform, new Vector3(0f, 0.02f, 38f), 90f, isVertical: false);

            // Raised Sidewalks along Main Boulevard
            CreateSidewalk(streetsRoot.transform, new Vector3(-8.5f, 0.08f, 40f), new Vector3(3f, 0.16f, 100f));
            CreateSidewalk(streetsRoot.transform, new Vector3(8.5f, 0.08f, 40f), new Vector3(3f, 0.16f, 100f));

            // Crosswalks at intersection
            CreateCrosswalk(streetsRoot.transform, new Vector3(0f, 0.03f, 30f), 12f);
            CreateCrosswalk(streetsRoot.transform, new Vector3(0f, 0.03f, 46f), 12f);
        }

        private void CreateRoadMarkings(Transform parent, Vector3 center, float length, bool isVertical)
        {
            GameObject marking = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marking.name = isVertical ? "Centerline_NS" : "Centerline_EW";
            marking.transform.SetParent(parent, false);
            marking.transform.position = center;
            marking.transform.localScale = isVertical ? new Vector3(0.25f, 0.02f, length) : new Vector3(length, 0.02f, 0.25f);
            marking.GetComponent<Renderer>().material = roadMarkingMat;
            SafeDestroy(marking.GetComponent<Collider>());
        }

        private void CreateSidewalk(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "Sidewalk";
            sw.transform.SetParent(parent, false);
            sw.transform.position = pos;
            sw.transform.localScale = size;
            sw.GetComponent<Renderer>().material = sidewalkMaterial;
        }

        private void CreateCrosswalk(Transform parent, Vector3 pos, float width)
        {
            int stripeCount = 7;
            float spacing = width / stripeCount;
            for (int i = 0; i < stripeCount; i++)
            {
                float x = -width * 0.5f + (i + 0.5f) * spacing;
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "CrosswalkStripe";
                stripe.transform.SetParent(parent, false);
                stripe.transform.position = new Vector3(pos.x + x, pos.y, pos.z);
                stripe.transform.localScale = new Vector3(0.6f, 0.02f, 3.0f);
                stripe.GetComponent<Renderer>().material = sidewalkMaterial;
                SafeDestroy(stripe.GetComponent<Collider>());
            }
        }

        private void BuildUrbanBuildings(Transform parent)
        {
            GameObject buildingsRoot = new GameObject("Buildings_Skyline");
            buildingsRoot.transform.SetParent(parent, false);

            // Left Block (West Side)
            CreateBuilding(buildingsRoot.transform, new Vector3(-24f, 10f, 18f), new Vector3(18f, 20f, 24f), buildingConcreteMat);
            CreateBuilding(buildingsRoot.transform, new Vector3(-26f, 15f, 55f), new Vector3(20f, 30f, 28f), buildingBrickMat);
            CreateBuilding(buildingsRoot.transform, new Vector3(-24f, 8f, 85f), new Vector3(18f, 16f, 22f), buildingConcreteMat);

            // Right Block (East Side)
            CreateBuilding(buildingsRoot.transform, new Vector3(24f, 14f, 20f), new Vector3(18f, 28f, 26f), buildingBrickMat);
            CreateBuilding(buildingsRoot.transform, new Vector3(26f, 11f, 58f), new Vector3(20f, 22f, 30f), buildingConcreteMat);
            CreateBuilding(buildingsRoot.transform, new Vector3(24f, 16f, 88f), new Vector3(18f, 32f, 24f), buildingBrickMat);

            // Distant Background Skyscrapers (Skyline depth)
            CreateBuilding(buildingsRoot.transform, new Vector3(-55f, 28f, 110f), new Vector3(28f, 56f, 32f), buildingConcreteMat);
            CreateBuilding(buildingsRoot.transform, new Vector3(0f, 32f, 120f), new Vector3(34f, 64f, 30f), buildingBrickMat);
            CreateBuilding(buildingsRoot.transform, new Vector3(55f, 26f, 115f), new Vector3(30f, 52f, 30f), buildingConcreteMat);
        }

        private void CreateBuilding(Transform parent, Vector3 pos, Vector3 size, Material facadeMat)
        {
            GameObject bldg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bldg.name = $"Building_{pos.x:0}_{pos.z:0}";
            bldg.transform.SetParent(parent, false);
            bldg.transform.position = pos;
            bldg.transform.localScale = size;
            bldg.GetComponent<Renderer>().material = facadeMat;

            // Add windows on street-facing side
            CreateBuildingWindows(bldg.transform, size);
        }

        private void CreateBuildingWindows(Transform building, Vector3 size)
        {
            int floors = Mathf.FloorToInt(size.y / 3.5f);
            int cols = Mathf.FloorToInt(Mathf.Min(size.x, size.z) / 3.0f);

            for (int f = 1; f < floors; f++)
            {
                float y = -0.5f + (float)f / floors;
                for (int c = 0; c < cols; c++)
                {
                    float x = -0.4f + (float)c / cols * 0.8f;
                    GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    win.name = "Window";
                    win.transform.SetParent(building, false);
                    win.transform.localPosition = new Vector3(x, y, 0.51f);
                    win.transform.localScale = new Vector3(0.08f, 0.08f, 0.02f);
                    win.GetComponent<Renderer>().material = windowGlassMat;
                    SafeDestroy(win.GetComponent<Collider>());
                }
            }
        }

        private void BuildStreetDetails(Transform parent)
        {
            GameObject propsRoot = new GameObject("Street_Props");
            propsRoot.transform.SetParent(parent, false);

            // Streetlights
            float[] lightZ = new float[] { 15f, 32f, 50f, 68f };
            foreach (float z in lightZ)
            {
                CreateStreetlamp(propsRoot.transform, new Vector3(-7.2f, 0f, z));
                CreateStreetlamp(propsRoot.transform, new Vector3(7.2f, 0f, z));
            }

            // Parked Cars / Vans along curbs
            CreateVehicle(propsRoot.transform, new Vector3(-6.2f, 0.75f, 22f), new Color(0.18f, 0.35f, 0.55f));
            CreateVehicle(propsRoot.transform, new Vector3(6.2f, 0.75f, 48f), new Color(0.65f, 0.18f, 0.18f));
            CreateVehicle(propsRoot.transform, new Vector3(-6.2f, 0.95f, 62f), new Color(0.85f, 0.85f, 0.85f), isVan: true);
        }

        private void CreateStreetlamp(Transform parent, Vector3 pos)
        {
            GameObject lamp = new GameObject("Streetlight");
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = pos;

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            pole.transform.localScale = new Vector3(0.15f, 3.0f, 0.15f);
            pole.GetComponent<Renderer>().material = metalRooftopMat;

            // Arm
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.SetParent(lamp.transform, false);
            arm.transform.localPosition = new Vector3(0f, 5.9f, 0.4f);
            arm.transform.localScale = new Vector3(0.12f, 0.12f, 0.9f);
            arm.GetComponent<Renderer>().material = metalRooftopMat;

            // Lamp Light fixture
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 5.75f, 0.8f);
            bulb.transform.localScale = Vector3.one * 0.3f;
            bulb.GetComponent<Renderer>().material = roadMarkingMat;

            // Warm spotlight casting down onto street
            Light light = bulb.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1.0f, 0.92f, 0.75f);
            light.range = 14f;
            light.spotAngle = 70f;
            light.intensity = 1.8f;
            light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void CreateVehicle(Transform parent, Vector3 pos, Color carColor, bool isVan = false)
        {
            GameObject vehicle = new GameObject(isVan ? "DeliveryVan" : "Sedan");
            vehicle.transform.SetParent(parent, false);
            vehicle.transform.position = pos;

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(vehicle.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = isVan ? new Vector3(2.2f, 1.8f, 5.2f) : new Vector3(2.0f, 1.2f, 4.4f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            Material carMat = new Material(litShader) { color = carColor };
            body.GetComponent<Renderer>().material = carMat;
        }

        private void BuildEscapeCorridors(Transform parent)
        {
            GameObject escapeRoot = new GameObject("Escape_Corridors");
            escapeRoot.transform.SetParent(parent, false);

            // Corridor 1: North Highway Tunnel (Z = +88m)
            CreateEscapeCorridorNode(escapeRoot.transform, "North Highway Tunnel", new Vector3(0f, 1.5f, 88f), new Vector3(16f, 4f, 6f));

            // Corridor 2: East Subway Station (X = +42m, Z = 38m)
            CreateEscapeCorridorNode(escapeRoot.transform, "East Subway Station", new Vector3(42f, 1.5f, 38f), new Vector3(8f, 4f, 14f));

            // Corridor 3: West River Bridge (X = -42m, Z = 38m)
            CreateEscapeCorridorNode(escapeRoot.transform, "West River Bridge", new Vector3(-42f, 1.5f, 38f), new Vector3(8f, 4f, 14f));

            // Setup Spawn Nodes directly on street and sidewalk pathways
            CreateSpawnNode("Spawn_BoulevardSouth", new Vector3(0f, 0.1f, 18f));
            CreateSpawnNode("Spawn_BoulevardNorth", new Vector3(0f, 0.1f, 65f));
            CreateSpawnNode("Spawn_WestSidewalk", new Vector3(-8.0f, 0.2f, 25f));
            CreateSpawnNode("Spawn_EastSidewalk", new Vector3(8.0f, 0.2f, 25f));
            CreateSpawnNode("Spawn_AvenueEast", new Vector3(28f, 0.1f, 38f));
            CreateSpawnNode("Spawn_AvenueWest", new Vector3(-28f, 0.1f, 38f));
            CreateSpawnNode("Spawn_Intersection", new Vector3(0f, 0.1f, 38f));
            CreateSpawnNode("Spawn_NorthSidewalk", new Vector3(-8.0f, 0.2f, 55f));

            // Setup Waypoint Nodes along street intersections
            CreateWaypointNode("Waypoint_CenterIntersection", new Vector3(0f, 0.2f, 38f));
            CreateWaypointNode("Waypoint_WestCrosswalk", new Vector3(-8f, 0.2f, 38f));
            CreateWaypointNode("Waypoint_EastCrosswalk", new Vector3(8f, 0.2f, 38f));
            CreateWaypointNode("Waypoint_NorthBoulevard", new Vector3(0f, 0.2f, 60f));
        }

        private void CreateEscapeCorridorNode(Transform parent, string name, Vector3 pos, Vector3 triggerSize)
        {
            GameObject node = new GameObject($"EscapeZone_{name}");
            node.transform.SetParent(parent, false);
            node.transform.position = pos;

            BoxCollider box = node.AddComponent<BoxCollider>();
            box.size = triggerSize;
            box.isTrigger = true;

            var escZone = node.AddComponent<DEADeyeEscapeZone>();
            escapePoints.Add(node.transform);

            // Visual Barrier / Hazard Sign
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "EscapeSign";
            sign.transform.SetParent(node.transform, false);
            sign.transform.localPosition = new Vector3(0f, 2f, 0f);
            sign.transform.localScale = new Vector3(triggerSize.x * 0.8f, 0.6f, 0.2f);
            sign.GetComponent<Renderer>().material = roadMarkingMat;
            SafeDestroy(sign.GetComponent<Collider>());
        }

        private void CreateSpawnNode(string name, Vector3 pos)
        {
            GameObject spawn = new GameObject(name);
            spawn.transform.SetParent(transform, false);
            spawn.transform.position = pos;
            spawnPoints.Add(spawn.transform);
        }

        private void CreateWaypointNode(string name, Vector3 pos)
        {
            GameObject wp = new GameObject(name);
            wp.transform.SetParent(transform, false);
            wp.transform.position = pos;
            waypoints.Add(wp.transform);
        }

        private void BuildRooftopSniperNest(Transform parent)
        {
            GameObject nest = new GameObject("Rooftop_SniperNest");
            nest.transform.SetParent(parent, false);

            // The Tower Building below the rooftop
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "SniperTowerBase";
            tower.transform.SetParent(nest.transform, false);
            tower.transform.position = new Vector3(0f, rooftopHeight * 0.5f, -6f);
            tower.transform.localScale = new Vector3(14f, rooftopHeight, 12f);
            tower.GetComponent<Renderer>().material = buildingConcreteMat;

            // Rooftop Floor (Player Standing Area at Y = rooftopHeight)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "RooftopFloor";
            floor.transform.SetParent(nest.transform, false);
            floor.transform.position = new Vector3(0f, rooftopHeight - 0.15f, -6f);
            floor.transform.localScale = new Vector3(12f, 0.3f, 10f);
            floor.GetComponent<Renderer>().material = metalRooftopMat;

            // Concrete Parapet Walls (Height: 1.15m - ideal rifle resting height)
            CreateParapetWall(nest.transform, new Vector3(0f, rooftopHeight + 0.55f, -1.1f), new Vector3(12f, 1.1f, 0.4f), "FrontParapet_OverlookingCity");
            CreateParapetWall(nest.transform, new Vector3(0f, rooftopHeight + 0.55f, -10.9f), new Vector3(12f, 1.1f, 0.4f), "BackParapet");
            CreateParapetWall(nest.transform, new Vector3(-5.9f, rooftopHeight + 0.55f, -6f), new Vector3(0.4f, 1.1f, 10f), "LeftParapet");
            CreateParapetWall(nest.transform, new Vector3(5.9f, rooftopHeight + 0.55f, -6f), new Vector3(0.4f, 1.1f, 10f), "RightParapet");

            // CRITICAL SAFETY COLLISION BOUNDARY (Invisible walls up to 4m height)
            CreateSafetyBoundary(nest.transform, new Vector3(0f, rooftopHeight + 2.0f, -0.9f), new Vector3(12f, 4f, 0.3f));
            CreateSafetyBoundary(nest.transform, new Vector3(0f, rooftopHeight + 2.0f, -11.1f), new Vector3(12f, 4f, 0.3f));
            CreateSafetyBoundary(nest.transform, new Vector3(-6.1f, rooftopHeight + 2.0f, -6f), new Vector3(0.3f, 4f, 10f));
            CreateSafetyBoundary(nest.transform, new Vector3(6.1f, rooftopHeight + 2.0f, -6f), new Vector3(0.3f, 4f, 10f));

            // Weapon Rest Sandbags / Parapet Notch
            GameObject sandbag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandbag.name = "SniperWeaponRest";
            sandbag.transform.SetParent(nest.transform, false);
            sandbag.transform.position = new Vector3(0f, rooftopHeight + 1.15f, -1.1f);
            sandbag.transform.localScale = new Vector3(1.6f, 0.18f, 0.5f);
            sandbag.GetComponent<Renderer>().material = sidewalkMaterial;

            // HVAC Unit on Rooftop
            GameObject hvac = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hvac.name = "HVAC_Unit";
            hvac.transform.SetParent(nest.transform, false);
            hvac.transform.position = new Vector3(-3.5f, rooftopHeight + 0.9f, -7.5f);
            hvac.transform.localScale = new Vector3(2.2f, 1.8f, 2.2f);
            hvac.GetComponent<Renderer>().material = metalRooftopMat;

            // Communication Antenna Mast
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "AntennaMast";
            antenna.transform.SetParent(nest.transform, false);
            antenna.transform.position = new Vector3(4.5f, rooftopHeight + 3.0f, -9.0f);
            antenna.transform.localScale = new Vector3(0.12f, 6.0f, 0.12f);
            antenna.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(antenna.GetComponent<Collider>());
        }

        private void CreateParapetWall(Transform parent, Vector3 pos, Vector3 size, string name)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = size;
            wall.GetComponent<Renderer>().material = buildingConcreteMat;
        }

        private void CreateSafetyBoundary(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject boundary = new GameObject("SafetyBoundary_Invisible");
            boundary.transform.SetParent(parent, false);
            boundary.transform.position = pos;
            BoxCollider col = boundary.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = false; // Physical solid barrier
        }
    }
}

