// Prototype NinjaScript file for NinjaTrader 8.
// Copy to: Documents\NinjaTrader 8\bin\Custom\DrawingTools
// Compile inside NinjaTrader 8 NinjaScript Editor.

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.DrawingTools
{
    public enum MpPanelDirection
    {
        Long,
        Short
    }

    public class MpRiskRewardPanel : DrawingTool
    {
        private const int CursorSensitivity = 15;

        private ChartAnchor entryAnchor;
        private ChartAnchor stopAnchor;
        private ChartAnchor targetAnchor;
        private ChartAnchor editingAnchor;
        private bool isDetachedFromPrice;
        private bool offsetsInitialized;
        private double stopOffsetFromEntry;
        private double targetOffsetFromEntry;
        private double lastPanelLeft;
        private double lastPanelRight;
        private double lastPanelCenterX;
        private Rect qtyMinusButton;
        private Rect qtyPlusButton;
        private Rect qtyValueBox;
        private Rect directionButton;
        private Rect attachToPriceButton;
        private Rect preset1Button;
        private Rect preset2Button;
        private Rect preset5Button;
        private Rect preset10Button;
        private Rect preset20Button;
        private Rect presetsToolbarRect;
        private Rect buyButton;
        private Rect sellButton;
        private Rect toolbarDirectionButton;
        private Rect targetDragRect;
        private Rect entryDragRect;
        private Rect stopDragRect;
        private bool suppressNextMouseUpSelection;
        private DateTime lastQuantityButtonClick = DateTime.MinValue;
        private DateTime lastChartTraderQuantityRead = DateTime.MinValue;
        private double minDistanceFromEntryPrice;
        private ChartControl lastChartControl;
        private DispatcherTimer chartTraderQuantityTimer;

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Kontrakty", GroupName = "Parametry", Order = 1)]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Kierunek", GroupName = "Parametry", Order = 2)]
        public MpPanelDirection Direction { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Wartosc ticka", GroupName = "Instrument", Order = 10)]
        public double TickValue { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Tick size", GroupName = "Instrument", Order = 11)]
        public double TickSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Startowo przypiety do ceny", GroupName = "Parametry", Order = 3)]
        public bool StartAttachedToCurrentPrice { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Synchronizuj Chart Trader Qty", GroupName = "Parametry", Order = 4)]
        public bool SyncChartTraderQuantity { get; set; }

        public override object Icon => "RR";

        public override IEnumerable<ChartAnchor> Anchors
        {
            get
            {
                if (entryAnchor != null)
                    yield return entryAnchor;
                if (stopAnchor != null)
                    yield return stopAnchor;
                if (targetAnchor != null)
                    yield return targetAnchor;
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MrRexo Panel";
                Description = "Interaktywny panel ryzyka i zysku dla jednego TP i jednego SL.";
                DrawingState = DrawingState.Building;
                Quantity = 1;
                Direction = MpPanelDirection.Long;
                StartAttachedToCurrentPrice = true;
                SyncChartTraderQuantity = true;
                TickSize = 0.25;
                TickValue = 0.50;
                DisplayOnChartsMenus = true;

                entryAnchor = new ChartAnchor
                {
                    IsEditing = true,
                    DrawingTool = this,
                    DisplayName = "OP",
                    IsBrowsable = true
                };

                stopAnchor = new ChartAnchor
                {
                    IsEditing = true,
                    DrawingTool = this,
                    DisplayName = "Stop Loss",
                    IsBrowsable = true
                };

                targetAnchor = new ChartAnchor
                {
                    IsEditing = true,
                    DrawingTool = this,
                    DisplayName = "Take Profit",
                    IsBrowsable = true
                };
            }
            else if (State == State.Active)
            {
                chartTraderQuantityTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                chartTraderQuantityTimer.Tick += ChartTraderQuantityTimerTick;
                chartTraderQuantityTimer.Start();
            }
            else if (State == State.Terminated)
            {
                if (chartTraderQuantityTimer != null)
                {
                    chartTraderQuantityTimer.Stop();
                    chartTraderQuantityTimer.Tick -= ChartTraderQuantityTimerTick;
                    chartTraderQuantityTimer = null;
                }
            }
        }

        private void ChartTraderQuantityTimerTick(object sender, EventArgs e)
        {
            if (lastChartControl == null || !SyncChartTraderQuantity)
                return;

            SyncPanelQuantityFromChartTrader(lastChartControl);
        }

        public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            if (DrawingState == DrawingState.Building)
            {
                dataPoint.CopyDataValues(entryAnchor);
                dataPoint.CopyDataValues(stopAnchor);
                dataPoint.CopyDataValues(targetAnchor);

                if (Direction == MpPanelDirection.Long)
                {
                    stopAnchor.Price = RoundToTick(entryAnchor.Price - GetPriceDistanceForDollars(50));
                    targetAnchor.Price = RoundToTick(entryAnchor.Price + GetPriceDistanceForDollars(100));
                }
                else
                {
                    stopAnchor.Price = RoundToTick(entryAnchor.Price + GetPriceDistanceForDollars(50));
                    targetAnchor.Price = RoundToTick(entryAnchor.Price - GetPriceDistanceForDollars(100));
                }

                entryAnchor.IsEditing = false;
                stopAnchor.IsEditing = false;
                targetAnchor.IsEditing = false;
                isDetachedFromPrice = !StartAttachedToCurrentPrice;
                offsetsInitialized = true;
                stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
                targetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
                DrawingState = DrawingState.Normal;
                IsSelected = true;
                return;
            }

            if (DrawingState == DrawingState.Normal)
            {
                Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);

                if (TryHandleQuantityButton(point))
                    return;

                if (TryHandlePresetButton(point))
                    return;

                if (TryHandleAttachToPriceButton(point))
                    return;

                if (TryHandleTradeSideButton(point))
                    return;

                if (TryHandleToolbarDirectionButton(point))
                    return;

                if (TryHandleDirectionButton(point))
                    return;

                editingAnchor = GetPanelDragAnchor(chartScale, point);

                if (editingAnchor != null)
                {
                    editingAnchor.IsEditing = true;
                    if (editingAnchor == entryAnchor)
                        isDetachedFromPrice = true;
                    DrawingState = DrawingState.Editing;
                    IsSelected = true;
                    return;
                }

                IsSelected = false;
            }
        }

        public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null)
                return;

            if (DrawingState == DrawingState.Building)
            {
                dataPoint.CopyDataValues(entryAnchor);
                dataPoint.CopyDataValues(stopAnchor);
                dataPoint.CopyDataValues(targetAnchor);

                if (Direction == MpPanelDirection.Long)
                {
                    stopAnchor.Price = RoundToTick(entryAnchor.Price - GetPriceDistanceForDollars(50));
                    targetAnchor.Price = RoundToTick(entryAnchor.Price + GetPriceDistanceForDollars(100));
                }
                else
                {
                    stopAnchor.Price = RoundToTick(entryAnchor.Price + GetPriceDistanceForDollars(50));
                    targetAnchor.Price = RoundToTick(entryAnchor.Price - GetPriceDistanceForDollars(100));
                }
            }
            else if (DrawingState == DrawingState.Editing && editingAnchor != null)
            {
                double newPrice = RoundToTick(dataPoint.Price);

                if (editingAnchor == entryAnchor)
                {
                    double priceDelta = newPrice - entryAnchor.Price;
                    entryAnchor.Price = newPrice;
                    stopAnchor.Price = RoundToTick(stopAnchor.Price + priceDelta);
                    targetAnchor.Price = RoundToTick(targetAnchor.Price + priceDelta);
                    stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
                    targetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
                }
                else if (editingAnchor == stopAnchor)
                {
                    stopAnchor.Price = ClampStopPrice(newPrice);
                    stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
                }
                else if (editingAnchor == targetAnchor)
                {
                    targetAnchor.Price = ClampTargetPrice(newPrice);
                    targetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
                }
                else
                    editingAnchor.Price = newPrice;
            }
        }

        public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            if (suppressNextMouseUpSelection)
            {
                DrawingState = DrawingState.Normal;
                IsSelected = false;
                editingAnchor = null;
                suppressNextMouseUpSelection = false;
                return;
            }

            if (DrawingState == DrawingState.Editing)
                DrawingState = DrawingState.Normal;

            if (editingAnchor != null)
                editingAnchor.IsEditing = false;

            editingAnchor = null;
        }

        public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
        {
            return Array.Empty<Point>();
        }

        public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
        {
            if (DrawingState == DrawingState.Building)
                return Cursors.Pen;

            if (IsPointInQuantityButtons(point) || IsPointInPresetButtons(point) || directionButton.Contains(point) || toolbarDirectionButton.Contains(point) || attachToPriceButton.Contains(point) || IsPointInTradeSideButtons(point))
                return Cursors.Hand;

            ChartAnchor closestAnchor = GetPanelDragAnchor(chartScale, point);
            return closestAnchor == null ? null : Cursors.SizeNS;
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null || ChartPanel == null)
                return;

            lastChartControl = chartControl;
            SyncPanelQuantityFromChartTrader(chartControl);
            UpdateAutoFollowPrice();

            double left = GetPanelLeftX(chartControl);
            double right = Math.Min(ChartPanel.X + ChartPanel.W, left + 340);
            if (right - left < 180)
            {
                right = ChartPanel.X + ChartPanel.W;
                left = Math.Max(ChartPanel.X, right - 340);
            }

            lastPanelLeft = left;
            lastPanelRight = right;
            lastPanelCenterX = left + (right - left) / 2.0;

            Point entry = new Point(lastPanelCenterX, chartScale.GetYByValue(entryAnchor.Price));
            Point stop = new Point(lastPanelCenterX, chartScale.GetYByValue(stopAnchor.Price));
            Point target = new Point(lastPanelCenterX, chartScale.GetYByValue(targetAnchor.Price));
            minDistanceFromEntryPrice = CalculateEntryVisualBufferPrice(chartScale, entry.Y);

            double top = Math.Min(target.Y, stop.Y);
            double bottom = Math.Max(target.Y, stop.Y);

            DrawPanelBackground(chartControl, left, right, top, bottom, entry.Y, target.Y, stop.Y);
            DrawVirtualLevelLines(left, target.Y, stop.Y);
            DrawPanelLabels(chartControl, left, right, entry, stop, target);
        }

        private void DrawVirtualLevelLines(double panelLeft, double targetY, double stopY)
        {
            if (RenderTarget == null || ChartPanel == null)
                return;

            float startX = (float)ChartPanel.X;
            float endX = (float)Math.Max(ChartPanel.X, panelLeft);
            if (endX <= startX)
                return;

            using (var tpBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.0f, 0.70f, 1.0f, 0.62f)))
            using (var slBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(1.0f, 0.18f, 0.24f, 0.62f)))
            using (var strokeStyle = new SharpDX.Direct2D1.StrokeStyle(Core.Globals.D2DFactory, new SharpDX.Direct2D1.StrokeStyleProperties
            {
                DashStyle = SharpDX.Direct2D1.DashStyle.Dash,
                StartCap = SharpDX.Direct2D1.CapStyle.Flat,
                EndCap = SharpDX.Direct2D1.CapStyle.Flat
            }))
            {
                RenderTarget.DrawLine(new SharpDX.Vector2(startX, (float)targetY), new SharpDX.Vector2(endX, (float)targetY), tpBrush, 1f, strokeStyle);
                RenderTarget.DrawLine(new SharpDX.Vector2(startX, (float)stopY), new SharpDX.Vector2(endX, (float)stopY), slBrush, 1f, strokeStyle);
            }
        }

        private void DrawPanelBackground(ChartControl chartControl, double left, double right, double top, double bottom, double entryY, double targetY, double stopY)
        {
            if (RenderTarget == null)
                return;

            using (var profitBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.04f, 0.72f, 1.0f, 0.30f)))
            using (var riskBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(1.0f, 0.12f, 0.18f, 0.24f)))
            using (var entryBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.18f, 0.22f, 0.26f, 0.94f)))
            using (var greenHeaderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.0f, 0.42f, 0.68f, 0.96f)))
            using (var redHeaderBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.92f, 0.02f, 0.08f, 0.96f)))
            using (var outlineBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.0f, 0.70f, 1.0f, 0.78f)))
            {
                float x = (float)left;
                float width = (float)(right - left);
                float headerHeight = 28f;
                float entryHeight = 30f;

                float profitTop = (float)Math.Min(targetY, entryY);
                float profitBottom = (float)Math.Max(targetY, entryY);
                float riskTop = (float)Math.Min(stopY, entryY);
                float riskBottom = (float)Math.Max(stopY, entryY);
                float targetHeaderY = (float)(targetY < entryY ? targetY - headerHeight : targetY);
                float stopHeaderY = (float)(stopY < entryY ? stopY - headerHeight : stopY);
                float outlineTop = Math.Min(targetHeaderY, stopHeaderY);
                float outlineBottom = Math.Max(targetHeaderY + headerHeight, stopHeaderY + headerHeight);

                targetDragRect = new Rect(left, targetHeaderY, right - left, headerHeight);
                entryDragRect = new Rect(left, entryY - entryHeight / 2, right - left, entryHeight);
                stopDragRect = new Rect(left, stopHeaderY, right - left, headerHeight);

                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, profitTop, width, profitBottom - profitTop), profitBrush);
                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, riskTop, width, riskBottom - riskTop), riskBrush);
                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, (float)entryY - entryHeight / 2, width, entryHeight), entryBrush);

                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, targetHeaderY, width, headerHeight), greenHeaderBrush);
                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, stopHeaderY, width, headerHeight), redHeaderBrush);
                RenderTarget.DrawRectangle(new SharpDX.RectangleF(x, outlineTop, width, outlineBottom - outlineTop), outlineBrush, 1.5f);
            }
        }

        private void DrawPanelLabels(ChartControl chartControl, double left, double right, Point entry, Point stop, Point target)
        {
            if (RenderTarget == null || entryAnchor == null || stopAnchor == null || targetAnchor == null)
                return;

            double stopTicks = CalculateTicks(entryAnchor.Price, stopAnchor.Price);
            double targetTicks = CalculateTicks(entryAnchor.Price, targetAnchor.Price);
            double risk = CalculateMoney(stopTicks);
            double reward = CalculateMoney(targetTicks);
            double rr = risk <= 0 ? 0 : reward / risk;

            double panelWidth = right - left;
            double targetLabelY = target.Y < entry.Y ? target.Y - 24 : target.Y + 4;
            double stopLabelY = stop.Y < entry.Y ? stop.Y - 24 : stop.Y + 4;
            const double halfEntryBarHeight = 15.0;
            double profitTextTop = target.Y < entry.Y ? target.Y : entry.Y + halfEntryBarHeight;
            double profitTextBottom = target.Y < entry.Y ? entry.Y - halfEntryBarHeight : target.Y;
            double riskTextTop = stop.Y < entry.Y ? stop.Y : entry.Y + halfEntryBarHeight;
            double riskTextBottom = stop.Y < entry.Y ? entry.Y - halfEntryBarHeight : stop.Y;
            double profitZoneHeight = Math.Abs(profitTextBottom - profitTextTop);
            double riskZoneHeight = Math.Abs(riskTextBottom - riskTextTop);

            DrawText(chartControl, $"TP: {targetAnchor.Price:F2}  Zysk: {reward:F2}$", left, targetLabelY, panelWidth, 22, true, true);
            DrawText(chartControl, $"OP: {entryAnchor.Price:F2}  K: {Quantity}  [R:R {rr:F2}]", left, entry.Y - 10, panelWidth, 22, true, true);
            if (profitZoneHeight >= 46)
                DrawText(chartControl, $"Zysk: {targetTicks:F0} tickow / {reward:F2}$", left, Math.Min(profitTextTop, profitTextBottom), panelWidth, profitZoneHeight, false, true);
            if (riskZoneHeight >= 46)
                DrawText(chartControl, $"Ryzyko: {stopTicks:F0} tickow / {risk:F2}$", left, Math.Min(riskTextTop, riskTextBottom), panelWidth, riskZoneHeight, false, true);
            DrawText(chartControl, $"SL: {stopAnchor.Price:F2}  Strata: {risk:F2}$", left, stopLabelY, panelWidth, 22, true, true);
            DrawPresetButtons(chartControl);
            DrawDirectionButton(chartControl, left, entry.Y);
            DrawAttachToPriceButton(chartControl, left, entry.Y);
            DrawQuantityButtons(chartControl, right, entry.Y);
        }

        private void DrawDirectionButton(ChartControl chartControl, double left, double entryY)
        {
            directionButton = new Rect(left + 6, entryY - 11, 30, 22);
            DrawButton(chartControl, directionButton, Direction == MpPanelDirection.Long ? "L" : "S");
        }

        private void DrawAttachToPriceButton(ChartControl chartControl, double left, double entryY)
        {
            attachToPriceButton = new Rect(left + 39, entryY - 11, 45, 22);
            DrawButton(chartControl, attachToPriceButton, isDetachedFromPrice ? "OP=C" : "AUTO");
        }

        private void DrawPresetButtons(ChartControl chartControl)
        {
            if (RenderTarget == null || ChartPanel == null)
                return;

            double h = 22;
            double gap = 3;
            double tradeWidth = 72;
            double directionWidth = 30;
            double totalWidth = tradeWidth + gap + tradeWidth + 12 + directionWidth + 12 + 24 + gap + 24 + gap + 24 + gap + 31 + gap + 31;
            double padding = 6;
            double toolbarWidth = totalWidth + 2 * padding;
            double toolbarHeight = h + 2 * padding;
            double x = ChartPanel.X + (ChartPanel.W - toolbarWidth) / 2.0 + padding;
            double y = ChartPanel.Y + 8 + padding;

            presetsToolbarRect = new Rect(x - padding, y - padding, toolbarWidth, toolbarHeight);
            DrawToolbarBackground(presetsToolbarRect);

            buyButton = new Rect(x, y, tradeWidth, h);
            sellButton = new Rect(buyButton.Right + gap, y, tradeWidth, h);
            toolbarDirectionButton = new Rect(sellButton.Right + 12, y, directionWidth, h);
            preset1Button = new Rect(toolbarDirectionButton.Right + 12, y, 24, h);
            preset2Button = new Rect(preset1Button.Right + gap, y, 24, h);
            preset5Button = new Rect(preset2Button.Right + gap, y, 24, h);
            preset10Button = new Rect(preset5Button.Right + gap, y, 31, h);
            preset20Button = new Rect(preset10Button.Right + gap, y, 31, h);

            DrawButton(chartControl, buyButton, GetBuyButtonLabel());
            DrawButton(chartControl, sellButton, GetSellButtonLabel());
            DrawButton(chartControl, toolbarDirectionButton, Direction == MpPanelDirection.Long ? "L" : "S");
            DrawButton(chartControl, preset1Button, "1");
            DrawButton(chartControl, preset2Button, "2");
            DrawButton(chartControl, preset5Button, "5");
            DrawButton(chartControl, preset10Button, "10");
            DrawButton(chartControl, preset20Button, "20");
        }

        private void DrawToolbarBackground(Rect rect)
        {
            using (var fill = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.04f, 0.16f, 0.26f, 0.62f)))
            using (var border = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.0f, 0.70f, 1.0f, 0.72f)))
            {
                var dxRect = new SharpDX.RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
                RenderTarget.FillRectangle(dxRect, fill);
                RenderTarget.DrawRectangle(dxRect, border, 1f);
            }
        }

        private void DrawQuantityButtons(ChartControl chartControl, double right, double entryY)
        {
            if (RenderTarget == null)
                return;

            double y = entryY - 11;
            double x = right - 88;
            double h = 22;
            double gap = 3;
            double wSmall = 22;
            double wValue = 36;

            qtyMinusButton = new Rect(x, y, wSmall, h);
            qtyValueBox = new Rect(qtyMinusButton.Right + gap, y, wValue, h);
            qtyPlusButton = new Rect(qtyValueBox.Right + gap, y, wSmall, h);

            DrawButton(chartControl, qtyMinusButton, "-");
            DrawButton(chartControl, qtyValueBox, Quantity.ToString());
            DrawButton(chartControl, qtyPlusButton, "+");
        }

        private void DrawButton(ChartControl chartControl, Rect rect, string label)
        {
            using (var fill = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.12f, 0.15f, 0.18f, 0.94f)))
            using (var border = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.82f, 0.92f, 1f, 0.78f)))
            {
                var dxRect = new SharpDX.RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
                RenderTarget.FillRectangle(dxRect, fill);
                RenderTarget.DrawRectangle(dxRect, border, 1f);
            }

            DrawText(chartControl, label, rect.X, rect.Y, rect.Width, rect.Height, true, true);
        }

        private void DrawText(ChartControl chartControl, string text, double x, double y, double width, double height, bool white, bool centered)
        {
            SimpleFont font = chartControl.Properties.LabelFont ?? new SimpleFont();
            SharpDX.DirectWrite.TextFormat textFormat = font.ToDirectWriteTextFormat();
            textFormat.TextAlignment = centered ? SharpDX.DirectWrite.TextAlignment.Center : SharpDX.DirectWrite.TextAlignment.Leading;
            textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
            SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, text, textFormat, (float)width, (float)height);

            using (var textBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, white ? new SharpDX.Color4(1f, 1f, 1f, 1f) : new SharpDX.Color4(0.70f, 0.92f, 1f, 0.96f)))
                RenderTarget.DrawTextLayout(new SharpDX.Vector2((float)x, (float)y), textLayout, textBrush, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

            textLayout.Dispose();
            textFormat.Dispose();
        }

        public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
        {
            return true;
        }

        private double CalculateTicks(double priceA, double priceB)
        {
            return Math.Abs(priceA - priceB) / TickSize;
        }

        private double CalculateMoney(double ticks)
        {
            return ticks * TickValue * Quantity;
        }

        private double GetPriceDistanceForDollars(double dollars)
        {
            double tickValueForQuantity = TickValue * Math.Max(1, Quantity);
            if (tickValueForQuantity <= 0)
                return TickSize;

            double ticks = dollars / tickValueForQuantity;
            return ticks * TickSize;
        }

        private double RoundToTick(double price)
        {
            return Math.Round(price / TickSize, MidpointRounding.AwayFromZero) * TickSize;
        }

        private double ClampStopPrice(double price)
        {
            double minDistance = GetMinimumDistanceFromEntry();

            if (Direction == MpPanelDirection.Long)
                return RoundToTick(Math.Min(price, entryAnchor.Price - minDistance));

            return RoundToTick(Math.Max(price, entryAnchor.Price + minDistance));
        }

        private double ClampTargetPrice(double price)
        {
            double minDistance = GetMinimumDistanceFromEntry();

            if (Direction == MpPanelDirection.Long)
                return RoundToTick(Math.Max(price, entryAnchor.Price + minDistance));

            return RoundToTick(Math.Min(price, entryAnchor.Price - minDistance));
        }

        private double GetMinimumDistanceFromEntry()
        {
            return Math.Max(TickSize, minDistanceFromEntryPrice);
        }

        private double CalculateEntryVisualBufferPrice(ChartScale chartScale, double entryY)
        {
            const double entryBoxHeight = 30.0;
            double bufferPixels = entryBoxHeight * 0.5;
            double priceAtEntry = chartScale.GetValueByY((float)entryY);
            double priceAtBuffer = chartScale.GetValueByY((float)(entryY - bufferPixels));
            double priceDistance = Math.Abs(priceAtBuffer - priceAtEntry);
            return Math.Max(TickSize, RoundToTick(priceDistance));
        }

        private double GetPanelLeftX(ChartControl chartControl)
        {
            ChartBars chartBars = GetAttachedToChartBars();
            if (chartBars == null || chartBars.Bars == null || chartBars.Bars.Count <= 0 || ChartPanel == null)
                return ChartPanel == null ? 0 : ChartPanel.X + 20;

            int currentIdx = Math.Min(chartBars.ToIndex, chartBars.Bars.Count - 1);
            if (currentIdx < 0)
                currentIdx = chartBars.Bars.Count - 1;

            double currentX = chartControl.GetXByBarIndex(chartBars, currentIdx);
            double barStep = 12;
            if (currentIdx > 0)
            {
                double previousX = chartControl.GetXByBarIndex(chartBars, currentIdx - 1);
                barStep = Math.Max(8, Math.Abs(currentX - previousX));
            }

            return currentX + barStep;
        }

        private ChartAnchor GetClosestPanelAnchor(ChartScale chartScale, Point point)
        {
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null)
                return null;

            ChartAnchor closest = null;
            double closestDistance = double.MaxValue;

            CheckAnchor(entryAnchor);
            CheckAnchor(stopAnchor);
            CheckAnchor(targetAnchor);

            return closestDistance <= CursorSensitivity ? closest : null;

            void CheckAnchor(ChartAnchor anchor)
            {
                double y = chartScale.GetYByValue(anchor.Price);
                double dx = point.X - lastPanelCenterX;
                double dy = point.Y - y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = anchor;
                }
            }
        }

        private ChartAnchor GetPanelDragAnchor(ChartScale chartScale, Point point)
        {
            if (IsPointInQuantityButtons(point) || IsPointInPresetButtons(point) || directionButton.Contains(point) || toolbarDirectionButton.Contains(point) || attachToPriceButton.Contains(point) || IsPointInTradeSideButtons(point))
                return null;

            if (targetDragRect.Contains(point))
                return targetAnchor;

            if (entryDragRect.Contains(point))
                return entryAnchor;

            if (stopDragRect.Contains(point))
                return stopAnchor;

            return GetClosestPanelAnchor(chartScale, point);
        }

        private bool TryHandleQuantityButton(Point point)
        {
            if (!IsPointInQuantityButtons(point))
                return false;

            if (ShouldSuppressFastQuantityClick())
                return true;

            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;

            if (qtyMinusButton.Contains(point))
            {
                SetPanelQuantity(Math.Max(1, Quantity - 1));
                suppressNextMouseUpSelection = true;
                return true;
            }

            if (qtyPlusButton.Contains(point))
            {
                SetPanelQuantity(Math.Min(999, Quantity + 1));
                suppressNextMouseUpSelection = true;
                return true;
            }

            return false;
        }

        private void SetPanelQuantity(int quantity)
        {
            Quantity = Math.Max(1, Math.Min(999, quantity));

            if (SyncChartTraderQuantity)
                TrySyncChartTraderQuantity(Quantity);
        }

        private void TrySyncChartTraderQuantity(int quantity)
        {
            try
            {
                ChartControl chartControl = lastChartControl;
                chartControl?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    Window window = Window.GetWindow(chartControl);
                    if (window == null)
                        return;

                    DependencyObject orderQuantityControl = FindOrderQuantityControl(window);
                    if (orderQuantityControl != null)
                        TrySetQuantityValue(orderQuantityControl, quantity);
                }));
            }
            catch
            {
                // Experimental Chart Trader UI sync: ignore failures and keep panel quantity authoritative.
            }
        }

        private void SyncPanelQuantityFromChartTrader(ChartControl chartControl)
        {
            if (!SyncChartTraderQuantity || chartControl == null)
                return;

            DateTime now = DateTime.UtcNow;
            if ((now - lastChartTraderQuantityRead).TotalMilliseconds < 300)
                return;

            // Give our own click handlers a short window to push their value to Chart Trader first.
            if ((now - lastQuantityButtonClick).TotalMilliseconds < 350)
                return;

            lastChartTraderQuantityRead = now;

            try
            {
                Window window = Window.GetWindow(chartControl);
                if (window == null)
                    return;

                DependencyObject orderQuantityControl = FindOrderQuantityControl(window);
                if (orderQuantityControl == null)
                    return;

                int chartTraderQuantity;
                if (TryReadQuantityValue(orderQuantityControl, out chartTraderQuantity) && chartTraderQuantity > 0 && chartTraderQuantity != Quantity)
                {
                    Quantity = Math.Min(999, chartTraderQuantity);
                    chartControl.InvalidateVisual();
                }
            }
            catch
            {
                // Experimental Chart Trader UI sync: ignore failures and keep panel quantity authoritative.
            }
        }

        private DependencyObject FindOrderQuantityControl(Window window)
        {
            List<DependencyObject> elements = EnumerateVisualTree(window).ToList();

            foreach (DependencyObject label in elements)
            {
                string labelText = GetElementText(label);
                if (string.IsNullOrWhiteSpace(labelText) || !labelText.ToLowerInvariant().Contains("order qty"))
                    continue;

                Rect labelBounds = GetElementBounds(label, window);
                DependencyObject bestCandidate = null;
                double bestScore = double.MaxValue;

                foreach (DependencyObject candidate in elements)
                {
                    if (!LooksLikeEditableQuantityInput(candidate))
                        continue;

                    Rect candidateBounds = GetElementBounds(candidate, window);
                    if (candidateBounds.IsEmpty)
                        continue;

                    bool belowLabel = candidateBounds.Top >= labelBounds.Bottom - 6 && candidateBounds.Top <= labelBounds.Bottom + 80;
                    bool horizontallyNear = candidateBounds.Right >= labelBounds.Left - 20 && candidateBounds.Left <= labelBounds.Right + 260;
                    if (!belowLabel || !horizontallyNear)
                        continue;

                    double score = Math.Abs(candidateBounds.Top - labelBounds.Bottom) + Math.Abs(candidateBounds.Left - labelBounds.Left) * 0.25;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate != null)
                    return bestCandidate;
            }

            return null;
        }

        private IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            if (root == null)
                yield break;

            int childCount = 0;
            try
            {
                childCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch
            {
                yield break;
            }

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child == null)
                    continue;

                yield return child;

                foreach (DependencyObject nested in EnumerateVisualTree(child))
                    yield return nested;
            }
        }

        private bool LooksLikeEditableQuantityInput(DependencyObject element)
        {
            if (element is Button || element is TextBlock || element is Label)
                return false;

            FrameworkElement frameworkElement = element as FrameworkElement;
            string typeName = element.GetType().Name ?? string.Empty;
            string name = frameworkElement?.Name ?? string.Empty;
            string automationName = AutomationProperties.GetName(element) ?? string.Empty;

            string marker = $"{typeName} {name} {automationName}".ToLowerInvariant();
            return element is TextBox
                || marker.Contains("quantity")
                || marker.Contains("orderqty")
                || marker.Contains("order qty")
                || marker.Contains("qty")
                || HasWritableProperty(element, "Value");
        }

        private bool HasWritableProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property != null && property.CanWrite;
        }

        private string GetElementText(DependencyObject element)
        {
            if (element is TextBlock textBlock)
                return textBlock.Text;

            if (element is Label label)
                return label.Content?.ToString();

            if (element is ContentControl contentControl)
                return contentControl.Content?.ToString();

            return AutomationProperties.GetName(element);
        }

        private Rect GetElementBounds(DependencyObject element, Visual relativeTo)
        {
            if (!(element is FrameworkElement frameworkElement) || relativeTo == null || frameworkElement.ActualWidth <= 0 || frameworkElement.ActualHeight <= 0)
                return Rect.Empty;

            try
            {
                GeneralTransform transform = frameworkElement.TransformToAncestor(relativeTo);
                return transform.TransformBounds(new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight));
            }
            catch
            {
                return Rect.Empty;
            }
        }

        private bool TrySetQuantityValue(DependencyObject element, int quantity)
        {
            object target = element;
            Type type = target.GetType();

            foreach (string propertyName in new[] { "Value", "Text", "SelectedValue" })
            {
                PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                    continue;

                try
                {
                    object value = ConvertQuantityForProperty(quantity, property.PropertyType);
                    if (value == null)
                        continue;

                    property.SetValue(target, value, null);
                    return true;
                }
                catch
                {
                }
            }

            if (element is TextBox textBox)
            {
                textBox.Text = quantity.ToString();
                return true;
            }

            return false;
        }

        private bool TryReadQuantityValue(DependencyObject element, out int quantity)
        {
            quantity = 0;

            if (element is TextBox textBox && TryParseQuantity(textBox.Text, out quantity))
                return true;

            object target = element;
            Type type = target.GetType();

            foreach (string propertyName in new[] { "Value", "Text", "SelectedValue" })
            {
                PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanRead)
                    continue;

                try
                {
                    object value = property.GetValue(target, null);
                    if (TryParseQuantity(value, out quantity))
                        return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private bool TryParseQuantity(object value, out int quantity)
        {
            quantity = 0;
            if (value == null)
                return false;

            if (value is int intValue)
            {
                quantity = intValue;
                return true;
            }

            if (value is double doubleValue)
            {
                quantity = (int)Math.Round(doubleValue);
                return true;
            }

            if (value is decimal decimalValue)
            {
                quantity = (int)Math.Round(decimalValue);
                return true;
            }

            string text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            int parsed;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                quantity = parsed;
                return true;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
            {
                quantity = parsed;
                return true;
            }

            return false;
        }

        private object ConvertQuantityForProperty(int quantity, Type propertyType)
        {
            if (propertyType == typeof(int))
                return quantity;
            if (propertyType == typeof(double))
                return (double)quantity;
            if (propertyType == typeof(decimal))
                return (decimal)quantity;
            if (propertyType == typeof(string))
                return quantity.ToString();
            if (propertyType == typeof(object))
                return quantity;

            Type nullableType = Nullable.GetUnderlyingType(propertyType);
            if (nullableType != null)
                return ConvertQuantityForProperty(quantity, nullableType);

            return null;
        }

        private bool TryHandlePresetButton(Point point)
        {
            if (!IsPointInPresetButtons(point))
                return false;

            if (ShouldSuppressFastQuantityClick())
                return true;

            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;
            suppressNextMouseUpSelection = true;

            if (preset1Button.Contains(point))
                SetPanelQuantity(1);
            else if (preset2Button.Contains(point))
                SetPanelQuantity(2);
            else if (preset5Button.Contains(point))
                SetPanelQuantity(5);
            else if (preset10Button.Contains(point))
                SetPanelQuantity(10);
            else if (preset20Button.Contains(point))
                SetPanelQuantity(20);

            return true;
        }

        private bool TryHandleAttachToPriceButton(Point point)
        {
            if (!attachToPriceButton.Contains(point))
                return false;

            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;
            suppressNextMouseUpSelection = true;

            stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
            targetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
            offsetsInitialized = true;
            isDetachedFromPrice = false;
            StartAttachedToCurrentPrice = true;
            return true;
        }

        private bool TryHandleDirectionButton(Point point)
        {
            if (!directionButton.Contains(point))
                return false;

            ToggleDirection();
            return true;
        }

        private bool TryHandleToolbarDirectionButton(Point point)
        {
            if (!toolbarDirectionButton.Contains(point))
                return false;

            ToggleDirection();
            return true;
        }

        private void ToggleDirection()
        {
            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;
            suppressNextMouseUpSelection = true;

            Direction = Direction == MpPanelDirection.Long ? MpPanelDirection.Short : MpPanelDirection.Long;
            FlipStopAndTargetAroundEntry();
        }

        private bool TryHandleTradeSideButton(Point point)
        {
            if (!IsPointInTradeSideButtons(point))
                return false;

            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;
            suppressNextMouseUpSelection = true;

            return true;
        }

        private void FlipStopAndTargetAroundEntry()
        {
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null)
                return;

            double stopDistance = Math.Abs(stopAnchor.Price - entryAnchor.Price);
            double targetDistance = Math.Abs(targetAnchor.Price - entryAnchor.Price);

            if (Direction == MpPanelDirection.Long)
            {
                stopAnchor.Price = RoundToTick(entryAnchor.Price - stopDistance);
                targetAnchor.Price = RoundToTick(entryAnchor.Price + targetDistance);
            }
            else
            {
                stopAnchor.Price = RoundToTick(entryAnchor.Price + stopDistance);
                targetAnchor.Price = RoundToTick(entryAnchor.Price - targetDistance);
            }

            stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
            targetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
            offsetsInitialized = true;
        }

        private bool IsPointInQuantityButtons(Point point)
        {
            return qtyMinusButton.Contains(point)
                || qtyPlusButton.Contains(point)
                || qtyValueBox.Contains(point);
        }

        private bool IsPointInPresetButtons(Point point)
        {
            return preset1Button.Contains(point)
                || preset2Button.Contains(point)
                || preset5Button.Contains(point)
                || preset10Button.Contains(point)
                || preset20Button.Contains(point);
        }

        private bool IsPointInTradeSideButtons(Point point)
        {
            return buyButton.Contains(point) || sellButton.Contains(point);
        }

        private string GetBuyButtonLabel()
        {
            double currentPrice = GetCurrentPrice();
            if (currentPrice <= 0 || entryAnchor == null)
                return "BUY";

            int cmp = ComparePrices(entryAnchor.Price, currentPrice);
            if (cmp == 0)
                return "BUY MKT";

            return cmp > 0 ? "BUY STP" : "BUY LMT";
        }

        private string GetSellButtonLabel()
        {
            double currentPrice = GetCurrentPrice();
            if (currentPrice <= 0 || entryAnchor == null)
                return "SELL";

            int cmp = ComparePrices(entryAnchor.Price, currentPrice);
            if (cmp == 0)
                return "SELL MKT";

            return cmp < 0 ? "SELL STP" : "SELL LMT";
        }

        private int ComparePrices(double a, double b)
        {
            double tolerance = Math.Max(TickSize / 2.0, 0.0000001);
            if (Math.Abs(a - b) <= tolerance)
                return 0;

            return a > b ? 1 : -1;
        }

        private bool ShouldSuppressFastQuantityClick()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - lastQuantityButtonClick).TotalMilliseconds < 200)
                return true;

            lastQuantityButtonClick = now;
            return false;
        }

        private void UpdateAutoFollowPrice()
        {
            if (isDetachedFromPrice || !StartAttachedToCurrentPrice || DrawingState == DrawingState.Building || DrawingState == DrawingState.Editing)
                return;

            double currentPrice = GetCurrentPrice();
            if (currentPrice <= 0)
                return;

            entryAnchor.Price = currentPrice;
            stopAnchor.Price = RoundToTick(entryAnchor.Price + stopOffsetFromEntry);
            targetAnchor.Price = RoundToTick(entryAnchor.Price + targetOffsetFromEntry);
        }

        private double GetCurrentPrice()
        {
            ChartBars chartBars = GetAttachedToChartBars();
            if (chartBars == null || chartBars.Bars == null || chartBars.Bars.Count <= 0)
                return 0;

            if (!offsetsInitialized)
            {
                stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
                targetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
                offsetsInitialized = true;
            }

            double currentPrice = RoundToTick(chartBars.Bars.LastPrice);
            if (currentPrice <= 0)
                currentPrice = RoundToTick(chartBars.Bars.GetClose(chartBars.Bars.Count - 1));

            return currentPrice;
        }
    }
}
