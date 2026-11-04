using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

namespace DEADeye
{
    /// <summary>
    /// Constructs the rich, multi-district interconnected urban tactical sandbox:
    /// 1. Central Commercial Plaza with Comms Tower, Jammer device, Power Junction prop, Alarm Bell, and Fountain cover.
    /// 2. Residential Neighbourhood with varied brownstone silhouettes, narrow alleys, and shootable Security Gate.
    /// 3. Transport Interchange with 4-lane boulevard, bus transit shelter, parked vehicles, and Convoy Evac route.
    /// 4. Construction Site with 4-floor skeleton, tower crane, scaffolding, and Distraction Transformer prop.
    /// 5. 3 Walkable Overwatch Positions connected by solid steel bridges and catwalks with physical railings,
    ///    allowing the player to freely reposition between vantage points using WASD locomotion!
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
        [SerializeField] private Material roadStripeWhiteMat;
        [SerializeField] private Material metalRooftopMat;
        [SerializeField] private Material facadeTrimMat;
        [SerializeField] private Material craneYellowMat;
        [SerializeField] private Material beaconRedMat;
        [SerializeField] private Material highwayGreenMat;
        [SerializeField] private Material marketClothMat;

        [SerializeField] private Material windowLitMat;
        [SerializeField] private Material foliageGreenMat;
        [SerializeField] private Material treeBarkBrownMat;
        [SerializeField] private Material vehiclePaintMat;

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
            if (generateOnAwake)
            {
                BuildCityEnvironment();
            }
            else
            {
                CollectExistingNodes();
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
            BuildCommercialPlazaAndCommsTower(cityRoot.transform);
            BuildConstructionDistrict(cityRoot.transform);
            BuildResidentialDetails(cityRoot.transform);
            BuildOverheadHighwaySign(cityRoot.transform);
            BuildEscapeCorridors(cityRoot.transform);
            BuildMultiOverwatchNetwork(cityRoot.transform);

            // Rebuild NavMesh on newly generated streets & sidewalks
            NavMeshSurface navSurface = GetComponent<NavMeshSurface>();
            if (navSurface != null)
            {
                navSurface.BuildNavMesh();
            }

            // Register nodes with Mission Director if present
            if (DEADeyeMissionDirector.Instance != null)
            {
                DEADeyeMissionDirector.Instance.SetSpawnPoints(spawnPoints, waypoints, escapePoints);
            }
        }

        private void ConfigureMat(Material mat, Color col, float smoothness = 0.15f, float metallic = 0.0f)
        {
            mat.color = col;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_SpecularHighlights", 1.0f);
            mat.SetFloat("_EnvironmentReflections", 1.0f);
            mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
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
                ConfigureMat(asphaltMaterial, new Color(0.18f, 0.19f, 0.20f), 0.12f, 0.0f);
            }
            if (sidewalkMaterial == null)
            {
                sidewalkMaterial = new Material(litShader) { name = "Mat_Sidewalk" };
                ConfigureMat(sidewalkMaterial, new Color(0.72f, 0.72f, 0.70f), 0.18f, 0.0f);
            }
            if (buildingConcreteMat == null)
            {
                buildingConcreteMat = new Material(litShader) { name = "Mat_BuildingConcrete" };
                ConfigureMat(buildingConcreteMat, new Color(0.42f, 0.44f, 0.46f), 0.16f, 0.05f);
            }
            if (buildingBrickMat == null)
            {
                buildingBrickMat = new Material(litShader) { name = "Mat_BuildingBrick" };
                ConfigureMat(buildingBrickMat, new Color(0.52f, 0.26f, 0.20f), 0.12f, 0.0f);
            }
            if (windowGlassMat == null)
            {
                windowGlassMat = new Material(litShader) { name = "Mat_WindowGlass" };
                ConfigureMat(windowGlassMat, new Color(0.10f, 0.18f, 0.26f), 0.88f, 0.15f);
            }
            if (windowLitMat == null)
            {
                windowLitMat = new Material(litShader) { name = "Mat_WindowLit" };
                ConfigureMat(windowLitMat, new Color(0.96f, 0.84f, 0.48f), 0.35f, 0.0f);
            }
            if (foliageGreenMat == null)
            {
                foliageGreenMat = new Material(litShader) { name = "Mat_FoliageGreen" };
                ConfigureMat(foliageGreenMat, new Color(0.20f, 0.45f, 0.18f), 0.10f, 0.0f);
            }
            if (treeBarkBrownMat == null)
            {
                treeBarkBrownMat = new Material(litShader) { name = "Mat_TreeBark" };
                ConfigureMat(treeBarkBrownMat, new Color(0.32f, 0.22f, 0.15f), 0.08f, 0.0f);
            }
            if (roadMarkingMat == null)
            {
                roadMarkingMat = new Material(unlitShader) { name = "Mat_RoadMarkingYellow" };
                roadMarkingMat.color = new Color(0.98f, 0.85f, 0.18f); // High-visibility road yellow
            }
            if (roadStripeWhiteMat == null)
            {
                roadStripeWhiteMat = new Material(unlitShader) { name = "Mat_RoadStripeWhite" };
                roadStripeWhiteMat.color = new Color(0.95f, 0.96f, 0.98f);
            }
            if (metalRooftopMat == null)
            {
                metalRooftopMat = new Material(litShader) { name = "Mat_RooftopMetal" };
                ConfigureMat(metalRooftopMat, new Color(0.24f, 0.26f, 0.28f), 0.35f, 0.65f);
            }
            if (facadeTrimMat == null)
            {
                facadeTrimMat = new Material(litShader) { name = "Mat_FacadeTrim" };
                ConfigureMat(facadeTrimMat, new Color(0.20f, 0.22f, 0.24f), 0.22f, 0.1f);
            }
            if (craneYellowMat == null)
            {
                craneYellowMat = new Material(litShader) { name = "Mat_CraneYellow" };
                ConfigureMat(craneYellowMat, new Color(0.95f, 0.68f, 0.08f), 0.25f, 0.0f);
            }
            if (beaconRedMat == null)
            {
                beaconRedMat = new Material(unlitShader) { name = "Mat_BeaconRed" };
                beaconRedMat.color = new Color(1.0f, 0.15f, 0.15f);
            }
            if (highwayGreenMat == null)
            {
                highwayGreenMat = new Material(litShader) { name = "Mat_HighwayGreen" };
                ConfigureMat(highwayGreenMat, new Color(0.08f, 0.44f, 0.24f), 0.2f, 0.0f);
            }
            if (marketClothMat == null)
            {
                marketClothMat = new Material(litShader) { name = "Mat_MarketCloth" };
                ConfigureMat(marketClothMat, new Color(0.85f, 0.28f, 0.22f), 0.1f, 0.0f);
            }
            if (vehiclePaintMat == null)
            {
                vehiclePaintMat = new Material(litShader) { name = "Mat_VehiclePaint" };
                ConfigureMat(vehiclePaintMat, new Color(0.22f, 0.45f, 0.75f), 0.78f, 0.4f);
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
            ground.transform.position = new Vector3(0f, 0f, 45f);
            ground.transform.localScale = new Vector3(18f, 1f, 18f); // 180m x 180m
            ground.GetComponent<Renderer>().material = asphaltMaterial;

            // Route 1: Main Central Boulevard (North-South, 14m wide)
            CreateRoadMarkings(streetsRoot.transform, new Vector3(-0.15f, 0.02f, 45f), 120f, isVertical: true);
            CreateRoadMarkings(streetsRoot.transform, new Vector3(0.15f, 0.02f, 45f), 120f, isVertical: true);

            // Route 2: Cross Avenue (East-West at Z = 38m, 12m wide)
            CreateRoadMarkings(streetsRoot.transform, new Vector3(0f, 0.02f, 38f), 100f, isVertical: false);

            // Route 3: Residential Side Street (East-West at Z = 22m, connecting West alley to Plaza)
            CreateRoadMarkings(streetsRoot.transform, new Vector3(-18f, 0.02f, 22f), 40f, isVertical: false);

            // Raised Sidewalks along Main Boulevard
            CreateSidewalk(streetsRoot.transform, new Vector3(-8.5f, 0.08f, 45f), new Vector3(3f, 0.16f, 120f));
            CreateSidewalk(streetsRoot.transform, new Vector3(8.5f, 0.08f, 45f), new Vector3(3f, 0.16f, 120f));

            // Cross Sidewalks along Cross Avenue
            CreateSidewalk(streetsRoot.transform, new Vector3(-25f, 0.08f, 31f), new Vector3(30f, 0.16f, 2.5f));
            CreateSidewalk(streetsRoot.transform, new Vector3(25f, 0.08f, 31f), new Vector3(30f, 0.16f, 2.5f));
            CreateSidewalk(streetsRoot.transform, new Vector3(-25f, 0.08f, 45f), new Vector3(30f, 0.16f, 2.5f));
            CreateSidewalk(streetsRoot.transform, new Vector3(25f, 0.08f, 45f), new Vector3(30f, 0.16f, 2.5f));

            // Crosswalks at intersection
            CreateCrosswalk(streetsRoot.transform, new Vector3(0f, 0.03f, 30f), 12f);
            CreateCrosswalk(streetsRoot.transform, new Vector3(0f, 0.03f, 46f), 12f);
            CreateCrosswalk(streetsRoot.transform, new Vector3(-7f, 0.03f, 38f), 10f, isNorthSouth: false);
            CreateCrosswalk(streetsRoot.transform, new Vector3(7f, 0.03f, 38f), 10f, isNorthSouth: false);

            // Transit Bus Stop Shelter at X = 8.5m, Z = 62m
            BuildTransitBusShelter(streetsRoot.transform, new Vector3(9.5f, 0.08f, 62f));
            CreateBus(streetsRoot.transform, new Vector3(8.5f, 0f, 74f), new Color(0.12f, 0.42f, 0.85f));
            CreateCrosswalk(streetsRoot.transform, new Vector3(0f, 0.03f, 68f), 14f, isNorthSouth: true);
        }

        private void BuildTransitBusShelter(Transform parent, Vector3 pos)
        {
            GameObject shelter = new GameObject("TransitBusShelter");
            shelter.transform.SetParent(parent, false);
            shelter.transform.position = pos;

            // Canopy Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "CanopyRoof";
            roof.transform.SetParent(shelter.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            roof.transform.localScale = new Vector3(2.5f, 0.15f, 6.0f);
            roof.GetComponent<Renderer>().material = facadeTrimMat;

            // Glass Back Wall
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "GlassWall";
            glass.transform.SetParent(shelter.transform, false);
            glass.transform.localPosition = new Vector3(1.1f, 1.3f, 0f);
            glass.transform.localScale = new Vector3(0.1f, 2.5f, 5.8f);
            glass.GetComponent<Renderer>().material = windowGlassMat;

            // Bench
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "ShelterBench";
            bench.transform.SetParent(shelter.transform, false);
            bench.transform.localPosition = new Vector3(0.5f, 0.45f, 0f);
            bench.transform.localScale = new Vector3(0.6f, 0.45f, 4.0f);
            bench.GetComponent<Renderer>().material = buildingBrickMat;
        }

        private void CreateRoadMarkings(Transform parent, Vector3 center, float length, bool isVertical)
        {
            GameObject marking = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marking.name = isVertical ? "CenterLine_V" : "CenterLine_H";
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

        private void CreateCrosswalk(Transform parent, Vector3 pos, float width, bool isNorthSouth = true)
        {
            int stripeCount = 6;
            float spacing = width / stripeCount;
            for (int i = 0; i < stripeCount; i++)
            {
                float offset = (i - (stripeCount * 0.5f) + 0.5f) * spacing;
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = $"CrosswalkStripe_{i}";
                stripe.transform.SetParent(parent, false);
                if (isNorthSouth)
                {
                    stripe.transform.position = new Vector3(pos.x + offset, pos.y, pos.z);
                    stripe.transform.localScale = new Vector3(0.65f, 0.02f, 3.2f);
                }
                else
                {
                    stripe.transform.position = new Vector3(pos.x, pos.y, pos.z + offset);
                    stripe.transform.localScale = new Vector3(3.2f, 0.02f, 0.65f);
                }
                stripe.GetComponent<Renderer>().material = roadStripeWhiteMat;
                SafeDestroy(stripe.GetComponent<Collider>());
            }
        }

        private void BuildUrbanBuildings(Transform parent)
        {
            GameObject buildingsRoot = new GameObject("Buildings_Skyline");
            buildingsRoot.transform.SetParent(parent, false);

            // Left Block (West Side - Residential & Construction)
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(-24f, 0f, 18f), new Vector3(18f, 22f, 22f), buildingBrickMat, "WestBrownstone_Front");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(-26f, 0f, 56f), new Vector3(20f, 32f, 26f), buildingConcreteMat, "WestTower_Mid");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(-24f, 0f, 88f), new Vector3(18f, 20f, 24f), buildingBrickMat, "WestOffice_North");

