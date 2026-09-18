using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PixelCamera
{
    /// <summary>
    /// Script para criar uma cena de teste automaticamente
    /// Adicione a um GameObject vazio na cena e pressione Play
    /// </summary>
    [ExecuteInEditMode]
    public class PixelCameraTestScene : MonoBehaviour
    {
        [Header("Configuração Automática")]
        [Tooltip("Criar objetos de teste ao iniciar")]
        [SerializeField] private bool createTestObjects = true;
        
        [Tooltip("Cor de fundo")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.2f);
        
        [Header("Objetos Gerados")]
        [SerializeField] private bool createCubes = true;
        [SerializeField] private bool createSpheres = true;
        [SerializeField] private bool createGround = true;
        [SerializeField] private bool createLights = true;

        private void Start()
        {
            if (!createTestObjects) return;
            
            SetupCamera();
            
            if (createCubes) CreateTestCubes();
            if (createSpheres) CreateTestSpheres();
            if (createGround) CreateGround();
            if (createLights) CreateLights();
            
            Debug.Log("[PixelCamera] Cena de teste criada com sucesso!");
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.transform.position = new Vector3(0, 3, -8);
            cam.transform.rotation = Quaternion.Euler(15, 0, 0);
            
            Debug.Log("[PixelCamera] Câmera configurada");
        }

        private void CreateTestCubes()
        {
            // Criar grid de cubos coloridos
            Color[] colors = {
                Color.red, Color.green, Color.blue,
                Color.yellow, Color.cyan, Color.magenta,
                new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f), new Color(0f, 1f, 0.5f)
            };

            int index = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"TestCube_{index}";
                    cube.transform.position = new Vector3(x * 2f, 0.5f, z * 2f);
                    cube.transform.localScale = Vector3.one * 0.9f;
                    
                    // Aplicar material colorido
                    var renderer = cube.GetComponent<MeshRenderer>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = colors[index % colors.Length];
                    renderer.material = mat;
                    
                    index++;
                }
            }
            
            Debug.Log("[PixelCamera] Cubos de teste criados");
        }

        private void CreateTestSpheres()
        {
            // Esferas acima dos cubos
            for (int i = 0; i < 3; i++)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"TestSphere_{i}";
                sphere.transform.position = new Vector3(-3 + i * 3, 2.5f, 0);
                sphere.transform.localScale = Vector3.one * 0.7f;
                
                var renderer = sphere.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(Random.value, Random.value, Random.value);
                renderer.material = mat;
            }
            
            Debug.Log("[PixelCamera] Esferas de teste criadas");
        }

        private void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TestGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2, 1, 2);
            
            var renderer = ground.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.3f, 0.4f);
            renderer.material = mat;
            
            Debug.Log("[PixelCamera] Chão de teste criado");
        }

        private void CreateLights()
        {
            // Directional Light
            var dirLight = new GameObject("TestDirectionalLight");
            var light = dirLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.0f;
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Point lights coloridos
            var pointLight1 = new GameObject("TestPointLight_Red");
            var pl1 = pointLight1.AddComponent<Light>();
            pl1.type = LightType.Point;
            pl1.color = Color.red;
            pl1.intensity = 2f;
            pl1.range = 5f;
            pointLight1.transform.position = new Vector3(-3, 3, 0);
            
            var pointLight2 = new GameObject("TestPointLight_Blue");
            var pl2 = pointLight2.AddComponent<Light>();
            pl2.type = LightType.Point;
            pl2.color = Color.blue;
            pl2.intensity = 2f;
            pl2.range = 5f;
            pointLight2.transform.position = new Vector3(3, 3, 0);
            
            Debug.Log("[PixelCamera] Luzes de teste criadas");
        }
    }
}
