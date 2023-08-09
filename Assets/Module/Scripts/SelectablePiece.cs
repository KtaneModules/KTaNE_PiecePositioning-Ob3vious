using UnityEngine;

public class SelectablePiece : MonoBehaviour
{
    [SerializeField]
    private Transform _pointer;
    [SerializeField]
    private Renderer _pointerHolder;

    public PieceData Data { get; private set; }

    public KMSelectable Selectable { get; private set; }
    public KMAudio Audio { get; set; }

    public SelectableTile Target { get; set; }
    public Vector3 TargetCoordinate { get; set; }

    private float _elevationLerp = 0;
    public bool AllowLowering { get; set; }

    void Awake()
    {
        Selectable = GetComponent<KMSelectable>();
        Target = null;
        AllowLowering = true;
    }

    void Update()
    {
        bool up = _elevationLerp > 0;

        Vector3 elevation = new Vector3(0, 0.0075f, 0) * _elevationLerp;
        Vector3 surfacePosition = transform.localPosition - elevation;

        bool inPosition = Mathf.Pow(transform.localPosition.x - TargetCoordinate.x, 2) + Mathf.Pow(transform.localPosition.z - TargetCoordinate.z, 2) < Mathf.Epsilon;

        _elevationLerp = Mathf.Clamp(_elevationLerp + (!inPosition || !AllowLowering ? 10 : -10) * Time.deltaTime, 0, 1);
        elevation = new Vector3(0, 0.0075f, 0) * _elevationLerp;

        if (_elevationLerp >= 1)
            surfacePosition = Vector3.Lerp(surfacePosition, TargetCoordinate, 0.25f * Time.deltaTime / (surfacePosition - TargetCoordinate).magnitude);

        transform.localPosition = surfacePosition + elevation;

        if (up && _elevationLerp == 0)
            Audio.PlaySoundAtTransform("Magnet", transform);
    }

    public void AssignPiece(PieceData data)
    {
        GetComponent<MeshRenderer>().material.color = PiecePositioningScript.Colours[data.Colour];

        _pointer.GetComponent<MeshRenderer>().enabled = true;

        bool isMovable = false;
        for (int i = 0; i < 6; i++)
        {
            if (!data.CanMove((PieceData.Move)i))
                continue;

            Transform pointerCopy = Instantiate(_pointer, _pointer.parent);
            pointerCopy.localEulerAngles = new Vector3(0, 0, 180 + 60 * i);
            isMovable = true;
        }

        _pointer.GetComponent<MeshRenderer>().enabled = false;
        _pointerHolder.enabled = isMovable;

        Data = data;
    }

    public SelectablePiece CreateCopy()
    {
        return Instantiate(this, transform.parent);
    }

    public void SetTarget(SelectableTile targetTile)
    {
        if (Target != null)
            Target.CoveringPiece = null;
        Target = targetTile;
        if (Target != null)
        {
            Target.CoveringPiece = this;
            TargetCoordinate = Target.transform.localPosition;
        }
    }
}
