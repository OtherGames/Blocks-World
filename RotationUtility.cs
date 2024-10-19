using UnityEngine;

public class RotationUtility
{
    // ћетод дл€ поворота фигуры в плоскости вокруг оси Y
    public static Vector3[] RotatePoints(Vector3[] points, float angleInDegrees)
    {
        // ѕреобразование угла в радианы
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // ¬ычисл€ем синус и косинус дл€ поворотной матрицы
        float cosAngle = Mathf.Cos(angleInRadians);
        float sinAngle = Mathf.Sin(angleInRadians);

        // ћассив дл€ хранени€ новых точек
        Vector3[] rotatedPoints = new Vector3[points.Length];

        // ѕрименение матрицы вращени€ к каждой точке
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 point = points[i];

            // ѕримен€ем вращение только по оси Y (т.е. X и Z координаты измен€ютс€)
            float newX = point.x * cosAngle - point.z * sinAngle;
            float newZ = point.x * sinAngle + point.z * cosAngle;

            // —охран€ем новую точку
            rotatedPoints[i] = new Vector3(newX, point.y, newZ);
        }

        // ¬озвращаем массив повернутых точек
        return rotatedPoints;
    }


    // ћетод дл€ поворота фигуры в плоскости вокруг выбранной оси
    public static Vector3[] RotatePoints(Vector3[] points, float angleInDegrees, RotationAxis rotationAxis)
    {
        // ѕреобразование угла в радианы
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // ¬ычисл€ем синус и косинус дл€ поворотной матрицы
        float cosAngle = Mathf.Cos(angleInRadians);
        float sinAngle = Mathf.Sin(angleInRadians);

        // ћассив дл€ хранени€ новых точек
        Vector3[] rotatedPoints = new Vector3[points.Length];

        // ѕрименение матрицы вращени€ к каждой точке
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 point = points[i];

            switch (rotationAxis)
            {
                case RotationAxis.X:
                    // ¬ращение вокруг оси X (измен€ютс€ Y и Z)
                    float newY_X = point.y * cosAngle - point.z * sinAngle;
                    float newZ_X = point.y * sinAngle + point.z * cosAngle;
                    rotatedPoints[i] = new Vector3(point.x, newY_X, newZ_X);
                    break;

                case RotationAxis.Y:
                    // ¬ращение вокруг оси Y (измен€ютс€ X и Z)
                    float newX_Y = point.x * cosAngle - point.z * sinAngle;
                    float newZ_Y = point.x * sinAngle + point.z * cosAngle;
                    rotatedPoints[i] = new Vector3(newX_Y, point.y, newZ_Y);
                    break;

                case RotationAxis.Z:
                    // ¬ращение вокруг оси Z (измен€ютс€ X и Y)
                    float newX_Z = point.x * cosAngle - point.y * sinAngle;
                    float newY_Z = point.x * sinAngle + point.y * cosAngle;
                    rotatedPoints[i] = new Vector3(newX_Z, newY_Z, point.z);
                    break;
            }
        }

        // ¬озвращаем массив повернутых точек
        return rotatedPoints;
    }


    // ћетод дл€ поворота фигуры вокруг произвольной оси вращени€, указанной в виде Vector3
    public static Vector3[] RotatePoints(Vector3[] points, float angleInDegrees, Vector3 rotationAxis)
    {
        // Ќормализуем ось вращени€ (она должна быть единичной длины)
        Vector3 normalizedAxis = rotationAxis.normalized;

        // —оздаем кватернион вращени€ вокруг заданной оси и угла
        Quaternion rotation = Quaternion.AngleAxis(angleInDegrees, normalizedAxis);

        // ћассив дл€ хранени€ новых точек
        Vector3[] rotatedPoints = new Vector3[points.Length];

        // ѕримен€ем вращение к каждой точке
        for (int i = 0; i < points.Length; i++)
        {
            rotatedPoints[i] = rotation * points[i]; // ѕрименение кватерниона к точке
        }

        // ¬озвращаем массив повернутых точек
        return rotatedPoints;
    }
}
