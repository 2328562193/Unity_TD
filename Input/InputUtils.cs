public class InputUtils {

    private static Camera mainCamera {
        get {
            if (_mainCamera == null) _mainCamera = Camera.main;
            return _mainCamera;
        }
    }
    private static Camera _mainCamera;

    public static void SetMainCamera(Camera cam){
        _mainCamera = cam;
    }

    public static T WorldInteractable<T>() where T : class, IInteractable {
        if (IsPointerOverUI()) return null;
        Vector2 mousePosition = GetInput<MousePosition>(InputEnum.MousePosition).value;
        return Physics2DRaycast<T>(mousePosition, out T interactable) ? interactable : null;
    }

    private static bool IsPointerOverUI() {
        if (EventSystem.current == null) {
            GameLogger.LogWarning("EventSystem.current is null.");
            return false;
        }
        return EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsPointerOverUI(Vector2 screenPos) {
        if (EventSystem.current == null) {
            GameLogger.LogWarning("EventSystem.current is null.");
            return false;
        }
        PointerEventData eventData = new PointerEventData(EventSystem.current); // 可优化
        eventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    private static bool GetUIAtPosition<T>(Vector2 screenPos, out T component) where T : Component {
        component = null;
        if (EventSystem.current == null) {
            GameLogger.LogWarning("EventSystem.current is null.");
            return false;
        }
        PointerEventData eventData = new PointerEventData(EventSystem.current); // 可优化
        eventData.position = screenPos;
        List<RaycastResult> uiHitResults = new List<RaycastResult>(); // 可优化
        EventSystem.current.RaycastAll(eventData, uiHitResults);

        foreach (var result in uiHitResults) {
            if (result.gameObject.TryGetComponent<T>(out component)) {
                return true;
            }
        }
        return false;
    }

    private static bool Physics2DRaycast<T>(Vector2 screenPos, out T component) where T : Component {
        component = null;
        if (mainCamera == null) {
            GameLogger.LogWarning("mainCamera is null.");
            return false;
        }
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPos);

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(interactable2DLayers);
        filter.useTriggers = true;

        Collider2D[] results = new Collider2D[10];
        int hitCount = Physics2D.OverlapPoint(worldPoint, filter, results);
        if (hitCount == 0) return false;

        List<Collider2D> hits = new List<Collider2D>(results.Take(hitCount));

        hits.Sort((a, b) => {
            int orderA = a.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
            int orderB = b.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
            return orderB.CompareTo(orderA);
        });

        foreach (var col in hits) {
            if (col != null && col.gameObject.TryGetComponent<T>(out component)) {
                return true;
            }
        }

        return false;
    }
}