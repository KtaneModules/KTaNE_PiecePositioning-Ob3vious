using System.Collections.Generic;
using UnityEngine;

public class IndicatorPanel : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer _referenceIndicator;
    [SerializeField]
    private Vector3 _minPosition;
    [SerializeField]
    private Vector3 _maxPosition;

    void Awake()
    {
        _referenceIndicator.enabled = false;
    }

    public void SetColourSequence(List<int> sequence)
    {
        _referenceIndicator.enabled = true;
        for (int i = 0; i < sequence.Count; i++)
        {
            MeshRenderer indicator = Instantiate(_referenceIndicator, _referenceIndicator.transform.parent);

            indicator.transform.localPosition = Vector3.Lerp(_minPosition, _maxPosition, sequence.Count == 1 ? 0.5f : ((float)i) / (sequence.Count - 1));
            indicator.material.color = PiecePositioningScript.Colours[sequence[i]];
        }
        _referenceIndicator.enabled = false;
    }
}
