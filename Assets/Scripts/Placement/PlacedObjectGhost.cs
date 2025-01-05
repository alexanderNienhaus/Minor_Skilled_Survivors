using UnityEngine;

public class PlacedObjectGhost : MonoBehaviour
{
    [SerializeField] private Material ghostMaterialBlue;
    [SerializeField] private Material ghostMaterialRed;
    //[SerializeField] private int ghostLayerBlue = 11;
    //[SerializeField] private int ghostLayerRed = 12;
    [SerializeField] private float lerpSpeed = 15f;
    [SerializeField] private float ghostY = 1f;

    private Transform visual;

    private void Start()
    {
        RefreshVisual();
    }

    private void OnEnable()
    {
        EventBus<OnSelectedPlacableObjectChanged>.OnEvent += OnSelectedChanged;
    }

    private void OnDisable()
    {
        EventBus<OnSelectedPlacableObjectChanged>.OnEvent -= OnSelectedChanged;
    }

    private void OnSelectedChanged(OnSelectedPlacableObjectChanged pOnSelectedPlacableObjectChanged)
    {
        RefreshVisual();
    }

    private void LateUpdate()
    {
        if (visual != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            Destroy(visual.gameObject);
            visual = null;
        }

        if (GridObjectPlacementManager.Instance == null)
        {
            return;
        }

        Vector3 targetPos = GridObjectPlacementManager.Instance.GetMouseWorldSnappedPos() - new Vector3(0.5f, 0, 0.5f) * GridObjectPlacementManager.Instance.GetCellSize();
        
        targetPos.y = ghostY;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, GridObjectPlacementManager.Instance.GetPlacedObjectRotation(), Time.deltaTime * lerpSpeed);

        if (GridObjectPlacementManager.Instance.GetCanBuild() && visual != null)
        {
            //SetLayerRecursive(visual.gameObject, ghostLayerBlue);
            foreach (MeshRenderer meshRenderer in visual.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = ghostMaterialBlue;
            }
        }
        else if (visual != null)
        {
            //SetLayerRecursive(visual.gameObject, ghostLayerRed);
            foreach (MeshRenderer meshRenderer in visual.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = ghostMaterialRed;
            }
        }
    }

    private void RefreshVisual()
    {
        if (visual != null)
        {
            Destroy(visual.gameObject);
            visual = null;
        }

        PlacableObjectTypeSO placedObjectTypeSO = GridObjectPlacementManager.Instance?.GetPlacedObjectTypeSO();

        if (placedObjectTypeSO != null)
        {
            visual = Instantiate(placedObjectTypeSO.visual, Vector3.zero, Quaternion.identity);
            visual.parent = transform;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
        }
    }    
    private void SetLayerRecursive(GameObject pTargetGameObject, int pLayer)
    {
        pTargetGameObject.layer = pLayer;
        foreach (Transform child in pTargetGameObject.transform)
        {
            child.gameObject.layer = pLayer;
            if (child.GetComponentInChildren<Transform>() != null)
            {
                SetLayerRecursive(child.gameObject, pLayer);
            }
        }
    }    
}
