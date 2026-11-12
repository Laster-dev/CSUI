// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//  Synopsis: Implements Geometry and PathFigureCollection mini-language parsers (zero reflection).

using System.Windows;
using System.Windows.Media;
using SR = MS.Internal.PresentationCore.SR;

#nullable disable

namespace MS.Internal
{
    internal static partial class Parsers
    {
        /// <summary>
        /// Parse a PathGeometry string.
        /// The PathGeometry syntax is the same as the PathFigureCollection syntax except that it
        /// may start with a "wsp*Fwsp*(0|1)" which indicate the winding mode (F0 is EvenOdd while
        /// F1 is NonZero).
        /// </summary>
        internal static Geometry ParseGeometry(
            string pathString,
            IFormatProvider formatProvider)
        {
            FillRule fillRule = FillRule.EvenOdd;
            StreamGeometry geometry = new StreamGeometry();
            StreamGeometryContext context = geometry.Open();

            ParseStringToStreamGeometryContext(context, pathString, formatProvider, ref fillRule);

            geometry.FillRule = fillRule;
            geometry.Freeze();

            return geometry;
        }

        /// <summary>
        /// Parse a PathFigureCollection string.
        /// </summary>
        internal static PathFigureCollection ParsePathFigureCollection(
            string pathString,
            IFormatProvider formatProvider)
        {
            PathStreamGeometryContext context = new PathStreamGeometryContext();

            AbbreviatedGeometryParser parser = new AbbreviatedGeometryParser();

            parser.ParseToGeometryContext(context, pathString, startIndex: 0);

            PathGeometry pathGeometry = context.GetPathGeometry();

            return pathGeometry.Figures;
        }

        private static void ParseStringToStreamGeometryContext(
            StreamGeometryContext context,
            string pathString,
            IFormatProvider formatProvider,
            ref FillRule fillRule)
        {
            using (context)
            {
                if (pathString != null)
                {
                    int curIndex = 0;

                    while ((curIndex < pathString.Length) && char.IsWhiteSpace(pathString, curIndex))
                    {
                        curIndex++;
                    }

                    if (curIndex < pathString.Length)
                    {
                        if (pathString[curIndex] == 'F')
                        {
                            curIndex++;

                            while ((curIndex < pathString.Length) && char.IsWhiteSpace(pathString, curIndex))
                            {
                                curIndex++;
                            }

                            if ((curIndex == pathString.Length) ||
                                ((pathString[curIndex] != '0') &&
                                 (pathString[curIndex] != '1')))
                            {
                                throw new FormatException(SR.Parsers_IllegalToken);
                            }

                            fillRule = pathString[curIndex] == '0' ? FillRule.EvenOdd : FillRule.Nonzero;

                            curIndex++;
                        }
                    }

                    AbbreviatedGeometryParser parser = new AbbreviatedGeometryParser();
                    parser.ParseToGeometryContext(context, pathString, curIndex);
                }
            }
        }
    }

    /// <summary>
    /// Parser for XAML abbreviated geometry.
    /// SVG path spec is closely followed http://www.w3.org/TR/SVG11/paths.html
    /// </summary>
    internal sealed class AbbreviatedGeometryParser
    {
        private const bool AllowSign = true;
        private const bool AllowComma = true;
        private const bool IsFilled = true;
        private const bool IsClosed = true;
        private const bool IsStroked = true;
        private const bool IsSmoothJoin = true;

        private IFormatProvider _formatProvider;

        private string _pathString;
        private int _pathLength;
        private int _curIndex;
        private bool _figureStarted;

        private Point _lastStart;
        private Point _lastPoint;
        private Point _secondLastPoint;

        private char _token;

        private StreamGeometryContext _context;

        private void ThrowBadToken()
        {
            throw new FormatException(SR.Format(SR.Parser_UnexpectedToken, _pathString, _curIndex - 1));
        }

        private bool More()
        {
            return _curIndex < _pathLength;
        }

        private bool SkipWhiteSpace(bool allowComma)
        {
            bool commaMet = false;

            while (More())
            {
                char ch = _pathString[_curIndex];

                switch (ch)
                {
                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        break;

                    case ',':
                        if (allowComma)
                        {
                            commaMet = true;
                            allowComma = false;
                        }
                        else
                        {
                            ThrowBadToken();
                        }
                        break;

                    default:
                        if (((ch > ' ') && (ch <= 'z')) || !char.IsWhiteSpace(ch))
                        {
                            return commaMet;
                        }
                        break;
                }

                _curIndex++;
            }

            return commaMet;
        }

