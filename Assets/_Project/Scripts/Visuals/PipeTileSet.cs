using TMPro;
using UnityEngine;

/// <summary>
/// Sprites and tuning for drawing pipes, exits, inlet counters and the board panel.
/// Designer-tunable, like ItemPalette and BoosterDefinition.
/// </summary>
[CreateAssetMenu(fileName = "PipeTileSet", menuName = "Game/Pipe Tile Set")]
public class PipeTileSet : ScriptableObject
{
    [Header("Tube pieces (1 cell, PPU 256)")]
    public Sprite straightVertical;
    public Sprite straightHorizontal;
    public Sprite cornerUpRight;
    public Sprite cornerUpLeft;
    public Sprite cornerDownRight;
    public Sprite cornerDownLeft;
    [Tooltip("Tiny overlap between neighbouring pieces to hide hairline seams")]
    public float pieceOverlapScale = 1.004f;

    [Header("Mouths (inlet and outlet)")]
    public Sprite mouthHorizontal;
    public Sprite mouthVertical;
    [Tooltip("Moves the outlet mouth back into the tube, in cells. 0.09 makes its lower edge touch the top of the exit platform.")]
    public float outletMouthInset = 0.09f;

    [Header("Exit platform")]
    public Sprite platformActive;
    public Sprite platformOff;

    [Header("Inlet counter")]
    public Sprite counterPlate;
    public Sprite counterPost;
    [Tooltip("Distance from the inlet mouth to the centre of the post / plate, in cells")]
    public float counterPostDistance = 0.19f;
    public float counterPlateDistance = 0.48f;
    public TMP_FontAsset counterFont;
    public float counterFontSize = 2.6f;
    public Color counterTextColor = new Color32(0x22, 0x30, 0x4A, 0xFF);
    [Tooltip("Hide the counter when no items are waiting")]
    public bool hideCounterWhenEmpty = true;

    [Header("Depth")]
    [Tooltip("Each layer below the top one is tinted this much darker")]
    [Range(0f, 0.2f)] public float depthTintStep = 0.05f;
    public Color shadowColor = new Color(0.13f, 0.19f, 0.29f, 0.35f);
    [Tooltip("Shadow offset in world units (right, down)")]
    public Vector2 shadowOffset = new Vector2(0.04f, -0.075f);

    [Header("Board panel (9-slice)")]
    public Sprite boardPanel;
    [Tooltip("Extra space around the cells, in cells")]
    public float panelSidePadding = 0.08f;
    public float panelTopPadding = 0.85f;    // Room for the inlet counters
    public float panelBottomPadding = 0.12f;
    [Tooltip("Height of the shadow baked under the panel sprite, in world units (28 px / 256)")]
    public float panelShadowHeight = 0.111f;

    public Sprite GetPiece(PipePieceType type)
    {
        switch (type)
        {
            case PipePieceType.StraightVertical: return straightVertical;
            case PipePieceType.StraightHorizontal: return straightHorizontal;
            case PipePieceType.CornerUpRight: return cornerUpRight;
            case PipePieceType.CornerUpLeft: return cornerUpLeft;
            case PipePieceType.CornerDownRight: return cornerDownRight;
            default: return cornerDownLeft;
        }
    }

    public Sprite GetMouth(bool horizontalBar) => horizontalBar ? mouthHorizontal : mouthVertical;

    /// <summary>Tint for a tube that sits `depth` layers below the top layer (0 = top, untinted).</summary>
    public Color DepthTint(int depth)
    {
        float b = Mathf.Clamp01(1f - depthTintStep * Mathf.Max(0, depth));
        return new Color(b, b, b, 1f);
    }
}
