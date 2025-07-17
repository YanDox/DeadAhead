using UnityEngine;

public class CharlotteController : MonoBehaviour
{
    [Header("Circle Settings")]
    public Vector3 centerPoint = Vector3.zero; // Центр окружности
    public float radius = 50f;                  // Радиус окружности
    public float speed = 1f;                   // Скорость движения (обороты/секунду)

    [Header("Orientation")]
    public bool faceMovementDirection = true;  // Поворачивать персонажа по направлению движения

    private float currentAngle;                // Текущий угол в радианах

    void Update()
    {
        // Увеличиваем угол с учетом времени и скорости
        currentAngle += speed * 2 * Mathf.PI * Time.deltaTime;

        // Рассчитываем новую позицию
        Vector3 newPosition = CalculateCirclePosition(currentAngle);

        // Применяем позицию
        transform.position = newPosition;

        // Поворот персонажа (если включен)
        if (faceMovementDirection)
        {
            OrientCharacter();
        }
    }

    // Вычисление позиции на окружности
    private Vector3 CalculateCirclePosition(float angle)
    {
        return new Vector3(
            centerPoint.x + Mathf.Cos(angle) * radius,
            centerPoint.y,
            centerPoint.z + Mathf.Sin(angle) * radius
        );
    }

    // Ориентация персонажа по касательной к окружности
    private void OrientCharacter()
    {
        // Вычисляем вектор направления движения (касательный вектор)
        Vector3 tangent = new Vector3(
            -Mathf.Sin(currentAngle),
            0,
            Mathf.Cos(currentAngle)
        ).normalized;

        // Применяем поворот
        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerPoint, radius);
        Gizmos.DrawLine(centerPoint, transform.position);
    }
}