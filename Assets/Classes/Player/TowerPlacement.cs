using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TowerPlacement : MonoBehaviour
{
    [SerializeField] private Camera PlayerCamera;

    // How high above the ground the footprint check looks for other objects
    [SerializeField] private float FootprintHeight = 1f;
    [SerializeField] private Color ValidColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color InvalidColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color RangeColor = new Color(1f, 1f, 1f, 0.8f);

    // Holding right mouse rotates the camera, so only a short right click without dragging cancels
    [SerializeField] private float CancelClickMaxTime = 0.25f;
    [SerializeField] private float CancelClickMaxDistance = 5f;

    private const string CantPlaceTag = "CantPlace";

    private GameObject CurrentPlacingTower;
    private TowerData CurrentTowerData;
    private Renderer[] PlacingRenderers;
    private Color[] OriginalColors;
    private bool CanPlaceHere;
    private int LastPlacedFrame = -1;
    private CircleIndicator FootprintCircle;
    private CircleIndicator RangeCircle;
    private float RightClickStartTime;
    private Vector3 RightClickStartPosition;
    private float FootprintRadius;

    // True while dragging a tower or on the frame it was placed, so that click doesn't also select it
    public bool IsPlacing => CurrentPlacingTower != null || LastPlacedFrame == Time.frameCount;

    void Start()
    {
        FootprintCircle = CircleIndicator.Create("Footprint Circle", 0.05f);
        FootprintCircle.SetVisible(false);

        RangeCircle = CircleIndicator.Create("Placement Range Circle", 0.06f);
        RangeCircle.SetColor(RangeColor);
        RangeCircle.SetVisible(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (CurrentPlacingTower != null)
        {
            CanPlaceHere = false;

            Ray camray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(camray, out RaycastHit hitInfo, 100f))
            {
                CurrentPlacingTower.transform.position = hitInfo.point;
                FootprintCircle.transform.position = hitInfo.point;
                RangeCircle.transform.position = hitInfo.point;
                CanPlaceHere = IsValidPlacement(hitInfo) && GameLoopManager.Money >= CurrentTowerData.Cost;
            }

            Color placementColor = CanPlaceHere ? ValidColor : InvalidColor;
            SetTowerColor(placementColor);
            FootprintCircle.SetColor(placementColor);

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && CanPlaceHere && !overUI)
            {
                PlaceTower();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || IsRightClick())
            {
                CancelPlacement();
            }
        }
    }

    private bool IsRightClick()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RightClickStartTime = Time.unscaledTime;
            RightClickStartPosition = Input.mousePosition;
        }

        return Input.GetMouseButtonUp(1)
            && Time.unscaledTime - RightClickStartTime <= CancelClickMaxTime
            && Vector3.Distance(Input.mousePosition, RightClickStartPosition) <= CancelClickMaxDistance;
    }

    public void SetTowerToPlace(TowerData tower)
    {
        if (GameLoopManager.Money < tower.Cost)
        {
            return;
        }

        if (CurrentPlacingTower != null)
        {
            Destroy(CurrentPlacingTower);
        }

        CurrentTowerData = tower;
        CurrentPlacingTower = Instantiate(tower.TowerPrefab, Vector3.zero, Quaternion.identity);

        // So the raycast and overlap check don't hit the tower we are dragging
        SetLayerRecursive(CurrentPlacingTower, LayerMask.NameToLayer("Ignore Raycast"));

        PlacingRenderers = CurrentPlacingTower.GetComponentsInChildren<Renderer>();
        OriginalColors = new Color[PlacingRenderers.Length];
        for (int i = 0; i < PlacingRenderers.Length; i++)
        {
            OriginalColors[i] = PlacingRenderers[i].material.color;
        }

        FootprintRadius = Mathf.Max(tower.FootprintRadius, GetTowerGroundRadius() + 0.05f);
        FootprintCircle.SetRadius(FootprintRadius);
        FootprintCircle.SetVisible(true);

        RangeCircle.SetRadius(CurrentPlacingTower.GetComponent<TowerBehaviour>().Range);
        RangeCircle.SetVisible(true);
    }

    private bool IsValidPlacement(RaycastHit hitInfo)
    {
        // The surface under the mouse must allow this tower
        string placementTag = CurrentTowerData.PlacementTag;
        if (GetPlacementTag(hitInfo.collider.transform) != placementTag)
        {
            return false;
        }

        // Nothing else may be inside the footprint circle, like the path, decoration or another tower.
        // The capsule starts at the surface so a path at the same height is also found.
        float radius = FootprintRadius;
        Vector3 bottom = hitInfo.point;
        Vector3 top = hitInfo.point + Vector3.up * FootprintHeight;
        Collider[] overlaps = Physics.OverlapCapsule(bottom, top, radius, Physics.DefaultRaycastLayers);

        foreach (Collider overlap in overlaps)
        {
            if (GetPlacementTag(overlap.transform) != placementTag)
            {
                return false;
            }
        }

        return true;
    }

    // Distance from the middle of the tower to its furthest edge, so the circle always goes around the whole tower
    private float GetTowerGroundRadius()
    {
        Vector3 center = CurrentPlacingTower.transform.position;
        float radius = 0f;
        foreach (Renderer renderer in PlacingRenderers)
        {
            Bounds bounds = renderer.bounds;
            float x = Mathf.Max(Mathf.Abs(bounds.min.x - center.x), Mathf.Abs(bounds.max.x - center.x));
            float z = Mathf.Max(Mathf.Abs(bounds.min.z - center.z), Mathf.Abs(bounds.max.z - center.z));
            radius = Mathf.Max(radius, Mathf.Sqrt(x * x + z * z));
        }
        return radius;
    }

    // Uses the first CanPlace/CantPlace-style tag up the hierarchy,
    // so the Pathpieces get the CantPlace tag of their Path parent
    private string GetPlacementTag(Transform target)
    {
        while (target != null)
        {
            if (!target.CompareTag("Untagged"))
            {
                return target.tag;
            }
            target = target.parent;
        }
        return CantPlaceTag;
    }

    private void PlaceTower()
    {
        if (!GameLoopManager.TrySpendMoney(CurrentTowerData.Cost))
        {
            return;
        }

        TowerBehaviour placedTower = CurrentPlacingTower.GetComponent<TowerBehaviour>();
        placedTower.Data = CurrentTowerData;
        GameLoopManager.TowersInGame.Add(placedTower);
        LastPlacedFrame = Time.frameCount;

        SetTowerColor(null);
        SetLayerRecursive(CurrentPlacingTower, LayerMask.NameToLayer("Default"));

        ClearPlacing();
    }

    private void CancelPlacement()
    {
        Destroy(CurrentPlacingTower);
        ClearPlacing();
    }

    private void ClearPlacing()
    {
        CurrentPlacingTower = null;
        CurrentTowerData = null;
        PlacingRenderers = null;
        OriginalColors = null;
        FootprintCircle.SetVisible(false);
        RangeCircle.SetVisible(false);
    }

    // null restores the original colors
    private void SetTowerColor(Color? color)
    {
        for (int i = 0; i < PlacingRenderers.Length; i++)
        {
            PlacingRenderers[i].material.color = color ?? OriginalColors[i];
        }
    }

    private void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