        private bool ReadToken()
        {
            SkipWhiteSpace(!AllowComma);

            if (More())
            {
                _token = _pathString[_curIndex++];
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsNumber(bool allowComma)
        {
            bool commaMet = SkipWhiteSpace(allowComma);

            if (More())
            {
                _token = _pathString[_curIndex];

                if ((_token == '.') || (_token == '-') || (_token == '+') || ((_token >= '0') && (_token <= '9'))
                    || (_token == 'I') || (_token == 'N'))
                {
                    return true;
                }
            }

            if (commaMet)
            {
                ThrowBadToken();
            }

            return false;
        }

        private void SkipDigits(bool signAllowed)
        {
            if (signAllowed && More() && ((_pathString[_curIndex] == '-') || _pathString[_curIndex] == '+'))
            {
                _curIndex++;
            }

            while (More() && (_pathString[_curIndex] >= '0') && (_pathString[_curIndex] <= '9'))
            {
                _curIndex++;
            }
        }

        private double ReadNumber(bool allowComma)
        {
            if (!IsNumber(allowComma))
            {
                ThrowBadToken();
            }

            bool simple = true;
            int start = _curIndex;

            if (More() && ((_pathString[_curIndex] == '-') || _pathString[_curIndex] == '+'))
            {
                _curIndex++;
            }

            if (More() && (_pathString[_curIndex] == 'I'))
            {
                _curIndex = Math.Min(_curIndex + 8, _pathLength);
                simple = false;
            }
            else if (More() && (_pathString[_curIndex] == 'N'))
            {
                _curIndex = Math.Min(_curIndex + 3, _pathLength);
                simple = false;
            }
            else
            {
                SkipDigits(!AllowSign);

                if (More() && (_pathString[_curIndex] == '.'))
                {
                    simple = false;
                    _curIndex++;
                    SkipDigits(!AllowSign);
                }

                if (More() && ((_pathString[_curIndex] == 'E') || (_pathString[_curIndex] == 'e')))
                {
                    simple = false;
                    _curIndex++;
                    SkipDigits(AllowSign);
                }
            }

            if (simple && (_curIndex <= (start + 8)))
            {
                int sign = 1;

                if (_pathString[start] == '+')
                {
                    start++;
                }
                else if (_pathString[start] == '-')
                {
                    start++;
                    sign = -1;
                }

                int value = 0;

                while (start < _curIndex)
                {
                    value = value * 10 + (_pathString[start] - '0');
                    start++;
                }

                return value * sign;
            }
            else
            {
                string subString = _pathString.Substring(start, _curIndex - start);

                try
                {
                    return Convert.ToDouble(subString, _formatProvider);
                }
                catch (FormatException except)
                {
                    throw new FormatException(SR.Format(SR.Parser_UnexpectedToken, _pathString, start), except);
                }
            }
        }

        private bool ReadBool()
        {
            SkipWhiteSpace(AllowComma);

            if (More())
            {
                _token = _pathString[_curIndex++];

                if (_token == '0')
                {
                    return false;
                }
                else if (_token == '1')
                {
                    return true;
                }
            }

            ThrowBadToken();
            return false;
        }

        private Point ReadPoint(char cmd, bool allowcomma)
        {
            double x = ReadNumber(allowcomma);
            double y = ReadNumber(AllowComma);

            if (cmd >= 'a')
            {
                x += _lastPoint.X;
                y += _lastPoint.Y;
            }

            return new Point(x, y);
        }

        private Point Reflect()
        {
            return new Point(2 * _lastPoint.X - _secondLastPoint.X,
                             2 * _lastPoint.Y - _secondLastPoint.Y);
        }

        private void EnsureFigure()
        {
            if (!_figureStarted)
            {
                _context.BeginFigure(_lastStart, IsFilled, !IsClosed);
                _figureStarted = true;
            }
        }

        internal void ParseToGeometryContext(
            StreamGeometryContext context,
            string pathString,
            int startIndex)
        {
            _formatProvider = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;

            _context = context;
            _pathString = pathString;
            _pathLength = pathString.Length;
            _curIndex = startIndex;

            _secondLastPoint = new Point(0, 0);
            _lastPoint = new Point(0, 0);
            _lastStart = new Point(0, 0);

            _figureStarted = false;

            bool first = true;
            char last_cmd = ' ';

            while (ReadToken())
            {
                char cmd = _token;

                if (first)
                {
                    if ((cmd != 'M') && (cmd != 'm'))
                    {
                        ThrowBadToken();
                    }

                    first = false;
                }

                switch (cmd)
                {
                    case 'm':
                    case 'M':
                        _lastPoint = ReadPoint(cmd, !AllowComma);

                        context.BeginFigure(_lastPoint, IsFilled, !IsClosed);
                        _figureStarted = true;
                        _lastStart = _lastPoint;
                        last_cmd = 'M';

                        while (IsNumber(AllowComma))
                        {
                            _lastPoint = ReadPoint(cmd, !AllowComma);

                            context.LineTo(_lastPoint, IsStroked, !IsSmoothJoin);
                            last_cmd = 'L';
                        }
                        break;

                    case 'l':
                    case 'L':
                    case 'h':
                    case 'H':
                    case 'v':
                    case 'V':
                        EnsureFigure();

                        do
                        {
                            switch (cmd)
                            {
                                case 'l': _lastPoint = ReadPoint(cmd, !AllowComma); break;
                                case 'L': _lastPoint = ReadPoint(cmd, !AllowComma); break;
                                case 'h': _lastPoint.X += ReadNumber(!AllowComma); break;
                                case 'H': _lastPoint.X = ReadNumber(!AllowComma); break;
                                case 'v': _lastPoint.Y += ReadNumber(!AllowComma); break;
                                case 'V': _lastPoint.Y = ReadNumber(!AllowComma); break;
                            }

                            context.LineTo(_lastPoint, IsStroked, !IsSmoothJoin);
                        }
                        while (IsNumber(AllowComma));

                        last_cmd = 'L';
                        break;

                    case 'c':
                    case 'C':
                    case 's':
                    case 'S':
                        EnsureFigure();

                        do
                        {
                            Point p;

                            if ((cmd == 's') || (cmd == 'S'))
                            {
                                if (last_cmd == 'C')
                                {
                                    p = Reflect();
                                }
                                else
                                {
                                    p = _lastPoint;
                                }

                                _secondLastPoint = ReadPoint(cmd, !AllowComma);
                            }
                            else
                            {
                                p = ReadPoint(cmd, !AllowComma);
                                _secondLastPoint = ReadPoint(cmd, AllowComma);
                            }

                            _lastPoint = ReadPoint(cmd, AllowComma);

                            context.BezierTo(p, _secondLastPoint, _lastPoint, IsStroked, !IsSmoothJoin);

                            last_cmd = 'C';
                        }
                        while (IsNumber(AllowComma));
                        break;

                    case 'q':
                    case 'Q':
                    case 't':
                    case 'T':
                        EnsureFigure();

                        do
                        {
                            if ((cmd == 't') || (cmd == 'T'))
                            {
                                if (last_cmd == 'Q')
                                {
                                    _secondLastPoint = Reflect();
                                }
                                else
                                {
                                    _secondLastPoint = _lastPoint;
                                }

                                _lastPoint = ReadPoint(cmd, !AllowComma);
                            }
                            else
                            {
                                _secondLastPoint = ReadPoint(cmd, !AllowComma);
                                _lastPoint = ReadPoint(cmd, AllowComma);
                            }

                            context.QuadraticBezierTo(_secondLastPoint, _lastPoint, IsStroked, !IsSmoothJoin);

                            last_cmd = 'Q';
                        }
                        while (IsNumber(AllowComma));
                        break;

                    case 'a':
                    case 'A':
                        EnsureFigure();

                        do
                        {
                            double w = ReadNumber(!AllowComma);
                            double h = ReadNumber(AllowComma);
                            double rotation = ReadNumber(AllowComma);
                            bool large = ReadBool();
                            bool sweep = ReadBool();

                            _lastPoint = ReadPoint(cmd, AllowComma);

                            context.ArcTo(
                                _lastPoint,
                                new Size(w, h),
                                rotation,
                                large,
                                sweep ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                                IsStroked,
                                !IsSmoothJoin);
                        }
                        while (IsNumber(AllowComma));

                        last_cmd = 'A';
                        break;

                    case 'z':
                    case 'Z':
                        EnsureFigure();
                        context.SetClosedState(IsClosed);

                        _figureStarted = false;
                        last_cmd = 'Z';

                        _lastPoint = _lastStart;
                        break;

                    default:
                        ThrowBadToken();
                        break;
                }
            }
        }
    }
}
