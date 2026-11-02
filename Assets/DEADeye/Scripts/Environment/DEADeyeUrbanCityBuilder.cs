using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

namespace DEADeye
{
    /// <summary>
    /// Constructs the high-rise urban sniper environment:
    /// - High-rise rooftop sniper nest with lowered parapet firing notch and safety collision boundaries.
    /// - City streets below with asphalt roadways, road markings, crosswalks, raised sidewalks, and streetlights.
    /// - Multi-story buildings with architectural setbacks, storefronts, balconies, and varied heights.
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
        [SerializeField] private Material roadStripeWhiteMat;
        [SerializeField] private Material metalRooftopMat;
        [SerializeField] private Material facadeTrimMat;

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
            BuildEscapeCorridors(cityRoot.transform);
            BuildRooftopSniperNest(cityRoot.transform);

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
            ground.transform.localScale = new Vector3(16f, 1f, 16f); // 160m x 160m
            ground.GetComponent<Renderer>().material = asphaltMaterial;

            // Main Boulevard (North-South, 14m wide)
            CreateRoadMarkings(streetsRoot.transform, new Vector3(-0.15f, 0.02f, 45f), 110f, isVertical: true);
            CreateRoadMarkings(streetsRoot.transform, new Vector3(0.15f, 0.02f, 45f), 110f, isVertical: true);

            // Cross Avenue (East-West at Z = 38m, 12m wide)
            CreateRoadMarkings(streetsRoot.transform, new Vector3(0f, 0.02f, 38f), 90f, isVertical: false);

            // Raised Sidewalks along Main Boulevard
            CreateSidewalk(streetsRoot.transform, new Vector3(-8.5f, 0.08f, 45f), new Vector3(3f, 0.16f, 110f));
            CreateSidewalk(streetsRoot.transform, new Vector3(8.5f, 0.08f, 45f), new Vector3(3f, 0.16f, 110f));

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

