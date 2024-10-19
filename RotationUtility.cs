using UnityEngine;

public class RotationUtility
{
    // ����� ��� �������� ������ � ��������� ������ ��� Y
    public static Vector3[] RotatePoints(Vector3[] points, float angleInDegrees)
    {
        // �������������� ���� � �������
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // ��������� ����� � ������� ��� ���������� �������
        float cosAngle = Mathf.Cos(angleInRadians);
        float sinAngle = Mathf.Sin(angleInRadians);

        // ������ ��� �������� ����� �����
        Vector3[] rotatedPoints = new Vector3[points.Length];

        // ���������� ������� �������� � ������ �����
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 point = points[i];

            // ��������� �������� ������ �� ��� Y (�.�. X � Z ���������� ����������)
            float newX = point.x * cosAngle - point.z * sinAngle;
            float newZ = point.x * sinAngle + point.z * cosAngle;

            // ��������� ����� �����
            rotatedPoints[i] = new Vector3(newX, point.y, newZ);
        }

        // ���������� ������ ���������� �����
        return rotatedPoints;
    }


    // ����� ��� �������� ������ � ��������� ������ ��������� ���
    public static Vector3[] RotatePoints(Vector3[] points, float angleInDegrees, RotationAxis rotationAxis)
    {
        // �������������� ���� � �������
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // ��������� ����� � ������� ��� ���������� �������
        float cosAngle = Mathf.Cos(angleInRadians);
        float sinAngle = Mathf.Sin(angleInRadians);

        // ������ ��� �������� ����� �����
        Vector3[] rotatedPoints = new Vector3[points.Length];

        // ���������� ������� �������� � ������ �����
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 point = points[i];

            switch (rotationAxis)
            {
                case RotationAxis.X:
                    // �������� ������ ��� X (���������� Y � Z)
                    float newY_X = point.y * cosAngle - point.z * sinAngle;
                    float newZ_X = point.y * sinAngle + point.z * cosAngle;
                    rotatedPoints[i] = new Vector3(point.x, newY_X, newZ_X);
                    break;

                case RotationAxis.Y:
                    // �������� ������ ��� Y (���������� X � Z)
                    float newX_Y = point.x * cosAngle - point.z * sinAngle;
                    float newZ_Y = point.x * sinAngle + point.z * cosAngle;
                    rotatedPoints[i] = new Vector3(newX_Y, point.y, newZ_Y);
                    break;

                case RotationAxis.Z:
                    // �������� ������ ��� Z (���������� X � Y)
                    float newX_Z = point.x * cosAngle - point.y * sinAngle;
                    float newY_Z = point.x * sinAngle + point.y * cosAngle;
                    rotatedPoints[i] = new Vector3(newX_Z, newY_Z, point.z);
                    break;
            }
        }

        // ���������� ������ ���������� �����
        return rotatedPoints;
    }


    // ����� ��� �������� ������ ������ ������������ ��� ��������, ��������� � ���� Vector3
    public static Vector3[] RotatePoints(Vector3[] points, float angleInDegrees, Vector3 rotationAxis)
    {
        // ����������� ��� �������� (��� ������ ���� ��������� �����)
        Vector3 normalizedAxis = rotationAxis.normalized;

        // ������� ���������� �������� ������ �������� ��� � ����
        Quaternion rotation = Quaternion.AngleAxis(angleInDegrees, normalizedAxis);

        // ������ ��� �������� ����� �����
        Vector3[] rotatedPoints = new Vector3[points.Length];

        // ��������� �������� � ������ �����
        for (int i = 0; i < points.Length; i++)
        {
            rotatedPoints[i] = rotation * points[i]; // ���������� ����������� � �����
        }

        // ���������� ������ ���������� �����
        return rotatedPoints;
    }
}
