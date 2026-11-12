using Godot;

public static class UiTheme
{
	public static readonly Color Black = new("0F1210");
	public static readonly Color Charcoal = new("2B2F2A");
	public static readonly Color Olive = new("4A5A45");
	public static readonly Color OliveDark = new("33402F");
	public static readonly Color Khaki = new("7A6B4F");
	public static readonly Color Sand = new("B2B09A");
	public static readonly Color LcdBack = new("1A2410");
	public static readonly Color LcdText = new("A8D86A");
	public static readonly Color LcdDim = new("5E7A38");
	public static readonly Color Warning = new("E8B33A");
	public static readonly Color Danger = new("E0573A");
	public static readonly Color Good = new("8FE07A");

	private static FontFile _font;
	private static Theme _theme;

	public static FontFile Font => _font ??= GD.Load<FontFile>("res://Themes/determination.ttf");

	public static Color ForRadio(RadioKind kind) => kind switch
	{
		RadioKind.Warning => Warning,
		RadioKind.Danger => Danger,
		RadioKind.Good => Good,
		_ => LcdText
	};

	public static StyleBoxFlat Box(Color background, Color border, int borderWidth = 2, int radius = 4, int padding = 8)
	{
		var box = new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			ShadowColor = new Color(0, 0, 0, 0.35f),
			ShadowSize = 3
		};
		box.SetBorderWidthAll(borderWidth);
		box.SetCornerRadiusAll(radius);
		box.SetContentMarginAll(padding);
		return box;
	}

	public static StyleBoxFlat Device() => Box(Charcoal, Black, 3, 6, 6);

	public static StyleBoxFlat Screen() => Box(LcdBack, Black, 2, 3, 6);

	public static Theme Build()
	{
		if (_theme != null) return _theme;
		var theme = new Theme { DefaultFont = Font, DefaultFontSize = 16 };

		theme.SetColor("font_color", "Label", LcdText);
		theme.SetColor("font_shadow_color", "Label", new Color(0, 0, 0, 0.5f));
		theme.SetConstant("shadow_offset_x", "Label", 1);
		theme.SetConstant("shadow_offset_y", "Label", 1);

		theme.SetStylebox("normal", "Button", Box(OliveDark, Black, 2, 3, 6));
		theme.SetStylebox("hover", "Button", Box(Olive, LcdText, 2, 3, 6));
		theme.SetStylebox("pressed", "Button", Box(LcdBack, LcdText, 2, 3, 6));
		theme.SetStylebox("disabled", "Button", Box(new Color(0.15f, 0.17f, 0.15f), Black, 2, 3, 6));
		theme.SetStylebox("focus", "Button", new StyleBoxEmpty());
		theme.SetColor("font_color", "Button", LcdText);
		theme.SetColor("font_hover_color", "Button", new Color("D8F5A0"));
		theme.SetColor("font_pressed_color", "Button", Sand);
		theme.SetColor("font_disabled_color", "Button", LcdDim);
		theme.SetFontSize("font_size", "Button", 15);

		theme.SetStylebox("background", "ProgressBar", Box(Black, Black, 1, 2, 0));
		theme.SetStylebox("fill", "ProgressBar", Box(LcdText, LcdText, 0, 2, 0));
		theme.SetColor("font_color", "ProgressBar", Black);

		theme.SetStylebox("panel", "PanelContainer", Device());
		theme.SetStylebox("panel", "TooltipPanel", Box(LcdBack, LcdText, 1, 3, 6));
		theme.SetColor("font_color", "TooltipLabel", LcdText);

		_theme = theme;
		return theme;
	}

	public static Label MakeLabel(string text, int size = 16, Color? color = null)
	{
		var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", size);
		if (color.HasValue) label.AddThemeColorOverride("font_color", color.Value);
		return label;
	}

	public static PanelContainer MakePanel(StyleBox style)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", style);
		return panel;
	}

	public static Button MakeButton(string text, int size = 15)
	{
		var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
		button.AddThemeFontSizeOverride("font_size", size);
		return button;
	}

	public static Control FullRect(Control control)
	{
		control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		return control;
	}
}
