﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Slide : MovableLongNoteBase
    {
        private static readonly Color BackgroundMiddleColor = Color.FromArgb(196, 0, 164, 146);
        private static readonly Color BackgroundEdgeColor = Color.FromArgb(196, 166, 44, 168);
        private static readonly Color BackgroundLineColor = Color.FromArgb(196, 0, 214, 192);


        [Newtonsoft.Json.JsonProperty]
        private int startWidth = 1;
        [Newtonsoft.Json.JsonProperty]
        private int startLaneIndex;
        [Newtonsoft.Json.JsonProperty]
        private List<StepTap> stepNotes = new List<StepTap>();

        /// <summary>
        /// 開始ノートの配置されるレーン番号を設定します。。
        /// </summary>
        public int StartLaneIndex
        {
            get { return startLaneIndex; }
            set
            {
                CheckPosition(value, startWidth);
                startLaneIndex = value;
            }
        }

        /// <summary>
        /// 開始ノートのレーン幅を設定します。
        /// </summary>
        public int StartWidth
        {
            get { return startWidth; }
            set
            {
                CheckPosition(startLaneIndex, value);
                startWidth = value;
            }
        }

        public List<StepTap> StepNotes { get { return stepNotes; } }
        public StartTap StartNote { get; }

        public Slide()
        {
            StartNote = new StartTap(this);
        }

        protected void CheckPosition(int startLaneIndex, int startWidth)
        {
            int maxRightOffset = Math.Max(0, StepNotes.Count == 0 ? 0 : StepNotes.Max(p => p.LaneIndexOffset + p.WidthChange));
            if (startWidth < Math.Abs(Math.Min(0, StepNotes.Count == 0 ? 0 : StepNotes.Min(p => p.WidthChange))) + 1 || startLaneIndex + startWidth + maxRightOffset > Constants.LanesCount)
                throw new ArgumentOutOfRangeException("startWidth", "Invalid note width.");

            if (StepNotes.Any(p =>
            {
                int laneIndex = startLaneIndex + p.LaneIndexOffset;
                return laneIndex < 0 || laneIndex + (startWidth + p.WidthChange) > Constants.LanesCount;
            })) throw new ArgumentOutOfRangeException("startLaneIndex", "Invalid lane index.");
            if (startLaneIndex < 0 || startLaneIndex + startWidth > Constants.LanesCount)
                throw new ArgumentOutOfRangeException("startLaneIndex", "Invalid lane index.");
        }

        public void SetPosition(int startLaneIndex, int startWidth)
        {
            CheckPosition(startLaneIndex, startWidth);
            this.startLaneIndex = startLaneIndex;
            this.startWidth = startWidth;
        }

        /// <summary>
        /// このスライドを反転します。
        /// </summary>
        public void Flip()
        {
            startLaneIndex = Constants.LanesCount - startLaneIndex - startWidth;
            foreach (var step in StepNotes)
            {
                step.LaneIndexOffset = -step.LaneIndexOffset - step.WidthChange;
            }
        }

        /// <summary>
        /// SLIDEの背景を描画します。
        /// </summary>
        /// <param name="g">描画先Graphics</param>
        /// <param name="width1">ノートの描画幅</param>
        /// <param name="x1">開始ノートの左端位置</param>
        /// <param name="y1">開始ノートのY座標</param>
        /// <param name="x2">終了ノートの左端位置</param>
        /// <param name="y2">終了ノートのY座標</param>
        /// <param name="gradStartY">始点Step以前の中継点のY座標(グラデーション描画用)</param>
        /// <param name="gradEndY">終点Step以後の中継点のY座標(グラデーション描画用)</param>
        /// <param name="noteHeight">ノートの描画高さ</param>
        internal void DrawBackground(Graphics g, float width1, float width2, float x1, float y1, float x2, float y2, float gradStartY, float gradEndY, float noteHeight)
        {
            var prevMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x1 - x2) + width1, Math.Abs(y1 - y2));
            var gradientRect = new RectangleF(rect.Left, gradStartY, rect.Width, gradEndY - gradStartY);
            using (var brush = new LinearGradientBrush(gradientRect, BackgroundEdgeColor, BackgroundMiddleColor, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend(4)
                {
                    Colors = new Color[] { BackgroundEdgeColor, BackgroundMiddleColor, BackgroundMiddleColor, BackgroundEdgeColor },
                    Positions = new float[] { 0.0f, 0.3f, 0.7f, 1.0f }
                };
                brush.InterpolationColors = blend;
                using (var path = GetBackgroundPath(width1, width2, x1, y1, x2, y2))
                {
                    g.FillPath(brush, path);
                }
            }
            using (var pen = new Pen(BackgroundLineColor, noteHeight * 0.4f))
            {
                g.DrawLine(pen, x1 + width1 / 2, y1, x2 + width2 / 2, y2);
            }
            g.SmoothingMode = prevMode;
        }

        internal GraphicsPath GetBackgroundPath(float width1, float width2, float x1, float y1, float x2, float y2)
        {
            var path = new GraphicsPath();
            path.AddPolygon(new PointF[]
            {
                new PointF(x1, y1),
                new PointF(x1 + width1, y1),
                new PointF(x2 + width2, y2),
                new PointF(x2, y2)
            });
            return path;
        }

        public override int GetDuration()
        {
            return StepNotes.Max(p => p.TickOffset);
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public abstract class TapBase : LongNoteTapBase
        {
            private readonly Color DarkNoteColor = Color.FromArgb(0, 16, 138);
            private readonly Color LightNoteColor = Color.FromArgb(86, 106, 255);

            [Newtonsoft.Json.JsonProperty]
            private Slide parentNote;

            public Slide ParentNote { get { return parentNote; } }

            public TapBase(Slide parent)
            {
                parentNote = parent;
            }

            protected override void DrawNote(Graphics g, RectangleF rect)
            {
                DrawNote(g, rect, DarkNoteColor, LightNoteColor);
            }
        }

        public class StartTap : TapBase
        {
            public override bool IsTap { get { return true; } }

            public override int Tick { get { return ParentNote.StartTick; } }

            public override int LaneIndex { get { return ParentNote.StartLaneIndex; } }

            public override int Width { get { return ParentNote.StartWidth; } }

            public StartTap(Slide parent) : base(parent)
            {
            }
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public class StepTap : TapBase
        {
            [Newtonsoft.Json.JsonProperty]
            private int laneIndexOffset;
            [Newtonsoft.Json.JsonProperty]
            private int widthChange;
            [Newtonsoft.Json.JsonProperty]
            private int tickOffset = 1;
            [Newtonsoft.Json.JsonProperty]
            private bool isVisible = true;

            public int TickOffset
            {
                get { return tickOffset; }
                set
                {
                    if (value <= 0) throw new ArgumentOutOfRangeException("value", "value must be positive.");
                    tickOffset = value;
                }
            }


            public bool IsVisible
            {
                get { return isVisible; }
                set { isVisible = value; }
            }

            public override bool IsTap { get { return false; } }

            public override int Tick { get { return ParentNote.StartTick + TickOffset; } }

            public override int LaneIndex { get { return ParentNote.StartLaneIndex + LaneIndexOffset; } }

            public int LaneIndexOffset
            {
                get { return laneIndexOffset; }
                set
                {
                    CheckPosition(value, widthChange);
                    laneIndexOffset = value;
                }
            }

            public int WidthChange
            {
                get { return widthChange; }
                set
                {
                    CheckPosition(laneIndexOffset, value);
                    widthChange = value;
                }
            }

            public override int Width { get { return ParentNote.StartWidth + WidthChange; } }

            public StepTap(Slide parent) : base(parent)
            {
            }

            public void SetPosition(int laneIndexOffset, int widthChange)
            {
                CheckPosition(laneIndexOffset, widthChange);
                this.laneIndexOffset = laneIndexOffset;
                this.widthChange = widthChange;
            }

            protected void CheckPosition(int laneIndexOffset, int widthChange)
            {
                int laneIndex = ParentNote.StartNote.LaneIndex + laneIndexOffset;
                if (laneIndex < 0 || laneIndex + (ParentNote.StartWidth + widthChange) > Constants.LanesCount)
                    throw new ArgumentOutOfRangeException("laneIndexOffset", "Invalid lane index offset.");

                int actualWidth = widthChange + ParentNote.StartWidth;
                if (actualWidth < 1 || laneIndex + actualWidth > Constants.LanesCount)
                    throw new ArgumentOutOfRangeException("widthChange", "Invalid width change value.");
            }

            protected override void DrawNote(Graphics g, RectangleF rect)
            {
                if (!IsVisible) return;
                base.DrawNote(g, rect);
            }
        }
    }
}
