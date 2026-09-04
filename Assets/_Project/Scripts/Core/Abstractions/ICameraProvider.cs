using UnityEngine;

public interface ICameraProvider
{
    Vector2 ScreenToWorldPoint(Vector2 screenPosition);
}