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
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.DrawingTools
{
    public enum MrRexoFreePanelDirection
    {
        Long,
        Short
    }

    public enum MpTargetMode
    {
        Single,
        Split
    }

    public enum MpShiftSnapMode
    {
        EvenPrice,
        FromOP
    }

    public enum MrRexoFreePanelLanguage
    {
        PL,
        EN,
        DE
    }

    [DisplayName("MrRexoRR Free")]
    [Description("Free edition of MrRexoRR risk/reward panel for NinjaTrader 8.")]
    public class MrRexoRR : DrawingTool
    {
        private const int CursorSensitivity = 15;

        private ChartAnchor entryAnchor;
        private ChartAnchor stopAnchor;
        private ChartAnchor targetAnchor;
        private ChartAnchor target2Anchor;
        private ChartAnchor target3Anchor;
        private ChartAnchor editingAnchor;
        private bool isDetachedFromPrice;
        private bool offsetsInitialized;
        private double stopOffsetFromEntry;
        private double targetOffsetFromEntry;
        private double target2OffsetFromEntry;
        private double target3OffsetFromEntry;
        private double finalTargetOffsetFromEntry;
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
        private Rect breakEvenButton;
        private Rect closePositionButton;
        private Rect panelVisibilityButton;
        private Rect toolbarDirectionButton;
        private Rect targetLevelsButton;
        private Rect targetDragRect;
        private Rect target2DragRect;
        private Rect target3DragRect;
        private Rect entryDragRect;
        private Rect stopDragRect;
        private bool suppressNextMouseUpSelection;
        private bool isRiskPanelHidden;
        private DateTime lastQuantityButtonClick = DateTime.MinValue;
        private DateTime lastPanelQuantityWrite = DateTime.MinValue;
        private DateTime lastTargetLevelsButtonClick = DateTime.MinValue;
        private DateTime lastTradeButtonClick = DateTime.MinValue;
        private DateTime lastChartTraderQuantityRead = DateTime.MinValue;
        private int pendingChartTraderQuantity;
        private int pendingPreviousChartTraderQuantity;
        private int lastKnownChartTraderQuantity;
        private int ignoredChartTraderQuantityAfterPanelChange;
        private double minDistanceFromEntryPrice;
        private string lastAutoSnapInstrumentName = string.Empty;
        private ChartControl lastChartControl;
        private DispatcherTimer chartTraderQuantityTimer;
        private Account subscribedOrderAccount;
        private readonly object orderLock = new object();
        private readonly Dictionary<Order, PendingBracket> pendingBrackets = new Dictionary<Order, PendingBracket>();
        private string orderStatusMessage = string.Empty;
        private DateTime orderStatusMessageTime = DateTime.MinValue;

        private class PendingBracket
        {
            public MrRexoFreePanelDirection Direction { get; set; }
            public double StopPrice { get; set; }
            public List<double> TargetPrices { get; set; }
            public int[] TargetQuantities { get; set; }
        }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Quantity", GroupName = "Parameters", Order = 1)]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable order submission", GroupName = "Orders", Order = 0)]
        public bool EnableOrderSubmission { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Account override", GroupName = "Orders", Order = 1)]
        public string OrderAccountName { get; set; }

        [Range(0, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "BE buffer ticks", GroupName = "Orders", Order = 2)]
        public int BreakEvenBufferTicks { get; set; }

        [NinjaScriptProperty]
        [Browsable(false)]
        public MrRexoFreePanelDirection Direction { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Tick value", GroupName = "Instrument", Order = 10)]
        public double TickValue { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Tick size", GroupName = "Instrument", Order = 11)]
        public double TickSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Auto instrument", GroupName = "Instrument", Order = 12)]
        public bool AutoInstrumentSpec { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Start attached to price", GroupName = "Parameters", Order = 3)]
        public bool StartAttachedToCurrentPrice { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Sync Chart Trader qty", GroupName = "Parameters", Order = 4)]
        public bool SyncChartTraderQuantity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Panel language", GroupName = "Parameters", Order = 0)]
        public MrRexoFreePanelLanguage PanelLanguage { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TP mode", GroupName = "Parameters", Order = 5)]
        public MpTargetMode TargetMode { get; set; }

        [Range(1, 3)]
        [NinjaScriptProperty]
        [Display(Name = "Split TP levels", GroupName = "Parameters", Order = 6)]
        public int TargetLevels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Shift snap step", GroupName = "Parameters", Order = 7)]
        public double ShiftSnapPoints { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Shift snap mode", GroupName = "Parameters", Order = 8)]
        public MpShiftSnapMode ShiftSnapMode { get; set; }

        public override object Icon => "FR";

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
                if (target2Anchor != null && GetEffectiveTargetLevels() >= 2)
                    yield return target2Anchor;
                if (target3Anchor != null && GetEffectiveTargetLevels() >= 3)
                    yield return target3Anchor;
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MrRexoRR Free";
                Description = "Interaktywny panel ryzyka i zysku z maksymalnie trzema poziomami TP i jednym SL.";
                DrawingState = DrawingState.Building;
                Quantity = 1;
                EnableOrderSubmission = true;
                OrderAccountName = string.Empty;
                BreakEvenBufferTicks = 1;
                PanelLanguage = MrRexoFreePanelLanguage.EN;
                TargetMode = MpTargetMode.Single;
                TargetLevels = 1;
                ShiftSnapPoints = 0;
                ShiftSnapMode = MpShiftSnapMode.EvenPrice;
                Direction = MrRexoFreePanelDirection.Long;
                StartAttachedToCurrentPrice = true;
                SyncChartTraderQuantity = true;
                TickSize = 0.25;
                TickValue = 0.50;
                AutoInstrumentSpec = true;
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
                    DisplayName = "TP1",
                    IsBrowsable = true
                };

                target2Anchor = new ChartAnchor
                {
                    IsEditing = true,
                    DrawingTool = this,
                    DisplayName = "TP2",
                    IsBrowsable = true
                };

                target3Anchor = new ChartAnchor
                {
                    IsEditing = true,
                    DrawingTool = this,
                    DisplayName = "TP3",
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

                UnsubscribeOrderAccount();
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
            UpdateInstrumentSpecFromChart();

            if (DrawingState == DrawingState.Building)
            {
                dataPoint.CopyDataValues(entryAnchor);
                dataPoint.CopyDataValues(stopAnchor);
                dataPoint.CopyDataValues(targetAnchor);
                dataPoint.CopyDataValues(target2Anchor);
                dataPoint.CopyDataValues(target3Anchor);
                SetInitialRiskRewardLevels(chartScale);

                entryAnchor.IsEditing = false;
                stopAnchor.IsEditing = false;
                targetAnchor.IsEditing = false;
                target2Anchor.IsEditing = false;
                target3Anchor.IsEditing = false;
                isDetachedFromPrice = !StartAttachedToCurrentPrice;
                offsetsInitialized = true;
                UpdateOffsetsFromAnchors();
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

                if (TryHandlePanelVisibilityButton(point))
                    return;

                if (TryHandleToolbarDirectionButton(point))
                    return;

                if (TryHandleTargetLevelsButton(point))
                    return;

                if (TryHandleDirectionButton(point))
                    return;

                if (isRiskPanelHidden)
                {
                    IsSelected = false;
                    return;
                }

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
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null || target2Anchor == null || target3Anchor == null)
                return;

            if (DrawingState == DrawingState.Building)
                UpdateInstrumentSpecFromChart();

            if (DrawingState == DrawingState.Building)
            {
                dataPoint.CopyDataValues(entryAnchor);
                dataPoint.CopyDataValues(stopAnchor);
                dataPoint.CopyDataValues(targetAnchor);
                dataPoint.CopyDataValues(target2Anchor);
                dataPoint.CopyDataValues(target3Anchor);
                SetInitialRiskRewardLevels(chartScale);
            }
            else if (DrawingState == DrawingState.Editing && editingAnchor != null)
            {
                double newPrice = RoundToTick(dataPoint.Price);

                if (editingAnchor == entryAnchor)
                {
                    newPrice = ApplyShiftSnapToEntryPrice(newPrice);
                    double finalTargetOffset = Math.Abs(finalTargetOffsetFromEntry) >= TickSize
                        ? finalTargetOffsetFromEntry
                        : GetCurrentFinalTargetPrice() - entryAnchor.Price;
                    double priceDelta = newPrice - entryAnchor.Price;
                    entryAnchor.Price = newPrice;
                    stopAnchor.Price = RoundToTick(stopAnchor.Price + priceDelta);
                    finalTargetOffsetFromEntry = finalTargetOffset;
                    DistributeTargetsToFinalTarget(RoundToTick(entryAnchor.Price + finalTargetOffsetFromEntry), false);
                    UpdateOffsetsFromAnchors();
                }
                else if (editingAnchor == stopAnchor)
                {
                    newPrice = ApplyShiftSnapToPrice(newPrice, false);
                    stopAnchor.Price = ClampStopPrice(newPrice);
                    stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
                }
                else if (editingAnchor == targetAnchor)
                {
                    newPrice = ApplyShiftSnapToPrice(newPrice, true);
                    targetAnchor.Price = ClampTargetPrice(newPrice);
                    DistributeTargetsToFinalTarget(targetAnchor.Price, true);
                }
                else if (editingAnchor == target2Anchor)
                {
                    newPrice = ApplyShiftSnapToPrice(newPrice, true);
                    target2Anchor.Price = ClampTargetPrice(newPrice);
                    DistributeTargetsToFinalTarget(target2Anchor.Price, true);
                }
                else if (editingAnchor == target3Anchor)
                {
                    newPrice = ApplyShiftSnapToPrice(newPrice, true);
                    target3Anchor.Price = ClampTargetPrice(newPrice);
                    DistributeTargetsToFinalTarget(target3Anchor.Price, true);
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

            if (IsPointInToolbarButtons(point) || IsPointInQuantityButtons(point) || IsPointInPresetButtons(point) || directionButton.Contains(point) || toolbarDirectionButton.Contains(point) || targetLevelsButton.Contains(point) || attachToPriceButton.Contains(point) || IsPointInTradeSideButtons(point))
                return Cursors.Hand;

            if (isRiskPanelHidden)
                return null;

            ChartAnchor closestAnchor = GetPanelDragAnchor(chartScale, point);
            return closestAnchor == null ? null : Cursors.SizeNS;
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null || target2Anchor == null || target3Anchor == null || ChartPanel == null)
                return;

            lastChartControl = chartControl;
            UpdateInstrumentSpecFromChart();
            SyncPanelQuantityFromChartTrader(chartControl);
            NormalizeTargetLevels();
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
            Point target2 = new Point(lastPanelCenterX, chartScale.GetYByValue(target2Anchor.Price));
            Point target3 = new Point(lastPanelCenterX, chartScale.GetYByValue(target3Anchor.Price));
            List<Point> targetPoints = GetActiveTargetPoints(target, target2, target3);
            minDistanceFromEntryPrice = CalculateEntryVisualBufferPrice(chartScale, entry.Y);

            double top = Math.Min(stop.Y, targetPoints.Min(tp => tp.Y));
            double bottom = Math.Max(stop.Y, targetPoints.Max(tp => tp.Y));

            if (isRiskPanelHidden)
            {
                ClearPanelInteractionRects();
            }
            else
            {
                DrawPanelBackground(chartControl, left, right, top, bottom, entry.Y, targetPoints, stop.Y);
                DrawVirtualLevelLines(left, targetPoints, stop.Y);
                DrawPanelLabels(chartControl, left, right, entry, stop, targetPoints);
            }

            DrawPresetButtons(chartControl);
        }

        private void ClearPanelInteractionRects()
        {
            targetDragRect = Rect.Empty;
            target2DragRect = Rect.Empty;
            target3DragRect = Rect.Empty;
            entryDragRect = Rect.Empty;
            stopDragRect = Rect.Empty;
            directionButton = Rect.Empty;
            attachToPriceButton = Rect.Empty;
            qtyMinusButton = Rect.Empty;
            qtyValueBox = Rect.Empty;
            qtyPlusButton = Rect.Empty;
        }

        private void DrawVirtualLevelLines(double panelLeft, List<Point> targetPoints, double stopY)
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
                foreach (Point targetPoint in targetPoints)
                    RenderTarget.DrawLine(new SharpDX.Vector2(startX, (float)targetPoint.Y), new SharpDX.Vector2(endX, (float)targetPoint.Y), tpBrush, 1f, strokeStyle);
                RenderTarget.DrawLine(new SharpDX.Vector2(startX, (float)stopY), new SharpDX.Vector2(endX, (float)stopY), slBrush, 1f, strokeStyle);
            }
        }

        private void DrawPanelBackground(ChartControl chartControl, double left, double right, double top, double bottom, double entryY, List<Point> targetPoints, double stopY)
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

                double farthestTargetY = targetPoints.OrderByDescending(tp => Math.Abs(tp.Y - entryY)).First().Y;
                float profitTop = (float)Math.Min(farthestTargetY, entryY);
                float profitBottom = (float)Math.Max(farthestTargetY, entryY);
                float riskTop = (float)Math.Min(stopY, entryY);
                float riskBottom = (float)Math.Max(stopY, entryY);
                float stopHeaderY = (float)(stopY < entryY ? stopY - headerHeight : stopY);
                float outlineTop = Math.Min((float)targetPoints.Min(tp => tp.Y) - headerHeight, stopHeaderY);
                float outlineBottom = Math.Max((float)targetPoints.Max(tp => tp.Y) + headerHeight, stopHeaderY + headerHeight);

                targetDragRect = Rect.Empty;
                target2DragRect = Rect.Empty;
                target3DragRect = Rect.Empty;
                entryDragRect = new Rect(left, entryY - entryHeight / 2, right - left, entryHeight);
                stopDragRect = new Rect(left, stopHeaderY, right - left, headerHeight);

                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, profitTop, width, profitBottom - profitTop), profitBrush);
                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, riskTop, width, riskBottom - riskTop), riskBrush);
                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, (float)entryY - entryHeight / 2, width, entryHeight), entryBrush);

                for (int i = 0; i < targetPoints.Count; i++)
                {
                    float targetHeaderY = (float)(targetPoints[i].Y < entryY ? targetPoints[i].Y - headerHeight : targetPoints[i].Y);
                    Rect targetRect = new Rect(left, targetHeaderY, right - left, headerHeight);
                    if (i == targetPoints.Count - 1)
                    {
                        if (i == 0)
                            targetDragRect = targetRect;
                        else if (i == 1)
                            target2DragRect = targetRect;
                        else if (i == 2)
                            target3DragRect = targetRect;
                    }

                    RenderTarget.FillRectangle(new SharpDX.RectangleF(x, targetHeaderY, width, headerHeight), greenHeaderBrush);
                }
                RenderTarget.FillRectangle(new SharpDX.RectangleF(x, stopHeaderY, width, headerHeight), redHeaderBrush);
                RenderTarget.DrawRectangle(new SharpDX.RectangleF(x, outlineTop, width, outlineBottom - outlineTop), outlineBrush, 1.5f);
            }
        }

        private void DrawPanelLabels(ChartControl chartControl, double left, double right, Point entry, Point stop, List<Point> targetPoints)
        {
            if (RenderTarget == null || entryAnchor == null || stopAnchor == null || targetAnchor == null)
                return;

            double stopTicks = CalculateTicks(entryAnchor.Price, stopAnchor.Price);
            double risk = CalculateMoney(stopTicks);
            double reward = CalculateTotalReward();
            double rr = risk <= 0 ? 0 : reward / risk;

            double panelWidth = right - left;
            double stopLabelY = stop.Y < entry.Y ? stop.Y - 24 : stop.Y + 4;
            const double headerHeight = 28.0;
            double stopHeaderY = stop.Y < entry.Y ? stop.Y - headerHeight : stop.Y;
            double profitSummaryY;
            double riskSummaryY;
            if (Direction == MrRexoFreePanelDirection.Short)
            {
                double bottomTargetY = targetPoints.Max(tp => tp.Y);
                double bottomTargetHeaderY = bottomTargetY < entry.Y ? bottomTargetY - headerHeight : bottomTargetY;
                profitSummaryY = Math.Min(ChartPanel.Y + ChartPanel.H - 22, bottomTargetHeaderY + headerHeight + 2);
                riskSummaryY = Math.Max(ChartPanel.Y, stopHeaderY - 24);
            }
            else
            {
                double topTargetY = targetPoints.Min(tp => tp.Y);
                double topTargetHeaderY = topTargetY < entry.Y ? topTargetY - headerHeight : topTargetY;
                profitSummaryY = Math.Max(ChartPanel.Y, topTargetHeaderY - 24);
                riskSummaryY = stopHeaderY + headerHeight + 2;
            }

            const double halfEntryBarHeight = 15.0;
            double riskBlockTop = Direction == MrRexoFreePanelDirection.Short
                ? stopHeaderY + headerHeight
                : entry.Y + halfEntryBarHeight;
            double riskBlockBottom = Direction == MrRexoFreePanelDirection.Short
                ? entry.Y - halfEntryBarHeight
                : stopHeaderY;
            if (riskBlockBottom - riskBlockTop >= 80)
                DrawLargeQuantityText(chartControl, $"{T("IK")}: {Quantity}", left, riskBlockTop, panelWidth, riskBlockBottom - riskBlockTop);

            List<ChartAnchor> targets = GetActiveTargetAnchors();
            int[] splits = GetTargetQuantitySplit();
            for (int i = 0; i < targets.Count; i++)
            {
                if (splits[i] <= 0)
                    continue;

                double targetLabelY = targetPoints[i].Y < entry.Y ? targetPoints[i].Y - 24 : targetPoints[i].Y + 4;
                DrawText(chartControl, $"TP{i + 1}: {targets[i].Price:F2}  K:{splits[i]}", left, targetLabelY, panelWidth, 22, true, true);
            }

            DrawText(chartControl, $"{T("TotalProfit")}: {reward:F2}$", left, profitSummaryY, panelWidth, 22, false, true);
            DrawText(chartControl, $"OP: {entryAnchor.Price:F2}  K: {Quantity}  [R:R {rr:F2}]", left, entry.Y - 10, panelWidth, 22, true, true);
            DrawText(chartControl, $"SL: {stopAnchor.Price:F2}", left, stopLabelY, panelWidth, 22, true, true);
            DrawText(chartControl, $"{T("Risk")}: {stopTicks:F0} {T("Ticks")} / {risk:F2}$", left, riskSummaryY, panelWidth, 22, false, true);
            DrawDirectionButton(chartControl, left, entry.Y);
            DrawAttachToPriceButton(chartControl, left, entry.Y);
            DrawQuantityButtons(chartControl, right, entry.Y);
        }

        private void DrawDirectionButton(ChartControl chartControl, double left, double entryY)
        {
            directionButton = new Rect(left + 6, entryY - 11, 30, 22);
            DrawButton(chartControl, directionButton, Direction == MrRexoFreePanelDirection.Long ? "L" : "S");
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
            double actionWidth = 52;
            double visibilityWidth = 48;
            double directionWidth = 30;
            double targetLevelsWidth = 38;
            double totalWidth = tradeWidth + gap + tradeWidth + gap + actionWidth + gap + actionWidth + gap + visibilityWidth + 12 + directionWidth + gap + targetLevelsWidth + 12 + 24 + gap + 24 + gap + 24 + gap + 31 + gap + 31;
            double padding = 6;
            double toolbarWidth = totalWidth + 2 * padding;
            double toolbarHeight = h + 2 * padding;
            double x = ChartPanel.X + (ChartPanel.W - toolbarWidth) / 2.0 + padding;
            double y = ChartPanel.Y + 48 + padding;

            presetsToolbarRect = new Rect(x - padding, y - padding, toolbarWidth, toolbarHeight);
            DrawToolbarBackground(presetsToolbarRect);

            buyButton = new Rect(x, y, tradeWidth, h);
            sellButton = new Rect(buyButton.Right + gap, y, tradeWidth, h);
            breakEvenButton = new Rect(sellButton.Right + gap, y, actionWidth, h);
            closePositionButton = new Rect(breakEvenButton.Right + gap, y, actionWidth, h);
            panelVisibilityButton = new Rect(closePositionButton.Right + gap, y, visibilityWidth, h);
            toolbarDirectionButton = new Rect(panelVisibilityButton.Right + 12, y, directionWidth, h);
            targetLevelsButton = new Rect(toolbarDirectionButton.Right + gap, y, targetLevelsWidth, h);
            preset1Button = new Rect(targetLevelsButton.Right + 12, y, 24, h);
            preset2Button = new Rect(preset1Button.Right + gap, y, 24, h);
            preset5Button = new Rect(preset2Button.Right + gap, y, 24, h);
            preset10Button = new Rect(preset5Button.Right + gap, y, 31, h);
            preset20Button = new Rect(preset10Button.Right + gap, y, 31, h);

            DrawButton(chartControl, buyButton, GetBuyButtonLabel(), new SharpDX.Color4(0.00f, 0.42f, 0.22f, 0.96f));
            DrawButton(chartControl, sellButton, GetSellButtonLabel(), new SharpDX.Color4(0.58f, 0.04f, 0.08f, 0.96f));
            DrawButton(chartControl, breakEvenButton, "BE", new SharpDX.Color4(0.02f, 0.28f, 0.54f, 0.96f));
            DrawButton(chartControl, closePositionButton, "CLOSE", new SharpDX.Color4(0.72f, 0.24f, 0.00f, 0.96f));
            DrawButton(chartControl, panelVisibilityButton, isRiskPanelHidden ? "SHOW" : "HIDE", new SharpDX.Color4(0.28f, 0.20f, 0.54f, 0.96f));
            DrawButton(chartControl, toolbarDirectionButton, Direction == MrRexoFreePanelDirection.Long ? "L" : "S");
            DrawButton(chartControl, targetLevelsButton, $"TP{GetEffectiveTargetLevels()}");
            DrawButton(chartControl, preset1Button, "1");
            DrawButton(chartControl, preset2Button, "2");
            DrawButton(chartControl, preset5Button, "5");
            DrawButton(chartControl, preset10Button, "10");
            DrawButton(chartControl, preset20Button, "20");

            DrawOrderStatus(chartControl, presetsToolbarRect);
        }

        private void DrawOrderStatus(ChartControl chartControl, Rect toolbarRect)
        {
            if (string.IsNullOrWhiteSpace(orderStatusMessage) || (DateTime.UtcNow - orderStatusMessageTime).TotalSeconds > 8)
                return;

            DrawText(chartControl, orderStatusMessage, toolbarRect.X, toolbarRect.Bottom + 2, toolbarRect.Width, 20, false, true);
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

        private void DrawButton(ChartControl chartControl, Rect rect, string label, SharpDX.Color4? fillColor = null)
        {
            SharpDX.Color4 buttonFill = fillColor ?? new SharpDX.Color4(0.12f, 0.15f, 0.18f, 0.94f);
            using (var fill = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, buttonFill))
            using (var border = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0.82f, 0.92f, 1f, 0.78f)))
            {
                var dxRect = new SharpDX.RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
                RenderTarget.FillRectangle(dxRect, fill);
                RenderTarget.DrawRectangle(dxRect, border, 1f);
            }

            DrawText(chartControl, label, rect.X, rect.Y, rect.Width, rect.Height, true, true);
        }

        private void DrawLargeQuantityText(ChartControl chartControl, string text, double x, double y, double width, double height)
        {
            if (RenderTarget == null)
                return;

            SimpleFont baseFont = chartControl.Properties.LabelFont ?? new SimpleFont();
            SharpDX.DirectWrite.TextFormat textFormat = baseFont.ToDirectWriteTextFormat();
            textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
            textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
            SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, text, textFormat, (float)width, (float)height);

            float fontSize = 56f;
            textLayout.SetFontSize(fontSize, new SharpDX.DirectWrite.TextRange(0, text.Length));

            using (var textBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(1f, 1f, 1f, 0.18f)))
                RenderTarget.DrawTextLayout(new SharpDX.Vector2((float)x, (float)y), textLayout, textBrush, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

            textLayout.Dispose();
            textFormat.Dispose();
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

        private void SetInitialRiskRewardLevels(ChartScale chartScale)
        {
            const double headerHeight = 28.0;
            double minimumVisualStopDistance = GetPriceDistanceForPixels(chartScale, entryAnchor.Price, headerHeight * 2.0);
            double stopDistance = RoundToTick(Math.Max(GetPriceDistanceForDollars(50), minimumVisualStopDistance));
            double finalTargetDistance = RoundToTick(stopDistance * 3.0);
            double target1Distance = finalTargetDistance;
            double target2Distance = RoundToTick(finalTargetDistance * 2.0 / 3.0);
            double target3Distance = finalTargetDistance;

            if (Direction == MrRexoFreePanelDirection.Long)
            {
                stopAnchor.Price = RoundToTick(entryAnchor.Price - stopDistance);
                targetAnchor.Price = RoundToTick(entryAnchor.Price + target1Distance);
                target2Anchor.Price = RoundToTick(entryAnchor.Price + target2Distance);
                target3Anchor.Price = RoundToTick(entryAnchor.Price + target3Distance);
            }
            else
            {
                stopAnchor.Price = RoundToTick(entryAnchor.Price + stopDistance);
                targetAnchor.Price = RoundToTick(entryAnchor.Price - target1Distance);
                target2Anchor.Price = RoundToTick(entryAnchor.Price - target2Distance);
                target3Anchor.Price = RoundToTick(entryAnchor.Price - target3Distance);
            }

            finalTargetOffsetFromEntry = GetCurrentFinalTargetPrice() - entryAnchor.Price;
            UpdateOffsetsFromAnchors();
        }

        private double GetPriceDistanceForPixels(ChartScale chartScale, double price, double pixels)
        {
            if (chartScale == null || pixels <= 0)
                return TickSize;

            double y = chartScale.GetYByValue(price);
            double priceAtOffset = chartScale.GetValueByY((float)(y - pixels));
            return Math.Max(TickSize, RoundToTick(Math.Abs(priceAtOffset - price)));
        }

        private void UpdateOffsetsFromAnchors()
        {
            stopOffsetFromEntry = stopAnchor.Price - entryAnchor.Price;
            targetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
            target2OffsetFromEntry = target2Anchor.Price - entryAnchor.Price;
            target3OffsetFromEntry = target3Anchor.Price - entryAnchor.Price;
        }

        private void NormalizeTargetLevels()
        {
            TargetLevels = Math.Max(1, Math.Min(3, TargetLevels));

            double minDistance = GetMinimumDistanceFromEntry();
            if (Math.Abs(target2Anchor.Price - entryAnchor.Price) < minDistance)
                target2Anchor.Price = Direction == MrRexoFreePanelDirection.Long
                    ? RoundToTick(entryAnchor.Price + Math.Max(minDistance, Math.Abs(targetAnchor.Price - entryAnchor.Price) * 1.5))
                    : RoundToTick(entryAnchor.Price - Math.Max(minDistance, Math.Abs(targetAnchor.Price - entryAnchor.Price) * 1.5));

            if (Math.Abs(target3Anchor.Price - entryAnchor.Price) < minDistance)
                target3Anchor.Price = Direction == MrRexoFreePanelDirection.Long
                    ? RoundToTick(entryAnchor.Price + Math.Max(minDistance, Math.Abs(targetAnchor.Price - entryAnchor.Price) * 2.0))
                    : RoundToTick(entryAnchor.Price - Math.Max(minDistance, Math.Abs(targetAnchor.Price - entryAnchor.Price) * 2.0));

            targetAnchor.Price = ClampTargetPrice(targetAnchor.Price);
            target2Anchor.Price = ClampTargetPrice(target2Anchor.Price);
            target3Anchor.Price = ClampTargetPrice(target3Anchor.Price);
            if (GetEffectiveTargetLevels() > 1)
                DistributeTargetsToFinalTarget(GetStoredFinalTargetPrice(), false);
            else
                ReorderActiveTargetPricesByDistance();
            UpdateOffsetsFromAnchors();
        }

        private void DistributeSplitTargetsFromSingleTarget()
        {
            DistributeTargetsToFinalTarget(GetCurrentFinalTargetPrice(), true);
        }

        private double GetCurrentFinalTargetPrice()
        {
            int levels = GetEffectiveTargetLevels();
            if (levels <= 1)
                return targetAnchor.Price;
            if (levels == 2)
                return target2Anchor.Price;
            return target3Anchor.Price;
        }

        private double GetStoredFinalTargetPrice()
        {
            if (Math.Abs(finalTargetOffsetFromEntry) < TickSize)
                finalTargetOffsetFromEntry = GetCurrentFinalTargetPrice() - entryAnchor.Price;

            return RoundToTick(entryAnchor.Price + finalTargetOffsetFromEntry);
        }

        private ChartAnchor GetDraggableTargetAnchor()
        {
            int levels = GetEffectiveTargetLevels();
            if (levels <= 1)
                return targetAnchor;
            if (levels == 2)
                return target2Anchor;
            return target3Anchor;
        }

        private void DistributeTargetsToFinalTarget(double finalTargetPrice, bool updateFinalOffset)
        {
            int levels = GetEffectiveTargetLevels();
            finalTargetPrice = ClampTargetPrice(finalTargetPrice);

            if (levels <= 1)
            {
                targetAnchor.Price = finalTargetPrice;
                if (updateFinalOffset)
                    finalTargetOffsetFromEntry = targetAnchor.Price - entryAnchor.Price;
                UpdateOffsetsFromAnchors();
                return;
            }

            double finalDistance = Math.Abs(finalTargetPrice - entryAnchor.Price);
            double minDistance = GetMinimumDistanceFromEntry();
            finalDistance = Math.Max(finalDistance, minDistance * levels);

            double directionSign = Direction == MrRexoFreePanelDirection.Long ? 1.0 : -1.0;
            targetAnchor.Price = RoundToTick(entryAnchor.Price + directionSign * finalDistance / levels);
            target2Anchor.Price = RoundToTick(entryAnchor.Price + directionSign * finalDistance * 2.0 / levels);
            target3Anchor.Price = RoundToTick(entryAnchor.Price + directionSign * finalDistance);
            if (updateFinalOffset)
                finalTargetOffsetFromEntry = GetCurrentFinalTargetPrice() - entryAnchor.Price;
            UpdateOffsetsFromAnchors();
        }

        private void ReorderActiveTargetPricesByDistance()
        {
            int levels = GetEffectiveTargetLevels();
            if (levels <= 1)
                return;

            List<double> orderedPrices = GetActiveTargetAnchors()
                .Select(anchor => anchor.Price)
                .OrderBy(price => Math.Abs(price - entryAnchor.Price))
                .ToList();

            targetAnchor.Price = orderedPrices[0];
            if (levels >= 2)
                target2Anchor.Price = orderedPrices[1];
            if (levels >= 3)
                target3Anchor.Price = orderedPrices[2];
        }

        private int GetEffectiveTargetLevels()
        {
            if (TargetMode == MpTargetMode.Single)
                return 1;

            return Math.Max(1, Math.Min(3, TargetLevels));
        }

        private List<ChartAnchor> GetActiveTargetAnchors()
        {
            List<ChartAnchor> targets = new List<ChartAnchor> { targetAnchor };
            int levels = GetEffectiveTargetLevels();
            if (levels >= 2)
                targets.Add(target2Anchor);
            if (levels >= 3)
                targets.Add(target3Anchor);
            return targets;
        }

        private List<ChartAnchor> GetAllTargetAnchors()
        {
            return new List<ChartAnchor> { targetAnchor, target2Anchor, target3Anchor };
        }

        private List<Point> GetActiveTargetPoints(Point target1, Point target2, Point target3)
        {
            List<Point> points = new List<Point> { target1 };
            int levels = GetEffectiveTargetLevels();
            if (levels >= 2)
                points.Add(target2);
            if (levels >= 3)
                points.Add(target3);
            return points;
        }

        private int[] GetTargetQuantitySplit()
        {
            int levels = GetEffectiveTargetLevels();
            int[] split = new int[levels];
            if (Quantity < levels)
            {
                for (int i = levels - Quantity; i < levels; i++)
                    split[i] = 1;

                return split;
            }

            int baseQty = Quantity / levels;
            int remainder = Quantity % levels;

            for (int i = 0; i < levels; i++)
                split[i] = baseQty + (i < remainder ? 1 : 0);

            return split;
        }

        private double CalculateTotalReward()
        {
            List<ChartAnchor> targets = GetActiveTargetAnchors();
            int[] split = GetTargetQuantitySplit();
            double total = 0;

            for (int i = 0; i < targets.Count; i++)
                total += CalculateMoney(CalculateTicks(entryAnchor.Price, targets[i].Price), split[i]);

            return total;
        }

        private double CalculateTicks(double priceA, double priceB)
        {
            return Math.Abs(priceA - priceB) / TickSize;
        }

        private double CalculateMoney(double ticks)
        {
            return ticks * TickValue * Quantity;
        }

        private double CalculateMoney(double ticks, int quantity)
        {
            return ticks * TickValue * Math.Max(0, quantity);
        }

        private void UpdateInstrumentSpecFromChart()
        {
            if (!AutoInstrumentSpec)
                return;

            ChartBars chartBars = GetAttachedToChartBars();
            if (chartBars == null)
                return;

            object bars = GetPropertyValue(chartBars, "Bars");
            object instrument = GetPropertyValue(bars, "Instrument") ?? GetPropertyValue(chartBars, "Instrument");
            string instrumentName = GetInstrumentName(instrument, bars, chartBars);

            if (TryApplyKnownFuturesSpec(instrumentName))
                return;

            object masterInstrument = GetPropertyValue(instrument, "MasterInstrument");
            if (masterInstrument == null)
                return;

            double instrumentTickSize = GetDoublePropertyValue(masterInstrument, "TickSize");
            double pointValue = GetDoublePropertyValue(masterInstrument, "PointValue");
            if (instrumentTickSize <= 0 || pointValue <= 0)
                return;

            TickSize = instrumentTickSize;
            TickValue = instrumentTickSize * pointValue;
        }

        private string GetInstrumentName(params object[] sources)
        {
            foreach (object source in sources)
            {
                string name = GetPropertyValue(source, "FullName")?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;

                name = GetPropertyValue(source, "Name")?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;

                name = source?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }

            return string.Empty;
        }

        private bool TryApplyKnownFuturesSpec(string instrumentName)
        {
            if (string.IsNullOrWhiteSpace(instrumentName))
                return false;

            string upperName = instrumentName.ToUpperInvariant();
            if (upperName.StartsWith("MNQ"))
            {
                TickSize = 0.25;
                TickValue = 0.50;
                ApplyDefaultShiftSnap(upperName, 20);
                return true;
            }

            if (upperName.StartsWith("NQ"))
            {
                TickSize = 0.25;
                TickValue = 5.00;
                ApplyDefaultShiftSnap(upperName, 20);
                return true;
            }

            if (upperName.StartsWith("FDXS"))
            {
                TickSize = 1;
                TickValue = 1.00;
                ApplyDefaultShiftSnap(upperName, 20);
                return true;
            }

            if (upperName.StartsWith("FDXM"))
            {
                TickSize = 1;
                TickValue = 5.00;
                ApplyDefaultShiftSnap(upperName, 20);
                return true;
            }

            if (upperName.StartsWith("FDAX"))
            {
                TickSize = 0.5;
                TickValue = 12.50;
                ApplyDefaultShiftSnap(upperName, 20);
                return true;
            }

            if (upperName.StartsWith("DAX"))
            {
                ApplyDefaultShiftSnap(upperName, 20);
                return false;
            }

            if (upperName.StartsWith("MGC"))
            {
                TickSize = 0.1;
                TickValue = 1.00;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            if (upperName.StartsWith("GC"))
            {
                TickSize = 0.1;
                TickValue = 10.00;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            if (upperName.StartsWith("SIL"))
            {
                TickSize = 0.005;
                TickValue = 5.00;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            if (upperName.StartsWith("SI"))
            {
                TickSize = 0.005;
                TickValue = 25.00;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            if (upperName.StartsWith("MES"))
            {
                TickSize = 0.25;
                TickValue = 1.25;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            if (upperName.StartsWith("ES"))
            {
                TickSize = 0.25;
                TickValue = 12.50;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            if (upperName.StartsWith("MYM"))
            {
                TickSize = 1;
                TickValue = 0.50;
                ApplyDefaultShiftSnap(upperName, 50);
                return true;
            }

            if (upperName.StartsWith("YM"))
            {
                TickSize = 1;
                TickValue = 5.00;
                ApplyDefaultShiftSnap(upperName, 50);
                return true;
            }

            if (upperName.StartsWith("M2K"))
            {
                TickSize = 0.1;
                TickValue = 0.50;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            if (upperName.StartsWith("RTY"))
            {
                TickSize = 0.1;
                TickValue = 5.00;
                ApplyDefaultShiftSnap(upperName, 10);
                return true;
            }

            return false;
        }

        private void ApplyDefaultShiftSnap(string instrumentName, double defaultSnapPoints)
        {
            if (string.Equals(lastAutoSnapInstrumentName, instrumentName, StringComparison.Ordinal))
                return;

            if (ShiftSnapPoints <= 0)
                ShiftSnapPoints = defaultSnapPoints;

            lastAutoSnapInstrumentName = instrumentName;
        }

        private object GetPropertyValue(object source, string propertyName)
        {
            if (source == null)
                return null;

            PropertyInfo property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property == null || !property.CanRead ? null : property.GetValue(source, null);
        }

        private double GetDoublePropertyValue(object source, string propertyName)
        {
            object value = GetPropertyValue(source, propertyName);
            if (value == null)
                return 0;

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
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

        private double ApplyShiftSnapToPrice(double price, bool isTarget)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift || ShiftSnapPoints <= 0 || entryAnchor == null)
                return price;

            double step = Math.Max(TickSize, ShiftSnapPoints);
            if (ShiftSnapMode == MpShiftSnapMode.EvenPrice)
                return RoundToTick(Math.Round(price / step, MidpointRounding.AwayFromZero) * step);

            double distance = Math.Abs(price - entryAnchor.Price);
            double snappedDistance = Math.Max(step, Math.Round(distance / step, MidpointRounding.AwayFromZero) * step);
            bool shouldBeAboveEntry = Direction == MrRexoFreePanelDirection.Long ? isTarget : !isTarget;
            double snappedPrice = shouldBeAboveEntry
                ? entryAnchor.Price + snappedDistance
                : entryAnchor.Price - snappedDistance;

            return RoundToTick(snappedPrice);
        }

        private double ApplyShiftSnapToEntryPrice(double price)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift || ShiftSnapPoints <= 0)
                return price;

            double step = Math.Max(TickSize, ShiftSnapPoints);
            return RoundToTick(Math.Round(price / step, MidpointRounding.AwayFromZero) * step);
        }

        private double ClampStopPrice(double price)
        {
            double minDistance = GetMinimumDistanceFromEntry();

            if (Direction == MrRexoFreePanelDirection.Long)
                return RoundToTick(Math.Min(price, entryAnchor.Price - minDistance));

            return RoundToTick(Math.Max(price, entryAnchor.Price + minDistance));
        }

        private double ClampTargetPrice(double price)
        {
            double minDistance = GetMinimumDistanceFromEntry();

            if (Direction == MrRexoFreePanelDirection.Long)
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
            CheckAnchor(GetDraggableTargetAnchor());

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
            if (isRiskPanelHidden || IsPointInToolbarButtons(point) || IsPointInQuantityButtons(point) || IsPointInPresetButtons(point) || directionButton.Contains(point) || toolbarDirectionButton.Contains(point) || targetLevelsButton.Contains(point) || attachToPriceButton.Contains(point) || IsPointInTradeSideButtons(point))
                return null;

            if (targetDragRect.Contains(point))
                return targetAnchor;

            if (target2DragRect.Contains(point))
                return target2Anchor;

            if (target3DragRect.Contains(point))
                return target3Anchor;

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
                int step = GetQuantityStep();
                SetPanelQuantity(Math.Max(step, Quantity - step));
                suppressNextMouseUpSelection = true;
                return true;
            }

            if (qtyPlusButton.Contains(point))
            {
                int step = GetQuantityStep();
                SetPanelQuantity(Math.Min(999, Quantity + step));
                suppressNextMouseUpSelection = true;
                return true;
            }

            return false;
        }

        private void SetPanelQuantity(int quantity)
        {
            int previousQuantity = Quantity;
            Quantity = NormalizeQuantityForTargetLevels(quantity);
            lastPanelQuantityWrite = DateTime.UtcNow;
            pendingChartTraderQuantity = Quantity;
            pendingPreviousChartTraderQuantity = previousQuantity;
            ignoredChartTraderQuantityAfterPanelChange = lastKnownChartTraderQuantity > 0 ? lastKnownChartTraderQuantity : previousQuantity;

            if (SyncChartTraderQuantity)
                TrySyncChartTraderQuantity(Quantity);
        }

        private int GetQuantityStep()
        {
            return Math.Max(1, GetEffectiveTargetLevels());
        }

        private int NormalizeQuantityForTargetLevels(int quantity)
        {
            int step = GetQuantityStep();
            int maxQuantity = 999 - (999 % step);
            int normalized = Math.Max(step, Math.Min(maxQuantity, quantity));
            int remainder = normalized % step;
            if (remainder != 0)
                normalized += step - remainder;

            return Math.Min(maxQuantity, normalized);
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
                if (!TryReadQuantityValue(orderQuantityControl, out chartTraderQuantity) || chartTraderQuantity <= 0)
                    return;

                if (ignoredChartTraderQuantityAfterPanelChange > 0)
                {
                    if (chartTraderQuantity == ignoredChartTraderQuantityAfterPanelChange)
                    {
                        lastKnownChartTraderQuantity = chartTraderQuantity;
                        return;
                    }

                    ignoredChartTraderQuantityAfterPanelChange = 0;
                }

                lastKnownChartTraderQuantity = chartTraderQuantity;

                if (pendingChartTraderQuantity > 0 && (now - lastPanelQuantityWrite).TotalMilliseconds < 3000)
                {
                    if (chartTraderQuantity == pendingChartTraderQuantity)
                    {
                        pendingChartTraderQuantity = 0;
                        pendingPreviousChartTraderQuantity = 0;
                    }
                    else if (chartTraderQuantity == pendingPreviousChartTraderQuantity)
                    {
                        return;
                    }
                    else if (chartTraderQuantity != Quantity)
                    {
                        pendingChartTraderQuantity = 0;
                        pendingPreviousChartTraderQuantity = 0;
                        Quantity = Math.Max(1, Math.Min(999, chartTraderQuantity));
                        chartControl.InvalidateVisual();
                    }

                    return;
                }

                if (chartTraderQuantity != Quantity)
                {
                    pendingChartTraderQuantity = 0;
                    pendingPreviousChartTraderQuantity = 0;
                    Quantity = Math.Max(1, Math.Min(999, chartTraderQuantity));
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

        private DependencyObject FindChartTraderAccountControl(Window window)
        {
            List<DependencyObject> elements = EnumerateVisualTree(window).ToList();

            foreach (DependencyObject label in elements)
            {
                string labelText = GetElementText(label);
                if (string.IsNullOrWhiteSpace(labelText) || !string.Equals(labelText.Trim(), "Account", StringComparison.OrdinalIgnoreCase))
                    continue;

                Rect labelBounds = GetElementBounds(label, window);
                DependencyObject bestCandidate = null;
                double bestScore = double.MaxValue;

                foreach (DependencyObject candidate in elements)
                {
                    if (!(candidate is ComboBox) && !HasReadableProperty(candidate, "SelectedItem") && !HasReadableProperty(candidate, "SelectedValue"))
                        continue;

                    Rect candidateBounds = GetElementBounds(candidate, window);
                    if (candidateBounds.IsEmpty)
                        continue;

                    bool belowLabel = candidateBounds.Top >= labelBounds.Bottom - 6 && candidateBounds.Top <= labelBounds.Bottom + 80;
                    bool horizontallyNear = candidateBounds.Right >= labelBounds.Left - 20 && candidateBounds.Left <= labelBounds.Right + 360;
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

        private bool HasReadableProperty(object target, string propertyName)
        {
            if (target == null)
                return false;

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property != null && property.CanRead;
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

        private string ReadAccountName(DependencyObject element)
        {
            if (element == null)
                return string.Empty;

            if (element is ComboBox comboBox)
            {
                string selected = ExtractAccountName(comboBox.SelectedItem);
                if (!string.IsNullOrWhiteSpace(selected))
                    return selected;

                selected = ExtractAccountName(comboBox.SelectedValue);
                if (!string.IsNullOrWhiteSpace(selected))
                    return selected;

                selected = comboBox.Text;
                if (!string.IsNullOrWhiteSpace(selected))
                    return selected.Trim();
            }

            object target = element;
            Type type = target.GetType();
            foreach (string propertyName in new[] { "SelectedItem", "SelectedValue", "Text", "Value" })
            {
                PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanRead)
                    continue;

                try
                {
                    string accountName = ExtractAccountName(property.GetValue(target, null));
                    if (!string.IsNullOrWhiteSpace(accountName))
                        return accountName;
                }
                catch
                {
                }
            }

            return string.Empty;
        }

        private string ExtractAccountName(object value)
        {
            if (value == null)
                return string.Empty;

            string name = GetPropertyValue(value, "Name")?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();

            name = GetPropertyValue(value, "DisplayName")?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();

            name = value.ToString();
            return string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
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
            target2OffsetFromEntry = target2Anchor.Price - entryAnchor.Price;
            target3OffsetFromEntry = target3Anchor.Price - entryAnchor.Price;
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

        private bool TryHandlePanelVisibilityButton(Point point)
        {
            if (!panelVisibilityButton.Contains(point))
                return false;

            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;
            suppressNextMouseUpSelection = true;
            isRiskPanelHidden = !isRiskPanelHidden;
            return true;
        }

        private bool TryHandleTargetLevelsButton(Point point)
        {
            if (!targetLevelsButton.Contains(point))
                return false;

            if (ShouldSuppressFastTargetLevelsClick())
                return true;

            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;
            suppressNextMouseUpSelection = true;

            double finalTargetPrice = GetStoredFinalTargetPrice();
            int currentLevels = GetEffectiveTargetLevels();
            TargetLevels = currentLevels >= 3 ? 1 : currentLevels + 1;
            DistributeTargetsToFinalTarget(finalTargetPrice, false);
            TargetMode = TargetLevels == 1 ? MpTargetMode.Single : MpTargetMode.Split;
            SetPanelQuantity(GetEffectiveTargetLevels());

            NormalizeTargetLevels();
            return true;
        }

        private void ToggleDirection()
        {
            DrawingState = DrawingState.Normal;
            IsSelected = false;
            editingAnchor = null;
            suppressNextMouseUpSelection = true;

            Direction = Direction == MrRexoFreePanelDirection.Long ? MrRexoFreePanelDirection.Short : MrRexoFreePanelDirection.Long;
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

            if (buyButton.Contains(point))
                SubmitPanelOrder(MrRexoFreePanelDirection.Long);
            else if (sellButton.Contains(point))
                SubmitPanelOrder(MrRexoFreePanelDirection.Short);
            else if (breakEvenButton.Contains(point))
                MoveStopsToBreakEven();
            else if (closePositionButton.Contains(point))
                CloseInstrumentPosition();

            return true;
        }

        private void MoveStopsToBreakEven()
        {
            if (ShouldSuppressFastTradeClick())
                return;

            Account account = ResolveOrderAccount();
            Instrument instrument = GetChartInstrument();
            if (account == null || instrument == null)
            {
                SetOrderStatus(account == null ? "Allowed account unavailable" : "Chart instrument not found");
                return;
            }

            Position position = GetOpenPositionOnInstrument(account, instrument);
            if (position == null)
            {
                SetOrderStatus("No open position for BE");
                return;
            }

            double breakEvenPrice = GetBreakEvenStopPrice(position);
            List<Order> stopOrders = GetActiveStopOrdersForPosition(account, instrument, position.MarketPosition);
            if (stopOrders.Count == 0)
            {
                SetOrderStatus("No active SL orders for BE");
                return;
            }

            try
            {
                foreach (Order order in stopOrders)
                    order.StopPriceChanged = breakEvenPrice;

                account.Change(stopOrders);
                SetOrderStatus($"BE set: {breakEvenPrice:F2} (+{BreakEvenBufferTicks}t)");
            }
            catch (Exception ex)
            {
                SetOrderStatus($"BE error: {ex.Message}");
            }
        }

        private void CloseInstrumentPosition()
        {
            if (ShouldSuppressFastTradeClick())
                return;

            Account account = ResolveOrderAccount();
            Instrument instrument = GetChartInstrument();
            if (account == null || instrument == null)
            {
                SetOrderStatus(account == null ? "Allowed account unavailable" : "Chart instrument not found");
                return;
            }

            try
            {
                account.Flatten(new[] { instrument });
                SetOrderStatus("Flatten submitted");
            }
            catch (Exception ex)
            {
                SetOrderStatus($"Close error: {ex.Message}");
            }
        }

        private double GetBreakEvenStopPrice(Position position)
        {
            if (position == null)
                return 0;

            double buffer = Math.Max(0, BreakEvenBufferTicks) * TickSize;
            double price = position.MarketPosition == MarketPosition.Long
                ? position.AveragePrice + buffer
                : position.AveragePrice - buffer;

            return RoundToTick(price);
        }

        private void SubmitPanelOrder(MrRexoFreePanelDirection side)
        {
            if (ShouldSuppressFastTradeClick())
                return;

            if (!EnableOrderSubmission)
            {
                SetOrderStatus("Order submission disabled");
                return;
            }

            if (HasPendingPanelEntryOrder())
            {
                SetOrderStatus("Entry order already pending");
                return;
            }

            if (!ValidateOrderGeometry(side))
                return;

            Account account = ResolveOrderAccount();
            if (account == null)
            {
                SetOrderStatus(string.IsNullOrWhiteSpace(OrderAccountName) ? "Chart Trader account unavailable" : $"Account not found: {OrderAccountName}");
                return;
            }

            Instrument instrument = GetChartInstrument();
            if (instrument == null)
            {
                SetOrderStatus("Chart instrument not found");
                return;
            }

            if (HasActivePanelEntryOrdersOnInstrument(account, instrument))
            {
                SetOrderStatus("Panel entry order already active");
                return;
            }

            double currentPrice = GetLiveOrderReferencePrice();
            if (currentPrice <= 0)
            {
                SetOrderStatus("No live market data for order");
                return;
            }

            EnsureOrderAccountSubscribed(account);

            OrderAction action = side == MrRexoFreePanelDirection.Long ? OrderAction.Buy : OrderAction.SellShort;
            OrderType orderType = GetEntryOrderType(side, currentPrice);
            double limitPrice = orderType == OrderType.Limit ? entryAnchor.Price : 0;
            double stopPrice = orderType == OrderType.StopMarket ? entryAnchor.Price : 0;
            string entryName = side == MrRexoFreePanelDirection.Long ? "MrRexo Long Entry" : "MrRexo Short Entry";

            try
            {
                Order entryOrder = account.CreateOrder(instrument, action, orderType, OrderEntry.Manual, TimeInForce.Day, Quantity, limitPrice, stopPrice, string.Empty, entryName, Core.Globals.MaxDate, null);
                PendingBracket bracket = new PendingBracket
                {
                    Direction = side,
                    StopPrice = stopAnchor.Price,
                    TargetPrices = GetActiveTargetAnchors().Select(anchor => anchor.Price).ToList(),
                    TargetQuantities = GetTargetQuantitySplit()
                };

                lock (orderLock)
                    pendingBrackets[entryOrder] = bracket;

                account.Submit(new[] { entryOrder });
                SetOrderStatus($"{entryName}: {orderType} x{Quantity}");
            }
            catch (Exception ex)
            {
                SetOrderStatus($"Order error: {ex.Message}");
            }
        }

        private bool ValidateOrderGeometry(MrRexoFreePanelDirection side)
        {
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null)
            {
                SetOrderStatus("Panel levels unavailable");
                return false;
            }

            bool valid = side == MrRexoFreePanelDirection.Long
                ? stopAnchor.Price < entryAnchor.Price && GetActiveTargetAnchors().All(anchor => anchor.Price > entryAnchor.Price)
                : stopAnchor.Price > entryAnchor.Price && GetActiveTargetAnchors().All(anchor => anchor.Price < entryAnchor.Price);

            if (!valid)
                SetOrderStatus(side == MrRexoFreePanelDirection.Long ? "BUY needs SL below OP and TP above OP" : "SELL needs SL above OP and TP below OP");

            return valid;
        }

        private OrderType GetEntryOrderType(MrRexoFreePanelDirection side, double currentPrice)
        {
            int cmp = ComparePrices(entryAnchor.Price, currentPrice);
            if (cmp == 0)
                return OrderType.Market;

            if (side == MrRexoFreePanelDirection.Long)
                return cmp > 0 ? OrderType.StopMarket : OrderType.Limit;

            return cmp < 0 ? OrderType.StopMarket : OrderType.Limit;
        }

        private bool HasPendingPanelEntryOrder()
        {
            lock (orderLock)
                return pendingBrackets.Count > 0;
        }

        private bool HasOpenPositionOnInstrument(Account account, Instrument instrument)
        {
            Position position = GetOpenPositionOnInstrument(account, instrument);
            return position != null;
        }

        private Position GetOpenPositionOnInstrument(Account account, Instrument instrument)
        {
            if (account == null || instrument == null)
                return null;

            try
            {
                lock (account.Positions)
                {
                    Position position = account.Positions.FirstOrDefault(item => IsSameInstrument(item.Instrument, instrument));
                    return position != null && position.MarketPosition != MarketPosition.Flat && position.Quantity > 0 ? position : null;
                }
            }
            catch
            {
                return null;
            }
        }

        private bool HasActivePanelEntryOrdersOnInstrument(Account account, Instrument instrument)
        {
            if (account == null || instrument == null)
                return false;

            try
            {
                lock (account.Orders)
                    return account.Orders.Any(order => IsSameInstrument(order.Instrument, instrument) && IsPanelEntryOrder(order) && IsActiveOrderState(order.OrderState));
            }
            catch
            {
                return false;
            }
        }

        private List<Order> GetActiveStopOrdersForPosition(Account account, Instrument instrument, MarketPosition marketPosition)
        {
            if (account == null || instrument == null || marketPosition == MarketPosition.Flat)
                return new List<Order>();

            try
            {
                lock (account.Orders)
                {
                    return account.Orders
                        .Where(order => IsSameInstrument(order.Instrument, instrument)
                            && IsActiveOrderState(order.OrderState)
                            && (order.OrderType == OrderType.StopMarket || order.OrderType == OrderType.StopLimit)
                            && IsProtectiveStopAction(order, marketPosition))
                        .ToList();
                }
            }
            catch
            {
                return new List<Order>();
            }
        }

        private bool IsProtectiveStopAction(Order order, MarketPosition marketPosition)
        {
            if (order == null)
                return false;

            if (marketPosition == MarketPosition.Long)
                return order.OrderAction == OrderAction.Sell;

            if (marketPosition == MarketPosition.Short)
                return order.OrderAction == OrderAction.BuyToCover;

            return false;
        }

        private bool IsPanelOrder(Order order)
        {
            return order != null
                && !string.IsNullOrWhiteSpace(order.Name)
                && order.Name.StartsWith("MrRexo", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPanelEntryOrder(Order order)
        {
            return IsPanelOrder(order)
                && (order.Name.IndexOf("Long Entry", StringComparison.OrdinalIgnoreCase) >= 0
                    || order.Name.IndexOf("Short Entry", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool IsActiveOrderState(OrderState orderState)
        {
            return orderState != OrderState.Filled
                && orderState != OrderState.Cancelled
                && orderState != OrderState.Rejected;
        }

        private bool IsSameInstrument(Instrument a, Instrument b)
        {
            if (a == null || b == null)
                return false;

            return string.Equals(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase);
        }

        private Account ResolveOrderAccount()
        {
            string accountName = string.IsNullOrWhiteSpace(OrderAccountName)
                ? GetChartTraderAccountName()
                : OrderAccountName.Trim();
            if (string.IsNullOrWhiteSpace(accountName))
                return null;

            lock (Account.All)
                return Account.All.FirstOrDefault(account => string.Equals(account.Name, accountName, StringComparison.OrdinalIgnoreCase));
        }

        private string GetChartTraderAccountName()
        {
            ChartControl chartControl = lastChartControl;
            if (chartControl == null)
                return string.Empty;

            try
            {
                Window window = Window.GetWindow(chartControl);
                if (window == null)
                    return string.Empty;

                DependencyObject accountControl = FindChartTraderAccountControl(window);
                return accountControl == null ? string.Empty : ReadAccountName(accountControl);
            }
            catch
            {
                return string.Empty;
            }
        }

        private Instrument GetChartInstrument()
        {
            ChartBars chartBars = GetAttachedToChartBars();
            return chartBars?.Bars?.Instrument;
        }

        private void EnsureOrderAccountSubscribed(Account account)
        {
            if (account == null || subscribedOrderAccount == account)
                return;

            UnsubscribeOrderAccount();
            subscribedOrderAccount = account;
            subscribedOrderAccount.OrderUpdate += OnAccountOrderUpdate;
        }

        private void UnsubscribeOrderAccount()
        {
            if (subscribedOrderAccount != null)
            {
                subscribedOrderAccount.OrderUpdate -= OnAccountOrderUpdate;
                subscribedOrderAccount = null;
            }

            lock (orderLock)
                pendingBrackets.Clear();
        }

        private void OnAccountOrderUpdate(object sender, OrderEventArgs e)
        {
            if (e == null || e.Order == null)
                return;

            PendingBracket bracket;
            lock (orderLock)
            {
                if (!pendingBrackets.TryGetValue(e.Order, out bracket))
                    return;

                if (e.OrderState != OrderState.Filled && e.OrderState != OrderState.Cancelled && e.OrderState != OrderState.Rejected)
                    return;

                pendingBrackets.Remove(e.Order);
            }

            if (e.OrderState == OrderState.Cancelled || e.OrderState == OrderState.Rejected)
            {
                SetOrderStatus($"Entry {e.OrderState}: {e.Order.Name}");
                return;
            }

            Account account = sender as Account ?? subscribedOrderAccount;
            if (account == null)
                return;

            SubmitProtectionOrders(account, e.Order, bracket);
        }

        private void SubmitProtectionOrders(Account account, Order filledEntry, PendingBracket bracket)
        {
            if (account == null || filledEntry == null || bracket == null)
                return;

            int filledQuantity = filledEntry.Filled > 0 ? filledEntry.Filled : filledEntry.Quantity;
            int[] targetQuantities = AllocateExitQuantities(filledQuantity, bracket.TargetQuantities);
            OrderAction exitAction = bracket.Direction == MrRexoFreePanelDirection.Long ? OrderAction.Sell : OrderAction.BuyToCover;
            List<Order> exitOrders = new List<Order>();

            try
            {
                for (int i = 0; i < bracket.TargetPrices.Count && i < targetQuantities.Length; i++)
                {
                    if (targetQuantities[i] <= 0)
                        continue;

                    string oco = Guid.NewGuid().ToString("N");
                    Order stopOrder = account.CreateOrder(filledEntry.Instrument, exitAction, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, targetQuantities[i], 0, bracket.StopPrice, oco, $"MrRexo SL{i + 1}", Core.Globals.MaxDate, null);
                    Order targetOrder = account.CreateOrder(filledEntry.Instrument, exitAction, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, targetQuantities[i], bracket.TargetPrices[i], 0, oco, $"MrRexo TP{i + 1}", Core.Globals.MaxDate, null);
                    exitOrders.Add(stopOrder);
                    exitOrders.Add(targetOrder);
                }

                if (exitOrders.Count > 0)
                {
                    account.Submit(exitOrders);
                    SetOrderStatus($"Bracket submitted: {exitOrders.Count / 2} OCO pairs");
                }
            }
            catch (Exception ex)
            {
                SetOrderStatus($"Bracket error: {ex.Message}");
            }
        }

        private int[] AllocateExitQuantities(int filledQuantity, int[] preferredSplit)
        {
            if (preferredSplit == null || preferredSplit.Length == 0)
                return new[] { Math.Max(1, filledQuantity) };

            int[] allocated = new int[preferredSplit.Length];
            int remaining = Math.Max(0, filledQuantity);
            for (int i = 0; i < preferredSplit.Length && remaining > 0; i++)
            {
                int quantity = Math.Min(Math.Max(0, preferredSplit[i]), remaining);
                allocated[i] = quantity;
                remaining -= quantity;
            }

            for (int i = 0; i < allocated.Length && remaining > 0; i++)
            {
                allocated[i]++;
                remaining--;
            }

            return allocated;
        }

        private void SetOrderStatus(string message)
        {
            orderStatusMessage = message ?? string.Empty;
            orderStatusMessageTime = DateTime.UtcNow;

            ChartControl chartControl = lastChartControl;
            chartControl?.Dispatcher?.BeginInvoke(new Action(() => chartControl.InvalidateVisual()));
        }

        private void FlipStopAndTargetAroundEntry()
        {
            if (entryAnchor == null || stopAnchor == null || targetAnchor == null)
                return;

            double stopDistance = Math.Abs(stopAnchor.Price - entryAnchor.Price);
            List<ChartAnchor> targets = GetAllTargetAnchors();
            List<double> targetDistances = targets.Select(anchor => Math.Abs(anchor.Price - entryAnchor.Price)).ToList();
            if (Math.Abs(finalTargetOffsetFromEntry) < TickSize)
                finalTargetOffsetFromEntry = GetCurrentFinalTargetPrice() - entryAnchor.Price;

            if (Direction == MrRexoFreePanelDirection.Long)
            {
                stopAnchor.Price = RoundToTick(entryAnchor.Price - stopDistance);
                for (int i = 0; i < targetDistances.Count; i++)
                    targets[i].Price = RoundToTick(entryAnchor.Price + targetDistances[i]);
                finalTargetOffsetFromEntry = Math.Abs(finalTargetOffsetFromEntry);
            }
            else
            {
                stopAnchor.Price = RoundToTick(entryAnchor.Price + stopDistance);
                for (int i = 0; i < targetDistances.Count; i++)
                    targets[i].Price = RoundToTick(entryAnchor.Price - targetDistances[i]);
                finalTargetOffsetFromEntry = -Math.Abs(finalTargetOffsetFromEntry);
            }

            UpdateOffsetsFromAnchors();
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
            return buyButton.Contains(point)
                || sellButton.Contains(point)
                || breakEvenButton.Contains(point)
                || closePositionButton.Contains(point);
        }

        private bool IsPointInToolbarButtons(Point point)
        {
            return panelVisibilityButton.Contains(point);
        }

        private string GetBuyButtonLabel()
        {
            string buy = T("Buy");
            double currentPrice = GetCurrentPrice();
            if (currentPrice <= 0 || entryAnchor == null)
                return buy;

            int cmp = ComparePrices(entryAnchor.Price, currentPrice);
            if (cmp == 0)
                return $"{buy} MKT";

            return cmp > 0 ? $"{buy} STP" : $"{buy} LMT";
        }

        private string GetSellButtonLabel()
        {
            string sell = T("Sell");
            double currentPrice = GetCurrentPrice();
            if (currentPrice <= 0 || entryAnchor == null)
                return sell;

            int cmp = ComparePrices(entryAnchor.Price, currentPrice);
            if (cmp == 0)
                return $"{sell} MKT";

            return cmp < 0 ? $"{sell} STP" : $"{sell} LMT";
        }

        private string T(string key)
        {
            switch (PanelLanguage)
            {
                case MrRexoFreePanelLanguage.EN:
                    switch (key)
                    {
                        case "Buy": return "BUY";
                        case "Sell": return "SELL";
                        case "TotalProfit": return "Total profit";
                        case "Loss": return "Loss";
                        case "Risk": return "Risk";
                        case "Ticks": return "ticks";
                        case "IK": return "QTY";
                    }
                    break;
                case MrRexoFreePanelLanguage.DE:
                    switch (key)
                    {
                        case "Buy": return "KAUF";
                        case "Sell": return "VERK";
                        case "TotalProfit": return "Gewinn gesamt";
                        case "Loss": return "Verlust";
                        case "Risk": return "Risiko";
                        case "Ticks": return "Ticks";
                        case "IK": return "KTR";
                    }
                    break;
                default:
                    switch (key)
                    {
                        case "Buy": return "KUP";
                        case "Sell": return "SPRZ";
                        case "TotalProfit": return "Zysk lacznie";
                        case "Loss": return "Strata";
                        case "Risk": return "Ryzyko";
                        case "Ticks": return "tickow";
                        case "IK": return "IK";
                    }
                    break;
            }

            return key;
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

        private bool ShouldSuppressFastTargetLevelsClick()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - lastTargetLevelsButtonClick).TotalMilliseconds < 200)
                return true;

            lastTargetLevelsButtonClick = now;
            return false;
        }

        private bool ShouldSuppressFastTradeClick()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - lastTradeButtonClick).TotalMilliseconds < 1200)
            {
                SetOrderStatus("Trade click ignored");
                return true;
            }

            lastTradeButtonClick = now;
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
            DistributeTargetsToFinalTarget(GetStoredFinalTargetPrice(), false);
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
                target2OffsetFromEntry = target2Anchor.Price - entryAnchor.Price;
                target3OffsetFromEntry = target3Anchor.Price - entryAnchor.Price;
                offsetsInitialized = true;
            }

            double currentPrice = RoundToTick(chartBars.Bars.LastPrice);
            if (currentPrice <= 0)
                currentPrice = RoundToTick(chartBars.Bars.GetClose(chartBars.Bars.Count - 1));

            return currentPrice;
        }

        private double GetLiveOrderReferencePrice()
        {
            ChartBars chartBars = GetAttachedToChartBars();
            if (chartBars == null || chartBars.Bars == null)
                return 0;

            double lastPrice = chartBars.Bars.LastPrice;
            if (lastPrice <= 0 || double.IsNaN(lastPrice) || double.IsInfinity(lastPrice))
                return 0;

            return RoundToTick(lastPrice);
        }
    }
}
