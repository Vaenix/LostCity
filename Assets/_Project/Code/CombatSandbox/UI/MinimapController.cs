using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class MinimapController : MonoBehaviour
    {
        [SerializeField] private RectTransform markerRoot;
        [SerializeField] private Image markerTemplate;
        [SerializeField] private Vector2 worldHalfExtents = new Vector2(20f, 20f);
        [SerializeField] private Color playerColor = new Color(0.25f, 0.95f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color bossColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float refreshIntervalSeconds = 0.25f;

        private readonly List<MinimapTrackedEntity> trackedEntities = new List<MinimapTrackedEntity>();
        private readonly Dictionary<MinimapTrackedEntity, Image> markers = new Dictionary<MinimapTrackedEntity, Image>();
        private float nextRefreshTime;

        private void Awake()
        {
            if (markerTemplate != null)
            {
                markerTemplate.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefreshTime)
            {
                RefreshTrackedEntities();
                nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
            }

            UpdateMarkers();
        }

        private void RefreshTrackedEntities()
        {
            trackedEntities.Clear();
            trackedEntities.AddRange(FindObjectsOfType<MinimapTrackedEntity>());

            List<MinimapTrackedEntity> missing = new List<MinimapTrackedEntity>();
            foreach (KeyValuePair<MinimapTrackedEntity, Image> pair in markers)
            {
                if (pair.Key == null || !trackedEntities.Contains(pair.Key))
                {
                    missing.Add(pair.Key);
                }
            }

            for (int i = 0; i < missing.Count; i++)
            {
                if (markers.TryGetValue(missing[i], out Image marker) && marker != null)
                {
                    Destroy(marker.gameObject);
                }

                markers.Remove(missing[i]);
            }
        }

        private void UpdateMarkers()
        {
            if (markerRoot == null || markerTemplate == null)
            {
                return;
            }

            for (int i = 0; i < trackedEntities.Count; i++)
            {
                MinimapTrackedEntity entity = trackedEntities[i];
                if (entity == null)
                {
                    continue;
                }

                Image marker = GetOrCreateMarker(entity);
                RectTransform markerTransform = (RectTransform)marker.transform;
                marker.color = GetMarkerColor(entity.EntityType);
                markerTransform.sizeDelta = entity.EntityType == MinimapEntityType.Boss ? new Vector2(10f, 10f) : new Vector2(6f, 6f);
                markerTransform.anchoredPosition = WorldToMinimapPosition(entity.transform.position);
            }
        }

        private Image GetOrCreateMarker(MinimapTrackedEntity entity)
        {
            if (markers.TryGetValue(entity, out Image marker) && marker != null)
            {
                return marker;
            }

            Image newMarker = Instantiate(markerTemplate, markerRoot);
            newMarker.name = entity.EntityType + "Marker";
            newMarker.raycastTarget = false;
            newMarker.gameObject.SetActive(true);
            markers[entity] = newMarker;
            return newMarker;
        }

        private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
        {
            Vector2 halfSize = markerRoot.rect.size * 0.5f;
            float normalizedX = Mathf.Clamp(worldPosition.x / Mathf.Max(0.01f, worldHalfExtents.x), -1f, 1f);
            float normalizedY = Mathf.Clamp(worldPosition.y / Mathf.Max(0.01f, worldHalfExtents.y), -1f, 1f);
            return new Vector2(normalizedX * halfSize.x, normalizedY * halfSize.y);
        }

        private Color GetMarkerColor(MinimapEntityType entityType)
        {
            switch (entityType)
            {
                case MinimapEntityType.Player:
                    return playerColor;
                case MinimapEntityType.Boss:
                    return bossColor;
                default:
                    return enemyColor;
            }
        }
    }
}