            // Right Block (East Side - Commercial Plaza & Comms)
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(24f, 0f, 20f), new Vector3(18f, 25f, 24f), buildingConcreteMat, "EastCommsBuilding_Front");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(26f, 0f, 58f), new Vector3(20f, 24f, 28f), buildingBrickMat, "EastCorporate_Mid");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(24f, 0f, 90f), new Vector3(18f, 34f, 24f), buildingConcreteMat, "EastSkyscraper_North");

            // Distant Background Skyscrapers (Atmospheric skyline depth)
            CreateBackgroundTower(buildingsRoot.transform, new Vector3(-55f, 0f, 115f), new Vector3(32f, 60f, 36f), buildingConcreteMat);
            CreateBackgroundTower(buildingsRoot.transform, new Vector3(0f, 0f, 125f), new Vector3(38f, 72f, 34f), buildingBrickMat);
            CreateBackgroundTower(buildingsRoot.transform, new Vector3(55f, 0f, 120f), new Vector3(34f, 58f, 34f), buildingConcreteMat);
        }

        private void CreateCompositeBuilding(Transform parent, Vector3 groundPos, Vector3 size, Material facadeMat, string name)
        {
            GameObject bldg = new GameObject(name);
            bldg.transform.SetParent(parent, false);
            bldg.transform.position = groundPos;

            // 1. Main Facade Block
            GameObject mainBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainBlock.name = "MainFacade";
            mainBlock.transform.SetParent(bldg.transform, false);
            mainBlock.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            mainBlock.transform.localScale = size;
            mainBlock.GetComponent<Renderer>().material = facadeMat;

            // 2. Commercial Ground Storefront Podium (Glass & Trim)
            float podiumHeight = 4.2f;
            GameObject podiumGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            podiumGlass.name = "StorefrontPodium";
            podiumGlass.transform.SetParent(bldg.transform, false);
            podiumGlass.transform.localPosition = new Vector3(0f, podiumHeight * 0.5f, 0f);
            podiumGlass.transform.localScale = new Vector3(size.x + 0.15f, podiumHeight, size.z + 0.15f);
            podiumGlass.GetComponent<Renderer>().material = windowGlassMat;
            SafeDestroy(podiumGlass.GetComponent<Collider>());

            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "StorefrontCanopy";
            canopy.transform.SetParent(bldg.transform, false);
            canopy.transform.localPosition = new Vector3(0f, podiumHeight + 0.15f, 0f);
            canopy.transform.localScale = new Vector3(size.x + 0.6f, 0.3f, size.z + 0.6f);
            canopy.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(canopy.GetComponent<Collider>());

            // 3. Facade Window Bands & Discrete Panes
            int floorCount = Mathf.FloorToInt((size.y - podiumHeight) / 3.6f);
            float sillX = groundPos.x < 0 ? (size.x * 0.5f + 0.12f) : (-size.x * 0.5f - 0.12f);
            int windowCount = Mathf.FloorToInt((size.z - 2.5f) / 3.4f);

            for (int f = 1; f <= floorCount; f++)
            {
                float floorY = podiumHeight + (f * 3.6f) - 1.2f;
                if (floorY > size.y - 1.5f) break;

                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = $"WindowBand_F{f}";
                band.transform.SetParent(bldg.transform, false);
                band.transform.localPosition = new Vector3(0f, floorY, 0f);
                band.transform.localScale = new Vector3(size.x + 0.08f, 1.4f, size.z + 0.08f);
                band.GetComponent<Renderer>().material = windowGlassMat;
                SafeDestroy(band.GetComponent<Collider>());

                GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = $"Trim_F{f}";
                trim.transform.SetParent(bldg.transform, false);
                trim.transform.localPosition = new Vector3(0f, floorY - 0.75f, 0f);
                trim.transform.localScale = new Vector3(size.x + 0.2f, 0.2f, size.z + 0.2f);
                trim.GetComponent<Renderer>().material = facadeTrimMat;
                SafeDestroy(trim.GetComponent<Collider>());

                // Discrete window frames on the street face
                for (int w = 0; w < windowCount; w++)
                {
                    float winZ = -((windowCount - 1) * 1.7f) + (w * 3.4f);

                    // Stone Sill
                    GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sill.name = $"Sill_F{f}_W{w}";
                    sill.transform.SetParent(bldg.transform, false);
                    sill.transform.localPosition = new Vector3(sillX, floorY - 0.72f, winZ);
                    sill.transform.localScale = new Vector3(0.28f, 0.14f, 2.1f);
                    sill.GetComponent<Renderer>().material = facadeTrimMat;
                    SafeDestroy(sill.GetComponent<Collider>());

                    // Window Pane (alternating lit and reflective glass)
                    GameObject pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pane.name = $"Pane_F{f}_W{w}";
                    pane.transform.SetParent(bldg.transform, false);
                    pane.transform.localPosition = new Vector3(sillX, floorY, winZ);
                    pane.transform.localScale = new Vector3(0.18f, 1.3f, 1.95f);
                    bool isLit = ((f * 3 + w * 2) % 4 == 0);
                    pane.GetComponent<Renderer>().material = isLit ? windowLitMat : windowGlassMat;
                    SafeDestroy(pane.GetComponent<Collider>());
                }
            }

            // 4. Roof Cornice
            GameObject cornice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornice.name = "RoofCornice";
            cornice.transform.SetParent(bldg.transform, false);
            cornice.transform.localPosition = new Vector3(0f, size.y + 0.3f, 0f);
            cornice.transform.localScale = new Vector3(size.x + 0.5f, 0.6f, size.z + 0.5f);
            cornice.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(cornice.GetComponent<Collider>());

            // 5. Rooftop Equipment Structures
            AddRooftopEquipment(bldg.transform, size, name);
        }

        private void AddRooftopEquipment(Transform bldg, Vector3 size, string name)
        {
            // 1. Stairwell Penthouse Bulkhead
            GameObject penthouse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            penthouse.name = "RooftopPenthouseBulkhead";
            penthouse.transform.SetParent(bldg, false);
            penthouse.transform.localPosition = new Vector3(size.x * 0.18f, size.y + 1.6f, -size.z * 0.22f);
            penthouse.transform.localScale = new Vector3(4.2f, 3.2f, 4.2f);
            penthouse.GetComponent<Renderer>().material = buildingConcreteMat;

            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "PenthouseDoor";
            door.transform.SetParent(penthouse.transform, false);
            door.transform.localPosition = new Vector3(0f, -0.4f, 0.52f);
            door.transform.localScale = new Vector3(0.35f, 0.7f, 0.05f);
            door.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(door.GetComponent<Collider>());

            // 2. Commercial HVAC Chiller Unit
            GameObject hvac = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hvac.name = "HVAC_ChillerUnit";
            hvac.transform.SetParent(bldg, false);
            hvac.transform.localPosition = new Vector3(-size.x * 0.2f, size.y + 1.1f, size.z * 0.2f);
            hvac.transform.localScale = new Vector3(3.2f, 2.2f, 2.4f);
            hvac.GetComponent<Renderer>().material = metalRooftopMat;

            for (int fan = -1; fan <= 1; fan += 2)
            {
                GameObject cowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cowl.name = $"FanCowl_{fan}";
                cowl.transform.SetParent(hvac.transform, false);
                cowl.transform.localPosition = new Vector3(0f, 0.55f, fan * 0.25f);
                cowl.transform.localScale = new Vector3(0.35f, 0.12f, 0.35f);
                cowl.GetComponent<Renderer>().material = facadeTrimMat;
                SafeDestroy(cowl.GetComponent<Collider>());
            }

            // 3. Water Tower (on residential / corporate mid buildings)
            if (name.Contains("Brownstone") || name.Contains("Corporate"))
            {
                GameObject waterTower = new GameObject("RooftopWaterTower");
                waterTower.transform.SetParent(bldg, false);
                waterTower.transform.localPosition = new Vector3(size.x * 0.22f, size.y, size.z * 0.22f);

                float legHeight = 3.6f;
                Vector3[] legOffsets = new Vector3[]
                {
                    new Vector3(-1.1f, legHeight * 0.5f, -1.1f),
                    new Vector3(1.1f, legHeight * 0.5f, -1.1f),
                    new Vector3(-1.1f, legHeight * 0.5f, 1.1f),
                    new Vector3(1.1f, legHeight * 0.5f, 1.1f)
                };
                foreach (var lo in legOffsets)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = "StiltLeg";
                    leg.transform.SetParent(waterTower.transform, false);
                    leg.transform.localPosition = lo;
                    leg.transform.localScale = new Vector3(0.12f, legHeight * 0.5f, 0.12f);
                    leg.GetComponent<Renderer>().material = metalRooftopMat;
                    SafeDestroy(leg.GetComponent<Collider>());
                }

                GameObject tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tank.name = "WaterTank";
                tank.transform.SetParent(waterTower.transform, false);
                tank.transform.localPosition = new Vector3(0f, legHeight + 1.5f, 0f);
                tank.transform.localScale = new Vector3(2.8f, 1.5f, 2.8f);
                tank.GetComponent<Renderer>().material = buildingBrickMat;

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "TankRoofCap";
                cap.transform.SetParent(waterTower.transform, false);
                cap.transform.localPosition = new Vector3(0f, legHeight + 3.1f, 0f);
                cap.transform.localScale = new Vector3(3.0f, 0.25f, 3.0f);
                cap.GetComponent<Renderer>().material = facadeTrimMat;
                SafeDestroy(cap.GetComponent<Collider>());
            }

            // 4. Communication Antenna Spire (on tall towers)
            if (name.Contains("Tower") || name.Contains("Skyscraper") || name.Contains("Office"))
            {
                GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                antenna.name = "RooftopAntennaMast";
                antenna.transform.SetParent(bldg, false);
                antenna.transform.localPosition = new Vector3(-size.x * 0.25f, size.y + 4.5f, -size.z * 0.2f);
                antenna.transform.localScale = new Vector3(0.14f, 4.5f, 0.14f);
                antenna.GetComponent<Renderer>().material = metalRooftopMat;
                SafeDestroy(antenna.GetComponent<Collider>());

                GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                beacon.name = "AviationWarningBeacon";
                beacon.transform.SetParent(antenna.transform, false);
                beacon.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                beacon.transform.localScale = new Vector3(2.2f, 0.12f, 2.2f);
                beacon.GetComponent<Renderer>().material = beaconRedMat;
                SafeDestroy(beacon.GetComponent<Collider>());

                Light bl = beacon.AddComponent<Light>();
                bl.type = LightType.Point;
                bl.color = Color.red;
                bl.range = 24f;
                bl.intensity = 2.0f;
            }
        }

        private void CreateBackgroundTower(Transform parent, Vector3 groundPos, Vector3 size, Material facadeMat)
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = $"BackgroundSkyscraper_{groundPos.x:0}_{groundPos.z:0}";
            tower.transform.SetParent(parent, false);
            tower.transform.position = new Vector3(groundPos.x, size.y * 0.5f, groundPos.z);
            tower.transform.localScale = size;
            tower.GetComponent<Renderer>().material = facadeMat;
        }

        private void BuildStreetDetails(Transform parent)
        {
            GameObject propsRoot = new GameObject("Street_Props");
            propsRoot.transform.SetParent(parent, false);

            float[] lightZ = new float[] { 16f, 32f, 50f, 68f };
            foreach (float z in lightZ)
            {
                CreateStreetlamp(propsRoot.transform, new Vector3(-7.2f, 0f, z));
                CreateStreetlamp(propsRoot.transform, new Vector3(7.2f, 0f, z));
            }

            // Parked Cars / Vans along curbs
            CreateVehicle(propsRoot.transform, new Vector3(-6.2f, 0.75f, 22f), new Color(0.18f, 0.35f, 0.55f));
            CreateVehicle(propsRoot.transform, new Vector3(6.2f, 0.75f, 48f), new Color(0.75f, 0.15f, 0.15f));
            CreateVehicle(propsRoot.transform, new Vector3(-6.2f, 0.95f, 64f), new Color(0.88f, 0.88f, 0.88f), isVan: true);
            CreateVehicle(propsRoot.transform, new Vector3(6.2f, 0.75f, 28f), new Color(0.20f, 0.22f, 0.25f));

            // Street Trees in Sidewalk Planters
            CreateStreetTree(propsRoot.transform, new Vector3(-8.5f, 0.08f, 20f));
            CreateStreetTree(propsRoot.transform, new Vector3(8.5f, 0.08f, 20f));
            CreateStreetTree(propsRoot.transform, new Vector3(-8.5f, 0.08f, 56f));
            CreateStreetTree(propsRoot.transform, new Vector3(8.5f, 0.08f, 56f));
            CreateStreetTree(propsRoot.transform, new Vector3(-8.5f, 0.08f, 74f));
            CreateStreetTree(propsRoot.transform, new Vector3(8.5f, 0.08f, 74f));

            // Traffic Light Signal Poles at Major Intersections
            CreateTrafficLight(propsRoot.transform, new Vector3(-7.5f, 0f, 31f), 0f);
            CreateTrafficLight(propsRoot.transform, new Vector3(7.5f, 0f, 45f), 180f);
            CreateTrafficLight(propsRoot.transform, new Vector3(-7.5f, 0f, 67f), 0f);

            // Jersey Barriers
            CreateJerseyBarrier(propsRoot.transform, new Vector3(-5.5f, 0.45f, 36f));
            CreateJerseyBarrier(propsRoot.transform, new Vector3(5.5f, 0.45f, 36f));

            // Dumpsters
            CreateDumpster(propsRoot.transform, new Vector3(-11.5f, 0.75f, 32f));
            CreateDumpster(propsRoot.transform, new Vector3(11.5f, 0.75f, 44f));
        }

        private void BuildCommercialPlazaAndCommsTower(Transform parent)
        {
            GameObject plazaRoot = new GameObject("CommercialPlaza_And_CommsTower");
            plazaRoot.transform.SetParent(parent, false);

            // Plaza Ground Paving
            GameObject plazaPaving = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plazaPaving.name = "PlazaPaving";
            plazaPaving.transform.SetParent(plazaRoot.transform, false);
            plazaPaving.transform.position = new Vector3(22.5f, 0.05f, 36f);
            plazaPaving.transform.localScale = new Vector3(24f, 0.10f, 28f);
            plazaPaving.GetComponent<Renderer>().material = sidewalkMaterial;

            // 3-Tier Landmark Fountain Monument (Tactical cover in center of plaza)
            GameObject fountainRoot = new GameObject("PlazaFountainMonument");
            fountainRoot.transform.SetParent(plazaRoot.transform, false);
            fountainRoot.transform.position = new Vector3(22.5f, 0f, 28f);

            // Tier 1 Base Basin (Outer rim)
            GameObject tier1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tier1.name = "Fountain_Tier1_Basin";
            tier1.transform.SetParent(fountainRoot.transform, false);
            tier1.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            tier1.transform.localScale = new Vector3(6.5f, 0.4f, 6.5f);
            tier1.GetComponent<Renderer>().material = buildingConcreteMat;

            // Water Surface inside Basin
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Fountain_Water";
            water.transform.SetParent(fountainRoot.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            water.transform.localScale = new Vector3(5.8f, 0.05f, 5.8f);
            Shader litSh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material waterMat = new Material(litSh) { color = new Color(0.15f, 0.55f, 0.75f) };
            water.GetComponent<Renderer>().material = waterMat;
            SafeDestroy(water.GetComponent<Collider>());

            // Tier 2 Middle Bowl
            GameObject tier2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tier2.name = "Fountain_Tier2_Bowl";
            tier2.transform.SetParent(fountainRoot.transform, false);
            tier2.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            tier2.transform.localScale = new Vector3(3.8f, 0.35f, 3.8f);
            tier2.GetComponent<Renderer>().material = buildingConcreteMat;

            // Tier 3 Upper Basin
            GameObject tier3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tier3.name = "Fountain_Tier3_Upper";
            tier3.transform.SetParent(fountainRoot.transform, false);
            tier3.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            tier3.transform.localScale = new Vector3(2.0f, 0.3f, 2.0f);
            tier3.GetComponent<Renderer>().material = buildingConcreteMat;

            // Spire Finial
            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "Fountain_Spire";
            spire.transform.SetParent(fountainRoot.transform, false);
            spire.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            spire.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            spire.GetComponent<Renderer>().material = metalRooftopMat;

            // Park Benches around fountain
            CreatePlazaBench(plazaRoot.transform, new Vector3(17.5f, 0f, 28f), 90f);
            CreatePlazaBench(plazaRoot.transform, new Vector3(27.5f, 0f, 28f), -90f);
            CreatePlazaBench(plazaRoot.transform, new Vector3(22.5f, 0f, 23f), 0f);
            CreatePlazaBench(plazaRoot.transform, new Vector3(22.5f, 0f, 33f), 180f);

            // Planters with Greenery
            CreatePlazaPlanter(plazaRoot.transform, new Vector3(16f, 0f, 35f));
            CreatePlazaPlanter(plazaRoot.transform, new Vector3(29f, 0f, 35f));

            // Commercial Shopfronts with Awnings along Plaza border (X = 34m)
            CreateShopfront(plazaRoot.transform, new Vector3(34.2f, 0f, 26f), "METRO DELI & BAKERY", new Color(0.85f, 0.25f, 0.2f));
            CreateShopfront(plazaRoot.transform, new Vector3(34.2f, 0f, 36f), "CITY PHARMACY", new Color(0.2f, 0.65f, 0.85f));
            CreateShopfront(plazaRoot.transform, new Vector3(34.2f, 0f, 46f), "TECH & COMMS REPAIR", new Color(0.2f, 0.75f, 0.35f));

            // 1. High-Tech Municipal Relay Comms Tower
            Vector3 towerBasePos = new Vector3(22.5f, 0f, 40f);
            GameObject commsTower = new GameObject("CommunicationsRelayTower");
            commsTower.transform.SetParent(plazaRoot.transform, false);
            commsTower.transform.position = towerBasePos;

            // Concrete Base Pedestal
            GameObject basePedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePedestal.name = "TowerPedestal";
            basePedestal.transform.SetParent(commsTower.transform, false);
            basePedestal.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            basePedestal.transform.localScale = new Vector3(6.5f, 0.7f, 6.5f);
            basePedestal.GetComponent<Renderer>().material = buildingConcreteMat;

            // 4 Steel Strut Legs reaching 24m high
            float strutHeight = 24f;
            Vector3[] strutOffsets = new Vector3[]
            {
                new Vector3(-2.2f, 0f, -2.2f),
                new Vector3(2.2f, 0f, -2.2f),
                new Vector3(-2.2f, 0f, 2.2f),
                new Vector3(2.2f, 0f, 2.2f)
            };
            foreach (var offset in strutOffsets)
            {
                GameObject strut = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strut.name = "TowerLeg";
                strut.transform.SetParent(commsTower.transform, false);
                strut.transform.localPosition = new Vector3(offset.x * 0.5f, strutHeight * 0.5f, offset.z * 0.5f);
                strut.transform.localScale = new Vector3(0.22f, strutHeight * 0.5f, 0.22f);
                strut.GetComponent<Renderer>().material = metalRooftopMat;
                SafeDestroy(strut.GetComponent<Collider>());
            }

            // Central Antenna Mast Spire
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "CentralSpire";
            mast.transform.SetParent(commsTower.transform, false);
            mast.transform.localPosition = new Vector3(0f, 13f, 0f);
            mast.transform.localScale = new Vector3(0.18f, 13f, 0.18f);
            mast.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(mast.GetComponent<Collider>());

            // Red Flashing Safety Beacon atop Comms Tower
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "HazardBeacon";
            beacon.transform.SetParent(commsTower.transform, false);
            beacon.transform.localPosition = new Vector3(0f, strutHeight + 0.6f, 0f);
            beacon.transform.localScale = Vector3.one * 0.7f;
            beacon.GetComponent<Renderer>().material = beaconRedMat;
            SafeDestroy(beacon.GetComponent<Collider>());

            Light beaconLight = beacon.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = Color.red;
            beaconLight.range = 35f;
            beaconLight.intensity = 3.5f;

            // =========================================================================
            // OBJECTIVE: HOSTILE COMMUNICATIONS JAMMER DEVICE (Electronic Warfare Array)
            // =========================================================================
            GameObject jammer = new GameObject("CommunicationsJammer");
            jammer.transform.SetParent(commsTower.transform, false);
            jammer.transform.localPosition = new Vector3(0f, 0.35f, -2.2f);

            // 1. Heavy Transport Skid Base
            GameObject skid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skid.name = "JammerSkidBase";
            skid.transform.SetParent(jammer.transform, false);
            skid.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            skid.transform.localScale = new Vector3(3.2f, 0.4f, 2.4f);
            skid.GetComponent<Renderer>().material = metalRooftopMat;

            // 4 Outrigger Leveling Jacks
            for (int jx = -1; jx <= 1; jx += 2)
            {
                for (int jz = -1; jz <= 1; jz += 2)
                {
                    GameObject jack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    jack.name = $"LevelingJack_{jx}_{jz}";
                    jack.transform.SetParent(skid.transform, false);
                    jack.transform.localPosition = new Vector3(jx * 0.48f, -0.4f, jz * 0.45f);
                    jack.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
                    jack.GetComponent<Renderer>().material = craneYellowMat;
                    SafeDestroy(jack.GetComponent<Collider>());
                }
            }

            // 2. Parabolic High-Gain Satellite Dish (3.5m)
            GameObject dishAssembly = new GameObject("ParabolicDishAssembly");
            dishAssembly.transform.SetParent(jammer.transform, false);
            dishAssembly.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            dishAssembly.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

            // Dish Reflector Face
            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "ParabolicDishReflector";
            dish.transform.SetParent(dishAssembly.transform, false);
            dish.transform.localPosition = Vector3.zero;
            dish.transform.localScale = new Vector3(3.5f, 0.22f, 3.5f);
            dish.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(dish.GetComponent<Collider>());

            // Outer Rim Trim with Hazard Accent
            GameObject dishRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dishRim.name = "DishRim";
            dishRim.transform.SetParent(dishAssembly.transform, false);
            dishRim.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            dishRim.transform.localScale = new Vector3(3.6f, 0.14f, 3.6f);
            dishRim.GetComponent<Renderer>().material = craneYellowMat;
            SafeDestroy(dishRim.GetComponent<Collider>());

            // Feed Horn Focal Mast and Struts
            GameObject feedHorn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            feedHorn.name = "FeedHornEmitter";
            feedHorn.transform.SetParent(dishAssembly.transform, false);
            feedHorn.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            feedHorn.transform.localScale = new Vector3(0.4f, 0.45f, 0.4f);
            feedHorn.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(feedHorn.GetComponent<Collider>());

            for (int s = 0; s < 3; s++)
            {
                float ang = s * 120f * Mathf.Deg2Rad;
                Vector3 rimPos = new Vector3(Mathf.Cos(ang) * 1.6f, 0.1f, Mathf.Sin(ang) * 1.6f);
                Vector3 hornPos = new Vector3(0f, 1.6f, 0f);
                Vector3 strutMid = (rimPos + hornPos) * 0.5f;
                Vector3 strutDir = (hornPos - rimPos).normalized;

                GameObject strut = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strut.name = $"FeedStrut_{s}";
                strut.transform.SetParent(dishAssembly.transform, false);
                strut.transform.position = dishAssembly.transform.TransformPoint(strutMid);
                strut.transform.rotation = dishAssembly.transform.rotation * Quaternion.FromToRotation(Vector3.up, strutDir);
                strut.transform.localScale = new Vector3(0.08f, Vector3.Distance(rimPos, hornPos) * 0.5f, 0.08f);
                strut.GetComponent<Renderer>().material = facadeTrimMat;
                SafeDestroy(strut.GetComponent<Collider>());
            }

            // 3. Electronic Warfare Server Racks
            for (int r = -1; r <= 1; r += 2)
            {
                GameObject rack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rack.name = $"EWS_ServerRack_{r}";
                rack.transform.SetParent(jammer.transform, false);
                rack.transform.localPosition = new Vector3(r * 1.25f, 1.1f, -0.65f);
                rack.transform.localScale = new Vector3(0.65f, 1.8f, 0.75f);
                rack.GetComponent<Renderer>().material = facadeTrimMat;

                // Status LEDs
                Shader unlitSh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                GameObject ledGreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledGreen.name = "LED_Green";
                ledGreen.transform.SetParent(rack.transform, false);
                ledGreen.transform.localPosition = new Vector3(0f, 0.35f, -0.52f);
                ledGreen.transform.localScale = new Vector3(0.5f, 0.1f, 0.05f);
                ledGreen.GetComponent<Renderer>().material = new Material(unlitSh) { color = new Color(0.1f, 0.95f, 0.2f) };
                SafeDestroy(ledGreen.GetComponent<Collider>());

                GameObject ledAmber = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledAmber.name = "LED_Amber";
                ledAmber.transform.SetParent(rack.transform, false);
                ledAmber.transform.localPosition = new Vector3(0f, 0.15f, -0.52f);
                ledAmber.transform.localScale = new Vector3(0.5f, 0.1f, 0.05f);
                ledAmber.GetComponent<Renderer>().material = new Material(unlitSh) { color = new Color(0.95f, 0.65f, 0.1f) };
                SafeDestroy(ledAmber.GetComponent<Collider>());
            }

            // 4. Amber Hazard Warning Strobe
            GameObject jammerStrobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jammerStrobe.name = "JammerWarningStrobe";
            jammerStrobe.transform.SetParent(jammer.transform, false);
            jammerStrobe.transform.localPosition = new Vector3(0f, 3.8f, 0.4f);
            jammerStrobe.transform.localScale = Vector3.one * 0.45f;
            jammerStrobe.GetComponent<Renderer>().material = craneYellowMat;
            SafeDestroy(jammerStrobe.GetComponent<Collider>());

            Light strobeLight = jammerStrobe.AddComponent<Light>();
            strobeLight.type = LightType.Point;
            strobeLight.color = new Color(1.0f, 0.7f, 0.1f);
            strobeLight.range = 22f;
            strobeLight.intensity = 2.5f;

            // Box collider on Jammer for hits and spotting
            BoxCollider jammerCol = jammer.AddComponent<BoxCollider>();
            jammerCol.center = new Vector3(0f, 1.6f, 0f);
            jammerCol.size = new Vector3(3.6f, 3.4f, 3.2f);

            // Attach Recon Spotable to Jammer
            var jammerSpot = jammer.AddComponent<DEADeyeSpotable>();
            jammerSpot.SetTargetInfo("COMMUNICATIONS JAMMER", SpotClassification.Objective, "High-frequency electronic warfare array hijacking emergency broadcasts. Cut its power junction to restore signals.", 100);

            // =========================================================================
            // ENVIRONMENTAL PROP 1: POWER JUNCTION (Disables Jammer)
            // =========================================================================
            GameObject junctionBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            junctionBox.name = "PowerJunction_BreakerBox";
            junctionBox.transform.SetParent(commsTower.transform, false);
            junctionBox.transform.localPosition = new Vector3(-2.8f, 1.2f, 0f);
            junctionBox.transform.localScale = new Vector3(0.5f, 1.2f, 1.2f);
            junctionBox.GetComponent<Renderer>().material = craneYellowMat;

            var junctionProp = junctionBox.AddComponent<DEADeyeEnvironmentalProp>();
            junctionProp.Initialize(EnvironmentalPropType.PowerJunction, "COMMUNICATIONS POWER JUNCTION");

            // Visible Power Conduits leading from Jammer to Power Junction
            CreatePowerConduit(commsTower.transform, new Vector3(0f, 0.2f, -1.8f), new Vector3(-2.8f, 1.0f, 0f));

            // =========================================================================
            // ENVIRONMENTAL PROP 2: ALARM BELL (Prevents reinforcement alert)
            // =========================================================================
            GameObject alarmBox = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            alarmBox.name = "EmergencyAlarmBell";
            alarmBox.transform.SetParent(plazaRoot.transform, false);
            alarmBox.transform.position = new Vector3(14.5f, 2.8f, 32f);
            alarmBox.transform.localScale = Vector3.one * 0.7f;
            alarmBox.GetComponent<Renderer>().material = beaconRedMat;

            var alarmProp = alarmBox.AddComponent<DEADeyeEnvironmentalProp>();
            alarmProp.Initialize(EnvironmentalPropType.AlarmBell, "EMERGENCY SIREN ALARM");

            // Server consoles where Elena & Marcus work
            GameObject serverConsole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            serverConsole.name = "ServerConsole_Workstation";
            serverConsole.transform.SetParent(commsTower.transform, false);
            serverConsole.transform.localPosition = new Vector3(0f, 0.9f, 1.8f);
            serverConsole.transform.localScale = new Vector3(2.4f, 1.2f, 1.0f);
            serverConsole.GetComponent<Renderer>().material = metalRooftopMat;

            // Market Stalls in Plaza
            CreateMarketStall(plazaRoot.transform, new Vector3(16f, 0f, 24f), Color.red, "MarketStall_Produce");
            CreateMarketStall(plazaRoot.transform, new Vector3(28f, 0f, 24f), new Color(0.15f, 0.45f, 0.85f), "MarketStall_Electronics");
            CreateMarketStall(plazaRoot.transform, new Vector3(28f, 0f, 46f), new Color(0.18f, 0.75f, 0.35f), "MarketStall_Textiles");
        }

        private void CreateMarketStall(Transform parent, Vector3 pos, Color stripeColor, string name)
        {
            GameObject stall = new GameObject(name);
            stall.transform.SetParent(parent, false);
            stall.transform.position = pos;

            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Counter";
            table.transform.SetParent(stall.transform, false);
            table.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            table.transform.localScale = new Vector3(3.2f, 1.0f, 1.6f);
            table.GetComponent<Renderer>().material = buildingBrickMat;

            // Striped Canopy
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy_Base";
            canopy.transform.SetParent(stall.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            canopy.transform.localScale = new Vector3(3.6f, 0.12f, 2.4f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material baseMat = new Material(litShader) { color = Color.white };
            canopy.GetComponent<Renderer>().material = baseMat;
            SafeDestroy(canopy.GetComponent<Collider>());

            // 3 Colored Stripes across canopy
            for (int s = -1; s <= 1; s++)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = $"Stripe_{s}";
                stripe.transform.SetParent(canopy.transform, false);
                stripe.transform.localPosition = new Vector3(s * 0.32f, 0.02f, 0f);
                stripe.transform.localScale = new Vector3(0.25f, 1.1f, 1.02f);
                stripe.GetComponent<Renderer>().material = new Material(litShader) { color = stripeColor };
                SafeDestroy(stripe.GetComponent<Collider>());
            }
        }

        private void CreatePlazaBench(Transform parent, Vector3 pos, float rotationY)
        {
            GameObject bench = new GameObject("PlazaBench");
            bench.transform.SetParent(parent, false);
            bench.transform.position = pos;
            bench.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.SetParent(bench.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            seat.transform.localScale = new Vector3(1.8f, 0.1f, 0.6f);
            seat.GetComponent<Renderer>().material = buildingBrickMat;

            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Backrest";
            back.transform.SetParent(bench.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.85f, -0.28f);
            back.transform.localScale = new Vector3(1.8f, 0.6f, 0.08f);
            back.GetComponent<Renderer>().material = buildingBrickMat;
            SafeDestroy(back.GetComponent<Collider>());
        }

        private void CreatePlazaPlanter(Transform parent, Vector3 pos)
        {
            GameObject planter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            planter.name = "PlazaPlanter";
            planter.transform.SetParent(parent, false);
            planter.transform.position = pos + new Vector3(0f, 0.4f, 0f);
            planter.transform.localScale = new Vector3(1.6f, 0.8f, 1.6f);
            planter.GetComponent<Renderer>().material = buildingConcreteMat;

            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Foliage";
            bush.transform.SetParent(planter.transform, false);
            bush.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            bush.transform.localScale = new Vector3(1.4f, 0.9f, 1.4f);
            bush.GetComponent<Renderer>().material = highwayGreenMat;
            SafeDestroy(bush.GetComponent<Collider>());
        }

        private void CreateShopfront(Transform parent, Vector3 pos, string signName, Color awningCol)
        {
            GameObject shop = new GameObject($"Shopfront_{signName}");
            shop.transform.SetParent(parent, false);
            shop.transform.position = pos;

            // Storefront Display Window
            GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            win.name = "StoreDisplayWindow";
            win.transform.SetParent(shop.transform, false);
            win.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            win.transform.localScale = new Vector3(0.1f, 3.2f, 7.0f);
            win.GetComponent<Renderer>().material = windowGlassMat;
            SafeDestroy(win.GetComponent<Collider>());

            // Fabric Awning
            GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awning.name = "StoreAwning";
            awning.transform.SetParent(shop.transform, false);
            awning.transform.localPosition = new Vector3(-0.9f, 3.6f, 0f);
            awning.transform.localScale = new Vector3(1.8f, 0.12f, 7.4f);
            awning.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material awMat = new Material(litShader) { color = awningCol };
            awning.GetComponent<Renderer>().material = awMat;
            SafeDestroy(awning.GetComponent<Collider>());
        }

        private void BuildConstructionDistrict(Transform parent)
        {
            GameObject constRoot = new GameObject("Construction_District_DeadGround");
            constRoot.transform.SetParent(parent, false);

            Vector3 sitePos = new Vector3(-26f, 0f, 72f);
            GameObject skeleton = new GameObject("UnfinishedSkyscraperSkeleton");
            skeleton.transform.SetParent(constRoot.transform, false);
            skeleton.transform.position = sitePos;

            // 4 Concrete Floor Slabs
            for (int f = 1; f <= 4; f++)
            {
                float floorY = f * 4.2f;
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"Slab_Floor_{f}";
                slab.transform.SetParent(skeleton.transform, false);
                slab.transform.localPosition = new Vector3(0f, floorY, 0f);
                slab.transform.localScale = new Vector3(20f, 0.4f, 22f);
                slab.GetComponent<Renderer>().material = buildingConcreteMat;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        pillar.name = $"SteelColumn_F{f}_{x}_{z}";
                        pillar.transform.SetParent(skeleton.transform, false);
                        pillar.transform.localPosition = new Vector3(x * 8.5f, floorY - 2.1f, z * 9.5f);
                        pillar.transform.localScale = new Vector3(0.55f, 4.2f, 0.55f);
                        pillar.GetComponent<Renderer>().material = metalRooftopMat;
                    }
                }
            }

            // Heavy Construction Tower Crane
            GameObject crane = new GameObject("TowerCrane");
            crane.transform.SetParent(constRoot.transform, false);
            crane.transform.position = new Vector3(-14f, 0f, 74f);

            GameObject craneMast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            craneMast.name = "CraneMast";
            craneMast.transform.SetParent(crane.transform, false);
            craneMast.transform.localPosition = new Vector3(0f, 14f, 0f);
            craneMast.transform.localScale = new Vector3(1.6f, 28f, 1.6f);
            craneMast.GetComponent<Renderer>().material = craneYellowMat;

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "CraneCabin";
            cabin.transform.SetParent(crane.transform, false);
            cabin.transform.localPosition = new Vector3(1.2f, 27.5f, 0f);
            cabin.transform.localScale = new Vector3(1.6f, 1.8f, 1.6f);
            cabin.GetComponent<Renderer>().material = windowGlassMat;
            SafeDestroy(cabin.GetComponent<Collider>());

            GameObject jibBoom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jibBoom.name = "JibBoom";
            jibBoom.transform.SetParent(crane.transform, false);
            jibBoom.transform.localPosition = new Vector3(6.5f, 29f, 0f);
            jibBoom.transform.localScale = new Vector3(22f, 1.2f, 1.2f);
            jibBoom.GetComponent<Renderer>().material = craneYellowMat;
            SafeDestroy(jibBoom.GetComponent<Collider>());

            // =========================================================================
            // ENVIRONMENTAL PROP 3: ELECTRICAL TRANSFORMER (Distraction)
            // =========================================================================
            GameObject transformerBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            transformerBox.name = "DistractionTransformer";
            transformerBox.transform.SetParent(constRoot.transform, false);
            transformerBox.transform.position = new Vector3(-12.5f, 3.2f, 54f);
            transformerBox.transform.localScale = new Vector3(0.9f, 1.4f, 0.9f);
            transformerBox.GetComponent<Renderer>().material = metalRooftopMat;

            var transProp = transformerBox.AddComponent<DEADeyeEnvironmentalProp>();
            transProp.Initialize(EnvironmentalPropType.DistractionTransformer, "ELECTRICAL TRANSFORMER DISTRACTION");
        }

        private void BuildResidentialDetails(Transform parent)
        {
            GameObject resRoot = new GameObject("Residential_Alley_Details");
            resRoot.transform.SetParent(parent, false);

            // Fire Escapes on West Facade (X = -15m)
            for (int f = 1; f <= 3; f++)
            {
                float y = f * 4.2f + 1.5f;
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = $"FireEscape_Platform_F{f}";
                platform.transform.SetParent(resRoot.transform, false);
                platform.transform.position = new Vector3(-14.2f, y, 32f);
                platform.transform.localScale = new Vector3(1.2f, 0.15f, 4.0f);
                platform.GetComponent<Renderer>().material = metalRooftopMat;

                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"FireEscape_Rail_F{f}";
                rail.transform.SetParent(platform.transform, false);
                rail.transform.localPosition = new Vector3(0.5f, 0.5f, 0f);
                rail.transform.localScale = new Vector3(0.08f, 1.0f, 1.0f);
                rail.GetComponent<Renderer>().material = metalRooftopMat;
                SafeDestroy(rail.GetComponent<Collider>());
            }

            // =========================================================================
            // ENVIRONMENTAL PROP 4: LOCKED SECURITY ALLEY GATE
            // =========================================================================
            GameObject gateRoot = new GameObject("SecurityGateBarrier");
            gateRoot.transform.SetParent(resRoot.transform, false);
            gateRoot.transform.position = new Vector3(-13.5f, 0f, 38f);

            // Gate Frame
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postL.transform.SetParent(gateRoot.transform, false);
            postL.transform.localPosition = new Vector3(0f, 1.8f, -2.4f);
            postL.transform.localScale = new Vector3(0.25f, 1.8f, 0.25f);
            postL.GetComponent<Renderer>().material = metalRooftopMat;

            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postR.transform.SetParent(gateRoot.transform, false);
            postR.transform.localPosition = new Vector3(0f, 1.8f, 2.4f);
            postR.transform.localScale = new Vector3(0.25f, 1.8f, 0.25f);
            postR.GetComponent<Renderer>().material = metalRooftopMat;

            // Liftable Gate Arm
            GameObject gateArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gateArm.name = "GateArm";
            gateArm.transform.SetParent(gateRoot.transform, false);
            gateArm.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            gateArm.transform.localScale = new Vector3(0.2f, 2.2f, 4.6f);
            gateArm.GetComponent<Renderer>().material = facadeTrimMat;

            // Shootable Control Panel Box
            GameObject ctrlBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ctrlBox.name = "GateControlPanel";
            ctrlBox.transform.SetParent(gateRoot.transform, false);
            ctrlBox.transform.localPosition = new Vector3(-0.35f, 1.4f, -2.4f);
            ctrlBox.transform.localScale = new Vector3(0.4f, 0.6f, 0.45f);
            ctrlBox.GetComponent<Renderer>().material = craneYellowMat;

            var gateProp = ctrlBox.AddComponent<DEADeyeEnvironmentalProp>();
            gateProp.Initialize(EnvironmentalPropType.SecurityGate, "SECURITY GATE CONTROL PANEL");
        }

        private void BuildOverheadHighwaySign(Transform parent)
        {
            GameObject gantryRoot = new GameObject("OverheadHighwayGantry");
            gantryRoot.transform.SetParent(parent, false);
            gantryRoot.transform.position = new Vector3(0f, 0f, 55f);

            GameObject colLeft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            colLeft.name = "GantryCol_Left";
            colLeft.transform.SetParent(gantryRoot.transform, false);
            colLeft.transform.localPosition = new Vector3(-7.5f, 3.5f, 0f);
            colLeft.transform.localScale = new Vector3(0.35f, 3.5f, 0.35f);
            colLeft.GetComponent<Renderer>().material = metalRooftopMat;

            GameObject colRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            colRight.name = "GantryCol_Right";
            colRight.transform.SetParent(gantryRoot.transform, false);
            colRight.transform.localPosition = new Vector3(7.5f, 3.5f, 0f);
            colRight.transform.localScale = new Vector3(0.35f, 3.5f, 0.35f);
            colRight.GetComponent<Renderer>().material = metalRooftopMat;

            GameObject truss = GameObject.CreatePrimitive(PrimitiveType.Cube);
            truss.name = "GantryTruss";
            truss.transform.SetParent(gantryRoot.transform, false);
            truss.transform.localPosition = new Vector3(0f, 7.0f, 0f);
            truss.transform.localScale = new Vector3(15.5f, 0.65f, 0.65f);
            truss.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(truss.GetComponent<Collider>());

            GameObject signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoard.name = "HighwaySignBoard";
            signBoard.transform.SetParent(gantryRoot.transform, false);
            signBoard.transform.localPosition = new Vector3(0f, 6.2f, -0.4f);
            signBoard.transform.localScale = new Vector3(12.0f, 1.8f, 0.15f);
            signBoard.GetComponent<Renderer>().material = highwayGreenMat;
            SafeDestroy(signBoard.GetComponent<Collider>());
        }

        private void CreateStreetlamp(Transform parent, Vector3 pos)
        {
            GameObject lamp = new GameObject("Streetlight");
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = pos;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            pole.transform.localScale = new Vector3(0.15f, 3.0f, 0.15f);
            pole.GetComponent<Renderer>().material = metalRooftopMat;

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.SetParent(lamp.transform, false);
            arm.transform.localPosition = new Vector3(0f, 5.9f, 0.4f);
            arm.transform.localScale = new Vector3(0.12f, 0.12f, 0.9f);
            arm.GetComponent<Renderer>().material = metalRooftopMat;

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 5.75f, 0.8f);
            bulb.transform.localScale = Vector3.one * 0.35f;
            bulb.GetComponent<Renderer>().material = roadMarkingMat;

            Light light = bulb.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1.0f, 0.92f, 0.78f);
            light.range = 16f;
            light.spotAngle = 75f;
            light.intensity = 2.0f;
            light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void CreateStreetTree(Transform parent, Vector3 pos)
        {
            GameObject tree = new GameObject("StreetTree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;

            // Planter Box Curb
            GameObject planter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            planter.name = "PlanterCurb";
            planter.transform.SetParent(tree.transform, false);
            planter.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            planter.transform.localScale = new Vector3(2.2f, 0.25f, 2.2f);
            planter.GetComponent<Renderer>().material = buildingConcreteMat;

            // Soil
            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Soil";
            soil.transform.SetParent(tree.transform, false);
            soil.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            soil.transform.localScale = new Vector3(1.8f, 0.14f, 1.8f);
            soil.GetComponent<Renderer>().material = treeBarkBrownMat;
            SafeDestroy(soil.GetComponent<Collider>());

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            trunk.transform.localScale = new Vector3(0.24f, 1.8f, 0.24f);
            trunk.GetComponent<Renderer>().material = treeBarkBrownMat;

            // Foliage Canopy (clustered spheres)
            Vector3[] canopyOffsets = new Vector3[]
            {
                new Vector3(0f, 4.0f, 0f),
                new Vector3(0.35f, 4.5f, 0.2f),
                new Vector3(-0.3f, 4.3f, -0.25f)
            };
            float[] canopyRadii = new float[] { 2.4f, 2.0f, 1.9f };

            for (int i = 0; i < canopyOffsets.Length; i++)
            {
                GameObject leafCluster = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leafCluster.name = $"LeafCluster_{i}";
                leafCluster.transform.SetParent(tree.transform, false);
                leafCluster.transform.localPosition = canopyOffsets[i];
                leafCluster.transform.localScale = Vector3.one * canopyRadii[i];
                leafCluster.GetComponent<Renderer>().material = foliageGreenMat;
                SafeDestroy(leafCluster.GetComponent<Collider>());
            }
        }

        private void CreateTrafficLight(Transform parent, Vector3 pos, float rotationY)
        {
            GameObject signal = new GameObject("TrafficSignalPole");
            signal.transform.SetParent(parent, false);
            signal.transform.position = pos;
            signal.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // Vertical Steel Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(signal.transform, false);
            pole.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            pole.transform.localScale = new Vector3(0.22f, 3.2f, 0.22f);
            pole.GetComponent<Renderer>().material = facadeTrimMat;

            // Cantilever Mast Arm reaching across lane
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "MastArm";
            arm.transform.SetParent(signal.transform, false);
            arm.transform.localPosition = new Vector3(0f, 6.2f, 2.2f);
            arm.transform.localScale = new Vector3(0.18f, 0.18f, 4.4f);
            arm.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(arm.GetComponent<Collider>());

            // 3-Aspect Signal Housing over lane
            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "SignalHousing";
            housing.transform.SetParent(signal.transform, false);
            housing.transform.localPosition = new Vector3(0f, 5.7f, 4.0f);
            housing.transform.localScale = new Vector3(0.45f, 1.25f, 0.35f);
            housing.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(housing.GetComponent<Collider>());

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            // Red light (top)
            GameObject red = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            red.name = "RedLens";
            red.transform.SetParent(housing.transform, false);
            red.transform.localPosition = new Vector3(0f, 0.33f, -0.45f);
            red.transform.localScale = new Vector3(0.55f, 0.22f, 0.35f);
            Material redMat = new Material(unlitShader) { color = new Color(1.0f, 0.1f, 0.1f) };
            red.GetComponent<Renderer>().material = redMat;
            SafeDestroy(red.GetComponent<Collider>());

            // Yellow light (mid)
            GameObject yellow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            yellow.name = "YellowLens";
            yellow.transform.SetParent(housing.transform, false);
            yellow.transform.localPosition = new Vector3(0f, 0.0f, -0.45f);
            yellow.transform.localScale = new Vector3(0.55f, 0.22f, 0.35f);
            Material yelMat = new Material(unlitShader) { color = new Color(0.95f, 0.85f, 0.15f) };
            yellow.GetComponent<Renderer>().material = yelMat;
            SafeDestroy(yellow.GetComponent<Collider>());

            // Green light (bottom)
            GameObject green = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            green.name = "GreenLens";
            green.transform.SetParent(housing.transform, false);
            green.transform.localPosition = new Vector3(0f, -0.33f, -0.45f);
            green.transform.localScale = new Vector3(0.55f, 0.22f, 0.35f);
            Material grnMat = new Material(unlitShader) { color = new Color(0.1f, 0.95f, 0.35f) };
            green.GetComponent<Renderer>().material = grnMat;
            SafeDestroy(green.GetComponent<Collider>());
        }

        private void CreatePowerConduit(Transform parent, Vector3 start, Vector3 end)
        {
            GameObject conduitRoot = new GameObject("HeavyPowerConduit_Harness");
            conduitRoot.transform.SetParent(parent, false);

            Vector3 groundStart = new Vector3(start.x, 0.12f, start.z);
            Vector3 groundEnd = new Vector3(end.x, 0.12f, end.z);

            Vector3[] path = new Vector3[] { start, groundStart, groundEnd, end };
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 p0 = path[i];
                Vector3 p1 = path[i + 1];
                float dist = Vector3.Distance(p0, p1);
                if (dist < 0.05f) continue;

                Vector3 mid = (p0 + p1) * 0.5f;
                Vector3 dir = (p1 - p0).normalized;

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                segment.name = $"ConduitSegment_{i}";
                segment.transform.SetParent(conduitRoot.transform, false);
                segment.transform.position = mid;
                segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                segment.transform.localScale = new Vector3(0.18f, dist * 0.5f, 0.18f);
                segment.GetComponent<Renderer>().material = facadeTrimMat;
                SafeDestroy(segment.GetComponent<Collider>());
            }
        }

        private void CreateVehicle(Transform parent, Vector3 pos, Color carColor, bool isVan = false)
        {
            GameObject vehicle = new GameObject(isVan ? "DeliveryVan" : "Sedan");
            vehicle.transform.SetParent(parent, false);
            vehicle.transform.position = pos;

            // Lower Chassis / Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(vehicle.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = isVan ? new Vector3(2.3f, 1.8f, 5.4f) : new Vector3(2.1f, 0.85f, 4.6f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material carMat = new Material(litShader) { color = carColor };
            ConfigureMat(carMat, carColor, 0.72f, 0.35f);
            body.GetComponent<Renderer>().material = carMat;

            // Cabin Roof & Glass
            if (!isVan)
            {
                GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cabin.name = "Cabin";
                cabin.transform.SetParent(vehicle.transform, false);
                cabin.transform.localPosition = new Vector3(0f, 1.15f, -0.2f);
                cabin.transform.localScale = new Vector3(1.85f, 0.72f, 2.5f);
                cabin.GetComponent<Renderer>().material = windowGlassMat;
                SafeDestroy(cabin.GetComponent<Collider>());

                GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.name = "Roof";
                roof.transform.SetParent(cabin.transform, false);
                roof.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                roof.transform.localScale = new Vector3(1.02f, 0.1f, 1.02f);
                roof.GetComponent<Renderer>().material = carMat;
                SafeDestroy(roof.GetComponent<Collider>());
            }
            else
            {
                // Van Windshield
                GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                windshield.name = "VanWindshield";
                windshield.transform.SetParent(vehicle.transform, false);
                windshield.transform.localPosition = new Vector3(0f, 1.0f, -2.1f);
                windshield.transform.localScale = new Vector3(2.0f, 0.65f, 0.25f);
                windshield.GetComponent<Renderer>().material = windowGlassMat;
                SafeDestroy(windshield.GetComponent<Collider>());
            }

            // 4 Rubber Wheels
            float wheelRadius = 0.38f;
            float wheelWidth = 0.28f;
            float wheelX = isVan ? 1.05f : 0.98f;
            float wheelFrontZ = isVan ? -1.7f : -1.4f;
            float wheelRearZ = isVan ? 1.7f : 1.4f;

            Vector3[] wheelPositions = new Vector3[]
            {
                new Vector3(-wheelX, wheelRadius, wheelFrontZ),
                new Vector3(wheelX, wheelRadius, wheelFrontZ),
                new Vector3(-wheelX, wheelRadius, wheelRearZ),
                new Vector3(wheelX, wheelRadius, wheelRearZ)
            };

            for (int w = 0; w < 4; w++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{w}";
                wheel.transform.SetParent(vehicle.transform, false);
                wheel.transform.localPosition = wheelPositions[w];
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(wheelRadius * 2f, wheelWidth * 0.5f, wheelRadius * 2f);
                wheel.GetComponent<Renderer>().material = facadeTrimMat;
                SafeDestroy(wheel.GetComponent<Collider>());
            }

            // Bumpers
            GameObject bumperF = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bumperF.name = "Bumper_Front";
            bumperF.transform.SetParent(vehicle.transform, false);
            bumperF.transform.localPosition = new Vector3(0f, 0.3f, isVan ? -2.75f : -2.35f);
            bumperF.transform.localScale = new Vector3(2.15f, 0.24f, 0.2f);
            bumperF.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(bumperF.GetComponent<Collider>());

            GameObject bumperR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bumperR.name = "Bumper_Rear";
            bumperR.transform.SetParent(vehicle.transform, false);
            bumperR.transform.localPosition = new Vector3(0f, 0.3f, isVan ? 2.75f : 2.35f);
            bumperR.transform.localScale = new Vector3(2.15f, 0.24f, 0.2f);
            bumperR.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(bumperR.GetComponent<Collider>());

            // Headlights & Taillights
            Shader unlitSh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hl.name = $"Headlight_{side}";
                hl.transform.SetParent(vehicle.transform, false);
                hl.transform.localPosition = new Vector3(side * 0.75f, 0.55f, isVan ? -2.72f : -2.32f);
                hl.transform.localScale = new Vector3(0.24f, 0.16f, 0.12f);
                hl.GetComponent<Renderer>().material = new Material(unlitSh) { color = new Color(1.0f, 0.95f, 0.75f) };
                SafeDestroy(hl.GetComponent<Collider>());

                GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tl.name = $"Taillight_{side}";
                tl.transform.SetParent(vehicle.transform, false);
                tl.transform.localPosition = new Vector3(side * 0.75f, 0.55f, isVan ? 2.72f : 2.32f);
                tl.transform.localScale = new Vector3(0.24f, 0.14f, 0.1f);
                tl.GetComponent<Renderer>().material = new Material(unlitSh) { color = new Color(0.95f, 0.12f, 0.12f) };
                SafeDestroy(tl.GetComponent<Collider>());
            }
        }

        private void CreateBus(Transform parent, Vector3 pos, Color busColor)
        {
            GameObject bus = new GameObject("TransitBus");
            bus.transform.SetParent(parent, false);
            bus.transform.position = pos;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "BusBody";
            body.transform.SetParent(bus.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            body.transform.localScale = new Vector3(2.8f, 2.8f, 10.5f);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material bMat = new Material(litShader) { color = busColor };
            body.GetComponent<Renderer>().material = bMat;

            GameObject windows = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windows.name = "WindowsStrip";
            windows.transform.SetParent(bus.transform, false);
            windows.transform.localPosition = new Vector3(0f, 1.9f, 0.4f);
            windows.transform.localScale = new Vector3(2.85f, 0.9f, 9.2f);
            windows.GetComponent<Renderer>().material = windowGlassMat;
            SafeDestroy(windows.GetComponent<Collider>());
        }

        private void CreateJerseyBarrier(Transform parent, Vector3 pos)
        {
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "JerseyBarrier";
            barrier.transform.SetParent(parent, false);
            barrier.transform.position = pos;
            barrier.transform.localScale = new Vector3(2.4f, 0.9f, 0.6f);
            barrier.GetComponent<Renderer>().material = sidewalkMaterial;
        }

        private void CreateDumpster(Transform parent, Vector3 pos)
        {
            GameObject dumpster = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dumpster.name = "Dumpster";
            dumpster.transform.SetParent(parent, false);
            dumpster.transform.position = pos;
            dumpster.transform.localScale = new Vector3(1.8f, 1.4f, 2.6f);
            dumpster.GetComponent<Renderer>().material = metalRooftopMat;
        }

        private void BuildEscapeCorridors(Transform parent)
        {
            GameObject escapeRoot = new GameObject("Escape_Corridors");
            escapeRoot.transform.SetParent(parent, false);

            CreateEscapeCorridorNode(escapeRoot.transform, "North Highway Tunnel", new Vector3(0f, 1.5f, 88f), new Vector3(16f, 4f, 6f));
            CreateEscapeCorridorNode(escapeRoot.transform, "East Subway Station", new Vector3(42f, 1.5f, 38f), new Vector3(8f, 4f, 14f));
            CreateEscapeCorridorNode(escapeRoot.transform, "West River Bridge", new Vector3(-42f, 1.5f, 38f), new Vector3(8f, 4f, 14f));

            // Setup Strategic Spawn Nodes across all districts
            CreateSpawnNode("Spawn_WestCrosswalk", new Vector3(-5.5f, 0.1f, 26f));
            CreateSpawnNode("Spawn_IntersectionCenter", new Vector3(1.5f, 0.1f, 36f));
            CreateSpawnNode("Spawn_EastSidewalk", new Vector3(8.0f, 0.1f, 24f));
            CreateSpawnNode("Spawn_BoulevardMid", new Vector3(-1.0f, 0.1f, 48f));
            CreateSpawnNode("Spawn_AvenueWest", new Vector3(-24f, 0.1f, 38f));
            CreateSpawnNode("Spawn_AvenueEast", new Vector3(24f, 0.1f, 38f));
            CreateSpawnNode("Spawn_NorthBoulevard", new Vector3(1.0f, 0.1f, 65f));
            CreateSpawnNode("Spawn_FarIntersection", new Vector3(-2.0f, 0.1f, 75f));

            // Waypoints
            CreateWaypointNode("Waypoint_CenterIntersection", new Vector3(0f, 0.2f, 38f));
            CreateWaypointNode("Waypoint_WestCrosswalk", new Vector3(-7f, 0.2f, 38f));
            CreateWaypointNode("Waypoint_EastCrosswalk", new Vector3(7f, 0.2f, 38f));
            CreateWaypointNode("Waypoint_NorthBoulevard", new Vector3(0f, 0.2f, 60f));
            CreateWaypointNode("Waypoint_SouthBoulevard", new Vector3(0f, 0.2f, 24f));
        }

        private void CreateEscapeCorridorNode(Transform parent, string name, Vector3 pos, Vector3 triggerSize)
        {
            GameObject node = new GameObject($"EscapeZone_{name}");
            node.transform.SetParent(parent, false);
            node.transform.position = pos;

            BoxCollider box = node.AddComponent<BoxCollider>();
            box.size = triggerSize;
            box.isTrigger = true;

            node.AddComponent<DEADeyeEscapeZone>();
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

        /// <summary>
        /// Builds the 3 Walkable Overwatch Positions interconnected by elevated bridges and catwalks.
        /// Enables full WASD locomotion between all three vantage perches without teleportation!
        /// </summary>
        private void BuildMultiOverwatchNetwork(Transform parent)
        {
            GameObject networkRoot = new GameObject("Overwatch_Network_Walkable");
            networkRoot.transform.SetParent(parent, false);

            // =========================================================================
            // PERCH 1: CENTRAL OVERWATCH (Initial Perch at Y = 26m)
            // =========================================================================
            GameObject perch1 = new GameObject("Overwatch_1_CenterPerch");
            perch1.transform.SetParent(networkRoot.transform, false);

            // Tower Base below central roof
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "SniperTowerBase";
            tower.transform.SetParent(perch1.transform, false);
            tower.transform.position = new Vector3(0f, rooftopHeight * 0.5f, -6f);
            tower.transform.localScale = new Vector3(14f, rooftopHeight, 12f);
            tower.GetComponent<Renderer>().material = buildingConcreteMat;

            // Rooftop Floor (Player Standing Area at Y = rooftopHeight)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "RooftopFloor";
            floor.transform.SetParent(perch1.transform, false);
            floor.transform.position = new Vector3(0f, rooftopHeight - 0.15f, -6f);
            floor.transform.localScale = new Vector3(14f, 0.3f, 12f);
            floor.GetComponent<Renderer>().material = metalRooftopMat;

            // Front Parapet with lowered central notch
            CreateParapetWall(perch1.transform, new Vector3(-4.4f, rooftopHeight + 0.55f, -1.1f), new Vector3(4.8f, 1.1f, 0.4f), "FrontParapet_WestWing");
            CreateParapetWall(perch1.transform, new Vector3(4.4f, rooftopHeight + 0.55f, -1.1f), new Vector3(4.8f, 1.1f, 0.4f), "FrontParapet_EastWing");
            CreateParapetWall(perch1.transform, new Vector3(0f, rooftopHeight + 0.38f, -1.1f), new Vector3(4.0f, 0.76f, 0.4f), "FrontParapet_SniperNotch");
            CreateParapetWall(perch1.transform, new Vector3(0f, rooftopHeight + 0.55f, -11.9f), new Vector3(14f, 1.1f, 0.4f), "BackParapet");

            // Side parapet walls on Perch 1 (extending from Z=-11.9 to Z=-7.1 rear, and Z=-3.1 to Z=-0.9 front, closing front corners)
            CreateParapetWall(perch1.transform, new Vector3(-6.9f, rooftopHeight + 0.55f, -9.5f), new Vector3(0.4f, 1.1f, 4.8f), "LeftParapet_Rear");
            CreateParapetWall(perch1.transform, new Vector3(-6.9f, rooftopHeight + 0.55f, -2.0f), new Vector3(0.4f, 1.1f, 2.2f), "LeftParapet_Front");

            CreateParapetWall(perch1.transform, new Vector3(6.9f, rooftopHeight + 0.55f, -9.5f), new Vector3(0.4f, 1.1f, 4.8f), "RightParapet_Rear");
            CreateParapetWall(perch1.transform, new Vector3(6.9f, rooftopHeight + 0.55f, -2.0f), new Vector3(0.4f, 1.1f, 2.2f), "RightParapet_Front");

            // Central Weapon Rest Sandbags
            GameObject sandbag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandbag.name = "SniperWeaponRest";
            sandbag.transform.SetParent(perch1.transform, false);
            sandbag.transform.position = new Vector3(0f, rooftopHeight + 0.82f, -1.1f);
            sandbag.transform.localScale = new Vector3(2.4f, 0.14f, 0.5f);
            sandbag.GetComponent<Renderer>().material = sidewalkMaterial;

            // Authentic Military / Tactical Sniper Overwatch Nest details on Perch 1
            BuildTacticalOverwatchNest(perch1.transform);

            // Tactical Mission Briefing Terminal Workstation on Perch 1
            BuildRooftopTacticalTerminal(perch1.transform);

            // =========================================================================
            // PERCH 2: EAST OVERWATCH (Comms Facility & Commercial Plaza Perch)
            // =========================================================================
            GameObject perch2 = new GameObject("Overwatch_2_EastPlazaPerch");
            perch2.transform.SetParent(networkRoot.transform, false);

            GameObject floor2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor2.name = "EastPerchFloor";
            floor2.transform.SetParent(perch2.transform, false);
            floor2.transform.position = new Vector3(18f, rooftopHeight - 0.15f, 4f);
            floor2.transform.localScale = new Vector3(8f, 0.3f, 8f);
            floor2.GetComponent<Renderer>().material = metalRooftopMat;

            // North Notch & Wings facing Commercial Plaza
            CreateParapetWall(perch2.transform, new Vector3(18f, rooftopHeight + 0.38f, 7.9f), new Vector3(4.0f, 0.76f, 0.4f), "EastPerch_NotchNorth");
            CreateParapetWall(perch2.transform, new Vector3(15.0f, rooftopHeight + 0.55f, 7.9f), new Vector3(2.0f, 1.1f, 0.4f), "EastPerch_NorthWing_West");
            CreateParapetWall(perch2.transform, new Vector3(21.0f, rooftopHeight + 0.55f, 7.9f), new Vector3(2.0f, 1.1f, 0.4f), "EastPerch_NorthWing_East");

            // Outer Perimeter Parapets (East, South, and Enclosed West Edge)
            CreateParapetWall(perch2.transform, new Vector3(21.9f, rooftopHeight + 0.55f, 4.0f), new Vector3(0.4f, 1.1f, 8.0f), "EastPerch_RightParapet");
            CreateParapetWall(perch2.transform, new Vector3(18f, rooftopHeight + 0.55f, 0.1f), new Vector3(8.0f, 1.1f, 0.4f), "EastPerch_BackParapet");
            CreateParapetWall(perch2.transform, new Vector3(14.1f, rooftopHeight + 0.55f, 5.75f), new Vector3(0.4f, 1.1f, 4.5f), "EastPerch_WestParapet_North");
            CreateParapetWall(perch2.transform, new Vector3(14.1f, rooftopHeight + 0.55f, 0.75f), new Vector3(0.4f, 1.1f, 1.5f), "EastPerch_WestParapet_South");

            GameObject sandbag2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandbag2.name = "EastPerch_Sandbag";
            sandbag2.transform.SetParent(perch2.transform, false);
            sandbag2.transform.position = new Vector3(18f, rooftopHeight + 0.82f, 7.9f);
            sandbag2.transform.localScale = new Vector3(2.2f, 0.14f, 0.5f);
            sandbag2.GetComponent<Renderer>().material = sidewalkMaterial;

            // =========================================================================
            // PERCH 3: WEST OVERWATCH (Construction Site & Residential Alley Perch)
            // =========================================================================
            GameObject perch3 = new GameObject("Overwatch_3_WestConstructionPerch");
            perch3.transform.SetParent(networkRoot.transform, false);

            GameObject floor3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor3.name = "WestPerchFloor";
            floor3.transform.SetParent(perch3.transform, false);
            floor3.transform.position = new Vector3(-18f, rooftopHeight - 0.15f, 6f);
            floor3.transform.localScale = new Vector3(8f, 0.3f, 8f);
            floor3.GetComponent<Renderer>().material = metalRooftopMat;

            // North Notch & Wings facing Construction Crane
            CreateParapetWall(perch3.transform, new Vector3(-18f, rooftopHeight + 0.38f, 9.9f), new Vector3(4.0f, 0.76f, 0.4f), "WestPerch_NotchNorth");
            CreateParapetWall(perch3.transform, new Vector3(-21.0f, rooftopHeight + 0.55f, 9.9f), new Vector3(2.0f, 1.1f, 0.4f), "WestPerch_NorthWing_West");
            CreateParapetWall(perch3.transform, new Vector3(-15.0f, rooftopHeight + 0.55f, 9.9f), new Vector3(2.0f, 1.1f, 0.4f), "WestPerch_NorthWing_East");

            // Outer Perimeter Parapets (West, South, and Enclosed East Edge)
            CreateParapetWall(perch3.transform, new Vector3(-21.9f, rooftopHeight + 0.55f, 6.0f), new Vector3(0.4f, 1.1f, 8.0f), "WestPerch_LeftParapet");
            CreateParapetWall(perch3.transform, new Vector3(-18f, rooftopHeight + 0.55f, 2.1f), new Vector3(8.0f, 1.1f, 0.4f), "WestPerch_BackParapet");
            CreateParapetWall(perch3.transform, new Vector3(-14.1f, rooftopHeight + 0.55f, 7.75f), new Vector3(0.4f, 1.1f, 4.5f), "WestPerch_EastParapet_North");
            CreateParapetWall(perch3.transform, new Vector3(-14.1f, rooftopHeight + 0.55f, 2.75f), new Vector3(0.4f, 1.1f, 1.5f), "WestPerch_EastParapet_South");

            GameObject sandbag3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandbag3.name = "WestPerch_Sandbag";
            sandbag3.transform.SetParent(perch3.transform, false);
            sandbag3.transform.position = new Vector3(-18f, rooftopHeight + 0.82f, 9.9f);
            sandbag3.transform.localScale = new Vector3(2.2f, 0.14f, 0.5f);
            sandbag3.GetComponent<Renderer>().material = sidewalkMaterial;

            // =========================================================================
            // TRANSITION LANDING SLABS (Eliminates all angled bridge gaps!)
            // =========================================================================
            CreateTransitionLanding(networkRoot.transform, new Vector3(6.8f, rooftopHeight - 0.15f, -5.1f), new Vector2(4.2f, 4.2f), "TransitionLanding_Perch1_East");
            CreateTransitionLanding(networkRoot.transform, new Vector3(14.0f, rooftopHeight - 0.15f, 2.0f), new Vector2(4.2f, 4.2f), "TransitionLanding_Perch2_West");
            CreateTransitionLanding(networkRoot.transform, new Vector3(-6.8f, rooftopHeight - 0.15f, -5.1f), new Vector2(4.2f, 4.2f), "TransitionLanding_Perch1_West");
            CreateTransitionLanding(networkRoot.transform, new Vector3(-14.0f, rooftopHeight - 0.15f, 4.0f), new Vector2(4.2f, 4.2f), "TransitionLanding_Perch3_East");

            // =========================================================================
            // WALKABLE SKYBRIDGES & CATWALKS (Connecting Perch 1 to Perch 2 & 3)
            // =========================================================================
            // 1. East Catwalk Bridge (3.2m wide, continuous collision)
            BuildWalkableBridge(networkRoot.transform, new Vector3(6.8f, rooftopHeight - 0.15f, -5.2f), new Vector3(14.0f, rooftopHeight - 0.15f, 2.0f), "Bridge_To_EastPlazaPerch");

            // 2. West Skybridge (3.2m wide, continuous collision)
            BuildWalkableBridge(networkRoot.transform, new Vector3(-6.8f, rooftopHeight - 0.15f, -5.2f), new Vector3(-14.0f, rooftopHeight - 0.15f, 4.0f), "Bridge_To_WestConstructionPerch");

            // Funnel guide walls at bridge junctions (angling outward from building openings, zero corridor obstruction)
            CreateFunnelParapet(networkRoot.transform, new Vector3(6.9f, rooftopHeight + 0.55f, -7.1f), new Vector3(7.6f, rooftopHeight + 0.55f, -6.3f), "Funnel_Perch1_East_Rear");
            CreateFunnelParapet(networkRoot.transform, new Vector3(6.9f, rooftopHeight + 0.55f, -3.1f), new Vector3(7.6f, rooftopHeight + 0.55f, -4.1f), "Funnel_Perch1_East_Front");
            CreateFunnelParapet(networkRoot.transform, new Vector3(14.1f, rooftopHeight + 0.55f, 1.4f), new Vector3(14.1f, rooftopHeight + 0.55f, 1.1f), "Funnel_Perch2_West_Rear");
            CreateFunnelParapet(networkRoot.transform, new Vector3(14.1f, rooftopHeight + 0.55f, 3.6f), new Vector3(14.8f, rooftopHeight + 0.55f, 2.9f), "Funnel_Perch2_West_Front");

            CreateFunnelParapet(networkRoot.transform, new Vector3(-6.9f, rooftopHeight + 0.55f, -7.1f), new Vector3(-7.6f, rooftopHeight + 0.55f, -6.3f), "Funnel_Perch1_West_Rear");
            CreateFunnelParapet(networkRoot.transform, new Vector3(-6.9f, rooftopHeight + 0.55f, -3.1f), new Vector3(-7.6f, rooftopHeight + 0.55f, -4.1f), "Funnel_Perch1_West_Front");
            CreateFunnelParapet(networkRoot.transform, new Vector3(-14.1f, rooftopHeight + 0.55f, 3.4f), new Vector3(-14.1f, rooftopHeight + 0.55f, 3.1f), "Funnel_Perch3_East_Rear");
            CreateFunnelParapet(networkRoot.transform, new Vector3(-14.1f, rooftopHeight + 0.55f, 5.6f), new Vector3(-14.8f, rooftopHeight + 0.55f, 4.9f), "Funnel_Perch3_East_Front");

            // Perimeter safety boundaries ensuring player cannot fall off outer edges
            CreateSafetyBoundary(networkRoot.transform, new Vector3(0f, rooftopHeight + 2.0f, -12.2f), new Vector3(14.5f, 4f, 0.4f));
            CreateSafetyBoundary(networkRoot.transform, new Vector3(22.2f, rooftopHeight + 2.0f, 4f), new Vector3(0.4f, 4f, 8.5f));
            CreateSafetyBoundary(networkRoot.transform, new Vector3(18f, rooftopHeight + 2.0f, -0.2f), new Vector3(8.5f, 4f, 0.4f));
            CreateSafetyBoundary(networkRoot.transform, new Vector3(18f, rooftopHeight + 2.0f, 8.2f), new Vector3(8.5f, 4f, 0.4f));

            CreateSafetyBoundary(networkRoot.transform, new Vector3(-22.2f, rooftopHeight + 2.0f, 6f), new Vector3(0.4f, 4f, 8.5f));
            CreateSafetyBoundary(networkRoot.transform, new Vector3(-18f, rooftopHeight + 2.0f, 1.8f), new Vector3(8.5f, 4f, 0.4f));
            CreateSafetyBoundary(networkRoot.transform, new Vector3(-18f, rooftopHeight + 2.0f, 10.2f), new Vector3(8.5f, 4f, 0.4f));
        }

        private void CreateTransitionLanding(Transform parent, Vector3 center, Vector2 size, string name)
        {
            GameObject landing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            landing.name = name;
            landing.transform.SetParent(parent, false);
            landing.transform.position = new Vector3(center.x, center.y, center.z);
            landing.transform.localScale = new Vector3(size.x, 0.3f, size.y);
            landing.GetComponent<Renderer>().material = metalRooftopMat;
        }

        private void CreateFunnelParapet(Transform parent, Vector3 start, Vector3 end, string name)
        {
            GameObject funnel = new GameObject(name);
            funnel.transform.SetParent(parent, false);
            Vector3 mid = (start + end) * 0.5f;
            float len = Vector3.Distance(start, end);
            Vector3 dir = (end - start).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                funnel.transform.position = mid;
                funnel.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "FunnelWall";
                wall.transform.SetParent(funnel.transform, false);
                wall.transform.localPosition = Vector3.zero;
                wall.transform.localScale = new Vector3(0.35f, 1.1f, len);
                wall.GetComponent<Renderer>().material = facadeTrimMat;
            }
        }

        private void BuildWalkableBridge(Transform parent, Vector3 start, Vector3 end, string name)
        {
            GameObject bridge = new GameObject(name);
            bridge.transform.SetParent(parent, false);

            Vector3 mid = (start + end) * 0.5f;
            float length = Vector3.Distance(start, end) + 0.3f; // Slight overlap to guarantee zero gap
            Vector3 dir = (end - start).normalized;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            bridge.transform.position = mid;
            bridge.transform.rotation = rot;

            // Solid Floor (3.2m wide with BoxCollider)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "BridgeFloor";
            floor.transform.SetParent(bridge.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(3.2f, 0.3f, length);
            floor.GetComponent<Renderer>().material = metalRooftopMat;

            // Left Handrail (spanning open gap between buildings, flush with funnel parapets)
            float handrailLength = Mathf.Max(2.0f, length - 1.8f);
            GameObject railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railL.name = "Handrail_Left";
            railL.transform.SetParent(bridge.transform, false);
            railL.transform.localPosition = new Vector3(-1.55f, 0.6f, 0f);
            railL.transform.localScale = new Vector3(0.12f, 1.1f, handrailLength);
            railL.GetComponent<Renderer>().material = facadeTrimMat;
            BoxCollider colL = railL.GetComponent<BoxCollider>();
            if (colL != null) colL.size = Vector3.one;

            // Right Handrail (spanning open gap between buildings, flush with funnel parapets)
            GameObject railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railR.name = "Handrail_Right";
            railR.transform.SetParent(bridge.transform, false);
            railR.transform.localPosition = new Vector3(1.55f, 0.6f, 0f);
            railR.transform.localScale = new Vector3(0.12f, 1.1f, handrailLength);
            railR.GetComponent<Renderer>().material = facadeTrimMat;
            BoxCollider colR = railR.GetComponent<BoxCollider>();
            if (colR != null) colR.size = Vector3.one;
        }

        private void BuildTacticalOverwatchNest(Transform perch1)
        {
            GameObject nestRoot = new GameObject("TacticalOverwatchNest_Details");
            nestRoot.transform.SetParent(perch1, false);

            // 1. Access Stairwell Bulkhead Housing in Rear Right
            GameObject stairHousing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stairHousing.name = "StairwellBulkhead";
            stairHousing.transform.SetParent(nestRoot.transform, false);
            stairHousing.transform.position = new Vector3(3.8f, rooftopHeight + 1.4f, -9.5f);
            stairHousing.transform.localScale = new Vector3(3.2f, 2.8f, 2.8f);
            stairHousing.GetComponent<Renderer>().material = buildingConcreteMat;

            // Stairwell Metal Door
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "StairwellDoor";
            door.transform.SetParent(nestRoot.transform, false);
            door.transform.position = new Vector3(3.8f, rooftopHeight + 1.1f, -8.05f);
            door.transform.localScale = new Vector3(1.1f, 2.2f, 0.08f);
            door.GetComponent<Renderer>().material = facadeTrimMat;

            // Hazard Warning Stripe Sign above door
            GameObject hazardSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hazardSign.name = "HazardSign";
            hazardSign.transform.SetParent(nestRoot.transform, false);
            hazardSign.transform.position = new Vector3(3.8f, rooftopHeight + 2.45f, -8.05f);
            hazardSign.transform.localScale = new Vector3(1.4f, 0.25f, 0.05f);
            hazardSign.GetComponent<Renderer>().material = craneYellowMat;
            SafeDestroy(hazardSign.GetComponent<Collider>());

            // 2. Satellite / Comms Dish on Tripod
            GameObject dishStand = new GameObject("TripodSatelliteDish");
            dishStand.transform.SetParent(nestRoot.transform, false);
            dishStand.transform.position = new Vector3(-3.8f, rooftopHeight, -8.5f);

            for (int i = 0; i < 3; i++)
            {
                float ang = i * 120f * Mathf.Deg2Rad;
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"TripodLeg_{i}";
                leg.transform.SetParent(dishStand.transform, false);
                leg.transform.localPosition = new Vector3(Mathf.Sin(ang) * 0.45f, 0.6f, Mathf.Cos(ang) * 0.45f);
                leg.transform.localScale = new Vector3(0.06f, 0.65f, 0.06f);
                leg.transform.localRotation = Quaternion.Euler(Mathf.Cos(ang) * 20f, 0f, -Mathf.Sin(ang) * 20f);
                leg.GetComponent<Renderer>().material = metalRooftopMat;
                SafeDestroy(leg.GetComponent<Collider>());
            }

            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "ParabolicDish";
            dish.transform.SetParent(dishStand.transform, false);
            dish.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            dish.transform.localScale = new Vector3(1.5f, 0.12f, 1.5f);
            dish.transform.localRotation = Quaternion.Euler(45f, 25f, 0f);
            dish.GetComponent<Renderer>().material = sidewalkMaterial;
            SafeDestroy(dish.GetComponent<Collider>());

            GameObject feedHorn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            feedHorn.name = "FeedHorn";
            feedHorn.transform.SetParent(dish.transform, false);
            feedHorn.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            feedHorn.transform.localScale = new Vector3(0.1f, 0.6f, 0.1f);
            feedHorn.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(feedHorn.GetComponent<Collider>());

            // 3. Military Tactical Radio Rack (positioned against rear wall)
            GameObject radioRack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            radioRack.name = "MilitaryRadioRack";
            radioRack.transform.SetParent(nestRoot.transform, false);
            radioRack.transform.position = new Vector3(-4.8f, rooftopHeight + 0.75f, -10.5f);
            radioRack.transform.localScale = new Vector3(0.7f, 1.5f, 0.55f);
            radioRack.GetComponent<Renderer>().material = metalRooftopMat;

            Color[] ledCols = new Color[] { Color.green, Color.green, Color.yellow, Color.red };
            for (int i = 0; i < 4; i++)
            {
                GameObject led = GameObject.CreatePrimitive(PrimitiveType.Cube);
                led.name = $"RadioLED_{i}";
                led.transform.SetParent(radioRack.transform, false);
                led.transform.localPosition = new Vector3(0.52f, 0.35f - (i * 0.12f), -0.2f + (i * 0.12f));
                led.transform.localScale = new Vector3(0.08f, 0.06f, 0.08f);
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                led.GetComponent<Renderer>().material = new Material(unlit) { color = ledCols[i] };
                SafeDestroy(led.GetComponent<Collider>());
            }

            GameObject whip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            whip.name = "WhipAntenna";
            whip.transform.SetParent(radioRack.transform, false);
            whip.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            whip.transform.localScale = new Vector3(0.04f, 1.2f, 0.04f);
            whip.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(whip.GetComponent<Collider>());

            GameObject whipBeacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            whipBeacon.name = "TipBeacon";
            whipBeacon.transform.SetParent(whip.transform, false);
            whipBeacon.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            whipBeacon.transform.localScale = Vector3.one * 0.15f;
            whipBeacon.GetComponent<Renderer>().material = beaconRedMat;
            SafeDestroy(whipBeacon.GetComponent<Collider>());

            // 4. Rugged Field Generator Pack (positioned against rear wall)
            GameObject gen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gen.name = "FieldGeneratorPack";
            gen.transform.SetParent(nestRoot.transform, false);
            gen.transform.position = new Vector3(-2.8f, rooftopHeight + 0.35f, -10.5f);
            gen.transform.localScale = new Vector3(0.85f, 0.7f, 0.65f);
            gen.GetComponent<Renderer>().material = facadeTrimMat;

            GameObject cable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cable.name = "PowerCableFloor";
            cable.transform.SetParent(nestRoot.transform, false);
            cable.transform.position = new Vector3(-3.8f, rooftopHeight + 0.03f, -10.5f);
            cable.transform.localScale = new Vector3(1.4f, 0.04f, 0.08f);
            cable.GetComponent<Renderer>().material = asphaltMaterial;
            SafeDestroy(cable.GetComponent<Collider>());

            // 5. Emergency Work Floodlight on Tripod
            GameObject floodlight = new GameObject("EmergencyWorkFloodlight");
            floodlight.transform.SetParent(nestRoot.transform, false);
            floodlight.transform.position = new Vector3(4.5f, rooftopHeight, -4.5f);

            GameObject flStand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flStand.name = "FloodlightStand";
            flStand.transform.SetParent(floodlight.transform, false);
            flStand.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            flStand.transform.localScale = new Vector3(0.08f, 0.9f, 0.08f);
            flStand.GetComponent<Renderer>().material = craneYellowMat;
            SafeDestroy(flStand.GetComponent<Collider>());

            GameObject flHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flHead.name = "FloodlightLampHead";
            flHead.transform.SetParent(floodlight.transform, false);
            flHead.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            flHead.transform.localScale = new Vector3(0.35f, 0.25f, 0.2f);
            flHead.transform.localRotation = Quaternion.Euler(30f, -40f, 0f);
            flHead.GetComponent<Renderer>().material = craneYellowMat;
            SafeDestroy(flHead.GetComponent<Collider>());

            Light flLight = flHead.AddComponent<Light>();
            flLight.type = LightType.Spot;
            flLight.color = new Color(1.0f, 0.95f, 0.85f);
            flLight.range = 18f;
            flLight.spotAngle = 60f;
            flLight.intensity = 2.2f;

            // 6. Spent Brass Casings near sniper rest
            Shader brassSh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material brassMat = new Material(brassSh) { color = new Color(0.85f, 0.72f, 0.25f) };
            Vector3[] casingPositions = new Vector3[]
            {
                new Vector3(-0.4f, rooftopHeight + 0.02f, -1.6f),
                new Vector3(-0.2f, rooftopHeight + 0.02f, -1.8f),
                new Vector3(0.1f, rooftopHeight + 0.02f, -1.5f),
                new Vector3(0.35f, rooftopHeight + 0.02f, -1.7f),
                new Vector3(0.5f, rooftopHeight + 0.02f, -1.4f),
                new Vector3(-0.6f, rooftopHeight + 0.02f, -1.9f)
            };
            for (int i = 0; i < casingPositions.Length; i++)
            {
                GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                casing.name = $"SpentBrass_{i}";
                casing.transform.SetParent(nestRoot.transform, false);
                casing.transform.position = casingPositions[i];
                casing.transform.localScale = new Vector3(0.04f, 0.07f, 0.04f);
                casing.transform.rotation = Quaternion.Euler(90f, (i * 53f) % 360f, 0f);
                casing.GetComponent<Renderer>().material = brassMat;
                SafeDestroy(casing.GetComponent<Collider>());
            }

            // 7. Layered Sandbags at Sniper Rest
            Vector3[] sandbagOffsets = new Vector3[]
            {
                new Vector3(-1.0f, rooftopHeight + 0.82f, -1.1f),
                new Vector3(1.0f, rooftopHeight + 0.82f, -1.1f),
                new Vector3(-0.5f, rooftopHeight + 0.94f, -1.1f),
                new Vector3(0.5f, rooftopHeight + 0.94f, -1.1f)
            };
            for (int i = 0; i < sandbagOffsets.Length; i++)
            {
                GameObject sb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sb.name = $"LayeredSandbag_{i}";
                sb.transform.SetParent(nestRoot.transform, false);
                sb.transform.position = sandbagOffsets[i];
                sb.transform.localScale = new Vector3(0.95f, 0.13f, 0.45f);
                sb.GetComponent<Renderer>().material = sidewalkMaterial;
            }
        }

        private void BuildRooftopTacticalTerminal(Transform parent)
        {
            GameObject terminalGO = new GameObject("TacticalBriefingTerminal_Station");
            terminalGO.transform.SetParent(parent, false);
            terminalGO.transform.position = new Vector3(-3.2f, rooftopHeight, -3.8f);
            terminalGO.transform.rotation = Quaternion.Euler(0f, 40f, 0f);

            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "TerminalDesk";
            desk.transform.SetParent(terminalGO.transform, false);
            desk.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            desk.transform.localScale = new Vector3(2.2f, 0.9f, 1.2f);
            desk.GetComponent<Renderer>().material = metalRooftopMat;

            GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "MainTacticalDisplay";
            monitor.transform.SetParent(terminalGO.transform, false);
            monitor.transform.localPosition = new Vector3(0f, 1.35f, 0.15f);
            monitor.transform.localScale = new Vector3(1.5f, 0.9f, 0.08f);
            monitor.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            Material screenMat = new Material(litShader) { color = new Color(0.02f, 0.15f, 0.25f) };
            monitor.GetComponent<Renderer>().material = screenMat;
            SafeDestroy(monitor.GetComponent<Collider>());

            GameObject kb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kb.name = "ConsoleKeyboard";
            kb.transform.SetParent(terminalGO.transform, false);
            kb.transform.localPosition = new Vector3(0f, 0.92f, -0.2f);
            kb.transform.localScale = new Vector3(1.2f, 0.05f, 0.45f);
            kb.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(kb.GetComponent<Collider>());
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
            boundary.layer = LayerMask.NameToLayer("Ignore Raycast");
            boundary.transform.SetParent(parent, false);
            boundary.transform.position = pos;
            BoxCollider col = boundary.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = false;
        }
    }
}
