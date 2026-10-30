using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TowerPlacement : MonoBehaviour
{
    [SerializeField] private Camera PlayerCamera;

    // Size of the area the tower takes up on the ground
    [SerializeField] private Vector3 FootprintSize = new Vector3(0.9f, 0.5f, 0.9f);
    [SerializeField] private Color ValidColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color InvalidColor = new Color(1f, 0.3f, 0.3f);

    private const string CantPlaceTag = "CantPlace";

    private GameObject CurrentPlacingTower;
    private TowerData CurrentTowerData;
    private Renderer[] PlacingRenderers;
    private Color[] OriginalColors;
    private bool CanPlaceHere;

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
                CanPlaceHere = IsValidPlacement(hitInfo);
            }

            SetTowerColor(CanPlaceHere ? ValidColor : InvalidColor);

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && CanPlaceHere && !overUI)
            {
                PlaceTower();
            }
        }
    }

    public void SetTowerToPlace(TowerData tower)
    {
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
    }

    private bool IsValidPlacement(RaycastHit hitInfo)
    {
        // The surface under the mouse must allow this tower
        string placementTag = CurrentTowerData.PlacementTag;
        if (GetPlacementTag(hitInfo.collider.transform) != placementTag)
        {
            return false;
        }

        // Nothing else may be inside the footprint, like the path, decoration or another tower.
        // The box goes slightly below the surface so a path at the same height is also found.
        Vector3 halfExtents = FootprintSize / 2f;
        Vector3 center = hitInfo.point + Vector3.up * (halfExtents.y - 0.05f);
        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, CurrentPlacingTower.transform.rotation, Physics.DefaultRaycastLayers);

        foreach (Collider overlap in overlaps)
        {
            if (GetPlacementTag(overlap.transform) != placementTag)
            {
                return false;
            }
        }

        return true;
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
        SetTowerColor(null);
        SetLayerRecursive(CurrentPlacingTower, LayerMask.NameToLayer("Default"));

        CurrentPlacingTower = null;
        CurrentTowerData = null;
        PlacingRenderers = null;
        OriginalColors = null;
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
