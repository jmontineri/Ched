﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components.Notes
{
    public class Tap : TappableBase
    {
        private static readonly Color DarkNoteColor = Color.FromArgb(138, 0, 0);
        private static readonly Color LightNoteColor = Color.FromArgb(255, 128, 128);

        protected override void DrawNote(Graphics g, RectangleF rect)
        {
            DrawNote(g, rect, DarkNoteColor, LightNoteColor);
            DrawTapSymbol(g, rect);
        }
    }
}
