﻿using System;
using System.Drawing;
//using FastColoredTextBoxNS;
using Microsoft.CodeAnalysis;

namespace Spg.LocationRefactor.TextRegion
{
    public class TRegion
    {
        /// <summary>
        /// Text
        /// </summary>
        public String Text { get; set; }

        /// <summary>
        /// Start position
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Region length
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Parent region
        /// </summary>
        public TRegion Parent { get; set; }

        /// <summary>
        /// Syntax node
        /// </summary>
        public SyntaxNode Node { get; set; }

        /// <summary>
        /// Region color
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Region range
        /// </summary>
       // public Range Range { get; internal set; }


        /// <summary>
        /// Evaluate region
        /// </summary>
        /// <param name="region">Region</param>
        /// <returns>Evaluation</returns>
        public Boolean IsParent(TRegion region) {
            String text = System.Text.RegularExpressions.Regex.Escape(this.Text);
            Boolean contains = System.Text.RegularExpressions.Regex.IsMatch(region.Text, text);
            Boolean parent = contains && region.Color != this.Color;
            return parent;
        }

       /* public override string ToString()
        {
            if (Text != null)
            {
                return Text;
            }

            return " ";
        }*/
    }
}
