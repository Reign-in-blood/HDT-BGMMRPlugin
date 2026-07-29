using BGMMRPlugin.Game;
using Hearthstone_Deck_Tracker.API;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace BGMMRPlugin.UI
{
    /// <summary>
    /// Displays MMR information and status icons beside Battlegrounds leaderboard slots.
    /// </summary>
    public sealed class PlayerMmrOverlay
    {
        private const int SlotCount = 8;

        private const double ReferenceWidth = 1920.0;
        private const double ReferenceHeight = 1080.0;

        // Reference coordinates for a 1920 x 1080 Hearthstone content area.

        private static readonly double[] ReferenceSlotLeft =
        {
            255.00,
            252.14,
            249.29,
            246.43,
            243.57,
            240.71,
            237.86,
            235.00
        };

        private static readonly double[] ReferenceSlotTop =
        {
            168.0,
            260.0,
            355.0,
            445.0,
            540.0,
            633.0,
            727.0,
            822.0
        };

        private const double ReferenceLabelWidth = 90.0;
        private const double ReferenceLabelHeight = 28.0;
        private const double ReferenceOpponentOffset = 30.0;

        private const double ReferenceTavernIconHeight = 35.0;
        private const double ReferenceTavernIconWidth =
            ReferenceTavernIconHeight * 129.0 / 134.0;
        private const double ReferenceTavernIconGap = 0.0;

        private const double ReferenceLastIconHeight = 35.0;
        private const double ReferenceLastIconWidth = 35.0;

        private readonly Border[] _containers =
            new Border[SlotCount];

        private readonly TextBlock[] _nameTexts =
            new TextBlock[SlotCount];

        private readonly TextBlock[] _ratingTexts =
            new TextBlock[SlotCount];

        private readonly bool[] _opponentSlots =
            new bool[SlotCount];

        private readonly Image[] _tavernImages =
            new Image[SlotCount];

        private readonly BitmapImage[] _tavernIcons =
            new BitmapImage[8];

        private readonly Image[] _lastOpponentImages =
            new Image[SlotCount];

        private BitmapImage _lastOpponentIcon;

        private readonly Brush _background =
            CreateFrozenBrush(
                Color.FromArgb(205, 5, 5, 5)
            );

        private readonly Brush _normalText =
            CreateFrozenBrush(
                Color.FromRgb(248, 248, 248)
            );

        private readonly Brush _localText =
            CreateFrozenBrush(
                Color.FromRgb(70, 230, 95)
            );

        private readonly Brush _opponentText =
            CreateFrozenBrush(
                Color.FromRgb(255, 70, 70)
            );

        private readonly Brush _ratingText =
            CreateFrozenBrush(
                Color.FromRgb(255, 190, 45)
            );

        private readonly Brush _deadText =
            CreateFrozenBrush(
                Color.FromRgb(145, 145, 145)
            );

        private bool _isAttached;

        public PlayerMmrOverlay()
        {
            LoadTavernIcons();
            LoadLastOpponentIcon();

            for (int index = 0; index < SlotCount; index++)
                CreateSlot(index);
        }

        public void Attach()
        {
            if (_isAttached)
                return;

            for (int index = 0; index < SlotCount; index++)
            {
                Core.OverlayCanvas.Children.Add(
                    _containers[index]
                );

                Core.OverlayCanvas.Children.Add(
                    _tavernImages[index]
                );

                Core.OverlayCanvas.Children.Add(
                    _lastOpponentImages[index]
                );
            }

            _isAttached = true;
            UpdateLayout();
        }

        public void Detach()
        {
            if (!_isAttached)
                return;

            for (int index = 0; index < SlotCount; index++)
            {
                Core.OverlayCanvas.Children.Remove(
                    _containers[index]
                );

                Core.OverlayCanvas.Children.Remove(
                    _tavernImages[index]
                );

                Core.OverlayCanvas.Children.Remove(
                    _lastOpponentImages[index]
                );
            }

            _isAttached = false;
        }

        public void Display(
            PlayerDisplayData[] display)
        {
            if (display == null || display.Length < SlotCount)
            {
                HideAll();
                return;
            }

            for (int index = 0; index < SlotCount; index++)
            {
                PlayerDisplayData item = display[index];
                Border container = _containers[index];

                if (item == null || !item.IsVisible)
                {
                    _opponentSlots[index] = false;
                    container.Visibility = Visibility.Collapsed;
                    _tavernImages[index].Visibility =
                        Visibility.Collapsed;
                    _lastOpponentImages[index].Visibility =
                        Visibility.Collapsed;
                    continue;
                }

                _opponentSlots[index] =
                    item.IsCurrentOpponent;

                _nameTexts[index].Text =
                    item.Name ?? string.Empty;

                _ratingTexts[index].Text =
                    item.RatingText ?? "...";

                Brush nameBrush =
                    item.IsCurrentOpponent
                        ? _opponentText
                        : item.IsLocalPlayer
                            ? _localText
                            : item.IsDead
                                ? _deadText
                                : _normalText;

                _nameTexts[index].Foreground = nameBrush;

                _ratingTexts[index].Foreground =
                    item.IsDead
                        ? _deadText
                        : _ratingText;

                container.Opacity =
                    item.IsDead ? 0.65 : 1.0;

                container.Visibility = Visibility.Visible;

                int tavernTier = item.TavernTier;

                if (
                    tavernTier >= 1
                    && tavernTier <= 7
                    && _tavernIcons[tavernTier] != null
                )
                {
                    _tavernImages[index].Source =
                        _tavernIcons[tavernTier];

                    _tavernImages[index].Opacity =
                        item.IsDead ? 0.65 : 1.0;

                    _tavernImages[index].Visibility =
                        Visibility.Visible;
                }
                else
                {
                    _tavernImages[index].Visibility =
                        Visibility.Collapsed;
                }

                if (
                    item.IsLastOpponent
                    && _lastOpponentIcon != null
                )
                {
                    _lastOpponentImages[index].Source =
                        _lastOpponentIcon;

                    _lastOpponentImages[index].Opacity =
                        item.IsDead ? 0.65 : 1.0;

                    _lastOpponentImages[index].Visibility =
                        Visibility.Visible;
                }
                else
                {
                    _lastOpponentImages[index].Visibility =
                        Visibility.Collapsed;
                }
            }
        }

        public void HideAll()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                _opponentSlots[index] = false;
                _containers[index].Visibility =
                    Visibility.Collapsed;
                _tavernImages[index].Visibility =
                    Visibility.Collapsed;
                _lastOpponentImages[index].Visibility =
                    Visibility.Collapsed;
            }
        }

        public void UpdateLayout()
        {
            double overlayWidth =
                Core.OverlayCanvas.ActualWidth;

            double overlayHeight =
                Core.OverlayCanvas.ActualHeight;

            if (overlayWidth <= 0 || overlayHeight <= 0)
                return;

            // Hearthstone's usable game area is treated as centered 16:9.
            // This avoids attaching positions to the physical edge of an
            // ultrawide monitor.
            double contentWidth = Math.Min(
                overlayWidth,
                overlayHeight * (16.0 / 9.0)
            );

            double contentHeight = Math.Min(
                overlayHeight,
                contentWidth * (9.0 / 16.0)
            );

            double contentLeft =
                (overlayWidth - contentWidth) / 2.0;

            double contentTop =
                (overlayHeight - contentHeight) / 2.0;

            double scale = Math.Max(
                0.60,
                Math.Min(
                    contentHeight / ReferenceHeight,
                    2.00
                )
            );

            for (int index = 0; index < SlotCount; index++)
            {
                Border container = _containers[index];
                Image tavernImage = _tavernImages[index];
                Image lastOpponentImage =
                    _lastOpponentImages[index];

                container.Width =
                    ReferenceLabelWidth * scale;

                container.Height =
                    ReferenceLabelHeight * scale;

                tavernImage.Width =
                    ReferenceTavernIconWidth * scale;

                tavernImage.Height =
                    ReferenceTavernIconHeight * scale;

                lastOpponentImage.Width =
                    ReferenceLastIconWidth * scale;

                lastOpponentImage.Height =
                    ReferenceLastIconHeight * scale;

                container.CornerRadius =
                    new CornerRadius(4.0 * scale);

                container.Padding =
                    new Thickness(
                        2.0 * scale,
                        0.0,
                        2.0 * scale,
                        0.0
                    );

                _nameTexts[index].FontSize =
                    9.5 * scale;

                _ratingTexts[index].FontSize =
                    9.5 * scale;

                double referenceLeft =
                    ReferenceSlotLeft[index]
                    + (
                        _opponentSlots[index]
                            ? ReferenceOpponentOffset
                            : 0.0
                    );

                double left =
                    contentLeft
                    + (
                        referenceLeft
                        / ReferenceWidth
                    )
                    * contentWidth;

                double top =
                    contentTop
                    + (
                        ReferenceSlotTop[index]
                        / ReferenceHeight
                    )
                    * contentHeight;

                Canvas.SetLeft(container, left);
                Canvas.SetTop(container, top);

                double tavernLeft =
                    left
                    + (
                        ReferenceLabelWidth
                        + ReferenceTavernIconGap
                    )
                    * scale;

                Canvas.SetLeft(
                    tavernImage,
                    tavernLeft
                );

                Canvas.SetTop(
                    tavernImage,
                    top
                    + (
                        ReferenceLabelHeight
                        - ReferenceTavernIconHeight
                    )
                    * 0.5
                    * scale
                );

                Canvas.SetLeft(
                    lastOpponentImage,
                    tavernLeft
                );

                Canvas.SetTop(
                    lastOpponentImage,
                    top
                    + (
                        ReferenceLabelHeight
                        - ReferenceTavernIconHeight
                    )
                    * 0.5
                    * scale
                    + ReferenceTavernIconHeight
                    * scale
                );
            }
        }

        private void LoadTavernIcons()
        {
            for (int tier = 1; tier <= 7; tier++)
            {
                try
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(
                        "pack://application:,,,/"
                        + "HDT-BGMMRPlugin;component/"
                        + $"Assets/T{tier}.png",
                        UriKind.Absolute
                    );
                    image.EndInit();
                    image.Freeze();

                    _tavernIcons[tier] = image;
                }
                catch
                {
                    _tavernIcons[tier] = null;
                }
            }
        }

        private void LoadLastOpponentIcon()
        {
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(
                    "pack://application:,,,/"
                    + "HDT-BGMMRPlugin;component/"
                    + "Assets/Last.PNG",
                    UriKind.Absolute
                );
                image.EndInit();
                image.Freeze();

                _lastOpponentIcon = image;
            }
            catch
            {
                _lastOpponentIcon = null;
            }
        }

        private void CreateSlot(int index)
        {
            Grid content = new Grid
            {
                IsHitTestVisible = false
            };

            content.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            content.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            TextBlock nameText = new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                IsHitTestVisible = false
            };

            TextBlock ratingText = new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                IsHitTestVisible = false
            };

            Grid.SetRow(nameText, 0);
            Grid.SetRow(ratingText, 1);

            content.Children.Add(nameText);
            content.Children.Add(ratingText);

            Border container = new Border
            {
                Background = _background,
                Child = content,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 4,
                    ShadowDepth = 1,
                    Opacity = 0.75
                }
            };

            Image tavernImage = new Image
            {
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            RenderOptions.SetBitmapScalingMode(
                tavernImage,
                BitmapScalingMode.HighQuality
            );

            Image lastOpponentImage = new Image
            {
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            RenderOptions.SetBitmapScalingMode(
                lastOpponentImage,
                BitmapScalingMode.HighQuality
            );

            _containers[index] = container;
            _nameTexts[index] = nameText;
            _ratingTexts[index] = ratingText;
            _tavernImages[index] = tavernImage;
            _lastOpponentImages[index] =
                lastOpponentImage;
        }

        private static Brush CreateFrozenBrush(
            Color color)
        {
            SolidColorBrush brush =
                new SolidColorBrush(color);

            brush.Freeze();
            return brush;
        }
    }
}
