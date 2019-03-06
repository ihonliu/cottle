﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using Cottle.Exceptions;

namespace Cottle.Parsers.Default
{
    internal class Lexer
    {
        #region Constructors

        public Lexer(string blockBegin, string blockContinue, string blockEnd, char escape)
        {
            var graph = new LexerGraph();

            if (!graph.Register(blockBegin, LexemType.BlockBegin))
                throw new ConfigException("blockBegin", blockBegin, "block delimiter used twice");

            if (!graph.Register(blockContinue, LexemType.BlockContinue))
                throw new ConfigException("blockContinue", blockContinue, "block delimiter used twice");

            if (!graph.Register(blockEnd, LexemType.BlockEnd))
                throw new ConfigException("blockEnd", blockEnd, "block delimiter used twice");

            graph.BuildFallbacks();

            _escape = escape;
            _pending = null;
            _root = graph.Root;
        }

        #endregion

        #region Properties

        public int Column { get; private set; }

        public Lexem Current { get; private set; }

        public int Line { get; private set; }

        #endregion

        #region Attributes

        // A buffer containing more than 85000 bytes will be allocated on LOH
        private const int MaxBufferSize = 84000 / sizeof(char);

        private bool _eof;

        private readonly char _escape;

        private char _last;

        private char? _next;

        private Lexem? _pending;

        private TextReader _reader;

        private readonly LexerNode _root;

        #endregion

        #region Methods / Public

        public void Next(LexerMode mode)
        {
            switch (mode)
            {
                case LexerMode.Block:
                    Current = NextBlock();

                    break;

                case LexerMode.Raw:
                    Current = NextRaw();

                    break;

                default:
                    throw new ParseException(Column, Line, "<?>", "block or raw text");
            }
        }

        public bool Reset(TextReader reader)
        {
            Column = 1;
            Current = new Lexem();
            Line = 1;

            _eof = false;
            _last = '\0';
            _next = null;
            _pending = null;
            _reader = reader;

            return Read();
        }

        #endregion

        #region Methods / Private

        private Lexem NextBlock()
        {
            while (true)
            {
                if (_eof)
                    return new Lexem(LexemType.EndOfFile, "<EOF>");

                switch (_last)
                {
                    case '\n':
                    case '\r':
                    case '\t':
                    case ' ':
                        while (_last <= ' ' && Read())
                        {
                        }

                        break;

                    case '!':
                        if (Read() && _last == '=')
                            return NextChar(LexemType.NotEqual);

                        return new Lexem(LexemType.Bang, string.Empty);

                    case '%':
                        return NextChar(LexemType.Percent);

                    case '&':
                        if (Read() && _last == '&')
                            return NextChar(LexemType.DoubleAmpersand);

                        _next = _last;
                        _last = '&';

                        return new Lexem(LexemType.None, _last.ToString());

                    case '(':
                        return NextChar(LexemType.ParenthesisBegin);

                    case ')':
                        return NextChar(LexemType.ParenthesisEnd);

                    case '*':
                        return NextChar(LexemType.Star);

                    case '+':
                        return NextChar(LexemType.Plus);

                    case ',':
                        return NextChar(LexemType.Comma);

                    case '-':
                        return NextChar(LexemType.Minus);

                    case '.':
                        return NextChar(LexemType.Dot);

                    case '/':
                        return NextChar(LexemType.Slash);

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        var numberBuffer = new StringBuilder();
                        var dot = false;

                        do
                        {
                            dot |= _last == '.';

                            numberBuffer.Append(_last);
                        } while (Read() && (_last >= '0' && _last <= '9' ||
                                            _last == '.' && !dot));

                        return new Lexem(LexemType.Number, numberBuffer.ToString());

                    case ':':
                        return NextChar(LexemType.Colon);

                    case '<':
                        if (Read() && _last == '=')
                            return NextChar(LexemType.LowerEqual);

                        return new Lexem(LexemType.LowerThan, string.Empty);

                    case '=':
                        return NextChar(LexemType.Equal);

                    case '>':
                        if (Read() && _last == '=')
                            return NextChar(LexemType.GreaterEqual);

                        return new Lexem(LexemType.GreaterThan, string.Empty);

                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case '_':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                        var symbolBuffer = new StringBuilder();

                        do
                        {
                            symbolBuffer.Append(_last);
                        } while (Read() && (_last >= '0' && _last <= '9' ||
                                            _last >= 'A' && _last <= 'Z' ||
                                            _last >= 'a' && _last <= 'z' ||
                                            _last == '_'));

                        return new Lexem(LexemType.Symbol, symbolBuffer.ToString());

                    case '|':
                        if (Read() && _last == '|')
                            return NextChar(LexemType.DoublePipe);

                        _next = _last;
                        _last = '|';

                        return new Lexem(LexemType.None, _last.ToString());

                    case '[':
                        return NextChar(LexemType.BracketBegin);

                    case ']':
                        return NextChar(LexemType.BracketEnd);

                    case '\'':
                    case '"':
                        var stringBuffer = new StringBuilder();
                        var end = _last;

                        while (Read() && _last != end)
                            if (_last != _escape || Read())
                                stringBuffer.Append(_last);

                        if (_eof)
                            throw new ParseException(Column, Line, "<eof>", "end of string");

                        Read();

                        return new Lexem(LexemType.String, stringBuffer.ToString());

                    default:
                        return new Lexem(LexemType.None, _last.ToString());
                }
            }
        }

        private Lexem NextChar(LexemType type)
        {
            var lexem = new Lexem(type, _last.ToString());

            Read();

            return lexem;
        }

        private Lexem NextRaw()
        {
            if (_pending.HasValue)
            {
                var lexem = _pending.Value;

                _pending = null;

                return lexem;
            }

            var buffer = new StringBuilder();
            var node = _root;

            while (!_eof)
            {
                var current = _last;

                Read();

                // Escape sequence found, flush pending match and append escaped character
                if (current == _escape && !_eof)
                {
                    buffer.Append(node.FallbackDrop).Append(_last);
                    node = _root;

                    Read();
                }

                // Not an escape sequence, move all cursors
                else
                {
                    node = node.MoveTo(current, buffer);

                    if (node.Type != LexemType.None)
                    {
                        var lexem = new Lexem(node.Type, string.Empty);

                        if (buffer.Length < 1)
                            return lexem;

                        _pending = lexem;

                        return new Lexem(LexemType.Text, buffer.ToString());
                    }

                    // Stop appending to buffer if we're about to reach LOH
                    // size and we are not in the middle of a match candidate
                    if (buffer.Length > MaxBufferSize && node.FallbackNode == null)
                        return new Lexem(LexemType.Text, buffer.ToString());
                }
            }

            buffer.Append(node.FallbackDrop);

            if (buffer.Length > 0)
                return new Lexem(LexemType.Text, buffer.ToString());

            return new Lexem(LexemType.EndOfFile, "<EOF>");
        }

        private bool Read()
        {
            if (_eof)
                return false;

            if (_next.HasValue)
            {
                _last = _next.Value;
                _next = null;

                return true;
            }

            var value = _reader.Read();

            if (value < 0)
            {
                _eof = true;

                return false;
            }

            _last = (char)value;

            if (_last == '\n')
            {
                Column = 1;
                ++Line;
            }
            else
            {
                ++Column;
            }

            return true;
        }

        #endregion
    }
}