        private void CreateCrosswalk(Transform parent, Vector3 pos, float width, bool isNorthSouth = true)
        {
            int stripeCount = 7;
            float spacing = width / stripeCount;
            for (int i = 0; i < stripeCount; i++)
            {
                float offset = -width * 0.5f + (i + 0.5f) * spacing;
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "CrosswalkStripe";
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

            // Left Block (West Side)
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(-24f, 0f, 18f), new Vector3(18f, 22f, 22f), buildingConcreteMat, "WestCommercial_Front");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(-26f, 0f, 56f), new Vector3(20f, 32f, 26f), buildingBrickMat, "WestTower_Mid");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(-24f, 0f, 88f), new Vector3(18f, 20f, 24f), buildingConcreteMat, "WestOffice_North");

            // Right Block (East Side)
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(24f, 0f, 20f), new Vector3(18f, 28f, 24f), buildingBrickMat, "EastPlaza_Front");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(26f, 0f, 58f), new Vector3(20f, 24f, 28f), buildingConcreteMat, "EastCorporate_Mid");
            CreateCompositeBuilding(buildingsRoot.transform, new Vector3(24f, 0f, 90f), new Vector3(18f, 34f, 24f), buildingBrickMat, "EastSkyscraper_North");

            // Distant Background Skyscrapers (Atmospheric skyline depth)
            CreateBackgroundTower(buildingsRoot.transform, new Vector3(-55f, 0f, 115f), new Vector3(32f, 60f, 36f), buildingConcreteMat);
            CreateBackgroundTower(buildingsRoot.transform, new Vector3(0f, 0f, 125f), new Vector3(38f, 72f, 34f), buildingBrickMat);
            CreateBackgroundTower(buildingsRoot.transform, new Vector3(55f, 0f, 120f), new Vector3(34f, 58f, 34f), buildingConcreteMat);
        }

        /// <summary>
        /// Creates a composite urban building with commercial ground podium, clean facade window bands,
        /// cornice trims, and rooftop utility mechanical structures.
        /// NO tiny subpixel cubes -> eliminates white speckling / shimmering completely!
        /// </summary>
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

            // Storefront canopy / overhang
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "StorefrontCanopy";
            canopy.transform.SetParent(bldg.transform, false);
            canopy.transform.localPosition = new Vector3(0f, podiumHeight + 0.15f, 0f);
            canopy.transform.localScale = new Vector3(size.x + 0.6f, 0.3f, size.z + 0.6f);
            canopy.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(canopy.GetComponent<Collider>());

            // 3. Clean Architectural Facade Window Bands (Floor by Floor, clean broad bands)
            int floorCount = Mathf.FloorToInt((size.y - podiumHeight) / 3.6f);
            for (int f = 1; f <= floorCount; f++)
            {
                float floorY = podiumHeight + (f * 3.6f) - 1.2f;
                if (floorY > size.y - 1.5f) break;

                // Horizontal Window Band facing street
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = $"WindowBand_F{f}";
                band.transform.SetParent(bldg.transform, false);
                band.transform.localPosition = new Vector3(0f, floorY, 0f);
                band.transform.localScale = new Vector3(size.x + 0.08f, 1.4f, size.z + 0.08f);
                band.GetComponent<Renderer>().material = windowGlassMat;
                SafeDestroy(band.GetComponent<Collider>());

                // Architectural sill/spandrel trim
                GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = $"Trim_F{f}";
                trim.transform.SetParent(bldg.transform, false);
                trim.transform.localPosition = new Vector3(0f, floorY - 0.75f, 0f);
                trim.transform.localScale = new Vector3(size.x + 0.2f, 0.2f, size.z + 0.2f);
                trim.GetComponent<Renderer>().material = facadeTrimMat;
                SafeDestroy(trim.GetComponent<Collider>());
            }

            // 4. Roof Cornice & Crown Parapet
            GameObject cornice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornice.name = "RoofCornice";
            cornice.transform.SetParent(bldg.transform, false);
            cornice.transform.localPosition = new Vector3(0f, size.y + 0.3f, 0f);
            cornice.transform.localScale = new Vector3(size.x + 0.5f, 0.6f, size.z + 0.5f);
            cornice.GetComponent<Renderer>().material = facadeTrimMat;
            SafeDestroy(cornice.GetComponent<Collider>());

            // 5. Rooftop Mechanical Infrastructure (HVAC Penthouse, Water Tank)
            GameObject penthouse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            penthouse.name = "MechanicalPenthouse";
            penthouse.transform.SetParent(bldg.transform, false);
            penthouse.transform.localPosition = new Vector3(0f, size.y + 1.8f, 0f);
            penthouse.transform.localScale = new Vector3(size.x * 0.45f, 2.4f, size.z * 0.45f);
            penthouse.GetComponent<Renderer>().material = buildingConcreteMat;

            GameObject chiller = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chiller.name = "HVAC_Chiller";
            chiller.transform.SetParent(bldg.transform, false);
            chiller.transform.localPosition = new Vector3(size.x * 0.3f, size.y + 1.0f, -size.z * 0.2f);
            chiller.transform.localScale = new Vector3(2.5f, 1.4f, 2.0f);
            chiller.GetComponent<Renderer>().material = metalRooftopMat;
        }

        private void CreateBackgroundTower(Transform parent, Vector3 groundPos, Vector3 size, Material facadeMat)
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = $"BackgroundSkyscraper_{groundPos.x:0}_{groundPos.z:0}";
            tower.transform.SetParent(parent, false);
            tower.transform.position = new Vector3(groundPos.x, size.y * 0.5f, groundPos.z);
            tower.transform.localScale = size;
            tower.GetComponent<Renderer>().material = facadeMat;

            // Spire / Crown
            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "Spire";
            spire.transform.SetParent(tower.transform, false);
            spire.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            spire.transform.localScale = new Vector3(0.06f, 0.16f, 0.06f);
            spire.GetComponent<Renderer>().material = metalRooftopMat;
            SafeDestroy(spire.GetComponent<Collider>());
        }

        private void BuildStreetDetails(Transform parent)
        {
            GameObject propsRoot = new GameObject("Street_Props");
            propsRoot.transform.SetParent(parent, false);

            // Streetlights
            float[] lightZ = new float[] { 16f, 32f, 50f, 68f };
            foreach (float z in lightZ)
            {
                CreateStreetlamp(propsRoot.transform, new Vector3(-7.2f, 0f, z));
                CreateStreetlamp(propsRoot.transform, new Vector3(7.2f, 0f, z));
            }

            // Parked Cars / Vans along curbs (Tactical cover objects)
            CreateVehicle(propsRoot.transform, new Vector3(-6.2f, 0.75f, 22f), new Color(0.18f, 0.35f, 0.55f));
            CreateVehicle(propsRoot.transform, new Vector3(6.2f, 0.75f, 48f), new Color(0.75f, 0.15f, 0.15f));
            CreateVehicle(propsRoot.transform, new Vector3(-6.2f, 0.95f, 64f), new Color(0.88f, 0.88f, 0.88f), isVan: true);
            CreateVehicle(propsRoot.transform, new Vector3(6.2f, 0.75f, 28f), new Color(0.20f, 0.22f, 0.25f));

            // Concrete Jersey Barriers near intersection
            CreateJerseyBarrier(propsRoot.transform, new Vector3(-5.5f, 0.45f, 36f));
            CreateJerseyBarrier(propsRoot.transform, new Vector3(5.5f, 0.45f, 36f));

            // Dumpsters along side alleys
            CreateDumpster(propsRoot.transform, new Vector3(-11.5f, 0.75f, 32f));
            CreateDumpster(propsRoot.transform, new Vector3(11.5f, 0.75f, 44f));
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

            // Lamp fixture
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

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(vehicle.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = isVan ? new Vector3(2.2f, 1.8f, 5.2f) : new Vector3(2.0f, 1.2f, 4.4f);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            Material carMat = new Material(litShader) { color = carColor };
            body.GetComponent<Renderer>().material = carMat;

            // Cabin / Roof for sedan
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

            // Corridor 1: North Highway Tunnel (Z = +88m)
            CreateEscapeCorridorNode(escapeRoot.transform, "North Highway Tunnel", new Vector3(0f, 1.5f, 88f), new Vector3(16f, 4f, 6f));

            // Corridor 2: East Subway Station (X = +42m, Z = 38m)
            CreateEscapeCorridorNode(escapeRoot.transform, "East Subway Station", new Vector3(42f, 1.5f, 38f), new Vector3(8f, 4f, 14f));

            // Corridor 3: West River Bridge (X = -42m, Z = 38m)
            CreateEscapeCorridorNode(escapeRoot.transform, "West River Bridge", new Vector3(-42f, 1.5f, 38f), new Vector3(8f, 4f, 14f));

            // Setup 8 Strategic Spawn Nodes
            // CRITICAL: First 3 nodes are in DIRECT, IMMEDIATE SIGHTLINE of the sniper perch!
            CreateSpawnNode("Spawn_WestCrosswalk", new Vector3(-5.5f, 0.1f, 26f));    // In view immediately
            CreateSpawnNode("Spawn_IntersectionCenter", new Vector3(1.5f, 0.1f, 36f)); // In view immediately
            CreateSpawnNode("Spawn_EastSidewalk", new Vector3(8.0f, 0.1f, 24f));       // In view immediately
            CreateSpawnNode("Spawn_BoulevardMid", new Vector3(-1.0f, 0.1f, 48f));       // In view down boulevard
            CreateSpawnNode("Spawn_AvenueWest", new Vector3(-24f, 0.1f, 38f));
            CreateSpawnNode("Spawn_AvenueEast", new Vector3(24f, 0.1f, 38f));
            CreateSpawnNode("Spawn_NorthBoulevard", new Vector3(1.0f, 0.1f, 65f));
            CreateSpawnNode("Spawn_FarIntersection", new Vector3(-2.0f, 0.1f, 75f));

            // Setup Waypoint Nodes along street intersections
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
            floor.transform.localScale = new Vector3(14f, 0.3f, 12f);
            floor.GetComponent<Renderer>().material = metalRooftopMat;

            // FRONT PARAPET WITH FIRING NOTCH (EMBRASURE):
            // Center is lowered to 0.78m height (top at Y = 26.78m) so player camera at Y = 27.6m has 100% CLEAR DOWNWARD SIGHTLINE!
            CreateParapetWall(nest.transform, new Vector3(-4.4f, rooftopHeight + 0.55f, -1.1f), new Vector3(4.8f, 1.1f, 0.4f), "FrontParapet_WestWing");
            CreateParapetWall(nest.transform, new Vector3(4.4f, rooftopHeight + 0.55f, -1.1f), new Vector3(4.8f, 1.1f, 0.4f), "FrontParapet_EastWing");
            // Central Lowered Firing Notch
            CreateParapetWall(nest.transform, new Vector3(0f, rooftopHeight + 0.38f, -1.1f), new Vector3(4.0f, 0.76f, 0.4f), "FrontParapet_SniperNotch");

            // Back & Side Parapets (Standard 1.15m height)
            CreateParapetWall(nest.transform, new Vector3(0f, rooftopHeight + 0.55f, -11.9f), new Vector3(14f, 1.1f, 0.4f), "BackParapet");
            CreateParapetWall(nest.transform, new Vector3(-6.9f, rooftopHeight + 0.55f, -6.5f), new Vector3(0.4f, 1.1f, 11f), "LeftParapet");
            CreateParapetWall(nest.transform, new Vector3(6.9f, rooftopHeight + 0.55f, -6.5f), new Vector3(0.4f, 1.1f, 11f), "RightParapet");

            // CRITICAL SAFETY COLLISION BOUNDARY (Invisible walls up to 4m height so player NEVER falls off)
            CreateSafetyBoundary(nest.transform, new Vector3(0f, rooftopHeight + 2.0f, -0.9f), new Vector3(14f, 4f, 0.3f));
            CreateSafetyBoundary(nest.transform, new Vector3(0f, rooftopHeight + 2.0f, -12.1f), new Vector3(14f, 4f, 0.3f));
            CreateSafetyBoundary(nest.transform, new Vector3(-7.1f, rooftopHeight + 2.0f, -6.5f), new Vector3(0.3f, 4f, 11f));
            CreateSafetyBoundary(nest.transform, new Vector3(7.1f, rooftopHeight + 2.0f, -6.5f), new Vector3(0.3f, 4f, 11f));

            // Weapon Rest Sandbags across the central firing notch (Top at Y = 26.88m)
            GameObject sandbag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sandbag.name = "SniperWeaponRest";
            sandbag.transform.SetParent(nest.transform, false);
            sandbag.transform.position = new Vector3(0f, rooftopHeight + 0.82f, -1.1f);
            sandbag.transform.localScale = new Vector3(2.4f, 0.14f, 0.5f);
            sandbag.GetComponent<Renderer>().material = sidewalkMaterial;

            // HVAC Unit on Rooftop (provides cover/rooftop ambiance)
            GameObject hvac = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hvac.name = "HVAC_Unit";
            hvac.transform.SetParent(nest.transform, false);
            hvac.transform.position = new Vector3(-3.8f, rooftopHeight + 0.9f, -7.5f);
            hvac.transform.localScale = new Vector3(2.4f, 1.8f, 2.4f);
            hvac.GetComponent<Renderer>().material = metalRooftopMat;

            // Communication Antenna Mast
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "AntennaMast";
            antenna.transform.SetParent(nest.transform, false);
            antenna.transform.position = new Vector3(5.0f, rooftopHeight + 3.0f, -9.5f);
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
            boundary.layer = LayerMask.NameToLayer("Ignore Raycast");
            boundary.transform.SetParent(parent, false);
            boundary.transform.position = pos;
            BoxCollider col = boundary.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = false; // Physical solid barrier
        }
    }
}
