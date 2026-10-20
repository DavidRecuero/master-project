using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Item rendering with an ItemPalette: each color shows its own finished sprite (untinted)
/// and hidden items show the palette's hidden sprite until they are revealed.
/// </summary>
public class ItemPaletteTests
{
    private GameObject _gameObject;
    private Item _item;
    private ItemPalette _palette;
    private Sprite _red, _blue, _hidden;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("Item");
        _item = _gameObject.AddComponent<Item>();

        _red = MakeSprite(); _blue = MakeSprite(); _hidden = MakeSprite();

        _palette = ScriptableObject.CreateInstance<ItemPalette>();
        _palette.SetData(new[]
        {
            new ItemPalette.Entry { name = "Red", color = Color.red, sprite = _red },
            new ItemPalette.Entry { name = "Blue", color = Color.blue, sprite = _blue }
        }, _hidden);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
        Object.DestroyImmediate(_palette);
    }

    private static Sprite MakeSprite()
    {
        var tex = new Texture2D(4, 4);
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    private static Pipe MakePipe(params Vector2Int[] cells)
    {
        return new Pipe { layer = 0, path = new List<Vector2Int>(cells) };
    }

    private SpriteRenderer Renderer => _gameObject.GetComponent<SpriteRenderer>();

    [Test]
    public void Palette_ReturnsColorAndSprite_ForValidIds_AndSafeDefaultsOtherwise()
    {
        Assert.AreEqual(2, _palette.Count);
        Assert.AreSame(_blue, _palette.GetSprite(1));
        Assert.AreEqual(Color.blue, _palette.GetColor(1));
        Assert.IsFalse(_palette.HasColor(5));
        Assert.IsNull(_palette.GetSprite(5));
        Assert.AreEqual(Color.white, _palette.GetColor(-1));
    }

    [Test]
    public void Init_WithPalette_ShowsTheColorSprite_Untinted()
    {
        _item.SetPalette(_palette);
        _item.Init(1, Color.blue, new Vector2Int(0, 0), MakePipe(new Vector2Int(0, 0)));

        Assert.AreSame(_blue, Renderer.sprite);
        Assert.AreEqual(Color.white, Renderer.color, "Palette sprites already carry their color.");
        Assert.AreEqual(Color.blue, _item.itemColor, "The logical color is still stored.");
    }

    [Test]
    public void Init_ColorMissingFromThePalette_ShowsNoSprite()
    {
        _item.SetPalette(_palette);
        _item.Init(4, Color.green, new Vector2Int(0, 0), MakePipe(new Vector2Int(0, 0)));

        Assert.IsNull(Renderer.sprite, "Items are only drawn with palette sprites; there is no tinted fallback.");
        Assert.AreEqual(Color.white, Renderer.color);
    }

    [Test]
    public void HiddenItem_ShowsHiddenSprite_ThenColorSprite_WhenRevealed()
    {
        _item.SetPalette(_palette);
        _item.Init(new ItemSpawnData { colorId = 0, type = ItemType.Hidden }, Color.red, new Vector2Int(0, 0), MakePipe(new Vector2Int(0, 0)));
        _item.ApplyHiddenLook();

        Assert.AreSame(_hidden, Renderer.sprite, "A hidden item must not show its real color.");

        _item.Reveal();

        Assert.AreSame(_red, Renderer.sprite);
        Assert.AreEqual(Color.white, Renderer.color);
    }

    [Test]
    public void SetIdentity_WithPalette_SwapsToTheNewColorSprite()
    {
        _item.SetPalette(_palette);
        _item.Init(0, Color.red, new Vector2Int(0, 0), MakePipe(new Vector2Int(0, 0)));

        _item.SetIdentity(1, Color.blue, ItemType.Normal);

        Assert.AreSame(_blue, Renderer.sprite);
    }

    [Test]
    public void BaseScale_IsBigger_OnTheExitCell_AndBackToDefault_InTheTray()
    {
        Pipe pipe = MakePipe(new Vector2Int(0, 1), new Vector2Int(0, 0));
        _item.Init(0, Color.red, new Vector2Int(0, 1), pipe);

        Assert.AreEqual(_item.defaultScale, _item.BaseScale, 1e-4f, "Inside the tube.");

        _item.UpdateGridPosition(new Vector2Int(0, 0));
        Assert.AreEqual(_item.defaultScale * _item.exitScaleMultiplier, _item.BaseScale, 1e-4f, "On the exit platform.");

        _item.inTray = true;
        Assert.AreEqual(_item.defaultScale, _item.BaseScale, 1e-4f, "In the tray.");
    }
}
