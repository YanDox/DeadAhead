using UnityEngine;

public class BusRepair : MonoBehaviour
{
    public int requiredParts = 5;
    public int installedParts = 0;
    public float repairTime = 20f;
    public float repairRadius = 3f;

    private float repairProgress = 0f;
    private bool isRepairing = false;

    public Vector3 GetRepairPosition(Vector3 companionPosition)
    {
        Vector3 directionToBus = (transform.position - companionPosition).normalized;

        Vector3 surfacePoint = transform.position - directionToBus * repairRadius;

        UnityEngine.AI.NavMeshHit navHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(surfacePoint, out navHit, repairRadius * 2, UnityEngine.AI.NavMesh.AllAreas))
        {
            return navHit.position;
        }

        return transform.position - directionToBus * repairRadius;
    }

    public bool TryRepair(CompanionInventory companionInventory)
    {
        if (installedParts >= requiredParts)
        {
            Debug.Log("Автобус уже полностью отремонтирован!");
            return false;
        }

        if (companionInventory.items[CompanionInventory.BUS_PART] <= 0)
        {
            Debug.Log("У компаньона нет деталей!");
            return false;
        }

        if (!isRepairing)
        {
            isRepairing = true;
        }

        repairProgress += Time.deltaTime;

        if (repairProgress >= repairTime)
        {
            repairProgress = 0f;
            companionInventory.UseItem(CompanionInventory.BUS_PART);
            installedParts++;
            Debug.Log($"Установлена деталь ({installedParts}/{requiredParts})");

            if (installedParts >= requiredParts)
                Debug.Log("Автобус полностью отремонтирован!");
                // Здесь можно добавить запуск кат-сцены
            return true;
        }
        return false;
    }

    public void ResetRepair()
    {
        repairProgress = 0f;
        isRepairing = false;
    }
}