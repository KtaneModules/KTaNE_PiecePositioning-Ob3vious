using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceHighlight : MonoBehaviour
{
    private MeshRenderer _highlight;
    private static readonly Vector3 _offset = new Vector3(0, 0.0000625f, 0);

    void Awake()
    {
        _highlight = GetComponent<MeshRenderer>();
        SetVisibility(false);
        StartCoroutine(AnimateHighlight());
    }

    public void SetVisibility(bool visible)
    {
        _highlight.enabled = visible;
    }

    public void SetPosition(Vector3 position)
    {
        transform.localPosition = position + _offset;
    }

    private IEnumerator AnimateHighlight()
    {
        float t = 0;
        while (true)
        {
            t += Time.deltaTime / 2f;
            t %= 1;

            Color c = _highlight.material.color;
            _highlight.material.color = new Color(c.r, c.g, c.b, 0.5f + Mathf.Sin(t * 2f * Mathf.PI) / 4f);

            yield return null;
        }
    }
}
