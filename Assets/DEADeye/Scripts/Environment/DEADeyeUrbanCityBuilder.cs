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

        private void ConfigureMat(Material mat, Color col, float smoothness = 0.0f)
        {
            mat.color = col;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_SpecularHighlights", 0.0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
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
                ConfigureMat(asphaltMaterial, new Color(0.14f, 0.15f, 0.16f), 0.0f);
            }
            if (sidewalkMaterial == null)
            {
                sidewalkMaterial = new Material(litShader) { name = "Mat_Sidewalk" };
                ConfigureMat(sidewalkMaterial, new Color(0.68f, 0.68f, 0.66f), 0.0f);
            }
            if (buildingConcreteMat == null)
            {
                buildingConcreteMat = new Material(litShader) { name = "Mat_BuildingConcrete" };
                ConfigureMat(buildingConcreteMat, new Color(0.32f, 0.35f, 0.38f), 0.0f);
            }
            if (buildingBrickMat == null)
            {
                buildingBrickMat = new Material(litShader) { name = "Mat_BuildingBrick" };
                ConfigureMat(buildingBrickMat, new Color(0.42f, 0.25f, 0.22f), 0.0f);
            }
            if (windowGlassMat == null)
            {
                windowGlassMat = new Material(litShader) { name = "Mat_WindowGlass" };
                ConfigureMat(windowGlassMat, new Color(0.12f, 0.22f, 0.30f), 0.0f);
            }
            if (roadMarkingMat == null)
            {
                roadMarkingMat = new Material(unlitShader) { name = "Mat_RoadMarkingYellow" };
                roadMarkingMat.color = new Color(0.95f, 0.82f, 0.18f); // Highway Yellow
            }
            if (roadStripeWhiteMat == null)
            {
                roadStripeWhiteMat = new Material(unlitShader) { name = "Mat_RoadStripeWhite" };
                roadStripeWhiteMat.color = new Color(0.92f, 0.94f, 0.95f);
            }
            if (metalRooftopMat == null)
            {
                metalRooftopMat = new Material(litShader) { name = "Mat_RooftopMetal" };
                ConfigureMat(metalRooftopMat, new Color(0.25f, 0.27f, 0.29f), 0.0f);
            }
            if (facadeTrimMat == null)
            {
                facadeTrimMat = new Material(litShader) { name = "Mat_FacadeTrim" };
                ConfigureMat(facadeTrimMat, new Color(0.18f, 0.20f, 0.22f), 0.0f);
            }
            if (craneYellowMat == null)
            {
                craneYellowMat = new Material(litShader) { name = "Mat_CraneYellow" };
                ConfigureMat(craneYellowMat, new Color(0.92f, 0.65f, 0.08f), 0.0f);
            }
            if (beaconRedMat == null)
            {
                beaconRedMat = new Material(unlitShader) { name = "Mat_BeaconRed" };
                beaconRedMat.color = new Color(1.0f, 0.15f, 0.15f);
            }
            if (highwayGreenMat == null)
            {
                highwayGreenMat = new Material(litShader) { name = "Mat_HighwayGreen" };
                ConfigureMat(highwayGreenMat, new Color(0.08f, 0.42f, 0.22f), 0.0f);
            }
            if (marketClothMat == null)
            {
                marketClothMat = new Material(litShader) { name = "Mat_MarketCloth" };
                ConfigureMat(marketClothMat, new Color(0.85f, 0.28f, 0.22f), 0.0f);
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

            // 3. Facade Window Bands
            int floorCount = Mathf.FloorToInt((size.y - podiumHeight) / 3.6f);
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
            }

            // 4. Roof Cornice
            GameObject cornice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornice.name = "RoofCornice";
            cornice.transform.SetParent(bldg.transform, false);
            cornice.transform.localPosition = new Vector3(0f, size.y + 0.3f, 0f);
            cornice.transform.localScale = new Vector3(size.x + 0.5f, 0.6f, size.z + 0.5f);
            cornice.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(cornice.GetComponent<Collider>());
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
            // OBJECTIVE: HOSTILE COMMUNICATIONS JAMMER DEVICE
            // =========================================================================
            GameObject jammer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jammer.name = "CommunicationsJammer";
            jammer.transform.SetParent(commsTower.transform, false);
            jammer.transform.localPosition = new Vector3(0f, 1.2f, -1.8f);
            jammer.transform.localScale = new Vector3(1.8f, 1.4f, 1.2f);
            jammer.GetComponent<Renderer>().material = facadeTrimMat;

            // Rotating Antenna dishes on Jammer
            GameObject jammerDish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jammerDish.name = "JammerDish";
            jammerDish.transform.SetParent(jammer.transform, false);
            jammerDish.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            jammerDish.transform.localScale = new Vector3(1.2f, 0.12f, 1.2f);
            jammerDish.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            jammerDish.GetComponent<Renderer>().material = craneYellowMat;
            SafeDestroy(jammerDish.GetComponent<Collider>());

            // Attach Recon Spotable to Jammer
            var jammerSpot = jammer.AddComponent<DEADeyeSpotable>();
            jammerSpot.SetTargetInfo("COMMUNICATIONS JAMMER", SpotClassification.Objective, "High-frequency jammer hijacking emergency broadcasts. Cut its power junction to restore signals.", 100);

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

        private void CreateVehicle(Transform parent, Vector3 pos, Color carColor, bool isVan = false)
        {
            GameObject vehicle = new GameObject(isVan ? "DeliveryVan" : "Sedan");
            vehicle.transform.SetParent(parent, false);
            vehicle.transform.position = pos;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(vehicle.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = isVan ? new Vector3(2.2f, 1.8f, 5.2f) : new Vector3(2.0f, 1.2f, 4.4f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            Material carMat = new Material(litShader) { color = carColor };
            body.GetComponent<Renderer>().material = carMat;

            if (!isVan)
            {
                GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cabin.name = "Cabin";
                cabin.transform.SetParent(vehicle.transform, false);
                cabin.transform.localPosition = new Vector3(0f, 0.85f, -0.2f);
                cabin.transform.localScale = new Vector3(1.75f, 0.8f, 2.4f);
                cabin.GetComponent<Renderer>().material = windowGlassMat;
                SafeDestroy(cabin.GetComponent<Collider>());
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

            // Side parapet walls with 3.5m WALKWAY PASSAGE OPENINGS to bridges!
            CreateParapetWall(perch1.transform, new Vector3(-6.9f, rooftopHeight + 0.55f, -9.5f), new Vector3(0.4f, 1.1f, 4.8f), "LeftParapet_Rear");
            CreateParapetWall(perch1.transform, new Vector3(-6.9f, rooftopHeight + 0.55f, -2.0f), new Vector3(0.4f, 1.1f, 2.0f), "LeftParapet_Front");

            CreateParapetWall(perch1.transform, new Vector3(6.9f, rooftopHeight + 0.55f, -9.5f), new Vector3(0.4f, 1.1f, 4.8f), "RightParapet_Rear");
            CreateParapetWall(perch1.transform, new Vector3(6.9f, rooftopHeight + 0.55f, -2.0f), new Vector3(0.4f, 1.1f, 2.0f), "RightParapet_Front");

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

            // Lowered sandbag notch facing Commercial Plaza (East & North-East)
            CreateParapetWall(perch2.transform, new Vector3(18f, rooftopHeight + 0.38f, 7.9f), new Vector3(4.0f, 0.76f, 0.4f), "EastPerch_NotchNorth");
            CreateParapetWall(perch2.transform, new Vector3(21.9f, rooftopHeight + 0.55f, 4.0f), new Vector3(0.4f, 1.1f, 8.0f), "EastPerch_RightParapet");
            CreateParapetWall(perch2.transform, new Vector3(18f, rooftopHeight + 0.55f, 0.1f), new Vector3(8.0f, 1.1f, 0.4f), "EastPerch_BackParapet");

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

            // Lowered sandbag notch facing Construction skeleton & crane (North-West)
            CreateParapetWall(perch3.transform, new Vector3(-18f, rooftopHeight + 0.38f, 9.9f), new Vector3(4.0f, 0.76f, 0.4f), "WestPerch_NotchNorth");
            CreateParapetWall(perch3.transform, new Vector3(-21.9f, rooftopHeight + 0.55f, 6.0f), new Vector3(0.4f, 1.1f, 8.0f), "WestPerch_LeftParapet");
            CreateParapetWall(perch3.transform, new Vector3(-18f, rooftopHeight + 0.55f, 2.1f), new Vector3(8.0f, 1.1f, 0.4f), "WestPerch_BackParapet");

            GameObject sandbag3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandbag3.name = "WestPerch_Sandbag";
            sandbag3.transform.SetParent(perch3.transform, false);
            sandbag3.transform.position = new Vector3(-18f, rooftopHeight + 0.82f, 9.9f);
            sandbag3.transform.localScale = new Vector3(2.2f, 0.14f, 0.5f);
            sandbag3.GetComponent<Renderer>().material = sidewalkMaterial;

            // =========================================================================
            // WALKABLE SKYBRIDGES & CATWALKS (Connecting Perch 1 to Perch 2 & 3)
            // =========================================================================
            // 1. East Catwalk Bridge (3.0m wide)
            BuildWalkableBridge(networkRoot.transform, new Vector3(6.8f, rooftopHeight - 0.15f, -5.2f), new Vector3(14.0f, rooftopHeight - 0.15f, 2.0f), "Bridge_To_EastPlazaPerch");

            // 2. West Skybridge (3.0m wide)
            BuildWalkableBridge(networkRoot.transform, new Vector3(-6.8f, rooftopHeight - 0.15f, -5.2f), new Vector3(-14.0f, rooftopHeight - 0.15f, 4.0f), "Bridge_To_WestConstructionPerch");

            // Perimeter safety boundaries ensuring player cannot fall off outer edges
            CreateSafetyBoundary(networkRoot.transform, new Vector3(0f, rooftopHeight + 2.0f, -12.2f), new Vector3(14f, 4f, 0.3f));
            CreateSafetyBoundary(networkRoot.transform, new Vector3(22.2f, rooftopHeight + 2.0f, 4f), new Vector3(0.3f, 4f, 8f));
            CreateSafetyBoundary(networkRoot.transform, new Vector3(-22.2f, rooftopHeight + 2.0f, 6f), new Vector3(0.3f, 4f, 8f));
        }

        private void BuildWalkableBridge(Transform parent, Vector3 start, Vector3 end, string name)
        {
            GameObject bridge = new GameObject(name);
            bridge.transform.SetParent(parent, false);

            Vector3 mid = (start + end) * 0.5f;
            float length = Vector3.Distance(start, end);
            Vector3 dir = (end - start).normalized;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            bridge.transform.position = mid;
            bridge.transform.rotation = rot;

            // Solid Floor (3.0m wide with BoxCollider)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "BridgeFloor";
            floor.transform.SetParent(bridge.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(3.0f, 0.3f, length);
            floor.GetComponent<Renderer>().material = metalRooftopMat;

            // Left Handrail (at ±1.45m)
            GameObject railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railL.name = "Handrail_Left";
            railL.transform.SetParent(bridge.transform, false);
            railL.transform.localPosition = new Vector3(-1.45f, 0.6f, 0f);
            railL.transform.localScale = new Vector3(0.1f, 1.1f, length);
            railL.GetComponent<Renderer>().material = facadeTrimMat;
            BoxCollider colL = railL.GetComponent<BoxCollider>();
            if (colL != null) colL.size = new Vector3(1f, 1f, 0.85f);

            // Right Handrail (at ±1.45m)
            GameObject railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railR.name = "Handrail_Right";
            railR.transform.SetParent(bridge.transform, false);
            railR.transform.localPosition = new Vector3(1.45f, 0.6f, 0f);
            railR.transform.localScale = new Vector3(0.1f, 1.1f, length);
            railR.GetComponent<Renderer>().material = facadeTrimMat;
            BoxCollider colR = railR.GetComponent<BoxCollider>();
            if (colR != null) colR.size = new Vector3(1f, 1f, 0.85f);
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
